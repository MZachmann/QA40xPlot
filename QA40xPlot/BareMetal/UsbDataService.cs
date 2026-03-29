using LibUsbDotNet.Main;
using Microsoft.VisualStudio.Threading;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

// ------- Events
// Start Engine: start the service task and wait for data available. ** set IsStarted
// New Data Available: in PostData -> halt ongoing acquisition, create new packet setup, ** set NewSendPacket
// Packet Sent: start next packet transfer if repeating. ** set PacketSent
// Packet Received: parse and return acquired packet ** set PacketReceived
// Engine stalled - wait for new data available ** turn off IsSending until new data available
// Stop engine: stop the service task and wait for it to end. ** set IsStarted false. halt ongoing acquisition
// ------- Register i/o to QA40x
// Enable data transmission/reception via Enable
// Set output range when transmitting data since max is known

namespace QA40xPlot.BareMetal
{
	public class UsbDataService
	{
		public readonly bool ShowDebug = false;

		readonly uint JOB_ITEM_PREBUFFER = 1000;    // so we know it's the prebuffer
		readonly uint MAX_LOGDATA = 100;        // how many past results to keep in the log
		readonly uint MIN_OUT_QUEUE = 3;       // how many packets we want in the queue at a minimum to keep the usb fed

		//readonly int RegReadWriteTimeout = 100;
		private static object UsbLock = new object();		// a locker
		readonly int MainI2SReadWriteTimeout = 3000; // per baremetal
		public static UsbDataService Singleton { get; } = new UsbDataService();
		SoundUtil? SoundObj { get; set; } = null;
		private int _LastExternalLatency = 0;        // currently last latency
		private bool _UseExternal = false;

		/// Tracks whether or not an acq is in process. The count starts at one, and when it goes busy
		/// it will drop to zero, and then return to 1 when not busy
		private uint CurrentJobNo { get; set; }
		// - states
		private ManualResetEvent IsStarted = new ManualResetEvent(false);           // engine is started
		private VerboseReset IsNotRunning = new VerboseReset(nameof(IsNotRunning), true);
		private VerboseReset EnableSend = new VerboseReset(nameof(EnableSend), false);
		// - packet status
		private VerboseReset NewSendPacket = new VerboseReset(nameof(NewSendPacket), false);            // new packet to send available
		private VerboseReset ReadAvailable = new VerboseReset(nameof(ReadAvailable), false);
		private VerboseReset PacketAvailable = new VerboseReset(nameof(PacketAvailable), false);
		private VerboseReset HasReceivePacket = new VerboseReset(nameof(HasReceivePacket), false);
		// - directives
		private VerboseReset UseInputData = new VerboseReset(nameof(UseInputData), true);           // allow access to input queue
																									// lists
		private List<AsyncSource> InDataQueue { get; set; } = new();
		// async queues for send and receive - in order of usage
		private VerboseQueue<AcqAsyncResult> SendPacketQueue = new(nameof(SendPacketQueue));
		// async queues
		private VerboseQueue<AcqAsyncResult> OutDataQueue = new(nameof(OutDataQueue));
		private VerboseQueue<AcqAsyncResult> ReadDataQueue = new(nameof(ReadDataQueue));
		// async queues for send and receive
		private VerboseQueue<AcqAsyncResult> ReceivePackets = new(nameof(ReceivePackets));
		// async queues for read data results
		// AllResultQueue is the log of all results
		private VerboseQueue<AcqResult> AcqResultQueue = new(nameof(AcqResultQueue));
		private VerboseQueue<AcqResult> AllResultQueue = new(nameof(AllResultQueue));

		private CancellationTokenSource RunTokenSource = new CancellationTokenSource();

		private (double Left, double Right) DacCalibration { get; set; }
		private (double Left, double Right) AdcCalibration { get; set; }
		private int ParamInput { get; set; }    // input and output range during setup
		private int ParamOutput { get; set; }
		private uint UsbBuffSize { get; set; } = 16384;
		private double DbfsAdjustment { get; set; }
		private uint PreBufSize { get; set; }
		private uint PostBufSize { get; set; }
		private uint SampleRate { get; set; }
		private uint FFTSize { get; set; }
		private bool IsUsbEnabled { get; set; } = false;
		private Task ServiceTask = Task.Delay(1);
		private byte[] ShaData { get; set; } = [];
		private double[] LeftData { get; set; } = [];
		private double[] RightData { get; set; } = [];

		public UsbDataService()
		{
			EnableUsbData(false);
		}

		private static void DebugLine(string s)
		{
			//Console.WriteLine(s);
		}

		/// <summary>
		/// start up the usb data service
		/// </summary>
		/// <returns></returns>
		public void Start()
		{
			if (!IsStarted.WaitOne(0))
			{
				DebugLine("Starting DataService...");
				RunTokenSource.Dispose();
				RunTokenSource = new CancellationTokenSource();
				ServiceTask = RunService(RunTokenSource.Token);
				IsStarted.Set();
				DebugLine("Started DataService...");
			}
		}

		public async Task Stop()
		{
			// shut the running loop down
			if (IsStarted.WaitOne(0))
			{
				DebugLine("Stopping UsbDataService...");
				EnableSend.Reset();  // stop the send loop
				await RunTokenSource.CancelAsync();
				await WaitForDoneQueue();
				await IsNotRunning.WaitAsync();  // wait for the service to finish
				//ServiceTask.Dispose();	// we should probably dispose this before starting a new service task
				ServiceTask = Task.Delay(1);
				IsStarted.Reset();
				DebugLine("Stopped UsbDataService...");
			}
		}

		public int GetLogCount()
		{
			int count = 0;
			lock (AllResultQueue)
			{
				count = AllResultQueue.Count;
			}
			return count;
		}

		private static void FlushUsb()
		{
#if DEBUG
			// Create and start the stopwatch
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
#endif
			// empty any read queue
			try
			{
				lock(UsbLock)
				{
					var qaUsb = QaComm.GetUsb();
					// calling flush here seems to crash but readflush works fine
					//qaUsb?.DataReader?.Reset();
					//qaUsb?.DataWriter?.Reset();
					qaUsb?.DataReader?.ReadFlush();
					qaUsb?.DataWriter?.Flush();
				}
			}
			catch (Exception ex)
			{
				DebugLine($"Error flushing USB: {ex.Message}");
			}
#if DEBUG
			// Stop the stopwatch
			stopwatch.Stop();
			// Display elapsed time in different formats
			DebugLine($"Elapsed flush time: {stopwatch.ElapsedMilliseconds} ms");
#endif
		}

		public AcqResult GetLogResult(int idx)
		{
			AcqResult r = new AcqResult();
			r.Valid = false;
			lock (AllResultQueue)
			{
				if (idx < AllResultQueue.Count)
				{
					r = AllResultQueue.ElementAt(idx);
				}
				else if (!AllResultQueue.IsEmpty)
				{
					var fir = AllResultQueue.ElementAt(0);
					r.Left = new double[fir.Left.Length];
					r.Right = r.Left;
					r.Valid = true;
				}
			}
			return r;
		}

		/// <summary>
		/// top level method to run the data service once and return time data
		/// </summary>
		/// <param name="iteration"></param>
		/// <param name="outL"></param>
		/// <param name="outR"></param>
		/// <param name="sampleRate"></param>
		/// <param name="ct">the application cancellation token</param>
		/// <returns></returns>
		public static async Task<AcqResult> UseDataService(BaseViewModel? bvm, bool forceUpdate, double[] outL, double[] outR, uint sampleRate, CancellationToken ct, bool runRepeat)
		{
			Debug.Assert(UsbDataService.Singleton != null, "UsbDataService singleton should not be null");
			return await UsbDataService.Singleton.UseMyDataService(bvm, forceUpdate, outL, outR, sampleRate, ct, runRepeat);
		}

		/// <summary>
		/// top level method to run the data service once and return time data
		/// </summary>
		/// <param name="iteration"></param>
		/// <param name="outL"></param>
		/// <param name="outR"></param>
		/// <param name="sampleRate"></param>
		/// <param name="ct">the application cancellation token</param>
		/// <returns></returns>
		private async Task<AcqResult> UseMyDataService(BaseViewModel? bvm, bool forceUpdate, double[] outL, double[] outR, uint sampleRate, CancellationToken ct, bool runRepeat)
		{
			Debug.WriteLineIf(ShowDebug, "--entering UseDataService");

			byte[] shaData = [];
			bool diffSha = false;
			if (!ViewSettings.Singleton.SettingsVm.AllowRepeating)
			{
				// if we do not allow repeating
				runRepeat = false;
			}
			// check for external audio device usage
			SoundObj?.Stop();
			_UseExternal = ViewSettings.Singleton.SettingsVm.UseExternalEcho && SoundUtil.ExternalPresent();
			if (_UseExternal)
			{
				runRepeat = false;
			}
			else
			{
				SoundObj = null;
			}
			// compare with prior sha to see if we need to update the usb stream.
			// If we're repeating then we only want to update if the data has changed
			// otherwise we want to update every time
			shaData = UsbDataService.CalculateSha(outL, outR);
			diffSha = !ShaSame(shaData, ShaData);
			Start();    // start the service. does nothing if already started
			if (forceUpdate || !RunRepeatedly || !IsUsbEnabled || diffSha)
			{
				var ius = IsUsbEnabled;
				var dfs = diffSha;
				Debug.WriteLineIf(ShowDebug && diffSha, $"New data sha: {Convert.ToBase64String(shaData)}");
				ShaData = shaData;
				if (_UseExternal)
				{
					// if we are using an external sound device, prepare the waveform
					SampleRate = QaComm.GetSampleRate();    // we need this value set for PrepareExternal
					SoundObj = PrepareExternal(true, outL, outR);
				}
				await PostData(bvm, outL, outR, runRepeat);
			}
			else
			{
				Debug.WriteLineIf(ShowDebug, "Using existing setup since sha is the same and service is running");
			}
			// in crude testing the average additional latency for a DAC
			// is about 20-80ms when using the usb service and doing Play() here
			SoundObj?.Play(); // roughly synched with data send for the qa40x
			var acqr = await WaitForResult(ct);
			// sound always runs one at a time so we can surround waitforresult here
			SoundObj?.Stop(); // roughly synched with data send for the qa40x
			if (!runRepeat)
			{
				RunRepeatedly = runRepeat;   // stop any repeating of data
			}
			if (acqr == null)
			{
				DebugLine("No data acquired");
			}
			else
			{
				DebugLine($"Acquired data {acqr.Left.Length}");
			}
			AcqResult r = new AcqResult();
			r.Left = acqr?.Left ?? [];
			r.Right = acqr?.Right ?? [];
			r.Valid = acqr != null;
			return r;
		}

		/// <summary>
		/// calculate the sha of our byte stream
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private static byte[] CalculateSha(double[] left, double[] right)
		{
			return System.Security.Cryptography.SHA256.HashData(QaUsb.ToByteStream(left, right));
		}

		/// <summary>
		/// compare two Shas and return true if they're the same
		/// this lets us keep sending packets repeatedly
		/// </summary>
		/// <param name="data"></param>
		/// <param name="sha"></param>
		/// <returns></returns>
		private static bool ShaSame(byte[] data, byte[] sha)
		{
			var same = data.SequenceEqual(sha);
			if (!same)
				return false;
			return true;
		}

		// manually say if we should send one packet or many packets
		// when set we send another packet when one finishes sending and one is in queue
		private bool _RunRepeatedly = false;
		public bool RunRepeatedly
		{
			get => _RunRepeatedly;
			set
			{
				_RunRepeatedly = value;
				//Debug.WriteLineIf(ShowDebug, $"Run repeatedly={value}");
				DebugLine($"Run repeatedly={value}");
			}
		}

		// manually say if we should send one packet or many packets
		// when set we send another packet when one finishes sending and one is in queue
		public async Task StopRunning()
		{
			await Stop();
		}

		public async Task WaitForDoneQueue()
		{
			// tell everyone to stop sending new data
			EnableSend.Reset();
			SendPacketQueue.Clear();
			// wait for the send to completely finish
			AcqAsyncResult? asr = null;
#if DEBUG
			// Create and start the stopwatch
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
#endif
			try
			{
				var ctk = RunTokenSource.Token;
				if (!ctk.IsCancellationRequested)
				{
					EnableUsbData(true);
					lock (OutDataQueue)
					{
						if (!OutDataQueue.IsEmpty)
						{
							asr = OutDataQueue.Last();
						}
					}
				}
				if (asr != null && !ctk.IsCancellationRequested)
				{
					DebugLine("Writing data to USB...");
					await asr.WaitAsync(Timeout.Infinite, ctk);
					DebugLine("Did write data to USB...");
					asr = null;
				}
				// wait for the read to completely finish
				if (!ctk.IsCancellationRequested)
				{
					lock (ReadDataQueue)
					{
						if (!ReadDataQueue.IsEmpty)
						{
							asr = ReadDataQueue.Last();
						}
					}
				}
				if (asr != null && !ctk.IsCancellationRequested)
				{
					await asr.WaitAsync(Timeout.Infinite, ctk);
				}
				// empty any read queue
				FlushUsb();
			}
			catch (Exception)
			{

			}
#if DEBUG
			// Stop the stopwatch
			stopwatch.Stop();
			// Display elapsed time in different formats
			DebugLine($"Elapsed async waits: {stopwatch.ElapsedMilliseconds} ms");
#endif

			// Stop streaming. This also extinguishes the RUN led
			// do this outside of the other thread
			EnableUsbData(false);
			// clean everything up
			NewSendPacket.Reset();  // clear this if needed
			PacketAvailable.Reset();
			ReadAvailable.Reset();
			HasReceivePacket.Reset();
			// empty any read queue
			FlushUsb();
			// cancelling the transfers seems to fail so instead wait for them to finish
			lock (ReadDataQueue)
			{
				ReadDataQueue.Clear();
			}
			lock (OutDataQueue)
			{
				OutDataQueue.Clear();
			}
			lock (AcqResultQueue)
			{
				AcqResultQueue.Clear();
				//AllResultQueue.Clear();
			}
			SendPacketQueue.Clear();
			ReceivePackets.Clear();
			// tell everyone they can run again
			SoundObj?.Stop();
			NewSendPacket.Reset();  // clear this if needed
			PacketAvailable.Reset();
			ReadAvailable.Reset();
			HasReceivePacket.Reset();
			EnableSend.Set();
		}

		/// <summary>
		/// submit data from the SendPackets queue to the data bus
		/// place each packet into the OutDataQueue for tracking
		/// for each packet submitted to the data bus
		/// submit and add a receive packet to the ReadDataQueue
		/// ensure OutDataQueue always has data in it if SendPackets has something in it
		/// </summary>
		/// <param name="ctk"></param>
		/// <returns></returns>
		public async Task<bool> DoPacketSubmit(CancellationToken ctk)
		{
			bool tSender = await Task<bool>.Run(async () =>
			{
				try
				{
					while (!ctk.IsCancellationRequested)
					{
						await EnableSend.WaitHandleAsync(Timeout.Infinite, ctk);

						// dequeue any finished packets
						while (OutDataQueue.TryPeek(out AcqAsyncResult? asr) && asr != null && asr.IsDone())
						{
							if (ctk.IsCancellationRequested)
								break;
							// remove this from the queue
							lock (OutDataQueue)
							{
								if (!OutDataQueue.IsEmpty)
								{
									DebugLine($"WriteDone === job:{asr.JobNumber}, item:{asr.JobItem}, total:{asr.JobTotal}");
									OutDataQueue.TryDequeue(out _);
								}
							}
						}

						// if we have a packet to send, queue it up immediately
						if (OutDataQueue.Count < MIN_OUT_QUEUE && !SendPacketQueue.IsEmpty && !ctk.IsCancellationRequested)
						{
							if (SendPacketQueue.TryDequeue(out AcqAsyncResult? asr) && asr != null)
							{
								if (SendPacketQueue.IsEmpty)
									NewSendPacket.Reset();  // this gets signaled when data is added to the sendpacketqueue
								var ec = SubmitSendData(asr.ReadBuffer, asr.JobNumber, asr.JobItem, asr.JobTotal);
								if (ec == ErrorCode.None)
								{
									var acqr = SubmitReadData(asr.ReadBuffer.Length, asr.JobNumber, asr.JobItem, asr.JobTotal);
								}
								EnableUsbData(true);   // make sure it's on when we have data to send
							}
						}

						// no data available at all...
						if (OutDataQueue.IsEmpty && SendPacketQueue.IsEmpty && !ctk.IsCancellationRequested)
						{
							// note that queue is empty?
							//Debug.WriteLineIf(ShowDebug, "Waiting for a NewSendPacket...");
							DebugLine("Waiting for a NewSendPacket...");
							var rslt = await NewSendPacket.WaitHandleAsync(2500, ctk);  // wait 1.5 seconds
							if (rslt != 1 && !ctk.IsCancellationRequested)
							{
								DebugLine("Timed out waiting for NewSendPacket...");
								// if we time out waiting for a new packet, turn off the stream
								EnableUsbData(false);
								if (!ctk.IsCancellationRequested)
									await NewSendPacket.WaitHandleAsync(Timeout.Infinite, ctk);  // wait forever
							}
							//Debug.WriteLineIf(ShowDebug, "NewSendPacket signalled...");
							DebugLine("NewSendPacket signalled...");
						}
						if (EnableSend.IsSet && !ctk.IsCancellationRequested &&
							SendPacketQueue.IsEmpty && RunRepeatedly)
						{
							// this puts more data into the sendpacketqueue
							HandleDataReady(false);
							CurrentJobNo++;
						}
					}
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					// Other exceptions will end up here
					DebugLine($"Error in acquisition sender: {ex.Message}");
				}
				finally
				{
					DebugLine("Finished DoPacketSubmit");
				}
				return true;
			});
			return tSender;
		}

		/// <summary>
		/// check the ReadDataQueue and put packets in the ReceivePackets queue 
		/// when each queued read buffer is filled
		/// </summary>
		/// <param name="ctk"></param>
		/// <returns></returns>
		public async Task<bool> DoPacketReceive(CancellationToken ctk)
		{
			bool tReader = await Task<bool>.Run(async () =>
			{
				try
				{
					while (!ctk.IsCancellationRequested)
					{
						await EnableSend.WaitHandleAsync(Timeout.Infinite, ctk);
						if (!ctk.IsCancellationRequested && ReadDataQueue.IsEmpty)
						{
							// note that queue is empty?
							DebugLine("Waiting for a ReadAvailable...");
							await ReadAvailable.WaitHandleAsync(Timeout.Infinite, ctk);
							DebugLine("ReadAvailable signalled...");
						}
						while (!ctk.IsCancellationRequested && ReadDataQueue.Count > 0)
						{
							if (!ctk.IsCancellationRequested && ReadDataQueue.TryDequeue(out AcqAsyncResult? asr))
							{
								DebugLine("Read data started");
								if (asr != null )
								{
									DebugLine($"WaitRead === size:{asr.ReadBuffer.Length} job:{asr.JobNumber}, item:{asr.JobItem}, total:{asr.JobTotal}");
									var uas = await asr.WaitAsync(300, ctk);
									if(uas)
									{
										DebugLine($"DoneRead === size:{asr.ReadBuffer.Length} job:{asr.JobNumber}, item:{asr.JobItem}, total:{asr.JobTotal}");
									}
									else if(!ctk.IsCancellationRequested)
									{
										var vtotal = 0;
										while( !uas && !ctk.IsCancellationRequested && vtotal<10)
										{

											uas = await asr.WaitAsync(500, ctk);
											vtotal++;
										}
										if(!uas)
										{
											DebugLine("Read data timed out and will be skipped");
											asr = null;
										}
										else
										{
											DebugLine($"DoneRead === size:{asr.ReadBuffer.Length} job:{asr.JobNumber}, item:{asr.JobItem}, total:{asr.JobTotal}");
										}
									}
									if (asr != null && !ctk.IsCancellationRequested)
									{
										lock (ReceivePackets)
										{
											if (EnableSend.IsSet)
											{
												ReceivePackets.Enqueue(asr);
												HasReceivePacket.Set();
											}
										}
									}
								}
							}
							if (ReadDataQueue.IsEmpty && !ctk.IsCancellationRequested)
							{
								ReadAvailable.Reset();
							}
						}
					}
				}
				catch (OperationCanceledException)
				{
					// Other exceptions will end up here
					DebugLine("Cancel stopping DoPacketReceive");
				}
				catch (Exception ex)
				{
					// Other exceptions will end up here
					DebugLine($"Error in DoPacketReceive: {ex.Message}");
				}
				finally
				{
					DebugLine($"Finished DoPacketReceive");
				}
				return true;
			});
			return tReader;
		}

		/// <summary>
		/// when there is enough read data to fullfill a request 
		/// create a receive data packet and place it in the AcqResultQueue for processing by the main thread
		/// also empty the read queue of those packets
		/// </summary>
		/// <param name="ctk"></param>
		/// <returns></returns>
		public async Task<bool> DoParseReceipt(CancellationToken ctk)
		{
			List<AcqAsyncResult> readResults = new();
			int lastCount = 0;
			bool tParser = await Task<bool>.Run(async () =>
			{
				try
				{
					while (!ctk.IsCancellationRequested)
					{
						await EnableSend.WaitHandleAsync(Timeout.Infinite, ctk);
						readResults.Clear();
						lock (ReceivePackets)
						{
							if (ReceivePackets.Count > 0 && !ctk.IsCancellationRequested)
							{
								readResults = ReceivePackets.ToList();
							}
							// we've looked at the packets
							HasReceivePacket.Reset();
						}
						if (readResults.Count != lastCount && !ctk.IsCancellationRequested)
						{
							lastCount = readResults.Count;
							if (lastCount > 0)
							{
								var thejob = readResults.First().JobNumber;
								if (thejob != readResults.Last().JobNumber && EnableSend.IsSet)
								{
									var jobdone = readResults.Count(x => x.JobNumber == thejob);
									var jobcnt = readResults.Count;
									// only stop when we have at least one extra
									// this lets us deal with the usb latency and keep fftsize values in synch
									// if there is no last one then we've cancelled
									DebugLine($"Job {thejob} done with {jobdone} of {jobcnt} blocks");
									if (!ctk.IsCancellationRequested)
										HandleJobDone();
								}
							}
						}
						else if (!ctk.IsCancellationRequested)
							await HasReceivePacket.WaitHandleAsync(Timeout.Infinite, ctk);
					}
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					// Other exceptions will end up here
					DebugLine($"Error in acquisition: {ex.Message}");
				}
				finally
				{
					DebugLine($"Finished DoParseReceipt");
					// Indicate an acq is no longer in progress
					//Debug.Assert(IsNotRunning.WaitOne(0), "IsRunning should always be done");
				}
				return true;
			});
			return tParser;
		}

		private void CleanService()
		{
			try
			{
				EnableSend.Reset();
				// clean everything up
				SendPacketQueue.Clear();
				OutDataQueue.Clear();
				ReadDataQueue.Clear();
				ReceivePackets.Clear();
				// clean everything up
				NewSendPacket.Reset();
				ReadAvailable.Reset();
				PacketAvailable.Reset();
				HasReceivePacket.Reset();
				// do this outside of the other thread
				EnableUsbData(true);
				FlushUsb();
			}
			catch (Exception ex)
			{
				DebugLine($"Error in CleanService: {ex.Message}");
			}
			// do this outside of the other thread
			EnableUsbData(false);
		}

		/// <summary>
		/// this is the primary task that gets run for the usbdataservice
		/// </summary>
		/// <param name="ctk"></param>
		/// <returns></returns>
		public Task RunService(CancellationToken ctk)
		{
			IsNotRunning.Reset();
			CleanService();
			// Check if the service is already running
			// Start a new task to run the acquisition
			Task t = Task.Run(async () =>
			{
				EnableSend.Reset();
				var tSender = DoPacketSubmit(ctk);
				var tReader = DoPacketReceive(ctk);
				var tParser = DoParseReceipt(ctk);
				try
				{
					EnableSend.Set();
					while (!ctk.IsCancellationRequested)
					{
						if (tSender.IsCompleted && tReader.IsCompleted && tParser.IsCompleted)
						{
							break;
						}
						await Task.Delay(100, ctk); // small delay to prevent tight loop
					}
				}
				catch (Exception ex)
				{
					DebugLine($"Error in RunService: {ex.Message}");
				}
			});
			DebugLine($"Finished RunService");
			IsNotRunning.Set();
			return t;
		}

		/// <summary>
		/// Submits the provided data for acquisition, and waits for the result. 
		/// The data is split into buffers and submitted one at a time.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public async Task PostData(BaseViewModel? bvm, double[] left, double[] right, bool runRepeat)
		{
			try
			{
				RunRepeatedly = false;  // stop any repeating of data
				LeftData = left;
				RightData = right;
				if (1 == await UseInputData.WaitHandleAsync(8000, RunTokenSource.Token))
				{
					UseInputData.Reset();
					DebugLine($"Posting data of length {left.Length}");
#if DEBUG
				// Create and start the stopwatch
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
#endif
					DebugLine("Is now stopping");
					await Stop();
					DebugLine("Is now starting");
					Start();
					DebugLine("Start is now complete");
#if DEBUG
				// Stop the stopwatch
				stopwatch.Stop();
				var totalTime = stopwatch.ElapsedMilliseconds;
					// Display elapsed time in different formats
					DebugLine($"*** Start-Stop Elapsed Time: {totalTime} ms");
#endif
					InDataQueue.Clear();
					//					await WaitForDoneQueue();
					await CalculateParameters(bvm, left, right);
					var sources = SplitIntoBuffers(left, right);
					InDataQueue = sources;
					CurrentJobNo = 0;
					UseInputData.Set(); // after setting the indataqueue\
					HandleDataReady(true);
					RunRepeatedly = runRepeat;
					CurrentJobNo++;
					if (!runRepeat)
					{
						HandleDataReady(false);
						CurrentJobNo++;
					}
				}
				else
				{
					DebugLine("PostData called while previous data is still being processed.");
				}
			}
			catch (OperationCanceledException)
			{
				// If we cancel an acq via the CancellationToken, we'll end up here
				DebugLine("Data posting was cancelled.");
			}
			catch (Exception ex)
			{
				// Other exceptions will end up here
				DebugLine($"Error in acquisition: {ex.Message}");
			}
		}

		/// <summary>
		/// Submits a buffer to be written and returns immediately. The submitted buffer is 
		/// copied to a local buffer before returning.
		/// </summary>
		/// <param name="data"></param>
		public ErrorCode SubmitSendData(byte[] dataSet, uint jobNo, uint jobItem, uint jobTotal)
		{
			ErrorCode ec = ErrorCode.None;

			if (dataSet == null || dataSet.Length == 0)
			{
				return ErrorCode.InvalidParam;
			}

			UsbTransfer? ar = null;
			lock (UsbLock)
			{
				// we need to flush before every send to avoid a weird issue where the first packet gets stuck and never sends
				var qaUsb = QaComm.GetUsb();
				ec = qaUsb?.DataWriter?.SubmitAsyncTransfer(dataSet, 0, dataSet.Length, MainI2SReadWriteTimeout, out ar) ?? ErrorCode.UnknownError;
			}
			if (ec != ErrorCode.None)
			{
				throw new Exception("Bad result in SendDataBegin");
			}
			if (ar != null)
			{
				DebugLine($"WriteSubmit === job:{jobNo}, item:{jobItem}, total:{jobTotal}");
				lock (OutDataQueue)
				{
					OutDataQueue.Enqueue(new AcqAsyncResult(ar, dataSet, jobNo, jobItem, jobTotal));
				}
			}
			return ec;
		}

		/// <summary>
		/// Creates and submits a buffer to be read asynchronously. Returns immediately.
		/// </summary>
		/// <param name="data"></param>
		public AcqAsyncResult? SubmitReadData(int bufSize, uint jobNo, uint jobItem, uint jobTotal)
		{
			AcqAsyncResult? result = null;
			{
				byte[] readBuffer = new byte[bufSize];
				UsbTransfer? ar = null;
				ErrorCode? ec = ErrorCode.DeviceNotFound;
				lock (UsbLock)
				{
					var qaUsb = QaComm.GetUsb();
					ec = qaUsb?.DataReader?.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, MainI2SReadWriteTimeout, out ar);
				}
				if (ec != ErrorCode.None)
				{
					throw new Exception("ERROR: Bad result in SubmitReadData");
				}
				if (ar != null)
				{
					lock (ReadDataQueue)
					{
						result = new AcqAsyncResult(ar, readBuffer, jobNo, jobItem, jobTotal);
						ReadDataQueue.Enqueue(result);
						DebugLine($"ReadSubmit === job:{jobNo}, item:{jobItem}, total:{jobTotal}");
					}
					ReadAvailable.Set(); // signal read queue has occupants
				}
			}
			return result;
		}

		private void DumpQueue(int itry)
		{
			if (ShowDebug)
			{
				DebugLine($"{itry} Send Packets: {SendPacketQueue.Count},{OutDataQueue.Count} Read: {ReadDataQueue.Count},{ReceivePackets.Count} ");
				var ro = SendPacketQueue.Select(x => $"{x.JobItem}.{x.JobNumber}").ToList();
				var so = ReceivePackets.Select(x => $"{x.JobItem}.{x.JobNumber}").ToList();
				var rox = string.Join(":", ro) + ":::" + string.Join(":", so);
				DebugLine(rox);
			}
		}

		public async Task<AcqResult?> WaitForResult(CancellationToken ctok)
		{
			var rslt = 0;
			try
			{
				var u = 1000.0 * FFTSize / SampleRate; // # of mseconds to do the dataset
#if DEBUG
				// Create and start the stopwatch
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
#endif
				if (ShowDebug)
				{
					int itry = (int)(2 + 4 * (u * 1.5 / 1000));
					while (1 != rslt && itry > 0)
					{
						DumpQueue(itry);
						rslt = await PacketAvailable.WaitHandleAsync(250, ctok);
						itry--;
					}
				}
				else
				{
					rslt = await PacketAvailable.WaitHandleAsync((int)(400 + u * 1.5), ctok);
				}
				PacketAvailable.Reset();

#if DEBUG
				// Stop the stopwatch
				stopwatch.Stop();
				// Display elapsed time in different formats
				DebugLine($"Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
#endif
			}
			catch (OperationCanceledException)
			{
				await WaitForDoneQueue();
				rslt = 0;
			}
			catch (Exception ex)
			{
				DebugLine($"{ex.Message}");
			}

			AcqResult? dataOut = null;
			if (rslt == 1 && !ctok.IsCancellationRequested)
			{
				DumpQueue(-1);
				lock (AcqResultQueue)
				{
					bool bslt = AcqResultQueue.TryDequeue(out dataOut);
					if (!AcqResultQueue.IsEmpty)
						PacketAvailable.Set();
					if (bslt && dataOut != null && dataOut.Valid)
					{
						var tt = Math.Max(dataOut.Left.Max(), dataOut.Right.Max());
						if (tt > 1e-4)
						{
							AcqResult acqr = new();
							acqr.Left = LeftData;
							acqr.Right = RightData;
							acqr.Valid = true;
							AllResultQueue.Enqueue(acqr);
							AllResultQueue.Enqueue(dataOut);
							while (AllResultQueue.Count > MAX_LOGDATA)
								AllResultQueue.TryDequeue(out _);
						}
					}
				}
			}
			if (dataOut == null)
			{
				DebugLine("No data result available");
				return null;
			}
			//if(dataOut.Left.Length > 0)
			//	WriteLine($"Data result max left: {dataOut.Left.Max()}");
			//else
			//	WriteLine("Data result empty");
			return dataOut;
		}

		private void EnableUsbData(bool enable)
		{
			if (IsUsbEnabled != enable)
			{
				lock (UsbLock)
				{
					var qausb = QaComm.GetUsb();
					if (qausb != null)
					{
						qausb.WriteRegister(8, (uint)(enable ? 5 : 0)); // start data transfer
						IsUsbEnabled = enable;
					}
				}
				DebugLine($"usb dataflow enable: {IsUsbEnabled}");
			}
		}

		public void HandleJobDone()
		{
			try
			{
				PerformJobDone();
			}
			catch(Exception ex)
			{
				DebugLine($"Error in HandleJobDone: {ex.Message}");
			}
		}

		public void PerformJobDone()
		{ 
			// we now have a list of all the rx buffers to convert to an array
			// use fixed size so that frombytestream and others work ok
			List<byte[]> rxResults = new();
			uint jobno = 0;
			uint jobSize = 0;
			lock (ReceivePackets)
			{
				AcqAsyncResult? ar = null;
				// the first packet might be a prelude
				if (!ReceivePackets.IsEmpty && ReceivePackets.TryDequeue(out ar))
				{
					jobno = ar?.JobNumber ?? 0;
					jobSize = ar?.JobTotal ?? 0;
					// the very first block isn't saved since it's the prebuf
					//if (ar != null && ar.JobItem != JOB_ITEM_PREBUFFER)
					if (ar != null)
						rxResults.Add(ar.ReadBuffer);
				}
				if(ar != null)
				{
					while (ReceivePackets.TryPeek(out ar))
					{
						if (ar.JobNumber == jobno)
						{
							ReceivePackets.TryDequeue(out ar);
							if (ar != null && ar.JobItem != JOB_ITEM_PREBUFFER)
								rxResults.Add(ar.ReadBuffer);
						}
						else if (ar != null && ar.JobItem != JOB_ITEM_PREBUFFER)
						{
							// add the last post-job buffer if it exists
							rxResults.Add(ar.ReadBuffer);
							//if(ReceivePackets.Count == 0)
							//	HasReceivePacket.Reset();
							break;
						}
					}
				}
			}
			if (rxResults.Count == 0)
			{
				// this must be a trailer buffer with no data
				return;
			}
			var rxLength = rxResults.Sum(x => x.Length);
			if (rxLength < FFTSize)
			{
				DebugLine($"Not enough data received for job {jobno}: {rxLength} bytes");
				return;
			}

			// now rxResults is all data results for the job
			var rxData = new byte[rxLength];
			//WriteLine($"RxData bytes received: {rxData.Length}, blocks: {rxResults.Count}");
			int offset = 0;
			foreach (var b in rxResults)
			{
				Buffer.BlockCopy(b, 0, rxData, offset, b.Length);
				offset += b.Length;
			}
			// now have a byte array of the read datablocks in rxData
			//var truncdata = rxData[(int)(PreBufSize*8)..(int)(PreBufSize*8 + jobSize*8)];
			var ars = DealWithReadData(rxData, (int)jobno);
			if (ars != null && ars.Valid)
			{
				lock (AcqResultQueue)
				{
					// when we're getting a fixed number of results we can't afford to toss some
					// but if we're free-running then we don't want this to get infinitely long or out of synch
					while (AcqResultQueue.Count > 2)
					{
						AcqResultQueue.TryDequeue(out _);
					}
					AcqResultQueue.Enqueue(ars);
				}
				DebugLine($"Job {jobno} acquisition complete with {ars.Left.Length} samples");
				PacketAvailable.Set();
			}
		}

		/// <summary>
		/// see if the left (true) or right (false) channel has an external signal
		/// </summary>
		/// <param name="isLeft"></param>
		/// <returns></returns>
		private static bool HasAChannel(bool isLeft)
		{
			var useExternal = ViewSettings.Singleton.SettingsVm.UseExternalEcho;
			if (!useExternal)
				return false;
			return SoundUtil.HasChannel(isLeft);
		}

		private int CheckStart(double[] data, int maxDelay)
		{
			// this checks the start of the data for a signal above the noise floor
			// it returns the offset to the start of the signal
			double maxNoise = 1e-4;   // empirically we see up to about -5e-6 in the first 1000 samples with no signal, so this is a little above that
			double[] deltas = new double[maxDelay];
			int total = 0;
			int offset = 0;
			for (int i=0; i<maxDelay; i++)
			{
				deltas[i] = data[i + 1] - data[i];  // first derivative
				if (deltas[i] > maxNoise || deltas[i] < -maxNoise)
				{
					// if we see 3 in a row above the noise floor, stop checking
					if(++total > 3)
					{
						offset = i;
						break;
					}
				}
				else if(total > 0)
				{
					total--;
				}
			}
			if(offset == maxDelay)
			{
				return 0;   // if we see no signal at all, just return 0 to avoid cutting off the start of the data
			}
			return offset;
		}

		public AcqResult DealWithReadData(byte[] rxData, int jobNumber)
		{
			// convert to double arrays
			AcqResult aResult = new AcqResult();
			aResult.Valid = false;
			aResult.JobNumber = jobNumber;
			if(rxData.Length == 0)
			{
				DebugLine("No data read from USB");
				return aResult;
			}

			QaUsb.FromByteStream(rxData, out aResult.Right, out aResult.Left);
			//WriteLine($"Deal with read data at {aResult.Left.Length}");
			// Note that left and right data is swapped on QA402, QA403, QA404. We do that via arg ordering below.
			// now we convert the stream to two data arrays
			// and scan the data to see where to clip the latency
			//if (aResult.Left.Length > PreBufSize)
			{
				// Convert from dBFS to dBV. Note the 6 dB factor--the ADC is differential
				// Apply scaling factor to map from dBFS to Volts. 
				int tused = (int)FFTSize;    // should be fftsize
											 // internal stuff has a fixed latency
											 // but external audio is all over the map
				var ltc = MathUtil.ToInt(ViewSettings.Singleton.SettingsVm.Latency, 47);
				var roff = ltc;
				var loff = ltc;
				if (_UseExternal)
				{
					var rxoff = _LastExternalLatency;
					var lxoff = _LastExternalLatency;
					var toff = ViewSettings.Singleton.SettingsVm.EchoDelay;
					var maxDelay = (int)Math.Abs(toff * SampleRate / 1000);  // delay in samples
					if(HasAChannel(true))
					{
						lxoff = Math.Max(rxoff, CheckStart(aResult.Left, maxDelay));
						loff = lxoff;
					}
					if (HasAChannel(false))
					{
						rxoff = Math.Max(lxoff, CheckStart(aResult.Right, maxDelay));
						DebugLine($"External latency for job {jobNumber}: {rxoff} samples");
						roff = rxoff;
					}
				}

				var adcCorrection = Math.Pow(10, (ParamInput - 6.0) / 20);
				var rlf = aResult.Left.Skip(loff).Take(tused);
				if (rlf.Count() < tused)
					rlf = rlf.Concat(new double[tused - rlf.Count()]);
				Debug.Assert((rlf.Count() % 1024) == 0, "Must be a power of 2");
				var troff = 0;// rlf.Average();  // dc offset
				aResult.Left = rlf.Select(x => (x - troff) * AdcCalibration.Left * adcCorrection).ToArray();
				Debug.WriteLineIf(ShowDebug, $"Job {jobNumber} Maximum rlf value {rlf.Max()}, left value: {aResult.Left.Max()}");

				var rrf = aResult.Right.Skip(roff).Take(tused);
		
				if (rrf.Count() > 0)
				{
					troff = 0;// rrf.Average();  // dc offset
					if (rrf.Count() < tused)
						rrf = rrf.Concat(new double[tused - rrf.Count()]);
					aResult.Right = rrf.Select(x => (x - troff) * AdcCalibration.Right * adcCorrection).ToArray();
				}
				aResult.Valid = true;
			}
			return aResult;
		}

		public void HandleDataReady(bool isFirst)
		{
			if(!UseInputData.IsSet)
			{
				DebugLine("HandleDataReady called while previous data is still being processed.");
				return;
			}
			UseInputData.Reset();
			if (InDataQueue != null && InDataQueue.Count > 0)
			{
				uint jobItem = 0;
				// If we have data to send, send it
				var dataQueue = InDataQueue.ToArray();
				if (isFirst)
				{
					var left = new double[PreBufSize];
					var theData = QaUsb.ToByteStream(left, left);
					AcqAsyncResult asr = new AcqAsyncResult(null!, theData, CurrentJobNo, JOB_ITEM_PREBUFFER, (uint)1);
					SendPacketQueue.Enqueue(asr);
				}
				uint bfrTotal = (uint)dataQueue.Sum(x => x.TheData?.Length ?? 0);
				foreach (var bfr in dataQueue)
				{
					AcqAsyncResult asr = new AcqAsyncResult(null!, bfr.TheData, CurrentJobNo, jobItem, bfrTotal);
					SendPacketQueue.Enqueue(asr);
					jobItem++;
				}
				if(ReadDataQueue.Count > 0 && isFirst)
				{
					DebugLine("Clearing read data queue");
					ReadDataQueue.Clear();  // just in case
				}
				NewSendPacket.Set(); // tell the service we have new data ready
				//SoundObj?.Play();
				if (SoundObj != null)
					DebugLine($"Sound device is playing: {SoundObj?.IsPlaying}");
			}
			UseInputData.Set();
		}

		/// <summary>
		/// Based on current settings, calculate any locals
		/// </summary>
		private async Task<bool> CalculateParameters(BaseViewModel? bvm, double[] dataLeft, double[] dataRight)
		{
			// set the output amplitude to support the data
			var maxOut = Math.Max(dataLeft.Max(), dataRight.Max());
			var minOut = Math.Min(dataLeft.Min(), dataRight.Min());
			maxOut = Math.Max(Math.Abs(maxOut), Math.Abs(minOut));  // maximum output voltage
																	// don't bother setting output amplitude if we have no output
			var mlevel = IODevUSB.DetermineOutput((maxOut > 0) ? maxOut : 1e-8, 1.05); // the setting for our voltage + 10%
			var minRange = (int)MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.MinOutputRange, -12);
			mlevel = Math.Max(mlevel, minRange); // noop for now but keep this line in for testing
												 // let's run this in the main dispatcher to improve multitasking
			System.Windows.Application.Current?.Dispatcher.Invoke(async () =>
			{
				await QaComm.SetOutputRange(mlevel);   // set the output voltage
				if (bvm != null)
				{
					await QaComm.SetInputRange((int)bvm.Attenuation);
				}
			});

			ParamOutput = QaComm.GetOutputRange();
			ParamInput = QaComm.GetInputRange();

			DacCalibration = QaUsb.GetDacCal(QaComm.GetCalData(), ParamOutput);
			AdcCalibration = QaUsb.GetAdcCal(QaComm.GetCalData(), ParamInput);

			// bufsize is leftout.length and usbBufSize is the size of the usb buffer
			// bufSize * 8 must be >= to usbBufSize, and bufSize * 8 must be an integer multiple of usbBufSize
			var usize = MathUtil.ToInt(ViewSettings.Singleton.SettingsVm.UsbBufferSize, 16384);
			double fusize = Math.Pow(2, Math.Floor(Math.Log(usize, 2)));     // nearest power of 2
			fusize = Math.Max(Math.Min(fusize, 131072), 2048);   // 2k to 128k ???

			UsbBuffSize = (uint)(0.1 + fusize);
			DbfsAdjustment = Math.Pow(10, -((ParamOutput + 3.0) / 20));

			// now get the prebuf and postbuf sizes in samples
			var preBuf = QaComm.GetPreBuffer();
			var postBuf = QaComm.GetPostBuffer();
			PreBufSize = Math.Max((uint)preBuf, UsbBuffSize / 8);
			PostBufSize = Math.Max((uint)postBuf, UsbBuffSize / 8);

			SampleRate = QaComm.GetSampleRate();
			FFTSize = QaComm.GetFftSize();

			DumpAllParams();

			return true;
		}

		private void DumpAllParams()
		{
			if(ShowDebug)
			{
				DebugLine($"****** Parameters *******");
				DebugLine($"ParamInput: {ParamInput}, ParamOutput: {ParamOutput}, UsbBuffSize: {UsbBuffSize}");
				DebugLine($"DacCalibration: {DacCalibration.Left}, {DacCalibration.Right}");
				DebugLine($"AdcCalibration: {AdcCalibration.Left}, {AdcCalibration.Right}");
				DebugLine($"DbfsAdjustment: {DbfsAdjustment}");
				DebugLine($"PreBufSize: {PreBufSize}, PostBufSize: {PostBufSize}");
				DebugLine($"****** Parameters *******");
			}
			else
				DebugLine($"****** ParamInput: {ParamInput}, ParamOutput: {ParamOutput}, UsbBuffSize: {UsbBuffSize}");

		}

		/// <summary>
		/// convert stereo data into a set of buffers
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private List<AsyncSource> SplitIntoBuffers(double[] left, double[] right)
		{
			List<AsyncSource> sources = new List<AsyncSource>();

			// now the data buffers
			int blocks = (int)(left.Length / UsbBuffSize);
			if ((left.Length - blocks * UsbBuffSize) > 0)
			{
				throw new Exception("Left length is not an integer multiple of the USB buffer size");
			}
			int usbb = (int)UsbBuffSize;
			for (int i = 0; i < blocks; i++)
			{
				int offs = i * usbb;
				var src = new AsyncSource()
				{
					Left = left[offs..(offs + usbb)],
					Right = right[offs..(offs + usbb)]
				};
				// scale to max and calibration
				src.Left = src.Left.Select(x => x * DbfsAdjustment * DacCalibration.Left).ToArray();
				src.Right = src.Right.Select(x => x * DbfsAdjustment * DacCalibration.Right).ToArray();
				src.TheData = QaUsb.ToByteStream(src.Left, src.Right);   // convert to bytes
				sources.Add(src);
			}

			foreach(var src in sources)
			{
				src.TheData = QaUsb.ToByteStream(src.Left, src.Right);
			}
			return sources;
		}

		// external sound device support
		private SoundUtil? PrepareExternal(bool useExternal, double[] leftOut, double[] rightOut)
		{
			SoundUtil? soundObj = null;

			// we are also sending to an external sound system device
			if (useExternal)
			{
				var lexout = leftOut.ToList();
				var rexout = rightOut.ToList();
				//
				double[] prebuf = new double[PreBufSize];
				double[] postbuf = new double[PostBufSize];
				lexout.InsertRange(0, prebuf);
				lexout.AddRange(postbuf);
				rexout.InsertRange(0, prebuf);
				rexout.AddRange(postbuf);
				soundObj = SoundUtil.CreateUtil(ViewSettings.Singleton.SettingsVm.EchoName, lexout.ToArray(), rexout.ToArray(), (int)SampleRate);
				if (soundObj != null && soundObj.IsNew)
				{
					soundObj.WasteOne((int)PostBufSize, SampleRate);  // play once to start up the DAC
					// we seem to have startup issues on every scan so constant waste is needed to keep the DAC alive.
					//soundObj.IsNew = false;
				}
			}
			//soundObj?.Play();
			return soundObj;
		}
	}
}
