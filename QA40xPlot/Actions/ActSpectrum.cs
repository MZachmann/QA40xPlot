using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.BareMetal;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;
using QA40x_BareMetal;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

	public class ActSpectrum : ActBase
    {
        public SpectrumData Data { get; set; }                  // Data used in this form instance

        private readonly Views.PlotControl fftPlot;

        private SpectrumMeasurementResult MeasurementResult;

        private float _Thickness = 2.0f;
		private static SpectrumViewModel MyVModel { get => ViewSettings.Singleton.SpectrumVm; }

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActSpectrum(ref SpectrumData data, Views.PlotControl graphFft)
        {
            Data = data;
            
			fftPlot = graphFft;

			ct = new CancellationTokenSource();

            // TODO: depends on graph settings which graph is shown
            MeasurementResult = new(MyVModel);
			UpdateGraph(true);
        }

        public void DoCancel()
        {
            ct.Cancel();
		}

		/// <summary>
		/// Create a blob for data export
		/// </summary>
		/// <returns></returns>
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();
			var vf = this.MeasurementResult.FrequencySteps;
			if (vf == null || vf.Count == 0)
				return null;

			var ffs = vf[0].fftData;
			if (ffs == null)
				return null;

			var vm = MyVModel;
			var sampleRate = MathUtil.ToUint(vm.SampleRate);
			var fftsize = ffs.Left.Length;
			var binSize = ffs.Df;
			if (vm.ShowRight && !vm.ShowLeft)
			{
				db.LeftData = ffs.Right.ToList();
			}
			else
			{
				db.LeftData = ffs.Left.ToList();
			}
			var frqs = Enumerable.Range(0, fftsize).ToList();
			var frequencies = frqs.Select(x => x * binSize).ToList(); // .Select(x => x * binSize);
			db.FreqData = frequencies;
			return db;
		}

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurementSteps(SpectrumMeasurementResult msr, CancellationToken ct)
        {
			// Setup
			SpectrumViewModel thd = msr.MeasurementSettings;
			var specVm = MyVModel;

			var freq = MathUtil.ToDouble(thd.Gen1Frequency, 0);
			var sampleRate = thd.SampleRateVal;
			if (freq == 0 || sampleRate == 0 || !SpectrumViewModel.FftSizes.Contains(thd.FftSize))
            {
                MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = thd.FftSizeVal;
			var binSize = QaLibrary.CalcBinSize(sampleRate, fftsize);

			// ********************************************************************  
			// Load a settings we want
			// ********************************************************************  
			if (true != QaUsb.InitializeDevice(sampleRate, fftsize, thd.WindowingMethod, (int)thd.Attenuation, msr.FrequencySteps.Count == 0))
				return false;

			try
			{
                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;

                if(msr.NoiseFloor == null)
                {
					msr.NoiseFloor = await MeasureNoise(ct);
				}
				var gains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				var genVolt = specVm.ToGenVoltage(thd.Gen1Voltage, [], GEN_INPUT, gains) ;
				if(genVolt > 5)
				{
					await showMessage($"Requesting input voltage of {genVolt} volts, check connection and settings");
					genVolt = 0.01;
				}

				// ********************************************************************
				// Do a spectral sweep once
				// ********************************************************************
				while(true)
				{
					// now do the step measurement
					await showMessage($"Measuring spectrum with input of {genVolt:G3}V.");
					await showProgress(0);

					// Set the generator
					QaUsb.SetGen1(freq, genVolt, thd.UseGenerator);
					// for the first go around, turn on the generator
					LeftRightSeries? lrfs;
					if (thd.UseGenerator)
					{
						QaUsb.SetOutputSource(OutputSources.Sine);	// this enables waveforms
						// Set the generators via a usermode
						var gw1 = new GenWaveform()
						{
							Frequency = freq,
							Voltage = genVolt,
							Name = thd.Gen1Waveform
						};
						var gws = new GenWaveSample()
						{
							SampleRate = (int)sampleRate,
							SampleSize = (int)fftsize
						};
						var wave = QAMath.CalculateWaveform([gw1], gws);
						lrfs = await QaUsb.DoAcquireUser(thd.Averages, ct, wave.ToArray(), wave.ToArray(), false);
						if(gw1.Name != "Chirp")
						{
							QaUsb.CalculateFreq(lrfs);  // do the fft and calculate the frequency response
						}
						else
						{
							QaUsb.CalculateChirpFreq(lrfs, wave.ToArray(), gw1, gws);	// normalize the result for flat response
						}
					}
					else
					{
						QaUsb.SetOutputSource(OutputSources.Off);            // We need to call this to make the averages reset
						lrfs = await QaUsb.DoAcquisitions(thd.Averages, ct);
					}
					if (lrfs == null)
						break;

					uint fundamentalBin = QaLibrary.GetBinOfFrequency(freq, binSize);
                    if (fundamentalBin >= (lrfs.FreqRslt?.Left.Length ?? -1))               // Check in bin within range
                        break;

                    ThdFrequencyStep step = new()
                    {
                        FundamentalFrequency = freq,
                        GeneratorVoltage = genVolt,
                        fftData = lrfs.FreqRslt,
                        timeData = lrfs.TimeRslt
                    };

					step.Left = ChannelCalculations(binSize, genVolt, step, msr, false);
					step.Right = ChannelCalculations(binSize, genVolt, step, msr, true);

					// Calculate the THD
					{	var maxf = 20000; // the app seems to use 20,000 so not sampleRate/ 2.0;
						LeftRightPair snrdb = QaCompute.GetSnrDb(lrfs, freq, 20.0, maxf);
						LeftRightPair thds = QaCompute.GetThdDb(lrfs, freq, 20.0, maxf);
						LeftRightPair thdN = QaCompute.GetThdnDb(lrfs, freq, 20.0, maxf);

						step.Left.Thd_dBN = thdN.Left;
						step.Right.Thd_dBN = thdN.Right;
						step.Left.Thd_dB = thds.Left;
						step.Right.Thd_dB = thds.Right;
						step.Left.Snr_dB = snrdb.Left;
						step.Right.Snr_dB = snrdb.Right;
                        step.Left.Thd_Percent = 100*Math.Pow(10, thds.Left / 20);
						step.Right.Thd_Percent = 100 * Math.Pow(10, thds.Right / 20);
                        step.Left.Thd_PercentN = 100 * Math.Pow(10, thdN.Left / 20);
						step.Right.Thd_PercentN = 100 * Math.Pow(10, thdN.Right / 20);
					}

					// Here we replace the last frequency step with the new one
					msr.FrequencySteps.Clear();
					msr.FrequencySteps.Add(step);

					// For now clear measurements to allow only one until we have a UI to manage them.
					if ( Data.Measurements.Count == 0)
						Data.Measurements.Add(MeasurementResult);

					ClearPlot();
					UpdateGraph(false);
					if(! thd.IsTracking)
					{
						thd.RaiseMouseTracked("track");
					}
					MyVModel.HasExport = true;

					// we always run this exactly once
                    break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Show message
			await showMessage($"Measurement finished");

            return !ct.IsCancellationRequested;
        }

        private void AddAMarker(SpectrumMeasurementResult fmr, double frequency, bool isred = false)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			var sampleRate = fmr.MeasurementSettings.SampleRateVal;
			var fftsize = fmr.MeasurementSettings.FftSizeVal;
			int bin = (int)QaLibrary.GetBinOfFrequency(frequency, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
			var leftData = fmr.FrequencySteps[0].fftData?.Left;
			var rightData = fmr.FrequencySteps[0].fftData?.Right;

			double markVal = 0;
			if( vm.ShowPercent)
			{
				if (rightData != null && !vm.ShowLeft)
				{
					double maxright = rightData.Max();
					markVal = 100 * rightData[bin] / maxright;
				}
				else if(leftData != null)
				{
					double maxleft = leftData.Max();
					markVal = 100 * leftData[bin] / maxleft;
				}
			}
			else
			{
				if (rightData != null && !vm.ShowLeft)
				{
					markVal = 20 * Math.Log10(rightData[bin]);
				}
				else if(leftData != null )
				{
					markVal = 20 * Math.Log10(leftData[bin]);
				}
			}
			ScottPlot.Color markerCol = new ScottPlot.Color();
            if( ! vm.ShowLeft)
            {
                markerCol = isred ? Colors.Green : Colors.DarkGreen;
			}
            else
            {
				markerCol = isred ? Colors.Red : Colors.DarkOrange;
			}
			if (vm.ShowPercent)
			{
				var mymark = myPlot.Add.Marker(Math.Log10(frequency), Math.Log10(markVal),
					MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
				mymark.LegendText = string.Format("{1}: {0:F6}", markVal, (int)frequency);
			}
			else
			{
				var mymark = myPlot.Add.Marker(Math.Log10(frequency), markVal,
					MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
				mymark.LegendText = string.Format("{1}: {0:F1}", markVal, (int)frequency);
			}
		}

		private void ShowHarmonicMarkers(SpectrumMeasurementResult fmr)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowMarkers)
			{
				ThdFrequencyStepChannel? step = null;
				if (vm.ShowLeft)
					step = fmr.FrequencySteps[0].Left;
				else if (vm.ShowRight)
					step = fmr.FrequencySteps[0].Right;
				if (step != null)
				{
					AddAMarker(fmr, fmr.FrequencySteps[0].FundamentalFrequency);
					var flist = step.Harmonics.OrderBy(x => x.Frequency).ToArray();
					var cn = flist.Length;
					for (int i = 0; i < cn; i++)
					{
						var frq = flist[i].Frequency;
						AddAMarker(fmr, frq);
					}
				}
			}
		}

		private void ShowPowerMarkers(SpectrumMeasurementResult fmr)
		{
			var vm = MyVModel;
			if (!vm.ShowLeft && !vm.ShowRight)
				return;

            List<double> freqchecks = new List<double> { 50, 60 };
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowPowerMarkers)
			{
				var sampleRate = fmr.MeasurementSettings.SampleRateVal;
				var fftsize = fmr.MeasurementSettings.FftSizeVal;
				var steps = vm.ShowLeft ? MeasurementResult.FrequencySteps[0].Left : MeasurementResult.FrequencySteps[0].Right;
                double fsel = 0;
                double maxdata = -10;
				var fftdata = vm.ShowLeft ? fmr.FrequencySteps[0].fftData?.Left : fmr.FrequencySteps[0].fftData?.Right;
				if (fftdata == null)
					return;
				// find if 50 or 60hz is higher, indicating power line frequency
				foreach (double freq in freqchecks)
				{
					var actfreq = QaLibrary.GetNearestBinFrequency(freq, sampleRate, fftsize);
					int bin = (int)QaLibrary.GetBinOfFrequency(actfreq, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
					var data = fftdata[bin];
                    if(data > maxdata)
                    {
                        fsel = freq;
                    }
				}
                // check 4 harmonics of power frequency
                for(int i=1; i<4; i++)
                {
                    var actfreq = QaLibrary.GetNearestBinFrequency(fsel * i, sampleRate, fftsize);
					int bin = (int)QaLibrary.GetBinOfFrequency(actfreq, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
                    var data = fftdata[bin];
                    double udif = 20 * Math.Log10(data);
                    AddAMarker(fmr, actfreq, true);
				}
			}
		}

		/// <summary>
		/// Perform the calculations of a single channel (left or right)
		/// </summary>
		/// <param name="binSize"></param>
		/// <param name="fundamentalFrequency"></param>
		/// <param name="generatorAmplitudeDbv"></param>
		/// <param name="fftData"></param>
		/// <param name="noiseFloorFftData"></param>
		/// <returns></returns>
		private ThdFrequencyStepChannel ChannelCalculations(double binSize, double generatorV, ThdFrequencyStep step, SpectrumMeasurementResult msr, bool isRight)
		{
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(step.FundamentalFrequency, binSize);
			var ffts = isRight ? step.fftData?.Right : step.fftData?.Left;
			var ltdata = step.timeData?.Left;

			// this should never happen
			if (ffts == null || ltdata == null)
				return new();

			double allvolts = Math.Sqrt(ltdata.Select(x => x * x ).Sum() / ltdata.Count()); // use the time data for best accuracy gain math
			//var windowBw = 1.5;	// hann
			//double allv2 = Math.Sqrt(ffts.Select(x => x * x / windowBw).Sum());

			ThdFrequencyStepChannel channelData = new()
			{
				Fundamental_V = ffts[fundamentalBin],
				Total_V = allvolts,
				Total_W = allvolts * allvolts / ViewSettings.AmplifierLoad,
				Fundamental_dBV = 20 * Math.Log10(ffts[fundamentalBin]),
				Gain_dB = 20 * Math.Log10(ffts[fundamentalBin] / generatorV)

			};
			// Calculate average noise floor
			var noiseFlr = (msr.NoiseFloor == null) ? null : (isRight ? msr.NoiseFloor.FreqRslt?.Left : msr.NoiseFloor.FreqRslt?.Right);
			channelData.TotalNoiseFloor_V = QaCompute.CalculateNoise(msr?.NoiseFloor?.FreqRslt, !isRight);

			// Reset harmonic distortion variables
			double distortionSqrtTotal = 0;
			double distortionSqrtTotalN = 0;
			double distortionD6plus = 0;

			// Loop through harmonics up tot the 10th
			for (int harmonicNumber = 2; harmonicNumber <= 10; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
			{
				double harmonicFrequency = step.FundamentalFrequency * harmonicNumber;
				uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

				if (bin >= ffts.Length)
					bin = (uint)Math.Max(0, ffts.Length - 1);             // Invalid bin, skip harmonic

				double amplitude_V = ffts[bin];
				double noise_V = channelData.TotalNoiseFloor_V;

				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
				double thd_Percent = (amplitude_V / channelData.Fundamental_V) * 100;
				double thdN_Percent = (noiseFlr == null) ? 0 : ((amplitude_V - noiseFlr[bin]) / channelData.Fundamental_V) * 100;

				HarmonicData harmonic = new()
				{
					HarmonicNr = harmonicNumber,
					Frequency = harmonicFrequency,
					Amplitude_V = amplitude_V,
					Amplitude_dBV = amplitude_dBV,
					Thd_Percent = thd_Percent,
					Thd_dB = 20 * Math.Log10(thd_Percent / 100.0),
					Thd_dBN = 20 * Math.Log10(thdN_Percent / 100.0),
					NoiseAmplitude_V = (noiseFlr == null) ? 1e-3 : noiseFlr[bin]
				};

				if (harmonicNumber >= 6)
					distortionD6plus += Math.Pow(amplitude_V, 2);

				distortionSqrtTotal += Math.Pow(amplitude_V, 2);
				distortionSqrtTotalN += Math.Pow(amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
			}

			// Calculate D6+ (D6 - D12)
			if (distortionD6plus != 0)
            {
                channelData.D6Plus_dBV = 20 * Math.Log10(Math.Sqrt(distortionD6plus));
                channelData.ThdPercent_D6plus = Math.Sqrt(distortionD6plus / Math.Pow(channelData.Fundamental_V, 2)) * 100;
                channelData.ThdDbD6plus = 20 * Math.Log10(channelData.ThdPercent_D6plus / 100.0);
            }

            // If load not zero then calculate load power
            if (ViewSettings.AmplifierLoad != 0)
                channelData.Power_Watt = Math.Pow(channelData.Fundamental_V, 2) / ViewSettings.AmplifierLoad;

            return channelData;
        }

		public Rect GetDataBounds()
		{
			var msr = MeasurementResult.MeasurementSettings;	// measurement settings
			if(msr == null || MeasurementResult.FrequencySteps.Count == 0)
				return Rect.Empty;
			var vmr = MeasurementResult.FrequencySteps.First();	// test data
			if(vmr == null || vmr.fftData == null)
				return Rect.Empty;
			var specVm = MyVModel;     // current settings

			Rect rrc = new Rect(0, 0, 0, 0);
			rrc.X = 20;
			double maxY = 0;
			if(specVm.ShowLeft)
			{
				rrc.Y = vmr.fftData.Left.Min();
				maxY = vmr.fftData.Left.Max();
				if (specVm.ShowRight)
				{
					rrc.Y = Math.Min(rrc.Y, vmr.fftData.Right.Min());
					maxY = Math.Max(maxY, vmr.fftData.Right.Max());
				}
			}
			else if (specVm.ShowRight)
			{
				rrc.Y = vmr.fftData.Right.Min();
				maxY = vmr.fftData.Right.Max();
			}

			rrc.Width = vmr.fftData.Left.Length * vmr.fftData.Df - rrc.X;       // max frequency
			rrc.Height = maxY - rrc.Y;      // max voltage absolute

			return rrc;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="freq">frequency on chart</param>
		/// <param name="posndBV">Y of mouse in plot</param>
		/// <param name="useRight">which channel</param>
		/// <returns>a tuple of df, value, value in pct</returns>
		public ValueTuple<double,double,double> LookupXY(double freq, double posndBV, bool useRight)
		{
			var steps = MeasurementResult.FrequencySteps;
			if (freq <= 0 || steps == null || steps.Count == 0)
				return ValueTuple.Create(0.0,0.0,0.0);
			var step = steps.First();
			try
			{
				// get the data to look through
				var fftdata = step.fftData;
				var ffs = useRight ? fftdata?.Right : fftdata?.Left;
				if (fftdata != null && ffs != null && ffs.Length > 0 && freq < fftdata.Df * ffs.Length)
				{
					int bin = 0;
					ScottPlot.Plot myPlot = fftPlot.ThePlot;
					var pixel = myPlot.GetPixel(new Coordinates(Math.Log10(freq), posndBV));

					// get screen coords for some of the data
					int abin = (int)(freq / fftdata.Df);       // apporoximate bin
					var binmin = Math.Max(1, abin - 200);            // random....
					var binmax = Math.Min(ffs.Length - 1, abin + 200);           // random....
					var distsx = ffs.Skip(binmin).Take(binmax - binmin).Select((fftd, index) => myPlot.GetPixel(new Coordinates(Math.Log10((index+binmin) * fftdata.Df), 20 * Math.Log10(ffs[binmin+index]))));
					var distx = distsx.Select(x => Math.Pow(x.X - pixel.X, 2) + Math.Pow(x.Y - pixel.Y, 2));
					var dlist = distx.ToList(); // no dc
					bin = binmin + dlist.IndexOf(dlist.Min());

					var vm = MyVModel;
					if ( bin < ffs.Length)
					{
						var vfun = useRight ? step.Right.Fundamental_V : step.Left.Fundamental_V;
						return ValueTuple.Create(bin * fftdata.Df, ffs[bin], 100 * ffs[bin] / vfun);
					}
				}
			}
			catch (Exception )
			{
			}
			return ValueTuple.Create(0.0,0.0,0.0);
		}

        /// <summary>
        /// Clear the plot
        /// </summary>
        void ClearPlot()
        {
            fftPlot.ThePlot.Clear();
            fftPlot.Refresh();
        }

        /// <summary>
        /// Ititialize the THD % plot
        /// </summary>
        void InitializefftPlot()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			PlotUtil.InitializePctFreqPlot(myPlot);
			var thdFreq = MyVModel;

            SpectrumViewModel thd = MyVModel;
            myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thd.GraphStartFreq)), Math.Log10(MathUtil.ToDouble(thd.GraphEndFreq)), Math.Log10(MathUtil.ToDouble(thd.RangeBottom)) - 0.00000001, Math.Log10(MathUtil.ToDouble(thd.RangeTop)));  // - 0.000001 to force showing label
            myPlot.Title("Spectrum");
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("%");

			fftPlot.Refresh();
        }

        /// <summary>
        /// Plot the THD % graph
        /// </summary>
        /// <param name="data"></param>
        void PlotValues(SpectrumMeasurementResult measurementResult, int measurementNr)
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			myPlot.Clear();

			var specVm = MyVModel;
			bool useLeft = specVm.ShowLeft;	// dynamically update these
			bool useRight = specVm.ShowRight;

			var fftData = MeasurementResult.FrequencySteps[0].fftData;
			if (fftData == null)
				return;

			List<double> freqX = [];
			List<double> dBV_Left_Y = [];
			List<double> dBV_Right_Y = [];
			double frequency = 0;
			double maxleft = fftData.Left.Max();
			double maxright = fftData.Right.Max();

			for (int f = 1; f < fftData.Left.Length; f++)   // Skip dc bin
			{
				frequency += fftData.Df;
				freqX.Add(frequency);
				if(specVm.ShowPercent)
				{
					if (useLeft)
					{
						var lv = fftData.Left[f];       // V of input data
						var lvp = 100 * lv / maxleft;
						dBV_Left_Y.Add(Math.Log10(lvp));
					}
					if (useRight)
					{
						var lv = fftData.Right[f];       // V of input data
						var lvp = 100 * lv / maxright;
						dBV_Right_Y.Add(Math.Log10(lvp));
					}
				}
				else
				{
					if (useLeft)
					{
						var lv = fftData.Left[f];       // V of input data
						dBV_Left_Y.Add(20*Math.Log10(lv));
					}
					if (useRight)
					{
						var lv = fftData.Right[f];       // V of input data
						dBV_Right_Y.Add(20*Math.Log10(lv));
					}
				}
			}

			// add a scatter plot to the plot
			double[] logFreqX = freqX.Select(Math.Log10).ToArray();
			double[] logHTot_Left_Y = dBV_Left_Y.ToArray();
			double[] logHTot_Right_Y = dBV_Right_Y.ToArray();

			var lineWidth = MyVModel.ShowThickLines ? _Thickness : 1;	// so it dynamically updates

			if (useLeft)
			{
				Scatter plotTot_Left = myPlot.Add.Scatter(logFreqX, logHTot_Left_Y);
				plotTot_Left.LineWidth = lineWidth;
				plotTot_Left.Color = QaLibrary.BlueColor;  // Blue
				plotTot_Left.MarkerSize = 1;
			}

			if (useRight)
			{
				Scatter plotTot_Right = myPlot.Add.Scatter(logFreqX, logHTot_Right_Y);
				plotTot_Right.LineWidth = lineWidth;
				if (useLeft)
					plotTot_Right.Color = QaLibrary.RedXColor; // Red transparant
				else
					plotTot_Right.Color = QaLibrary.RedColor; // Red
				plotTot_Right.MarkerSize = 1;
			}

			fftPlot.Refresh();
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
            PlotUtil.InitializeMagFreqPlot(myPlot);

			var thdFreq = MyVModel;

			myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thdFreq.GraphStartFreq)), Math.Log10(MathUtil.ToDouble(thdFreq.GraphEndFreq)), 
				MathUtil.ToDouble(thdFreq.RangeBottomdB), MathUtil.ToDouble(thdFreq.RangeTopdB));

            myPlot.Title("Spectrum");
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("dBV");

            fftPlot.Refresh();
        }

        /// <summary>
        ///  Start measurement button click
        /// </summary>
        public async void StartMeasurement()
        {
			//await TestUsbLib.Test();
			var specVm = MyVModel;
			if (!StartAction(specVm))
				return;

			ct = new();
			// Clear measurement result
			MeasurementResult = new(specVm)
			{
				CreateDate = DateTime.Now,
				Show = true,                                      // Show in graph
			};
			Data.Measurements.Clear();

			var genType = ToDirection(specVm.GenDirection);
			var freq = MathUtil.ToDouble(specVm.Gen1Frequency, 1000);
			var binSize = QaLibrary.CalcBinSize(specVm.SampleRateVal, specVm.FftSizeVal);
			// if we're doing adjusting here
			if (specVm.DoAutoAttn || genType != E_GeneratorDirection.INPUT_VOLTAGE)
			{
				// show that we're autoing...
				if (specVm.DoAutoAttn)
					specVm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;
				LRGains = await DetermineGainAtFreq(freq, true, 1);
			}

			if (specVm.DoAutoAttn && LRGains != null)
			{
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
				var vinL = specVm.ToGenVoltage(specVm.Gen1Voltage, [], GEN_INPUT, gains);	// get primary input voltage
				double voutL = ToGenOutVolts(vinL, [], LRGains.Left);	// what is that as output voltage?
				double voutR = ToGenOutVolts(vinL, [], LRGains.Right);	// for both channels
				var vdbv = QaLibrary.ConvertVoltage( Math.Max(voutL, voutR), E_VoltageUnit.Volt, E_VoltageUnit.dBV );
				specVm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);				// find attenuation for both
				MeasurementResult.MeasurementSettings.Attenuation = specVm.Attenuation;	// update the specVm to update the gui, then this for the steps
			}

			bool rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
            var fftsize = specVm.FftSize;
            var sampleRate = specVm.SampleRate;
            var atten = specVm.Attenuation;
            if (rslt)
            {
                await showMessage("Running");
                while (!ct.IsCancellationRequested)
                {
					var msrSet = MeasurementResult.MeasurementSettings;
                    await Task.Delay(250);
                    if (ct.IsCancellationRequested)
                        break;
					if( specVm.Gen1Frequency != msrSet.Gen1Frequency || msrSet.GenDirection != specVm.GenDirection)
					{
						msrSet.Gen1Frequency = specVm.Gen1Frequency;
						msrSet.GenDirection = specVm.GenDirection;
						var genoType = ToDirection(msrSet.GenDirection);
						if (LRGains != null && genoType == E_GeneratorDirection.OUTPUT_VOLTAGE)
							LRGains = await DetermineGainAtFreq(MathUtil.ToDouble(msrSet.Gen1Frequency, 1000), false, 1);
					}
                    if (specVm.FftSize != fftsize || specVm.SampleRate != sampleRate || specVm.Attenuation != atten)
                    {
						fftsize = specVm.FftSize;
						sampleRate = specVm.SampleRate;
						atten = specVm.Attenuation;
						MeasurementResult = new(specVm)
                        {
                            CreateDate = DateTime.Now,
                            Show = true,                                      // Show in graph
                        };
                    }
                    else
                    {
                        MyVModel.CopyPropertiesTo(MeasurementResult.MeasurementSettings);
                    }
					rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
					if (ct.IsCancellationRequested || !rslt)
						break;
				}
			}

			specVm.IsRunning = false;
			await showMessage("");
			MyVModel.HasExport = this.MeasurementResult.FrequencySteps.Count > 0;
			EndAction();
		}


        /// <summary>
        /// Validate the generator voltage and show red text if invalid
        /// </summary>
        /// <param name="sender"></param>
        //private void ValidateGeneratorAmplitude(object sender)
        //{
        //    if (cmbGeneratorVoltageUnit.SelectedIndex == (int)E_VoltageUnit.MilliVolt)
        //        QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_MV, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_MV);        // mV
        //    else if (cmbGeneratorVoltageUnit.SelectedIndex == (int)E_VoltageUnit.Volt)
        //        QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_V, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_V);     // V
        //    else
        //        QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_DBV, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_DBV);       // dBV
        //}
        
        // show the latest step values in the table
        public void DrawChannelInfoTable()
        {
			SpectrumViewModel thd = MyVModel;
			var vm = ViewSettings.Singleton.ChannelLeft;
            vm.FundamentalFrequency = 0;
            vm.CalculateChannelValues(MeasurementResult.FrequencySteps[0].Left, MathUtil.ToDouble( thd.Gen1Frequency), thd.ShowDataPercent);
			vm = ViewSettings.Singleton.ChannelRight;
			vm.FundamentalFrequency = 0;
			vm.CalculateChannelValues(MeasurementResult.FrequencySteps[0].Right, MathUtil.ToDouble(thd.Gen1Frequency), thd.ShowDataPercent);
		}

		public void UpdateGraph(bool settingsChanged)
        {
            fftPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			fftPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			SpectrumViewModel thd = MyVModel;

			if (!thd.ShowPercent)
            {
                if (settingsChanged)
                {
                    InitializeMagnitudePlot();
                }

                foreach (var result in Data.Measurements.Where(m => m.Show))
                {
					PlotValues(result, resultNr++);
                }
            }
            else
            {
                if (settingsChanged)
                {
                    InitializefftPlot();
                }
          
                foreach (var result in Data.Measurements.Where(m => m.Show))
                {
					PlotValues(result, resultNr++);
				}
			}

            if( MeasurementResult.FrequencySteps.Count > 0)
            {
				ShowHarmonicMarkers(MeasurementResult);
				ShowPowerMarkers(MeasurementResult);
				DrawChannelInfoTable();
			}

			fftPlot.Refresh();
		}
	}
}