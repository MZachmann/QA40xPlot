using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Windows;
using System.Xml.Linq;

namespace QA40xPlot.Actions
{
	public class ActBase
	{
		// this is the initial gain calculation so that we can get attenuation and input voltage settings
		private LeftRightFrequencySeries? _LRGains = null;
		public LeftRightFrequencySeries? LRGains
		{
			get => _LRGains;
			set => _LRGains = value;
		}

		/// <summary>
		/// Start an action by checking for device connected
		/// If all ok set IsRunning bool and return true
		/// </summary>
		/// <param name="bvm">the local video model</param>
		/// <returns>true if all good</returns>
		public static async Task<bool> StartAction(BaseViewModel bvm)
		{
			if (bvm.IsRunning)
			{
				MessageBox.Show("Device is already running");
				return false;
			}
			bvm.IsRunning = true;

			if (await QaLibrary.CheckDeviceConnected() == false)
			{
				bvm.IsRunning = false;
				return false;
			}
			return true;
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

		protected async Task<LeftRightFrequencySeries?> DetermineGainAtFreq(double dfreq, bool inits, int average = 3)
		{
			await showMessage("Calculating DUT gain");
			// initialize very quick run
			var fftsize = BaseViewModel.FftActualSizes[0];
			var sampleRate = MathUtil.ToUint(BaseViewModel.SampleRates[0]);
			if (true != await QaLibrary.InitializeDevice(sampleRate, fftsize, "Hann", QaLibrary.DEVICE_MAX_ATTENUATION, inits))
				return null;

			// the simplest thing here is to do a chirp at a low value...
			var generatorV = 0.01;          // random low test value
			var generatordBV = 20 * Math.Log10(generatorV); // or -40
			await Qa40x.SetGen1(dfreq, generatordBV, true);             // send a sine wave
			await Qa40x.SetOutputSource(OutputSources.Sine);            // since we're single frequency
			var ct = new CancellationTokenSource();
			// do two and average them
			LeftRightSeries acqData = await QaLibrary.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
			if (acqData == null || acqData.FreqRslt == null || acqData.TimeRslt == null || ct.IsCancellationRequested)
				return null;

			uint fundamentalBin = QaLibrary.GetBinOfFrequency(dfreq, acqData.FreqRslt.Df);
			int binmin = (int)Math.Max(0, fundamentalBin - 2);
			int bintrack = (int)(Math.Min(fftsize, fundamentalBin + 2) - binmin);

			// the amplitude is max of a small area
			var maxl = GetFGain(acqData.FreqRslt.Left, generatorV, binmin, bintrack);
			var maxr = GetFGain(acqData.FreqRslt.Right, generatorV, binmin, bintrack);

			var maxi = Math.Max(maxl, maxr);
			// since we're running with 42db of attenuation...
			if (maxi < 1)
			{
				// get some more accuracy with this
				await Qa40x.SetInputRange(18);
				// do two and average them
				acqData = await QaLibrary.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
				if (acqData == null || acqData.FreqRslt == null || ct.IsCancellationRequested)
					return null;
			}

			// calculate gain for each channel from frequency response
			LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries();
			lrfs.Left = new double[] { GetFGain(acqData.FreqRslt.Left, generatorV, binmin, bintrack) };
			lrfs.Right = new double[] { GetFGain(acqData.FreqRslt.Right, generatorV, binmin, bintrack) };
			lrfs.Df = acqData.FreqRslt.Df;
				
			// if we're asking for averaging
			for (int j = 1; j < average; j++)
			{
				acqData = await QaLibrary.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
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
			return lrfs;       // Return the new generator amplitude and acquisition data
		}

		protected async Task<LeftRightFrequencySeries?> DetermineGainCurve(bool inits, int average = 3)
		{
			await showMessage("Calculating DUT gain");
			// initialize very quick run
			var fftsize = BaseViewModel.FftActualSizes[0];
			var sampleRate = MathUtil.ToUint(BaseViewModel.SampleRates[0]);
			if (true != await QaLibrary.InitializeDevice(sampleRate, fftsize, "Hann", QaLibrary.DEVICE_MAX_ATTENUATION, inits))
				return null;

			{
				// the simplest thing here is to do a chirp at a low value...
				var generatorV = 0.01;			// random low test value
				var generatordBV = 20*Math.Log10(generatorV);	// or -40
				await Qa40x.SetExpoChirpGen(generatordBV, 0, 0, false);				// don't use right as reference on input
				await Qa40x.SetOutputSource(OutputSources.ExpoChirp);                   // Set sine wave
				var ct = new CancellationTokenSource();
				// do two and average them
				LeftRightSeries acqData = await QaLibrary.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
				if (acqData == null || acqData.FreqRslt == null || acqData.TimeRslt == null || ct.IsCancellationRequested)
					return null;

				// what's the maximum input here?
				var maxl = acqData.TimeRslt.Left.Skip(10).Max();
				var maxr = acqData.TimeRslt.Right.Skip(10).Max();

				var maxi = Math.Max(maxl, maxr);
				// since we're running with 42db of attenuation...
				if( maxi < 1)
				{
					// get some more accuracy with this
					await Qa40x.SetInputRange(QaLibrary.DEVICE_MAX_ATTENUATION - 24);
					// do two and average them
					acqData = await QaLibrary.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
					if (acqData == null || acqData.FreqRslt == null || ct.IsCancellationRequested)
						return null;
				}

				// calculate gain for each channel from frequency response
				LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries();
				lrfs.Left = acqData.FreqRslt.Left.Select(x => x / generatorV).ToArray();
				lrfs.Right = acqData.FreqRslt.Right.Select(x => x / generatorV).ToArray();
				lrfs.Df = acqData.FreqRslt.Df;
				//
				for(int j=1; j<average; j++)
				{
					acqData = await QaLibrary.DoAcquisitions(1, ct.Token);        // Do a single aqcuisition
					if (acqData == null || acqData.FreqRslt == null || ct.IsCancellationRequested)
						return null;

					// calculate gain for each channel from frequency response
					lrfs.Left = acqData.FreqRslt.Left.Select((x, index) => ((lrfs.Left[index] + x / generatorV))).ToArray();
					lrfs.Right = acqData.FreqRslt.Right.Select((x, index) => ((lrfs.Right[index] + x / generatorV))).ToArray();
				}
				if (average > 1)
				{
					lrfs.Left = lrfs.Left.Select(x => x/average).ToArray();
					lrfs.Right = lrfs.Right.Select(x => x / average).ToArray();
				}

				return lrfs;       // Return the new generator amplitude and acquisition data
			}
		}
	}
}
