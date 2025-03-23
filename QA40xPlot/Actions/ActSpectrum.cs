using FftSharp;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

	public class ActSpectrum : ActBase
    {
        public SpectrumData Data { get; set; }                  // Data used in this form instance

        private readonly Views.PlotControl fftPlot;

        private SpectrumMeasurementResult MeasurementResult;

        private float _Thickness = 2.0f;

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
            MeasurementResult = new(ViewSettings.Singleton.SpectrumVm);
			UpdateGraph(true);
        }

        public void DoCancel()
        {
            ct.Cancel();
		}

		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();
			var vf = this.MeasurementResult.FrequencySteps;
			if (vf == null || vf.Count == 0)
				return null;

			var ffs = vf[0].fftData;
			if (ffs == null)
				return null;

			var vm = ViewSettings.Singleton.SpectrumVm;
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

			var freq = MathUtil.ToDouble(thd.Gen1Frequency, 0);
			var sampleRate = MathUtil.ToUint(thd.SampleRate);
			if (freq == 0 || sampleRate == 0 || !SpectrumViewModel.FftSizes.Contains(thd.FftSize))
            {
                MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = SpectrumViewModel.FftActualSizes.ElementAt(SpectrumViewModel.FftSizes.IndexOf(thd.FftSize));

            // ********************************************************************  
            // Load a settings we want
            // ********************************************************************  
			if (false == await QaLibrary.InitializeDevice(sampleRate, fftsize, thd.WindowingMethod, (int)thd.Attenuation, msr.FrequencySteps.Count == 0))
				return false;

			try
			{
                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;

                // ********************************************************************
                // Calculate frequency steps to do
                // ********************************************************************
                var binSize = QaLibrary.CalcBinSize(sampleRate, fftsize);
				// Generate a list of frequencies
				double[] stepFrequencies = [freq];
                // Translate the generated list to bin center frequencies
                var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize).ToArray();

                if(msr.NoiseFloor == null)
                {
					// ********************************************************************
					// Do noise floor measurement with source off
					// ********************************************************************
					await showMessage($"Determining noise floor.");
					await Qa40x.SetOutputSource(OutputSources.Off);
					await Qa40x.DoAcquisition();    // do a single acquisition for settling
					msr.NoiseFloor = await QaLibrary.DoAcquisitions(thd.Averages, ct);
					if (ct.IsCancellationRequested)

						return false;
				}

				var genVolt = MathUtil.ToDouble(thd.Gen1Voltage, 0.001);
				double amplitudeSetpointdBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

				// ********************************************************************
				// Do a spectral sweep once
				// ********************************************************************
				while(true)
				{
					// now do the step measurement
					await showMessage($"Measuring spectrum.");
					await showProgress(0);

					// Set the generators
					await Qa40x.SetGen1(stepBinFrequencies[0], amplitudeSetpointdBV, thd.UseGenerator);
					// for the first go around, turn on the generator
					if (thd.UseGenerator)
					{
						await Qa40x.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset
					}
					else
					{
						await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make the averages reset
					}

					LeftRightSeries lrfs = await QaLibrary.DoAcquisitions(thd.Averages, ct, true, true);
					//var crp = QAMath.CalculateChirp(20, 20000, 1000, sampleRate);
					//LeftRightSeries lrfs = await QaLibrary.DoAcquireChirp(ct, crp.ToArray(), crp.ToArray());

					//if (ct.IsCancellationRequested)
					//                   break;

					uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[0], binSize);
                    if (fundamentalBin >= (lrfs.FreqRslt?.Left.Length ?? -1))               // Check in bin within range
                        break;

                    ThdFrequencyStep step = new()
                    {
                        FundamentalFrequency = stepBinFrequencies[0],
                        GeneratorVoltage = QaLibrary.ConvertVoltage(amplitudeSetpointdBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
                        fftData = lrfs.FreqRslt,
                        timeData = lrfs.TimeRslt
                    };

					step.Left = ChannelCalculations(binSize, amplitudeSetpointdBV, step, msr, false);
					step.Right = ChannelCalculations(binSize, amplitudeSetpointdBV, step, msr, true);

					// Calculate the THD
					{
						var maxf = 20000;   // the app seems to use 20,000 so not sampleRate/ 2.0;
						var snrdb = await Qa40x.GetSnrDb(stepBinFrequencies[0], 20.0, maxf);
						var thds = await Qa40x.GetThdDb(stepBinFrequencies[0], maxf);
						var thdN = await Qa40x.GetThdnDb(stepBinFrequencies[0], 20.0, maxf);

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

					// Add step data to list
					msr.FrequencySteps.Clear();
					msr.FrequencySteps.Add(step);

					// For now clear measurements to allow only one until we have a UI to manage them.
					if( Data.Measurements.Count == 0)
						Data.Measurements.Add(MeasurementResult);

					ClearPlot();
					UpdateGraph(false);
					if(! thd.IsTracking)
					{
						thd.RaiseMouseTracked("track");
					}
					ViewSettings.Singleton.SpectrumVm.HasExport = true;

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
			var vm = ViewSettings.Singleton.SpectrumVm;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			var sampleRate = MathUtil.ToUint(vm.SampleRate);
			var fftsize = SpectrumViewModel.FftActualSizes.ElementAt(SpectrumViewModel.FftSizes.IndexOf(vm.FftSize));
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
			var vm = ViewSettings.Singleton.SpectrumVm;
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
			var vm = ViewSettings.Singleton.SpectrumVm;
			if (!vm.ShowLeft && !vm.ShowRight)
				return;

            List<double> freqchecks = new List<double> { 50, 60 };
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowPowerMarkers)
			{
				var sampleRate = MathUtil.ToUint(vm.SampleRate);
				var fftsize = SpectrumViewModel.FftActualSizes.ElementAt(SpectrumViewModel.FftSizes.IndexOf(vm.FftSize));
				var steps = vm.ShowLeft ? MeasurementResult.FrequencySteps[0].Left : MeasurementResult.FrequencySteps[0].Right;
				var nfloor = steps.Average_NoiseFloor_dBV;   // Average noise floor in dBVolts after the fundamental
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
		private ThdFrequencyStepChannel ChannelCalculations(double binSize, double generatorAmplitudeDbv, ThdFrequencyStep step, SpectrumMeasurementResult msr, bool isRight)
		{
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(step.FundamentalFrequency, binSize);
			var ffts = isRight ? step.fftData?.Right : step.fftData?.Left;
			var lfdata = step.timeData?.Left;

			// this should never happen
			if (ffts == null || lfdata == null)
				return new();

			double allvolts = Math.Sqrt(lfdata.Select(x => x * x ).Sum() / lfdata.Count());	// use the time data for best accuracy gain math

			ThdFrequencyStepChannel channelData = new()
			{
				Fundamental_V = ffts[fundamentalBin],
				Total_V = allvolts,
				Fundamental_dBV = 20 * Math.Log10(ffts[fundamentalBin]),
				Gain_dB = 20 * Math.Log10(ffts[fundamentalBin] / Math.Pow(10, generatorAmplitudeDbv / 20))

			};
			// Calculate average noise floor
			var noiseFlr = (msr.NoiseFloor == null) ? null : (isRight ? msr.NoiseFloor.FreqRslt?.Left : msr.NoiseFloor.FreqRslt?.Right);
			if (noiseFlr != null)
			{
				channelData.Average_NoiseFloor_V = noiseFlr.Average();   // Average noise floor in Volts after the fundamental
				var v2 = noiseFlr.Select(x => x * x).Sum() / (1.5 * noiseFlr.Length);    // 1.5 for hann window Squared noise floor in Volts after the fundamental
				channelData.TotalNoiseFloor_V = Math.Sqrt(v2);   // Average noise floor in Volts after the fundamental
				channelData.Average_NoiseFloor_dBV = 20 * Math.Log10(channelData.TotalNoiseFloor_V);         // Average noise floor in dBV
			}

			// Reset harmonic distortion variables
			double distortionSqrtTotal = 0;
			double distortionSqrtTotalN = 0;
			double distortionD6plus = 0;

			// Loop through harmonics up tot the 12th
			for (int harmonicNumber = 2; harmonicNumber <= 12; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
			{
				double harmonicFrequency = step.FundamentalFrequency * harmonicNumber;
				uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

				if (bin >= ffts.Length)
					break;                                          // Invalid bin, skip harmonic

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

				//if( harmonicNumber == 2)
				//{
				//	Debug.WriteLine("a Harmonic: funddBV {3} ampdBv {0} thd% {1} thddB {2}", amplitude_dBV, thd_Percent, harmonic.Thd_dB, channelData.Fundamental_dBV);
				//	Debug.WriteLine("b Harmonic: fundv {3} ampv {0} thd% {1} thddB {2}", amplitude_V, thd_Percent, harmonic.Thd_dB, channelData.Fundamental_V);
				//}

				if (harmonicNumber >= 6)
					distortionD6plus += Math.Pow(amplitude_V, 2);

				distortionSqrtTotal += Math.Pow(amplitude_V, 2);
				distortionSqrtTotalN += Math.Pow(amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
			}

			// Calculate THD
			//         if (distortionSqrtTotal != 0)
			//         {
			//             channelData.Thd_Percent = (Math.Sqrt(distortionSqrtTotal) / channelData.Fundamental_V) * 100;
			//             channelData.Thd_PercentN = = (Math.Sqrt(distortionSqrtTotal) / channelData.Fundamental_V) * 100;
			//	channelData.Thd_dB = 20 * Math.Log10(channelData.Thd_Percent / 100.0);
			//	var Thdn_Percent = (Math.Sqrt(distortionSqrtTotalN) / channelData.Fundamental_V) * 100;
			//	channelData.Thd_dBN = 20 * Math.Log10(Thdn_Percent / 100.0);
			//}

			// Calculate D6+ (D6 - D12)
			if (distortionD6plus != 0)
            {
                channelData.D6Plus_dBV = 20 * Math.Log10(Math.Sqrt(distortionD6plus));
                channelData.ThdPercent_D6plus = Math.Sqrt(distortionD6plus / Math.Pow(channelData.Fundamental_V, 2)) * 100;
                channelData.ThdDbD6plus = 20 * Math.Log10(channelData.ThdPercent_D6plus / 100.0);
            }

            // If load not zero then calculate load power
            if (msr.MeasurementSettings.AmpLoad != 0)
                channelData.Power_Watt = Math.Pow(channelData.Fundamental_V, 2) / msr.MeasurementSettings.AmpLoad;

            return channelData;
        }

		// here posn is in dBV
		public ValueTuple<double,double> LookupXY(double freq, double posndBV, bool useRight)
		{
			var steps = MeasurementResult.FrequencySteps;
			if (freq <= 0 || steps == null || steps.Count == 0)
				return ValueTuple.Create(0.0,0.0);

			try
			{
				// get the data to look through
				var fftdata = steps.First().fftData;
				var ffs = useRight ? fftdata?.Right : fftdata?.Left;
				if (fftdata != null && ffs != null && ffs.Length > 0)
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

					var vm = ViewSettings.Singleton.SpectrumVm;
					if ( bin < ffs.Length)
						return ValueTuple.Create(bin*fftdata.Df, ffs[bin]);
				}
			}
			catch (Exception )
			{
			}
			return ValueTuple.Create(0.0,0.0);
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
			InitializePctFreqPlot(myPlot);
			var thdFreq = ViewSettings.Singleton.SpectrumVm;

            SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;
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

			var specVm = ViewSettings.Singleton.SpectrumVm;
			bool leftChannelEnabled = specVm.ShowLeft;	// dynamically update these
			bool rightChannelEnabled = specVm.ShowRight;

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
					if (leftChannelEnabled)
					{
						var lv = fftData.Left[f];       // V of input data
						var lvp = 100 * lv / maxleft;
						dBV_Left_Y.Add(Math.Log10(lvp));
					}
					if (rightChannelEnabled)
					{
						var lv = fftData.Right[f];       // V of input data
						var lvp = 100 * lv / maxright;
						dBV_Right_Y.Add(Math.Log10(lvp));
					}
				}
				else
				{
					if (leftChannelEnabled)
					{
						var lv = fftData.Left[f];       // V of input data
						dBV_Left_Y.Add(20*Math.Log10(lv));
					}
					if (rightChannelEnabled)
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

			var showThick = ViewSettings.Singleton.SpectrumVm.ShowThickLines;	// so it dynamically updates

			if (leftChannelEnabled)
			{
				Scatter plotTot_Left = myPlot.Add.Scatter(logFreqX, logHTot_Left_Y);
				plotTot_Left.LineWidth = showThick ? _Thickness : 1;
				plotTot_Left.Color = new ScottPlot.Color(1, 97, 170, 255);  // Blue
				plotTot_Left.MarkerSize = 1;
			}

			if (rightChannelEnabled)
			{
				Scatter plotTot_Right = myPlot.Add.Scatter(logFreqX, logHTot_Right_Y);
				plotTot_Right.LineWidth = showThick ? _Thickness : 1;
				if (leftChannelEnabled)
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 120); // Red transparant
				else
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 255); // Red
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
            InitializeMagFreqPlot(myPlot);

			var thdFreq = ViewSettings.Singleton.SpectrumVm;

			myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thdFreq.GraphStartFreq)), Math.Log10(MathUtil.ToDouble(thdFreq.GraphEndFreq)), 
				MathUtil.ToDouble(thdFreq.RangeBottomdB), MathUtil.ToDouble(thdFreq.RangeTopdB));

            myPlot.Title("Spectrum");
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("dBV");

            fftPlot.Refresh();
        }

		// this calculates gain using all input voltage because we use it to set the attenuator
		private async Task<double> CalculateInVolts()
		{
			var domore = await PerformMeasurementSteps(MeasurementResult, ct.Token);
			if( domore && MeasurementResult.FrequencySteps?.Count > 0)
			{
				return MeasurementResult.FrequencySteps[0].Left.Total_V;
			}
			return 1.0;
		}

        /// <summary>
        ///  Start measurement button click
        /// </summary>
        public async void StartMeasurement()
        {
			var specVm = ViewSettings.Singleton.SpectrumVm;
			if (!await StartAction(specVm))
				return;

			ct = new();
			// Clear measurement result
			MeasurementResult = new(specVm)
			{
				CreateDate = DateTime.Now,
				Show = true,                                      // Show in graph
			};
			Data.Measurements.Clear();

			if(specVm.DoAutoAttn)
			{
				double inpVolts = 0;
				var msr = MeasurementResult.MeasurementSettings;
				msr.Attenuation = 42;        // set to max attenuation
				specVm.Attenuation = 42;    // to update the gui while testing
				var aves = msr.Averages;
				msr.Averages = 1;
				inpVolts = await CalculateInVolts();
				msr.Averages = aves;
				msr.Attenuation = QaLibrary.DetermineAttenuation(20*Math.Log10(inpVolts));
				specVm.Attenuation = msr.Attenuation;    // to update the gui while testing
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
                    await Task.Delay(250);
                    if (ct.IsCancellationRequested)
                        break;
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
                        ViewSettings.Singleton.SpectrumVm.CopyPropertiesTo(MeasurementResult.MeasurementSettings);
                    }
					rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
					if (ct.IsCancellationRequested || !rslt)
						break;
				}
			}

			// Turn the generator off since we leave it on during testing
			await Qa40x.SetOutputSource(OutputSources.Off);

			specVm.IsRunning = false;
			await showMessage("");
			ViewSettings.Singleton.SpectrumVm.HasExport = this.MeasurementResult.FrequencySteps.Count > 0;
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
			SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;
			var vm = ViewSettings.Singleton.ChannelLeft;
            vm.FundamentalFrequency = 0;
            vm.CalculateChannelValues(MeasurementResult.FrequencySteps[0].Left, MathUtil.ToDouble( thd.Gen1Frequency), thd.ShowDataPercent);
		}

		public void UpdateGraph(bool settingsChanged)
        {
            fftPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			fftPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;

			if (!thd.ShowPercent)
            {
                if (settingsChanged)
                {
                    InitializeMagnitudePlot();
                }

                foreach (var result in Data.Measurements.Where(m => m.Show))
                {
					SpectrumViewModel mvs = result.MeasurementSettings;
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
					SpectrumViewModel mvs = result.MeasurementSettings;
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