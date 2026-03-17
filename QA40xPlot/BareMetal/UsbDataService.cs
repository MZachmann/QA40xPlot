//#define SHOWDBG

using LibUsbDotNet.Main;
using QA40xPlot.Extensions;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace QA40xPlot.BareMetal
{
	public class UsbDataService
	{
		//readonly int RegReadWriteTimeout = 100;
		readonly int MainI2SReadWriteTimeout = 1000; // per baremetal
		public static UsbDataService Singleton { get; } = new UsbDataService();
		private bool _RunRepeatedly = false;
		public bool RunRepeatedly 
			{ 
			 get => _RunRepeatedly;
			 set
				{
					_RunRepeatedly = value;
#if SHOWDBG
				Debug.WriteLine($"Run repeatedly={value}");
#endif
				}
			}

		/// Tracks whether or not an acq is in process. The count starts at one, and when it goes busy
		/// it will drop to zero, and then return to 1 when not busy
		private static uint CurrentJobNo = 0;
		private static ManualResetEvent IsNotRunning = new ManualResetEvent(true);
		private static ManualResetEvent HasDataReady = new ManualResetEvent(false);
		private static SemaphoreSlim DataAvailable = new SemaphoreSlim(0, 1);
		private static SemaphoreSlim UseInputData = new SemaphoreSlim(1, 1);
		private static ManualResetEvent HasJobDone = new ManualResetEvent(false);
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
		private bool IsEnabled { get; set; } = false;
		private List<AsyncSource> InDataQueue { get; set; } = new();
		// async queues
		private ConcurrentQueue<AsyncResult> ReadDataQueue = new();
		private ConcurrentQueue<AsyncResult> OutDataQueue = new();
		private ConcurrentQueue<AcqResult> AcqResultQueue = new();
		private Task ServiceTask = Task.Delay(1);
		private byte[] ShaData { get; set; } = [];

		public UsbDataService()
		{
			EnableUsbData(false);
		}

		private static byte[] CalculateSha(double[] left, double[] right)
		{
			return System.Security.Cryptography.SHA256.HashData(QaUsb.ToByteStream(left, right));
		}

		private static bool ShaSame(byte[] data, byte[] sha)
		{
			
			var same = data.SequenceEqual(sha);
			if (!same)
				return false;
			return true;
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
			var uss = UsbDataService.Singleton;
			return await uss.UseMyDataService(bvm, forceUpdate, outL, outR, sampleRate, ct, runRepeat);
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
#if SHOWDBG
			Debug.WriteLine("--entering UseDataService");
#endif
			byte[] shaData = [];
			bool diffSha = false;
			if(runRepeat && ! ViewSettings.Singleton.SettingsVm.AllowRepeating)
			{
				// if we do not allow repeating
				runRepeat = false;
			}
			await Task.Run(() =>
			{
				shaData = UsbDataService.CalculateSha(outL, outR);
				diffSha = !ShaSame(shaData, ShaData);
			});
			if (forceUpdate || !IsRunning() || !RunRepeatedly || !IsEnabled || diffSha)
			{
				Start();    // does nothing if already started
#if SHOWDBG
				Debug.WriteLine($"New data sha: {Convert.ToBase64String(shaData)}");
#endif
				ShaData = shaData;
				RunRepeatedly = runRepeat;
				await PostData(bvm, outL, outR);
			}
			else
			{
				// if we're already running but this is the last one of the group
				RunRepeatedly = runRepeat;
#if SHOWDBG
				Debug.WriteLine("Using existing setup since sha is the same and service is running");
#endif
			}
			var acqr = await WaitForResult(ct);
			if(acqr == null)
			{
				Debug.WriteLine("No data acquired");
			}
			else
			{
				Debug.WriteLine($"Acquired data {acqr.Left.Length}");
			}
			AcqResult r = new AcqResult();
			r.Left = acqr?.Left ?? [];
			r.Right = acqr?.Right ?? [];
			r.Valid = acqr != null;
			return r;
		}

		public bool IsRunning()
		{
			return !IsNotRunning.WaitOne(0);
		}

		public async Task WaitForDoneQueue()
		{
			await Task.Run(() =>
			{
				// wait for the send to completely finish
				lock (OutDataQueue)
				{
					if (!OutDataQueue.IsEmpty)
					{
						var asr = OutDataQueue.Last();
						if (!asr.IsDone())
							asr.Wait();
					}
				}
				// wait for the read to completely finish
				lock (ReadDataQueue)
				{
					if (!ReadDataQueue.IsEmpty)
					{
						var asr = ReadDataQueue.Last();
						if (!asr.IsDone())
							asr.Wait();
					}
				}
			});

			// Stop streaming. This also extinguishes the RUN led
			// do this outside of the other thread
			EnableUsbData(false);
			// clean everything up
			//Debug.WriteLine($"Data available countC: {DataAvailable.CurrentCount}");
			if (DataAvailable.CurrentCount > 0)
				DataAvailable.Wait(0);  // clear this if needed
			//Debug.WriteLine($"Data available countD: {DataAvailable.CurrentCount}");
			HasDataReady.Reset();
			HasJobDone.Reset();
			// cancelling the transfers seems to fail so instead wait for them to finish
			lock(ReadDataQueue)
			{
				ReadDataQueue.Clear();
			}
			lock (OutDataQueue)
			{
				OutDataQueue.Clear();
			}
			lock(AcqResultQueue)
			{
				AcqResultQueue.Clear();
			}
			// empty any read queue
			var qaUsb = QaComm.GetUsb();
			// calling flush here seems to crash but readflush works fine
			qaUsb?.DataReader?.ReadFlush();
			qaUsb?.DataWriter?.Flush();
		}

		/// <summary>
		/// start up the usb data service
		/// </summary>
		/// <returns></returns>
		public void Start()
		{
			if(!IsRunning())
			{
				//RunTokenSource.Dispose();
				RunTokenSource = new CancellationTokenSource();
				var tk = RunTokenSource.Token;
				ServiceTask = RunService(tk);
			}
		}

		public async Task Stop()
		{
			// shut the running loop down
			if(IsRunning())
			{
				RunTokenSource.Cancel();
				await Task.Run(() =>
				{
					// wait for the service task to end
					IsNotRunning.WaitOne(9000);  // let it finish cancelling
				});
				await WaitForDoneQueue();
				ServiceTask.Dispose();
				ServiceTask = Task.Delay(1);
			}
			//ServiceTask.Wait();
		}

		public Task RunService(CancellationToken ctk)
		{
			// Check if the service is already running
			if (!IsRunning())
			{
				IsNotRunning.Reset();
				// Start a new task to run the acquisition
				Task t = Task.Run(() =>
				{
					try
					{
						while (!ctk.IsCancellationRequested)
						{
							// we start by always doing two cycles of the current test
							// one at Jobno=0 and one at Jobno=1.
							if (HasDataReady.WaitOne(100) && !ctk.IsCancellationRequested)
							{
								HasDataReady.Reset();
								HandleDataReady(false);
								CurrentJobNo++;
								if (CurrentJobNo == 1 && RunRepeatedly)
								{
									// submit a second run immediately to prep for repeating
									HandleDataReady(false);
									CurrentJobNo++;
									if(InDataQueue.Count <= 2)
									{
										// for faster scans we need two runs queued up
										HandleDataReady(false);
										CurrentJobNo++;
									}
								}
								else if(!RunRepeatedly)
								{
									// submit only the first block of the dataset
									HandleDataReady(true);
									CurrentJobNo++;
								}
							}
							List<AsyncResult> readResults = new();
							lock (ReadDataQueue)
							{
								if (ReadDataQueue.Count > 0 && !ctk.IsCancellationRequested)
								{
									readResults = ReadDataQueue.ToList();
								}
							}
							if(readResults.Count > 0)
							{
								var thejob = readResults.First().JobNumber;
								var jobcnt = readResults.CountWhile(x => x.JobNumber == thejob);
								var jobdone = ReadDataQueue.CountWhile(x => x.IsDone());
								if(jobdone > jobcnt)
								{
									// only stop when we have at least one extra
									HasJobDone.Set();
								}
								// check the last one in the job for done
								//var jobdone = readResults[jobcnt - 1].IsDone();
								//if (jobdone)
							}
							if (HasJobDone.WaitOne(0) && !ctk.IsCancellationRequested)
							{
								HasJobDone.Reset();
								if (RunRepeatedly)
								{
									HandleDataReady(false);
									++CurrentJobNo; // next job
								}
								HandleJobDone();
							}
						}
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception ex)
					{
						// Other exceptions will end up here
						Debug.WriteLine($"Error in acquisition: {ex.Message}");
					}
					finally
					{
						// Indicate an acq is no longer in progress
						IsNotRunning.Set();
						//Debug.Assert(IsNotRunning.WaitOne(0), "IsRunning should always be done");
					}
				});
				return t;
			}
			else
				return Task.Delay(1);
		}

		/// <summary>
		/// Submits the provided data for acquisition, and waits for the result. The data is split into buffers and submitted one at a time.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public async Task PostData(BaseViewModel? bvm, double[] left, double[] right)
		{
			if (HasDataReady.WaitOne(0))
			{
				// we got here before the indataqueue was detected
				HasDataReady.Reset();
				Debug.WriteLine("Replacing data.");
			}
			try
			{
				if (! await UseInputData.WaitAsync(8000))
				{
					Debug.WriteLine("PostData called while previous data is still being processed.");
				}
				else
				{
					Debug.WriteLine($"Posting data of length {left.Length}");
					InDataQueue.Clear();
					await WaitForDoneQueue();
					await CalculateParameters(bvm, left, right);
					var sources = SplitIntoBuffers(left, right);
					InDataQueue = sources;
					CurrentJobNo = 0;
					UseInputData.Release(); // after setting the indataqueue
					HasDataReady.Set();     // tell the service we have new data ready
				}
			}
			catch (OperationCanceledException)
			{
				// If we cancel an acq via the CancellationToken, we'll end up here
			}
			catch (Exception ex)
			{
				// Other exceptions will end up here
				Debug.WriteLine($"Error in acquisition: {ex.Message}");
			}
		}

		/// <summary>
		/// Submits a buffer to be written and returns immediately. The submitted buffer is 
		/// copied to a local buffer before returning.
		/// </summary>
		/// <param name="data"></param>
		public ErrorCode SendDataBegin(AsyncSource asSource, uint jobNo, uint jobItem, uint jobTotal)
		{
			ErrorCode ec = ErrorCode.None;

			if (asSource.TheData == null || asSource.TheData.Length == 0)
			{
				return ErrorCode.InvalidParam;
			}

			UsbTransfer? ar = null;
			var qaUsb = QaComm.GetUsb();
			if (qaUsb != null)
			{
				ec = qaUsb.DataWriter?.SubmitAsyncTransfer(asSource.TheData, 0, asSource.TheData.Length, MainI2SReadWriteTimeout, out ar) ?? ErrorCode.UnknownError;
				if (ec != ErrorCode.None)
				{
					//Log.WriteLine(LogType.Error, "Error code in Usb.WriteDataBegin: ");
					throw new Exception("Bad result in WriteDataBegin in Usb.cs");
				}
				if (ar != null)
				{
					lock (OutDataQueue)
					{
						OutDataQueue.Enqueue(new AsyncResult(ar, asSource.TheData, jobNo, jobItem, jobTotal));
					}
				}
			}
			else
			{
				ec = ErrorCode.DeviceNotFound;
			}
			return ec;
		}

		/// <summary>
		/// Creates and submits a buffer to be read asynchronously. Returns immediately.
		/// </summary>
		/// <param name="data"></param>
		public AsyncResult? ReadDataBegin(int bufSize, uint jobNo, uint jobItem, uint jobTotal)
		{
			AsyncResult? result = null;
			var qaUsb = QaComm.GetUsb();
			if (qaUsb != null)
			{
				byte[] readBuffer = new byte[bufSize];
				UsbTransfer? ar = null;
				qaUsb.DataReader?.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, MainI2SReadWriteTimeout, out ar);
				if (ar != null)
				{
					lock (ReadDataQueue)
					{
						result = new AsyncResult(ar, readBuffer, jobNo, jobItem, jobTotal);
						ReadDataQueue.Enqueue(result);
					}
				}
			}
			return result;
		}

		private void DumpQueue(int itry)
		{
#if SHOWDBG
			int ul, ul2, xl, xl2;
			lock(OutDataQueue)
			{
				ul = OutDataQueue.Count;
				ul2 = OutDataQueue.Count(x => x.IsDone());
			}
			lock(ReadDataQueue)
			{
				xl = ReadDataQueue.Count;
				xl2 = ReadDataQueue.Count(x => x.IsDone());
			}
			Debug.WriteLine($"{itry} Waiting for data with Out: {ul},{ul2} Read: {xl},{xl2} ");
#endif
		}

		public async Task<AcqResult?> WaitForResult(CancellationToken ctok)
		{
			AcqResult? dataOut = null;
			var rslt = false;
			try
			{
#if SHOWDBG
				int itry = 0;
				while(!rslt && itry < 25)
				{
					DumpQueue(itry);
					rslt = await DataAvailable.WaitAsync(250, ctok); // Timeout.Infinite, ctok);
					itry++;
				}
#else
				rslt = await DataAvailable.WaitAsync(5000, ctok); // Timeout.Infinite, ctok);
#endif
			}
			catch (OperationCanceledException )
			{
				UsbDataService.Singleton.RunRepeatedly = false;
				await WaitForDoneQueue();
			}
			catch(Exception) { }

			if(rslt)
			{
				DumpQueue(-1);
				lock(AcqResultQueue)
				{
					rslt = AcqResultQueue.TryDequeue(out dataOut);
				}
			}
			if(!rslt || dataOut == null)
			{
				Debug.WriteLine("No data result available");
				return null;
			}
			//if(dataOut.Left.Length > 0)
			//	Debug.WriteLine($"Data result max left: {dataOut.Left.Max()}");
			//else
			//	Debug.WriteLine("Data result empty");
			return dataOut;
		}

		private void EnableUsbData(bool enable)
		{
			Debug.WriteLine($"usb dataflow enable: {enable}");
			if(IsEnabled != enable)
			{
				var qausb = QaComm.GetUsb();
				qausb?.WriteRegister(8, (uint)(enable ? 5 : 0)); // start data transfer
				IsEnabled = enable;
			}
		}

		public void HandleJobDone()
		{
			// we now have a list of all the rx buffers to convert to an array
			// use fixed size so that frombytestream and others work ok
			List<byte[]> rxResults = new();
			uint jobno = 0;
			uint jobSize = 0;
			lock (ReadDataQueue)
			{
				if (!ReadDataQueue.IsEmpty)
				{
					if (ReadDataQueue.TryDequeue(out AsyncResult? ar))
					{
						jobno = ar?.JobNumber ?? 0;
						jobSize = ar?.JobTotal ?? 0;
						// the very first block isn't saved since it's the prebuf
						if (ar != null && jobno != 0)
							rxResults.Add(ar.ReadBuffer);
						while (ReadDataQueue.TryPeek(out ar))
						{
							if (ar.JobNumber == jobno)
							{
								ReadDataQueue.TryDequeue(out ar);
								if (ar != null)
									rxResults.Add(ar.ReadBuffer);
							}
							else
							{
								// add the last post-job buffer if it exists
								rxResults.Add(ar.ReadBuffer);
								break;
							}
						}
					}
				}
			}
			lock(OutDataQueue)
			{
				// just clean up old OutDataQueue cache entries
				while (OutDataQueue.Count > 0)
				{
					AsyncResult? ar;
					if(OutDataQueue.TryPeek(out ar))
					{
						if (ar.JobNumber <= jobno && ar.IsDone())
							OutDataQueue.TryDequeue(out _);
						else
							break;
					}
				}
			}
			if(rxResults.Count == 0)
			{
				// this must be a trailer buffer with no data
				return;
			}
			// now rxResults is all data results for the job
			var rxData = new byte[rxResults.Sum(x => x.Length)];
			//Debug.WriteLine($"RxData bytes received: {rxData.Length}, blocks: {rxResults.Count}");
			int offset = 0;
			foreach (var b in rxResults)
			{
				Buffer.BlockCopy(b, 0, rxData, offset, b.Length);
				offset += b.Length;
			}
			// now have a byte array of the read datablocks in rxData
			//var truncdata = rxData[(int)(PreBufSize*8)..(int)(PreBufSize*8 + jobSize*8)];
			var ars = DealWithReadData(rxData, (int)jobno);
			lock(AcqResultQueue)
			{
				//while (AcqResultQueue.Count > 0)
				//{
				//	AcqResultQueue.TryDequeue(out _);
				//}
				AcqResultQueue.Enqueue(ars);
			}
			if(DataAvailable.CurrentCount == 0 )
			{
				DataAvailable.Release();
			}
		}

		private static bool HasAChannel(bool isLeft)
		{
			var useExternal = ViewSettings.Singleton.SettingsVm.UseExternalEcho;
			if (!useExternal)
				return false;
			return SoundUtil.HasChannel(isLeft);
		}

		public AcqResult DealWithReadData(byte[] rxData, int jobNumber)
		{
			// convert to double arrays
			AcqResult aResult = new AcqResult();
			aResult.Valid = true;
			aResult.JobNumber = jobNumber;
			QaUsb.FromByteStream(rxData, out aResult.Right, out aResult.Left);
			//Debug.WriteLine($"Deal with read data at {aResult.Left.Length}");
			// Note that left and right data is swapped on QA402, QA403, QA404. We do that via arg ordering below.
			// now we convert the stream to two data arrays
			// and scan the data to see where to clip the latency
			if (aResult.Valid == true && aResult.Left.Length > PreBufSize)
			{
				// Convert from dBFS to dBV. Note the 6 dB factor--the ADC is differential
				// Apply scaling factor to map from dBFS to Volts. 
				int tused = (int)FFTSize;    // should be fftsize

				var loff = 0;
				var roff = 0;
#if FALSE
				// check delay offset and calculate latency
				if (leftOut.Max() > 1e-7 || rightOut.Max() > 1e-7)
				{
					// amount of signal to check for latency
					var samplerate = QaComm.GetSampleRate();
					var adelay = _Qa40xDelay;// latency for internal generator empirically set to max # samples seen
					if (useExternal)
						adelay += (int)(samplerate / 100); // add 10 ms to internal checking to be safe from interrupt issues
					var edelay = (int)(ViewSettings.Singleton.SettingsVm.EchoDelay * sampleRate / 1000); // echodelay ms- latency for windows sound generator
					edelay = Math.Abs(edelay);

					// rough calculate the DC offset
					const int tossdc = 30;  // skip leading gunk
					var dcoffsetL = r.Left.Skip(tossdc).Take(preBuf - tossdc).Average();
					var dcoffsetR = r.Right.Skip(tossdc).Take(preBuf - tossdc).Average();
					// correct the ADC data along the way
					double ldmax = (HasAChannel(true) ? 0.01 : 1e-3) / (adcCal.Left * adcCorrection);
					double rdmax = (HasAChannel(false) ? 0.01 : 1e-3) / (adcCal.Right * adcCorrection);
					//double dmax = 1e-3;      // look for dMax deviation from dc offset
					// this scans through the input data looking for the first sample that exceeds a small threshold
					// locations are found per channel in case they come from different devices
					var checkamount = preBuf + adelay;
					if (useExternal)
						checkamount = preBuf + Math.Max(adelay, edelay);
					for (int i = preBuf; i < checkamount; i++)
					{
						// empirically we get -7e-5 until signal shows up
						// i assume that's dc offset...
						var inx = r.Left[i];
						var iny = r.Right[i];
						if (loff == 0 && Math.Abs(inx - dcoffsetL) > ldmax)
						{
							//Debug.WriteLine($"Detect left {inx} at {i}");
							loff = i;
							if (roff != 0)
								break;
						}
						if (roff == 0 && Math.Abs(iny - dcoffsetR) > rdmax)
						{
							//Debug.WriteLine($"Detect right {iny} at {i}");
							roff = i;
							if (loff != 0)
								break;
						}
					}

					if (loff == 0)
						loff = preBuf + (HasAChannel(true) ? edelay : adelay);
					if (roff == 0)
						roff = preBuf + (HasAChannel(false) ? edelay : adelay);
					Debug.WriteLine($"Prebuf={preBuf} Delay offset: {loff}, {roff}  DC offset: {dcoffsetL:G3},{dcoffsetR:G3}");
					if (!useExternal || !SoundUtil.HasChannel(true))
					{
						if (loff > (preBuf + adelay))
							loff = preBuf + adelay;
						if (loff < preBuf)
							loff = preBuf;
					}
					else
					{   // external audio going to left channel
						if (ViewSettings.Singleton.SettingsVm.EchoDelay < 0)
						{
							loff = Math.Min(preBuf + edelay, loff);  // calculated
							loff = Math.Min(r.Left.Length - tused, loff);
						}
						else
						{
							loff = preBuf + edelay; // fixed
						}
					}
					if (!useExternal || !SoundUtil.HasChannel(false))
					{
						if (roff > (preBuf + adelay))
							roff = preBuf + adelay;
						if (roff < preBuf)
							roff = preBuf;
					}
					else
					{   // external audio going to right channel
						if (ViewSettings.Singleton.SettingsVm.EchoDelay < 0)
						{
							roff = Math.Min(preBuf + edelay, roff); // calculated
							roff = Math.Min(r.Right.Length - tused, roff);
						}
						else
						{
							roff = preBuf + edelay; // fixed
						}
					}

					// programming bug from earlier, so guard
					if (loff < 0 || roff < 0)
					{
						roff = Math.Abs(roff);
						loff = Math.Abs(loff);
					}

					// both internal or both external, use min latency
					// this keeps the two channels in phase with each other when using a single output device (QA40x or Windows)
					if (!useExternal || 3 == SoundUtil.GetChannels())
					{
						loff = Math.Min(loff, roff);
						roff = loff;
					}
					Debug.WriteLine($"Final Prebuf={preBuf} Delay offset: {loff}, {roff}  DC offset: {dcoffsetL:G3},{dcoffsetR:G3}");

					//loff = 0;
					//roff = 0;
				}
#else
				{
					// --- external audio handling ---
					// echodelay ms- latency for windows sound generator
					//var sr = QaComm.GetSampleRate();
					//var edelay = (int)(ViewSettings.Singleton.SettingsVm.EchoDelay * sr / 1000);
					//var extLeft = HasAChannel(true);
					//var extRight = HasAChannel(false);  // external data is being sent here
					// --- external audio handling ---
					// fixed offset in samples
					var ltc = MathUtil.ToInt(ViewSettings.Singleton.SettingsVm.Latency, 47);
					roff = ltc;
					loff = ltc;
				}
#endif
				//var pmi = QaComm.GetInputRange();
				//Debug.Assert(pmi == ParamInput, "Different paraminput");
				var adcCorrection = Math.Pow(10, (ParamInput - 6.0) / 20);
				var rlf = aResult.Left.Skip(loff).Take(tused);
				if (rlf.Count() < tused)
					rlf = rlf.Concat(new double[tused - rlf.Count()]);
				Debug.Assert((rlf.Count() % 1024) == 0, "Must be a power of 2");
				var troff = 0;// rlf.Average();  // dc offset
				aResult.Left = rlf.Select(x => (x - troff) * AdcCalibration.Left * adcCorrection).ToArray();
				Debug.WriteLine($"Job {jobNumber} Maximum rlf value {rlf.Max()}, left value: {aResult.Left.Max()}");

				var rrf = aResult.Right.Skip(roff).Take(tused);
		
				if (rrf.Count() > 0)
				{
					troff = 0;// rrf.Average();  // dc offset
					if (rrf.Count() < tused)
						rrf = rrf.Concat(new double[tused - rrf.Count()]);
					aResult.Right = rrf.Select(x => (x - troff) * AdcCalibration.Right * adcCorrection).ToArray();
				}
			}
			return aResult;
		}

		public void HandleDataReady(bool isLast)
		{
			if(! UseInputData.Wait(100))
			{
				Debug.WriteLine("HandleDataReady called while previous data is still being processed.");
				return;
			}
			if (InDataQueue != null && InDataQueue.Count > 0)
			{
				uint jobItem = 0;
				// If we have data to send, send it
				var dataArray = InDataQueue.ToArray();
				// if we are starting a new data source use all
				// otherwise skip the prebuf
				var dataQueue = (CurrentJobNo == 0) ? dataArray : dataArray.Skip(1);
				if(isLast)
				{
					dataQueue = [dataArray[(CurrentJobNo == 1) ? 1 : 0]]; // if this is the last one, send the first block
				}
				uint bfrTotal = (uint)dataQueue.Sum(x => x.TheData?.Length ?? 0);
				foreach (var bfr in dataQueue)
				{
					var ec = SendDataBegin(bfr, CurrentJobNo, jobItem, bfrTotal);
					if (ec != ErrorCode.None)
					{
						Debug.WriteLine($"Error in SendDataBegin: {ec}");
						break;
					}
					ReadDataBegin(bfr.TheData.Length, CurrentJobNo, jobItem, bfrTotal);   // submit a read for each write
					jobItem++;
				}

				// when we're done ar is the last item in the group
				if(CurrentJobNo == 0)
				{
					EnableUsbData(true);
				}
			}
			UseInputData.Release();
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
			await QaComm.SetOutputRange(mlevel);   // set the output voltage
			if (bvm != null)
			{
				await QaComm.SetInputRange((int)bvm.Attenuation);
			}

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

			// now pad front and back of the values via prebuf and postbuf 
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
#if SHOWDBG
			Debug.WriteLine($"****** Parameters *******");
			Debug.WriteLine($"ParamInput: {ParamInput}, ParamOutput: {ParamOutput}, UsbBuffSize: {UsbBuffSize}");
			Debug.WriteLine($"DacCalibration: {DacCalibration.Left}, {DacCalibration.Right}");
			Debug.WriteLine($"AdcCalibration: {AdcCalibration.Left}, {AdcCalibration.Right}");
			Debug.WriteLine($"DbfsAdjustment: {DbfsAdjustment}");
			Debug.WriteLine($"PreBufSize: {PreBufSize}, PostBufSize: {PostBufSize}");
			Debug.WriteLine($"****** Parameters *******");
#else
			Debug.WriteLine($"****** ParamInput: {ParamInput}, ParamOutput: {ParamOutput}, UsbBuffSize: {UsbBuffSize}");
#endif
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

			// PreBuffer
			var lfa = new double[PreBufSize];
			sources.Add(new AsyncSource() { Left = lfa, Right = lfa });

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

			//// PostBuffer
			//lfa = new double[PostBufSize];
			//sources.Add(new AsyncSource() { Left = lfa, Right = lfa });

			foreach(var src in sources)
			{
				src.TheData = QaUsb.ToByteStream(src.Left, src.Right);
			}
			return sources;
		}


	}
}
