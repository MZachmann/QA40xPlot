using LibUsbDotNet.Main;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Diagnostics;

// this does the actual data send/receive
// it does this by creating a task that's a loop that runs until the app closes
// the loop deals with sending and receiving data while the higher level code
// handles parsing and creating the data packets and then dealing with results as requested
// The loop runs at a high level like this:
//	constantly create a new job based off the current document when the last job finishes
//
//  if we have a new document to send, create a job and submit the data for that job
//		otherwise keep sending any current document
//  if told to Pause, turn off an event so the loop sits waiting and stops the dataflow
//		otherwise it freeruns constantly sending the last job or blank if things are finished

namespace QA40xPlot.BareMetal
{
	public class UsbDataService
	{
		// how many finished jobs to keep in the done queue 
		private static readonly int DONE_JOB_MAX = 5;
		private static readonly int WATCH_JOB_MAX = 50;
		public static UsbDataService Singleton { get; } = new UsbDataService();
		public static readonly bool ShowDebug = false;
		private CancellationTokenSource RunTokenSource = new CancellationTokenSource();
		private Task ServiceTask = Task.Delay(1);
		SoundUtil? SoundObj { get; set; } = null;
		private bool _UseExternal = false;
		private int _LastExternalLatency = -1;        // currently last latency

		// a blank document for when we are idling
		// and just want to run the service and wait for a job to come in
		private static SendDoc BlankDoc = new();

		private static bool IsUsbEnabled { get; set; } = false;
		private static object UsbLock = new object();
		readonly int MainI2SReadWriteTimeout = 6000; // per baremetal

		private SendDoc? CurrentDoc = null;
		private ReceiveJob? CurrentJob = null;
		//
		private VerboseReset IsNextJobReady = new VerboseReset(nameof(IsNextJobReady), false);
		private VerboseReset IsAccepted = new VerboseReset(nameof(IsAccepted), false);
		private VerboseReset EnableSend = new VerboseReset(nameof(EnableSend), false);
		private VerboseReset EnableLoop = new VerboseReset(nameof(EnableLoop), false);
		private VerboseReset IsNotRunning = new VerboseReset(nameof(IsNotRunning), true);
		private VerboseReset IsStarted = new VerboseReset(nameof(IsStarted), false); // engine is started

		// short queue of the job we're doing and the next job pretty much
		private VerboseQueue<ReceiveJob> JobQueue = new(nameof(JobQueue));
		// list of jobs that finished along with their data and descriptors
		// one of these is enqueued each time an acquisition completes
		private VerboseQueue<ReceiveJob> DoneJobQueue = new(nameof(DoneJobQueue));
		// list of documents to send. this list is empty until we decide to acquire data
		private VerboseQueue<SendDoc> SendDocQueue = new(nameof(SendDocQueue));
		// list of documents sent. the last one on this list is the one we are currently sending
		// this list can get trimmed
		private VerboseQueue<SendDoc> SentDocQueue = new(nameof(SentDocQueue));
		// list of documents sent. the last one on this list is the one we are currently sending
		// this list can get trimmed
		private VerboseQueue<ReceiveJob> WatchedJobsQueue = new(nameof(WatchedJobsQueue));


		public UsbDataService()
		{
			IsUsbEnabled = true;    // ensure the next line writes to register 8
			EnableUsbData(false);
		}

		private void EnableUsbData(bool enable)
		{
			if (IsUsbEnabled != enable)
			{
				var qausb = QaComm.GetUsb();
				if (qausb != null)
				{
					lock (qausb)
					{
						qausb.WriteRegister(8, (uint)(enable ? 5 : 0)); // start data transfer
						IsUsbEnabled = enable;
					}
				}
				UsbSubs.DebugLineIf(ShowDebug, $"usb dataflow enable: {IsUsbEnabled}");
			}
		}

		// manually say if we should send one packet or many packets
		// when set we send another packet when one finishes sending and one is in queue
		// user tells us to stop running
		public async Task StopRunning()
		{
			if (IsStarted.IsSet && !_IsPausing)
			{
				var rqb = new ReceiveJob(BlankDoc);
				SendDocQueue.Enqueue(BlankDoc);
			}
			SoundObj?.Stop();   // turn off external generator now
			SoundObj = null;
			await Pause();
		}

		public ReceiveJob? GetLogResult(int idx)
		{
			if (idx < WatchedJobsQueue.Count)
			{
				// count backwards so 0 is the latest
				return WatchedJobsQueue.ElementAt(WatchedJobsQueue.Count - idx - 1);
			}
			return null;
		}

		/// <summary>
		/// top level method to run the data service once and return time data
		/// </summary>
		/// <param name="ct">the application cancellation token</param>
		/// <returns></returns>
		public static async Task<AcqResult?> UseDataService(BaseViewModel? bvm, bool forceUpdate, double[] outL, double[] outR, CancellationToken ct, bool runRepeat)
		{
			Debug.Assert(UsbDataService.Singleton != null, "UsbDataService singleton should not be null");
			return await Singleton.UseMyDataService(forceUpdate, outL, outR, ct, runRepeat);
		}

		/// <summary>
		/// top level method to run the data service once and return time data
		/// </summary>
		private async Task<AcqResult?> UseMyDataService(bool forceUpdate, double[] outL, double[] outR, CancellationToken ct, bool runRepeat)
		{
			if (IsNotRunning.IsSet)
			{
				// create a blank document
				await BlankDoc.CalculateParameters(null, [], []);
				BlankDoc.SplitIntoBuffers(BlankDoc.LeftData, BlankDoc.RightData);
				Start();
				IsNotRunning.Reset();
			}
			await UnPause();
			// check for external audio device usage
			UsbSubs.DebugLine("--entering UseMyDataService");
			_UseExternal = ViewSettings.Singleton.SettingsVm.UseExternalEcho && SoundUtil.ExternalPresent();

			// create the SendDoc for this run
			var newDoc = new SendDoc();
			await newDoc.CalculateParameters(null, outL, outR);
			bool needSend = true;
			// just turn it on for now and leave it on.
			// if it was off it will start i/o when we submit the first write block
			EnableUsbData(true);
			// see if it's the same as the running document
			var emptyJobs = JobQueue.IsEmpty;
			bool changedOutput = false;
			if (!emptyJobs)
			{
				// jobs waiting to complete list
				var latestJob = JobQueue.LastOrDefault();
				var latestDoc = latestJob?.TheSendDoc;
				changedOutput = newDoc.ParamOutput != latestDoc?.ParamOutput;
				if (latestDoc != null && newDoc.CompareDoc(latestDoc))
				{
					needSend = JobQueue.IsEmpty;
					newDoc = latestDoc;
					UsbSubs.DebugLine($"UseMyDataService: new doc is the same so need={needSend}");
				}
			}

			if (needSend )
			{
				if (_UseExternal)
				{
					runRepeat = false;
					SoundObj?.Stop();
					SoundObj = null;
					// if we are using an external sound device, prepare the waveform
					SoundObj = PrepareExternal(true, outL, outR, (int)newDoc.SampleRate);

				}
				else
				{
					SoundObj = null;
				}
				newDoc.SplitIntoBuffers(outL, outR);
				// now start data transfer
				SendDocQueue.Enqueue(newDoc);
				SoundObj?.PlayRepeat();
				_LastExternalLatency = -1;  // set to unknown
				UsbSubs.DebugLineIf(ShowDebug, $"SendDocQ.Enqueue -- bfrs={newDoc.Buffers.Count} tsize={newDoc.Buffers.Sum(x => x.Length)}");
			}
			// find the job we expect to finish soonest
			var job = await WaitForNextJob(newDoc, ct);
			// if it's external or the output relay has twigged we need to wait for
			// the next input to let it settle if output or external
			if (job != null && ((_UseExternal && needSend) || changedOutput))
			{
				// ignore the first job with this document
				var job2 = await WaitForNextJob(newDoc, ct);    // wait for the second pass
				job = job2;
			}
			UsbSubs.DebugLineIf(ShowDebug, $"Waitforjob: {job?.JobNumber}");
			if (job != null)
			{
				var acr = await WaitForResults(job, ct);
				UsbSubs.DebugLineIf(ShowDebug, $"Waitforresults: {job?.JobNumber}");
				return acr;
			}
			return null;
		}

		private static bool _IsStarting = false;
		/// <summary>
		/// start up the usb data service
		/// </summary>
		/// <returns></returns>
		public void Start()
		{
			if (_IsStarting)
				return;
			_IsStarting = true;
			if (!IsStarted.IsSet)
			{
				UsbSubs.DebugLine("Starting DataService...");
				//RunTokenSource.Dispose();
				RunTokenSource = new CancellationTokenSource();
				ServiceTask = RunService(RunTokenSource.Token);
				IsStarted.Set();
				EnableSend.Set();
				EnableLoop.Set();
				UsbSubs.DebugLine("Started DataService...");
			}
			_IsStarting = false;
		}

		private static bool _IsPausing = false;
		public async Task Pause()
		{
			if (_IsPausing)
				return;
			_IsPausing = true;
			// shut the running loop down
			if (IsStarted.IsSet && EnableLoop.IsSet)
			{
				UsbSubs.DebugLine("Pausing UsbDataService...");
				// wait for data to stop flowing
				EnableSend.Reset();  // stop sending data
				while (!JobQueue.IsEmpty)
					await Task.Delay(100);
				EnableLoop.Reset(); // now halt the loop entirely
				await Task.Delay(100); // let it pause on the infinite wait
									   // clear all the queues to get ready for next time
				EnableUsbData(false);
				JobQueue.Clear();
				DoneJobQueue.Clear();
				SentDocQueue.Clear();
				SendDocQueue.Clear();
				UsbSubs.DebugLine("Pause Completed");
			}
			_IsPausing = false;

		}

		public async Task UnPause()
		{
			// shut the running loop down
			if (IsStarted.IsSet && !EnableLoop.IsSet)
			{
				EnableUsbData(true);
				UsbSubs.DebugLine("Unpause...");
				// turn stuff on again
				EnableSend.Set();  // start sending data
				EnableLoop.Set(); // now halt the loop entirely
			}
		}

		private void SubmitData(ReceiveJob? job)
		{
			if (job != null)
			{
				UsbSubs.DebugLineIf(ShowDebug, $"SubmitData: submitting job {job.JobNumber} with {job.TheSendDoc.Buffers.Count} buffers");
				uint jobItem = 0;
				foreach (var bfr in job.TheSendDoc.Buffers)
				{
					var ec = SubmitSendData(job.SendPackets, bfr, job.JobNumber, jobItem, bfr.Length);
					if (ec == ErrorCode.None)
					{
						var acqr = SubmitReadData(job.ReadPackets, bfr.Length, job.JobNumber, jobItem, bfr.Length);
					}
					jobItem++;
				}
			}
		}

		/// <summary>
		/// job 0 is theprebuffer - white space then signal beginning
		/// job 1...n
		/// is the signal data
		/// </summary>
		/// <param name="ctk"></param>
		/// <returns></returns>
		private async Task RunService(CancellationToken ctk)
		{
			try
			{
				uint jobNumber = 0;
				bool newDoc = false;
				ReceiveJob? receiveJob = null;
				while (!ctk.IsCancellationRequested)
				{
					newDoc = false;
					try
					{
						await EnableLoop.WaitHandleAsync(Timeout.Infinite, ctk);
					}
					catch (Exception ex)
					{
						// it's probably a cancellation so ignore it and just
						UsbSubs.DebugLine($"RunService: EnableLoop wait was {ex.Message}");
					}
					if (ctk.IsCancellationRequested)
					{
						break;
					}
					// do we have a new document to send?
					while (!SendDocQueue.IsEmpty && !ctk.IsCancellationRequested)
					{
						if (SendDocQueue.TryDequeue(out SendDoc? prev))
						{
							if (prev != null)
							{
								SentDocQueue.Enqueue(prev);
							}
						}
						newDoc = true;
					}
					while (SentDocQueue.Count > 3)
					{
						SentDocQueue.TryDequeue(out _);
					}

					// ensure the job queue always has at least two jobs in it
					// time for another job to queue up?
					if (EnableSend.IsSet && (newDoc || JobQueue.Count < 3) && !ctk.IsCancellationRequested)
					{
						if (!SentDocQueue.IsEmpty)
						{
							// use the newest available SendDoc to create a job
							receiveJob = new ReceiveJob(SentDocQueue.Last());
							receiveJob.JobNumber = ++jobNumber;
							SubmitData(receiveJob); // submit the asynchronous r and w requests
							JobQueue.Enqueue(receiveJob);
							UsbSubs.DebugLineIf(ShowDebug, $"JobQueue count is {JobQueue.Count}, SentDocQueue count is {SentDocQueue.Count}");
						}
					}

					{
						// the wait for job code (in a different thread) needs to tell us to 
						// save the results of this job when it's done, so check IsNextJobReady
						// if we want a new job to watch find the first one that matches
						// so we don't have to wait through the entire queue to get a result
						// because there are always three jobs in queue
						if (!IsNextJobReady.IsSet && receiveJob != null && ReferenceEquals(CurrentDoc, receiveJob.TheSendDoc))
						{
							if(receiveJob.IsWatched == false && !ctk.IsCancellationRequested)
							{
								// find the first available queued job that matches this document
								for(int i = 0; i < JobQueue.Count; i++)
								{
									var jbs = JobQueue.ElementAt(i);
									if (ReferenceEquals(jbs.TheSendDoc, CurrentDoc) && !jbs.IsWatched)
									{
										CurrentJob = jbs;
										break;
									}
								}
								// we have someone waiting for this job
								if (CurrentJob != null)
								{
									IsNextJobReady.Set();   // inform the waiter
								}
							}
						}
					}

					// see if the current job is finished receiving
					if (!ctk.IsCancellationRequested)
					{
						if (JobQueue.TryPeek(out ReceiveJob? peek))
						{
							if (true == peek?.IsFinished())
							{
								// this job is done, remove it from the queue and add the result to the result queue
								// next loop we'll add another job to the queue
								if (JobQueue.TryDequeue(out ReceiveJob? doneJob))
								{
									//UsbSubs.DebugLine($"Job {doneJob?.JobNumber} finished and dequeued. Count is now {JobQueue.Count}");
									if (doneJob != null)
									{
										DoneJobQueue.Enqueue(doneJob);
									}
								}
								while(DoneJobQueue.Count > DONE_JOB_MAX)
								{
									DoneJobQueue.TryDequeue(out _);
								}
							}
							else
							{
								// there is a job running but it's not finished yet
								await Task.Delay(30);
							}
						}
						else
						{
							// there are no jobs running currently
							await Task.Delay(100);
						}
					}
				}
			}
			catch (Exception e) 
			{ 
				UsbSubs.DebugLine($"Exception in RunService: {e.Message}");
			}
			IsNotRunning.Set();
			IsStarted.Reset();
			UsbSubs.DebugLine("DataService stopped.");
		}

		/// <summary>
		/// Submits a buffer to be written and returns immediately. The submitted buffer is 
		/// copied to a local buffer before returning.
		/// </summary>
		/// <param name="data"></param>
		public ErrorCode SubmitSendData(Queue<AcqAsyncResult> pktQ, byte[] dataSet, uint jobNo, uint jobItem, int jobTotal)
		{
			ErrorCode ec = ErrorCode.None;

			if (dataSet == null || dataSet.Length == 0)
			{
				return ErrorCode.InvalidParam;
			}

			UsbTransfer? ar = null;
			var qaUsb = QaComm.GetUsb();
			lock (qaUsb ?? UsbLock)
			{
				// we need to flush before every send to avoid a weird issue
				// where the first packet gets stuck and never sends
				ec = qaUsb?.DataWriter?.SubmitAsyncTransfer(dataSet, 0, dataSet.Length, MainI2SReadWriteTimeout, out ar) ?? ErrorCode.UnknownError;
			}
			if (ec != ErrorCode.None)
			{
				UsbSubs.DebugLine($"Bad result in SendDataBegin {ec}");
			}
			if (ar != null)
			{
				//UsbSubs.DebugLine($"WriteSubmit === job:{jobNo}, item:{jobItem}, total:{jobTotal}");
				pktQ.Enqueue(new AcqAsyncResult(ar, dataSet, jobNo, jobItem, (uint)jobTotal));
			}
			return ec;
		}

		/// <summary>
		/// Creates and submits a buffer to be read asynchronously. Returns immediately.
		/// </summary>
		/// <param name="data"></param>
		public AcqAsyncResult? SubmitReadData(Queue<AcqAsyncResult> pktQ, int bufSize, uint jobNo, uint jobItem, int jobTotal)
		{
			AcqAsyncResult? result = null;
			{
				byte[] readBuffer = new byte[bufSize];
				UsbTransfer? usbT = null;
				ErrorCode? ec = ErrorCode.DeviceNotFound;
				var qaUsb = QaComm.GetUsb();
				lock (qaUsb ?? UsbLock)
				{
					ec = qaUsb?.DataReader?.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, MainI2SReadWriteTimeout, out usbT);
				}
				if (ec != ErrorCode.None || usbT == null)
				{
					UsbSubs.DebugLine($"ERROR: {ec} in SubmitReadData");
				}
				if (usbT != null)
				{
					result = new AcqAsyncResult(usbT, readBuffer, jobNo, jobItem, (uint)jobTotal);
					pktQ.Enqueue(result);
					//UsbSubs.DebugLine($"ReadSubmit === job:{jobNo}, item:{jobItem}, total:{jobTotal}");
				}
			}
			return result;
		}

		/// <summary>
		///  wait for the next job using this document
		/// </summary>
		/// <param name="myDoc"></param>
		/// <param name="ctk"></param>
		/// <returns></returns>
		private async Task<ReceiveJob?> WaitForNextJob(SendDoc myDoc, CancellationToken ctk)
		{
			try
			{
				ReceiveJob? myJob = null;
				CurrentJob = null;
				CurrentDoc = myDoc;
				IsNextJobReady.Reset();     // for for next job
				var ux = await IsNextJobReady.WaitHandleAsync(Timeout.Infinite, ctk);
				if (ux == 1)
				{
					myJob = CurrentJob;			// this gets set by the guy that turns on IsNextJobReady
					if(myJob != null)
					{
						myJob.IsWatched = true;     // don't find this one again
						UsbSubs.DebugLineIf(ShowDebug, $"WaitForNextJob got job {myJob?.JobNumber}");
					}
				}
				else
				{
					UsbSubs.DebugLineIf(ShowDebug, $"WaitForNextJob was cancelled while waiting for next job");
				}
				return myJob;
			}
			catch (Exception ex) 
			{
				UsbSubs.DebugLineIf(ShowDebug, $"WaitForNextJob was {ex.Message} ");
			}
			return null;
		}

		private async Task<AcqResult?> WaitForResults(ReceiveJob myJob, CancellationToken ctk)
		{
			// for now just wait for the first result to come in
			UsbSubs.DebugLineIf(ShowDebug, $"WaitForResults with job {(myJob?.JobNumber)??100}");
			// wait for myjob to finish (it should be)
			if (myJob != null && !ctk.IsCancellationRequested)
			{
				// get the handle of the last packet in the job we need to read
				var vhand = myJob.AsyncHandle();
				if (vhand != null) 
				{
					try
					{
						var x = await vhand.WaitHandleAsync(Timeout.Infinite, ctk);
					}
					catch(Exception ex)
					{
						// it's probably a cancellation so ignore it and just
						UsbSubs.DebugLine($"Wait for job {myJob.JobNumber} was {ex.Message}");
					}
				}
			}

			// parse job data into AcqResult
			AcqResult acqr = await DealWithReadData(myJob, ctk);

			// save the job into history if it's not blank and we care about it
			// and we have EarlyRelease == true
			if (ViewSettings.Singleton.MainVm.EarlyRelease && myJob != null && !ctk.IsCancellationRequested)
			{
				if (!ReferenceEquals(myJob.TheSendDoc, BlankDoc))
					if (myJob.TheSendDoc.LeftData.Length > 0)
						if (myJob.TheSendDoc.LeftData.Max() > 1e-3 || myJob.TheSendDoc.RightData.Max() > 1e-3)
							WatchedJobsQueue.Enqueue(myJob);
				while (WatchedJobsQueue.Count > WATCH_JOB_MAX && !ctk.IsCancellationRequested)
				{
					WatchedJobsQueue.TryDequeue(out _);
				}
			}

			// get the data from myjob
			return ctk.IsCancellationRequested ? null : acqr;
		}

		private int CheckStart(double[] data, int maxDelay)
		{
			if (maxDelay == 0)
				return 0;
			return maxDelay;
			//// this checks the start of the data for a signal above the noise floor
			//// it returns the offset to the start of the signal
			//double maxNoise = 1e-4;   // empirically we see up to about -5e-6 in the first 1000 samples with no signal, so this is a little above that
			//double[] deltas = new double[maxDelay];
			//int total = 0;
			//int offset = 0;
			//for (int i = 0; i < maxDelay; i++)
			//{
			//	deltas[i] = data[i + 1] - data[i];  // first derivative
			//	if (deltas[i] > maxNoise || deltas[i] < -maxNoise)
			//	{
			//		// if we see 3 in a row above the noise floor, stop checking
			//		if (++total > 3)
			//		{
			//			offset = i;
			//			break;
			//		}
			//	}
			//	else if (total > 0)
			//	{
			//		total--;
			//	}
			//}
			//if (offset == maxDelay)
			//{
			//	return 0;   // if we see no signal at all, just return 0 to avoid cutting off the start of the data
			//}
			//return offset;
		}

		/// <summary>
		/// find a job by job number
		/// </summary>
		/// <param name="jobNumber"></param>
		/// <returns>the job or null</returns>
		private ReceiveJob? FindByNumber(int jobNumber)
		{
			var job = JobQueue.FirstOrDefault(j => j.JobNumber == jobNumber);
			if(job == null)
			{
				job = DoneJobQueue.FirstOrDefault(j => j.JobNumber == jobNumber);
			}
			return job;
		}

		/// <summary>
		/// convert job data into a timeseries
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		public async Task<AcqResult> DealWithReadData(ReceiveJob? job, CancellationToken ctk)
		{
			AcqResult aResult = new AcqResult();
			// convert to double arrays
			aResult.Valid = false;
			aResult.JobNumber = (int)(job?.JobNumber ?? 0);
			var theDoc = job?.TheSendDoc;
			if (theDoc == null || job == null)
				return aResult;

			UsbSubs.DebugLine($"DealWithReadData with job {job.JobNumber} and isdone: {job.IsFinished()}");
			var rxResults = job.ReadPackets.Select(x => x.ReadBuffer).ToList();
			if (rxResults.Count == 0)
			{
				// this must be a trailer buffer with no data
				UsbSubs.DebugLineIf(ShowDebug, $"{rxResults.Count} results collected for job {aResult.JobNumber}");
				return aResult;
			}
			// look to see if we have one more buffer we can use
			var nxtJob = FindByNumber(aResult.JobNumber + 1);				
			if (nxtJob != null)
			{
				var pkq = nxtJob.ReadPackets.First();
				if( await pkq.WaitAsync(Timeout.Infinite, ctk))
				{
					var nxt = nxtJob.ReadPackets.First().ReadBuffer;    // the next buffer
					//var skip = Math.Abs(nxtJob.TheSendDoc.BlockSkip);
					//var nxt2 = nxt.Skip(skip).Concat(nxt.Take(skip));	// rotate
					rxResults.Add(nxt);
				}
			}
			var rxLength = rxResults.Sum(x => x.Length);
			if (rxLength < (8*theDoc.FFTSize))
			{
				UsbSubs.DebugLineIf(ShowDebug, $"Not enough data received for job {aResult.JobNumber}: {rxLength} bytes");
				return aResult;
			}
			if(ctk.IsCancellationRequested)
				return aResult;

			// now rxResults is all data results for the job
			var rxData = new byte[rxLength];
			//WriteLine($"RxData bytes received: {rxData.Length}, blocks: {rxResults.Count}");
			int offset = 0;
			foreach (var b in rxResults)
			{
				Buffer.BlockCopy(b, 0, rxData, offset, b.Length);
				offset += b.Length;
			}

			if (rxData.Length == 0 || ctk.IsCancellationRequested)
			{
				UsbSubs.DebugLine("No data read from USB");
				return aResult;
			}
			UsbSubs.DebugLineIf(ShowDebug, $"Dealing with read data of length {rxData.Length} for job {aResult.JobNumber}");
			QaUsb.FromByteStream(rxData, out aResult.Right, out aResult.Left);

			// now we convert the stream to two data arrays
			// and scan the data to see where to clip the latency
			{
				// Convert from dBFS to dBV. Note the 6 dB factor--the ADC is differential
				// Apply scaling factor to map from dBFS to Volts. 
				int tused = (int)theDoc.FFTSize;    // should be fftsize
											 // internal stuff has a fixed latency
											 // but external audio is all over the map
				var ltc = MathUtil.ToInt(ViewSettings.Singleton.SettingsVm.Latency, 47);
				var roff = ltc;
				var loff = ltc;

				if (_UseExternal)
				{
					// lastexternallatency tracks where the first latency value
					// since repeats keep the exact same latency and can't do the same search
					var rxoff = _LastExternalLatency;
					var lxoff = _LastExternalLatency;

					var toff = ViewSettings.Singleton.SettingsVm.EchoDelay;
					var maxDelay = (int)Math.Abs(toff * theDoc.SampleRate / 1000);  // delay in samples

					// left channel check
					if (QaUsb.HasAChannel(true))
					{
						var moff = aResult.Left.Average();
						aResult.Left = aResult.Left.Select(x => x - moff).ToArray();  // remove dc offset
						if (lxoff == -1)
						{
							lxoff = Math.Max(lxoff, CheckStart(aResult.Left, loff+maxDelay));
							_LastExternalLatency = lxoff;
						}
						else
							lxoff = _LastExternalLatency;
						loff = lxoff;
					}
					// right channel check
					if (QaUsb.HasAChannel(false))
					{
						var moff = aResult.Right.Average();
						aResult.Right = aResult.Right.Select(x => x - moff).ToArray();  // remove dc offset
						if(rxoff == -1)
						{
							rxoff = Math.Max(rxoff, CheckStart(aResult.Right, roff+maxDelay));
							if (_LastExternalLatency == -1)
								_LastExternalLatency = rxoff;
						}
						else
							rxoff = _LastExternalLatency;
						roff = rxoff;
					}
				}

				if (ctk.IsCancellationRequested)
					return aResult;
				var adcCorrection = Math.Pow(10, (theDoc.ParamInput - 6.0) / 20);
				var rlf = aResult.Left.Skip(loff).Take(tused);
				if (rlf.Count() < tused)
					rlf = rlf.Concat(new double[tused - rlf.Count()]);
				//Debug.Assert((rlf.Count() % 1024) == 0, "Must be a power of 2");
				var troff = 0;// rlf.Average();  // dc offset
				aResult.Left = rlf.Select(x => (x - troff) * theDoc.AdcCalibration.Left * adcCorrection).ToArray();
				UsbSubs.DebugLine($"Job {aResult.JobNumber} Maximum rlf value {rlf.Max()}, left value: {aResult.Left.Max()}");

				var rrf = aResult.Right.Skip(roff).Take(tused);
				if (rrf.Count() > 0)
				{
					troff = 0;// rrf.Average();  // dc offset
					if (rrf.Count() < tused)
						rrf = rrf.Concat(new double[tused - rrf.Count()]);
					aResult.Right = rrf.Select(x => (x - troff) * theDoc.AdcCalibration.Right * adcCorrection).ToArray();
				}
				if(ctk.IsCancellationRequested) 
					return aResult;
				UsbSubs.DebugLine($"Job {aResult.JobNumber} Maximum right value: {aResult.Right.Max()}");
				aResult.Valid = true;
			}
			UsbSubs.DebugLineIf(ShowDebug, $"DealWithReadData finished with valid={aResult.Valid} length={aResult.Left.Length} ");

			// note this data gets flipped for the qa402 in DoAcquireUser
			LeftRightTimeSeries lrts = new();
			lrts.Left = aResult.Left;
			lrts.Right = aResult.Right;
			lrts.dt = 1.0 / theDoc.SampleRate;
			job.LrtsJob = lrts;

			return aResult;
		}

		// external sound device support
		private SoundUtil? PrepareExternal(bool useExternal, double[] leftOut, double[] rightOut, int sampleRate)
		{
			SoundUtil? soundObj = null;
			// we are sending to an external sound system device
			if (useExternal)
			{
				soundObj = SoundUtil.CreateUtil(ViewSettings.Singleton.SettingsVm.EchoName, leftOut, rightOut, sampleRate);
			}
			return soundObj;
		}

	}
}
