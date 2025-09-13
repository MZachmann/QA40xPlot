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


		protected static double ToD(string sval, double defval = 0.0)
		{
			return MathUtil.ToDouble(sval, defval);
		}

		public void PinGraphRanges(ScottPlot.Plot myPlot, BaseViewModel bvm, string who)
		{
			switch (who)
			{
				case "XF":
					{
						var u = myPlot.Axes.Bottom.Min;
						var w = myPlot.Axes.Bottom.Max;
						bvm.GraphStartX = Math.Pow(10, u).ToString("0");
						bvm.GraphEndX = Math.Pow(10, w).ToString("0");
					}
					break;
				case "YP":  // Y percents
					{
						var u = Math.Pow(10, Math.Round(myPlot.Axes.Left.Min, 1));
						var w = Math.Pow(10, Math.Round(myPlot.Axes.Left.Max, 1));
						bvm.RangeTop = (w > 10) ? w.ToString("0") : w.ToString("G2");
						bvm.RangeBottom = (u > 10) ? u.ToString("0") : u.ToString("G2");
					}
					break;
				case "YM":  // Y magnitude
					{
						var u = myPlot.Axes.Left.Min;
						var w = myPlot.Axes.Left.Max;
						bvm.RangeTopdB = w.ToString("0");
						bvm.RangeBottomdB = u.ToString("0");
					}
					break;
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
					fresult.Right = fresult.Right.Zip(fst.Right, (x, y) => x + y * y).ToArray();
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
			bvm.ShowMiniPlots = true; // enable mini plots for the duration of the action
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
			if( !bvm.KeepMiniPlots)
			{
				_ = Task.Delay(1000).ContinueWith(x => bvm.ShowMiniPlots = false); // disable mini plots after a second
			}
			FrequencyHistory.Clear();	// empty averaging history
		}

		protected async Task showMessage(String msg, int delay = 0)
		{
			var vm = ViewSettings.Singleton.MainVm;
			await vm.SetProgressMessage(msg, delay);
		}

		protected async Task showProgress(int progress, int delay = 0)
		{
			var vm = ViewSettings.Singleton.MainVm;
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

		/// <summary>
		/// calculate noise summaries
		/// </summary>
		/// <param name="bvm">the view model</param>
		/// <param name="ct">token</param>
		/// <param name="setRange">set range to 0 then back</param>
		/// <returns>Noise unweighted, A weighted, and C weighted</returns>
		protected async Task<(LeftRightPair,LeftRightPair,LeftRightPair)> MeasureNoise(BaseViewModel bvm, CancellationToken ct, bool setRange = false)
		{
			bvm.GeneratorVoltage = "off"; // no generator voltage during noise measurement
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
				await QaComm.SetInputRange(0); // and a small range for better noise...
			var lrs = await QaComm.DoAcquisitions(1, ct); // now that it's settled...
			if (setRange)
				await QaComm.SetInputRange(range); // restore the range

			LeftRightPair nfgr = QaCompute.CalculateNoise(bvm.WindowingMethod, lrs.FreqRslt,"");
			LeftRightPair nfgrA = QaCompute.CalculateNoise(bvm.WindowingMethod, lrs.FreqRslt, "A");
			LeftRightPair nfgrC = QaCompute.CalculateNoise(bvm.WindowingMethod, lrs.FreqRslt, "C");
			var ux = (nfgr, nfgrA, nfgrC);
			return ux;
		}

		/// <summary>
		/// Determine the gain at a specific frequency. This is done by sending a sine wave at the given frequency
		/// This set the attenuation in hardare to 42 before the test
		/// </summary>
		/// <param name="dfreq"></param>
		/// <param name="inits"></param>
		/// <param name="average"></param>
		/// <returns></returns>
		protected static async Task<LeftRightFrequencySeries?> DetermineGainAtFreq(BaseViewModel bvm, double dfreq, int average = 1)
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
			WaveGenerator.SetEnabled(true);                 // enable generator
			bvm.GeneratorVoltage = MathUtil.FormatVoltage(generatorV); // update the viewmodel so we can show it on-screen
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
			LRGains = await DetermineGainCurve(bvm, true, 1);
			bvm.Attenuation = atten; // restore the original value just in case bvm is also the local model
			// in general this gets reset immediately for the next test
		}

		protected async Task CalculateGainAtFreq(BaseViewModel bvm, double dFreq, int averages = 1)
		{
			// show that we're autoing...
			var atten = bvm.Attenuation;
			bvm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;	// update the GUI button
			await showMessage($"Calculating DUT gain at {dFreq}");
			LRGains = await DetermineGainAtFreq(bvm, dFreq, averages);
			bvm.Attenuation = atten;    // restore the original value and button display
		}

		/// <summary>
		/// Determine the gain curve for the device. This is done by sending a chirp at 96KHz
		/// this sets the attenuation in hardware to 42 before the test
		/// </summary>
		/// <param name="inits"></param>
		/// <param name="average"></param>
		/// <returns></returns>
		protected static async Task<LeftRightFrequencySeries?> DetermineGainCurve(BaseViewModel bvm, bool inits, int average = 1)
		{
			// initialize very quick run, use 192K so valid up to 20KHz
			uint fftsize = 32768;
			uint sampleRate = 192000;
			string swindow = "Hann";		// we need a reasonable windowing no matter user request
			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, swindow, QaLibrary.DEVICE_MAX_ATTENUATION))
				return null;

			{
				// the simplest thing here is to do a chirp at a low value...
				var generatorV = 0.01;			// random low test value
				var chirpy = Chirps.ChirpVp((int)fftsize, sampleRate, generatorV, 6, 24000);
				var ct = new CancellationTokenSource();
				// get the data
				// now do the frequency transformation
				LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries();
				for (int j = 0; j < average; j++)
				{
					LeftRightSeries acqData = await QaComm.DoAcquireUser(1, ct.Token, chirpy, chirpy, false);
					if (acqData == null || acqData.TimeRslt == null || ct.IsCancellationRequested)
						return null;

					// calculate gain for each channel from frequency response
					LeftRightTimeSeries lrts = new LeftRightTimeSeries();
					lrts.Left = acqData.TimeRslt.Left;
					lrts.Right = acqData.TimeRslt.Right;
					lrts.dt = acqData.TimeRslt.dt;
					LeftRightFrequencySeries lraddon = new LeftRightFrequencySeries();
					(lraddon.Left, lraddon.Right) = Chirps.NormalizeChirpDbl(swindow, chirpy, generatorV, (lrts.Left, lrts.Right));
					if(j == 0)
					{
						lrfs.Left = lraddon.Left;
						lrfs.Right = lraddon.Right;
					}
					else
					{
						lrfs.Left = lrfs.Left.Zip(lraddon.Left, (x, y) => x + y).ToArray();
						lrfs.Right = lrfs.Right.Zip(lraddon.Right, (x, y) => x + y).ToArray();
					}
				}
				lrfs.Df = QaLibrary.CalcBinSize(sampleRate, fftsize);
				if (average > 1)
				{
					lrfs.Left = lrfs.Left.Select(x => x / average).ToArray();
					lrfs.Right = lrfs.Right.Select(x => x / average).ToArray();
				}

				// now normalize by the input voltage so we get gain instead
				var gv = generatorV;
				lrfs.Left = lrfs.Left.Select(x => x / gv).ToArray();
				lrfs.Right = lrfs.Right.Select(x => x / gv).ToArray();
				//
				// now hack some stuff in for the gain curve...
				//
				for(int i=0; i<5; i++)
				{
					lrfs.Left[i] = lrfs.Left[5];
					lrfs.Right[i] = lrfs.Right[5];
				}
				// smoothing
				double dl = lrfs.Left[0];
				double dr = lrfs.Right[0];
				for (int i=0; i<lrfs.Left.Length; i++)
				{
					lrfs.Left[i] = dl * .1 + lrfs.Left[i] * 0.9;
					lrfs.Right[i] = dl * .1 + lrfs.Right[i] * 0.9;
				}
				//var newlen = 10000 / lrfs.Df; // 10KHz max frequency
				return lrfs;       // Return the new generator amplitude and acquisition data
			}
		}
	}
}
