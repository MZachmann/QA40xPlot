using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Data;
using System.IO;
using System.Windows;

namespace QA40xPlot.Actions
{
	public class ActBase
	{
		public List<LeftRightFrequencySeries> FrequencyHistory { get; set; } = new();   // for averaging

		// this is the initial gain calculation so that we can get attenuation and input voltage settings
		private LeftRightFrequencySeries? _LRGains = null;
		public LeftRightFrequencySeries? LRGains
		{
			get => _LRGains;
			set => _LRGains = value;
		}

		public static void ClipName(DataDescript defn, string fileName)
		{
			if (defn.Name.Length == 0)
			{
				FileInfo fileInfo = new FileInfo(fileName);
				defn.Name = fileInfo.Name;
				if (defn.Name.EndsWith(".zip"))
				{
					defn.Name = defn.Name.Substring(0, defn.Name.Length - 4);
				}
				if (defn.Name.EndsWith(".plt"))
				{
					defn.Name = defn.Name.Substring(0, defn.Name.Length - 4);
				}
			}

		}

		protected LeftRightFrequencySeries CalculateAverages(LeftRightFrequencySeries fseries, uint averages)
		{
			LeftRightFrequencySeries fresult = new();
			if (averages <= 1 || FrequencyHistory.Count == 0)
			{
				fresult = fseries;
				if (averages > 1)
					FrequencyHistory.Add(fseries);
			}
			else
			{
				// change in averages or full???
				while (FrequencyHistory.Count > (averages - 1))
				{
					FrequencyHistory.RemoveAt(0);
				}
				if (fseries.Df != FrequencyHistory.First().Df
					|| fseries.Left.Length != FrequencyHistory.First().Left.Length)
				{
					// entirely new values
					FrequencyHistory.Clear();
				}

				// instead of doing some moving average or first-last thing just brute add it
				// it's not that much overhead
				FrequencyHistory.Add(fseries);          // plus 1
														//
				fresult.Df = FrequencyHistory.First().Df;
				fresult.Left = new double[FrequencyHistory[0].Left.Length];
				fresult.Right = new double[FrequencyHistory[0].Right.Length];
				foreach (var fst in FrequencyHistory)
				{
					fresult.Left = fresult.Left.Zip(fst.Left, (x, y) => x + y * y).ToArray();
					fresult.Right = fresult.Left.Zip(fst.Right, (x, y) => x + y * y).ToArray();
				}
				var cnt = FrequencyHistory.Count;
				fresult.Left = fresult.Left.Select(x => Math.Sqrt(x / cnt)).ToArray();
				fresult.Right = fresult.Right.Select(x => Math.Sqrt(x / cnt)).ToArray();
			}
			return fresult;
		}

		/// <summary>
		/// Start an action by checking for device connected
		/// If all ok set IsRunning bool and return true
		/// </summary>
		/// <param name="bvm">the local video model</param>
		/// <returns>true if all good</returns>
		public async Task<bool> StartAction(BaseViewModel bvm)
		{
			if (bvm.IsRunning)
			{
				MessageBox.Show("Device is already running");
				return false;
			}
			if (await QaComm.CheckDeviceConnected() == false)
			{
				MessageBox.Show("Unable to connect to the device.");
				return false;
			}
			bvm.IsRunning = true;
			WaveGenerator.Clear();  // disable both generators and the WaveGenerator itself
			FrequencyHistory.Clear();
			return true;
		}

		public async Task EndAction(BaseViewModel bvm)
		{
			// Turn the generator off
			WaveGenerator.SetEnabled(false);
			if ( ViewSettings.Singleton.SettingsVm.RelayUsage == "OnFinish")
				await QaComm.SetInputRange(QaLibrary.DEVICE_MAX_ATTENUATION);  // set max attenuation while idle...
																	// detach from usb port
			//QaComm.Close(false);
			bvm.IsRunning = false;
			bvm.HasSave = true; // set the save flag
			FrequencyHistory.Clear();	// empty averaging history
		}

		protected async Task showMessage(String msg, int delay = 0)
		{
			var vm = ViewSettings.Singleton.Main;
			await vm.SetProgressMessage(msg, delay);
		}

		protected async Task showProgress(int progress, int delay = 0)
		{
			var vm = ViewSettings.Singleton.Main;
			await vm.SetProgressBar(progress, delay);
		}

		private static double GetFGain(double[] values, double inputV, int binmin, int bintrack)
		{
			return values.Skip(binmin).Take(bintrack).Max() / inputV;
		}

		protected static int ToBinNumber(double dFreq, LeftRightFrequencySeries? lrGain)
		{
			return (int)Math.Floor(dFreq / (lrGain?.Df ?? 1));
		}

		protected async Task<LeftRightSeries> MeasureNoise(CancellationToken ct, bool setRange = false)
		{
			var range = 0;
			if(setRange)
				range = QaComm.GetInputRange();
			// ********************************************************************
			// Do noise floor measurement with source off
			// ********************************************************************
			await showMessage($"Determining noise floor.");
			System.Diagnostics.Debug.WriteLine("***-------------Measuring noise-------------.");
			WaveGenerator.SetEnabled(false);
			if( setRange)
				await QaComm.SetInputRange(6); // and a small range for better noise...
			//Thread.Sleep(1000);
			//await QaComm.DoAcquisitions(1, ct);			// this one returns a high value until settled
			var lrs = await QaComm.DoAcquisitions(1, ct); // now that it's settled...
			if (setRange)
				await QaComm.SetInputRange(range); // restore the range

			return lrs;
		}

		/// <summary>
		/// Determine the gain at a specific frequency. This is done by sending a sine wave at the given frequency
		/// This set the attenuation in hardare to 42 before the test
		/// </summary>
		/// <param name="dfreq"></param>
		/// <param name="inits"></param>
		/// <param name="average"></param>
		/// <returns></returns>
		protected static async Task<LeftRightFrequencySeries?> DetermineGainAtFreq(double dfreq, int average = 1)
		{
			// initialize very quick run
			uint fftsize = 65536;
			uint sampleRate = 96000;
			// flattop windowing gives us the best fundamental value accuracy even if not bin center...
			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, "FlatTop", QaLibrary.DEVICE_MAX_ATTENUATION))
				return null;

			// the simplest thing here is to do a quick burst low value...
			var generatorV = 0.01;          // random low test value
			// we must have this in the bin center here
			dfreq = QaLibrary.GetNearestBinFrequency(dfreq, sampleRate, fftsize);
			WaveGenerator.SetGen1(dfreq, generatorV, true); // send a sine wave
			WaveGenerator.SetEnabled(true);					// enable generator
			var ct = new CancellationTokenSource();
			// do two and average them
			//await QaComm.DoAcquisitions(1, ct.Token);        // Do a single acquisition to settle stuff
			LeftRightSeries acqData = await QaComm.DoAcquisitions(1, ct.Token);        // Do a single acquisition
			if (acqData == null || acqData.FreqRslt == null || acqData.TimeRslt == null || ct.IsCancellationRequested)
				return null;

			uint fundamentalBin = QaLibrary.GetBinOfFrequency(dfreq, acqData.FreqRslt.Df);
			int binmin = (int)Math.Max(0, fundamentalBin - 2);
			int bintrack = (int)(Math.Min(fftsize, fundamentalBin + 2) - binmin);

			//// the amplitude is max of a small area
			//var maxl = GetFGain(acqData.FreqRslt.Left, generatorV, binmin, bintrack);
			//var maxr = GetFGain(acqData.FreqRslt.Right, generatorV, binmin, bintrack);

			//var maxi = Math.Max(maxl, maxr);
			//// since we're running with 42db of attenuation...
			//if (maxi < 0.1)
			//{
			//	// get some more accuracy with this
			//	await QaComm.SetInputRange(18);
			//	// do two and average them
			//	await QaComm.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
			//	acqData = await QaComm.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
			//	if (acqData == null || acqData.FreqRslt == null || ct.IsCancellationRequested)
			//		return null;
			//}

			// calculate gain for each channel from frequency response
			LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries();
			lrfs.Left = new double[] { GetFGain(acqData.FreqRslt.Left, generatorV, binmin, bintrack) };
			lrfs.Right = new double[] { GetFGain(acqData.FreqRslt.Right, generatorV, binmin, bintrack) };
			lrfs.Df = acqData.FreqRslt.Df;
				
			// if we're asking for averaging
			for (int j = 1; j < average; j++)
			{
				acqData = await QaComm.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
				if (acqData == null || acqData.FreqRslt == null || ct.IsCancellationRequested)
					return null;

				// calculate gain for each channel from frequency response
				lrfs.Left[0] += GetFGain(acqData.FreqRslt.Left, generatorV, binmin, bintrack);
				lrfs.Right[0] += GetFGain(acqData.FreqRslt.Right, generatorV, binmin, bintrack);
			}
			if (average > 1)
			{
				lrfs.Left = lrfs.Left.Select(x => x / average).ToArray();
				lrfs.Right = lrfs.Right.Select(x => x / average).ToArray();
			}
			return lrfs;       // Return the gain information as a one element frequency series
		}

		protected async Task CalculateGainCurve(BaseViewModel bvm)
		{
			// show that we're autoing...
			var atten = bvm.Attenuation;
			bvm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;
			await showMessage("Calculating DUT gain");
			LRGains = await DetermineGainCurve(true, 1);
			bvm.Attenuation = atten; // restore the original value just in case bvm is also the local model
			// in general this gets reset immediately for the next test
		}

		protected async Task CalculateGainAtFreq(BaseViewModel bvm, double dFreq, int averages = 1)
		{
			// show that we're autoing...
			var atten = bvm.Attenuation;
			bvm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;
			await showMessage($"Calculating DUT gain at {dFreq}");
			LRGains = await DetermineGainAtFreq(dFreq, averages);
			bvm.Attenuation = atten;    // restore the original value
		}


		/// <summary>
		/// Determine the gain curve for the device. This is done by sending a chirp at 96KHz
		/// this sets the attenuation in hardware to 42 before the test
		/// </summary>
		/// <param name="inits"></param>
		/// <param name="average"></param>
		/// <returns></returns>
		protected static async Task<LeftRightFrequencySeries?> DetermineGainCurve(bool inits, int average = 1)
		{
			// initialize very quick run
			uint fftsize = 65536;
			uint sampleRate = 96000;
			string swindow = ViewSettings.IsUseREST ? "Rectangle" : "Rectangular";
			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, swindow, QaLibrary.DEVICE_MAX_ATTENUATION))
				return null;

			{
				// the simplest thing here is to do a chirp at a low value...
				var generatorV = 0.01;			// random low test value
				var chirpy = Chirps.ChirpVp((int)fftsize, sampleRate, generatorV, 6, 24000);
				var ct = new CancellationTokenSource();
				// get the data
				LeftRightSeries acqData = await QaComm.DoAcquireUser(1, ct.Token, chirpy, chirpy, false);
				if (acqData == null || acqData.TimeRslt == null || ct.IsCancellationRequested)
					return null;

				// calculate gain for each channel from frequency response
				LeftRightTimeSeries lrts = new LeftRightTimeSeries();
				lrts.Left = acqData.TimeRslt.Left;
				lrts.Right = acqData.TimeRslt.Right;
				lrts.dt = acqData.TimeRslt.dt;
				//
				for(int j=1; j<average; j++)
				{
					acqData = await QaComm.DoAcquireUser(1, ct.Token, chirpy, chirpy, false);        // Do a single aqcuisition
					if (acqData == null || (acqData.TimeRslt == null) || ct.IsCancellationRequested)
						return null;

					// calculate gain for each channel from frequency response
					lrts.Left = acqData.TimeRslt.Left.Zip(lrts.Left, (x, y) => x+y).ToArray();
					lrts.Right = acqData.TimeRslt.Right.Zip(lrts.Right, (x, y) => x + y).ToArray();
				}
				if (average > 1)
				{
					lrts.Left = lrts.Left.Select(x => x/average).ToArray();
					lrts.Right = lrts.Right.Select(x => x / average).ToArray();
				}
				// now do the frequency transformation
				LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries();
				var norms = Chirps.NormalizeChirpDbl(chirpy, generatorV, (lrts.Left, lrts.Right));
				lrfs.Left = norms.Item1;
				lrfs.Right = norms.Item2;
				lrfs.Df = QaLibrary.CalcBinSize(sampleRate, fftsize);
				// now normalize by the input voltage so we get gain instead
				var gv = generatorV;
				lrfs.Left = lrfs.Left.Select(x => x / gv).ToArray();
				lrfs.Right = lrfs.Right.Select(x => x / gv).ToArray();
				return lrfs;       // Return the new generator amplitude and acquisition data
			}
		}
	}
}
