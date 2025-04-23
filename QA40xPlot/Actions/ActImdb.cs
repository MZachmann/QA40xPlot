using QA40x_BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

	public class ActImd : ActBase
    {
        public ImdData Data { get; set; }                  // Data used in this form instance

        private readonly Views.PlotControl fftPlot;

        private ImdMeasurementResult MeasurementResult;

        private float _Thickness = 2.0f;
		private static ImdViewModel MyVModel { get => ViewSettings.Singleton.ImdVm; }

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActImd(ref ImdData data, Views.PlotControl graphFft)
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

		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();

			var vf = this.MeasurementResult.FrequencySteps;
			if( vf == null || vf.Count == 0)
			{
				return null;
			}
			var vfs = vf[0].fftData;
			if (vfs == null)
				return null;

			var vm = MyVModel;
			var sampleRate = MathUtil.ToUint(vm.SampleRate);
			var fftsize = vfs.Left.Length;
			var binSize = QaLibrary.CalcBinSize(sampleRate, (uint)fftsize);
			if (vf != null && vf.Count > 0)
			{
				if(vm.ShowRight && ! vm.ShowLeft)
				{
					db.LeftData = vfs.Right.ToList();
				}
				else
				{
					db.LeftData = vfs.Left.ToList();
				}
				// db.RightData = vf[0].fftData.Right.ToList();
				var frqs = Enumerable.Range(0, fftsize).ToList();
				var frequencies = frqs.Select(x => x * binSize).ToList(); // .Select(x => x * binSize);
				db.FreqData = frequencies;
			}
			return db;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="freq">frequency on chart</param>
		/// <param name="posndBV">Y of mouse in plot</param>
		/// <param name="useRight">which channel</param>
		/// <returns>a tuple of df, value, value in pct</returns>
		public ValueTuple<double, double, double> LookupXY(double freq, double posndBV, bool useRight)
		{
			var steps = MeasurementResult.FrequencySteps;
			if (freq <= 0 || steps == null || steps.Count == 0)
				return ValueTuple.Create(0.0, 0.0, 0.0);
			var step = steps.First();
			try
			{
				// get the data to look through
				var fftdata = step.fftData;
				var ffs = useRight ? fftdata?.Right : fftdata?.Left;
				if (fftdata != null && ffs != null && ffs.Length > 0 && freq < ffs.Length * fftdata.Df)
				{
					int bin = 0;

					// prefer distance to value vs freq hence the 200x also log(freq) is 0....6 while mag goes to 200
					ScottPlot.Plot myPlot = fftPlot.ThePlot;
					var pixel = myPlot.GetPixel(new Coordinates(Math.Log10(freq), posndBV));

					// get screen coords for some of the data
					int abin = (int)(freq / fftdata.Df);       // apporoximate bin
					var binmin = Math.Max(1, abin - 200);            // random....
					var binmax = Math.Min(ffs.Length - 1, abin + 200);           // random....
					var distsx = ffs.Skip(binmin).Take(binmax - binmin).Select((fftd, index) => myPlot.GetPixel(new Coordinates(Math.Log10((index + binmin) * fftdata.Df), 20 * Math.Log10(ffs[binmin + index]))));
					var distx = distsx.Select(x => Math.Pow(x.X - pixel.X, 2) + Math.Pow(x.Y - pixel.Y, 2));
					var dlist = distx.ToList(); // no dc
					bin = binmin + dlist.IndexOf(dlist.Min());

					var vm = MyVModel;
					if (bin < ffs.Length)
					{
						var vfun = useRight ? step.Right.Fundamental1_V : step.Left.Fundamental1_V;
						return ValueTuple.Create(bin * fftdata.Df, ffs[bin], 100 * ffs[bin] / vfun);
					}
				}
			}
			catch (Exception)
			{
			}
			return ValueTuple.Create(0.0, 0.0, 0.0);
		}



		public Rect GetDataBounds()
		{
			var msr = MeasurementResult.MeasurementSettings;    // measurement settings
			if (msr == null || MeasurementResult.FrequencySteps.Count == 0)
				return Rect.Empty;
			var vmr = MeasurementResult.FrequencySteps.First(); // test data
			if (vmr == null || vmr.fftData == null)
				return Rect.Empty;
			var ImdVm = MyVModel;     // current settings

			Rect rrc = new Rect(0, 0, 0, 0);
			rrc.X = 20;
			double maxY = 0;
			if (ImdVm.ShowLeft)
			{
				rrc.Y = vmr.fftData.Left.Min();
				maxY = vmr.fftData.Left.Max();
				if (ImdVm.ShowRight)
				{
					rrc.Y = Math.Min(rrc.Y, vmr.fftData.Right.Min());
					maxY = Math.Max(maxY, vmr.fftData.Right.Max());
				}
			}
			else if (ImdVm.ShowRight)
			{
				rrc.Y = vmr.fftData.Right.Min();
				maxY = vmr.fftData.Right.Max();
			}

			rrc.Width = vmr.fftData.Left.Length * vmr.fftData.Df - rrc.X;       // max frequency
			rrc.Height = maxY - rrc.Y;      // max voltage absolute

			return rrc;
		}


		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurementSteps(ImdMeasurementResult msr, CancellationToken ct)
        {
			// Setup
			ImdViewModel msrImd = msr.MeasurementSettings;
			var imdVm = MyVModel;

			var freq = MathUtil.ToDouble(msrImd.Gen1Frequency, 0);
			var freq2 = MathUtil.ToDouble(msrImd.Gen2Frequency, 0);
			var sampleRate = msrImd.SampleRateVal;
			if (freq == 0 || freq2 == 0 || sampleRate == 0 || !FreqRespViewModel.FftSizes.Contains(msrImd.FftSize))
            {
                MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = msrImd.FftSizeVal;
			if (true != QaUsb.InitializeDevice(sampleRate, fftsize, msrImd.WindowingMethod, (int)msrImd.Attenuation, msr.FrequencySteps.Count == 0))
				return false;
			QaUsb.SetOutputSource(OutputSources.Off);            // We need to call this to make it turn on or off

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
				double[] stepFrequencies = [freq, freq2];
                // Translate the generated list to bin center frequencies
                var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize).ToArray();

                if(msr.NoiseFloor == null)
                {
					// ********************************************************************
					// Do noise floor measurement with source off
					// ********************************************************************
					await showMessage($"Determining noise floor.");
					QaUsb.SetOutputSource(OutputSources.Off);
					msr.NoiseFloor = await QaUsb.DoAcquisitions(msrImd.Averages, ct);
					if (ct.IsCancellationRequested)

						return false;
				}

				var gains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				int[] frqtest = [ToBinNumber(freq, LRGains)];
				var genVolt = imdVm.ToGenVoltage(msrImd.Gen1Voltage, frqtest, GEN_INPUT, gains);	// input voltage 1
				double amplitudeSetpoint1dBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				// the other voltage calculated via divisor
				genVolt = genVolt / msrImd.GenDivisor;
				double amplitudeSetpoint2dBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

				// ********************************************************************
				// Do a spectral sweep once (ignore the true)
				// ********************************************************************
				while(true)
				{
					// now do the step measurement
					await showMessage($"Measuring spectrum.");
					await showProgress(0);

                    // Set the generators
                    QaUsb.SetGen1(stepBinFrequencies[0], amplitudeSetpoint1dBV, msrImd.UseGenerator);
					//!!!await Qa40x.SetGen2(stepBinFrequencies[1], amplitudeSetpoint2dBV, msrImd.UseGenerator2);
					// for the first go around, turn on the generator
					if ( msrImd.UseGenerator || msrImd.UseGenerator2)
                    {
						QaUsb.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset
					}
					else
					{
						QaUsb.SetOutputSource(OutputSources.Off);            // We need to call this to make the averages reset
					}

					LeftRightSeries lrfs = await QaUsb.DoAcquisitions(msrImd.Averages, ct);
                    if (ct.IsCancellationRequested || lrfs.FreqRslt?.Left == null)
                        break;

                    ImdStep step = new()
                    {
						Gen1Freq = stepBinFrequencies[0],
						Gen2Freq = stepBinFrequencies[1],
						Gen1Volts = QaLibrary.ConvertVoltage(amplitudeSetpoint1dBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
						Gen2Volts = QaLibrary.ConvertVoltage(amplitudeSetpoint2dBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
						fftData = lrfs.FreqRslt,
                        timeData = lrfs.TimeRslt
                    };

					step.Left = ChannelCalculations(binSize, amplitudeSetpoint1dBV, step, msr, false);
					step.Right = ChannelCalculations(binSize, amplitudeSetpoint1dBV, step, msr, true);

					// Add step data to list
					msr.FrequencySteps.Clear();
					msr.FrequencySteps.Add(step);

					// For now clear measurements to allow only one until we have a UI to manage them.
					MyVModel.HasExport = true;

					// the first one gets added. after that it's the same object
					if (Data.Measurements.Count == 0)
						Data.Measurements.Add(msr);

					ClearPlot();
					UpdateGraph(false);
					if (!imdVm.IsTracking)
					{
						// if we're not tracking the mouse, update the fixed frequency piece
						imdVm.RaiseMouseTracked("track");
					}
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

        private void AddAMarker(ImdMeasurementResult fmr, double frequency, bool isred = false)
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
				else if (leftData != null)
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

		private void ShowHarmonicMarkers(ImdMeasurementResult fmr)
        {
            var vm = MyVModel;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if ( vm.ShowMarkers)
            {
				ImdStepChannel? step = null;
				if (vm.ShowLeft)
					step = fmr.FrequencySteps[0].Left;
				else if(vm.ShowRight)
					step = fmr.FrequencySteps[0].Right;
				if (step != null)
				{
					AddAMarker(fmr, fmr.FrequencySteps[0].Gen1Freq);
					AddAMarker(fmr, fmr.FrequencySteps[0].Gen2Freq);
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

		private void ShowPowerMarkers(ImdMeasurementResult fmr)
		{
			var vm = MyVModel;
            List<double> freqchecks = new List<double> { 50, 60 };
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowPowerMarkers)
			{
				var sampleRate = fmr.MeasurementSettings.SampleRateVal;
				var fftsize = fmr.MeasurementSettings.FftSizeVal;
                var nfloor = MeasurementResult.FrequencySteps[0].Left.Average_NoiseFloor_dBV;   // Average noise floor in dBVolts after the fundamental
                double fsel = 0;
                double maxdata = -10;

				var fftdata = vm.ShowLeft ? fmr.FrequencySteps[0].fftData?.Left : fmr.FrequencySteps[0].fftData?.Right;
				if (fftdata == null)
					return;
				var df = MeasurementResult.FrequencySteps[0].fftData?.Df;

				// find if 50 or 60hz is higher, indicating power line frequency
				foreach (double freq in freqchecks)
				{
					var data = QAMath.MagAtFreq(fftdata, df ?? 1, freq);
                    if(data > maxdata)
                    {
                        fsel = freq;
                    }
				}
                // check 4 harmonics of power frequency
                for(int i=1; i<4; i++)
                {
					var actfreq = QaLibrary.GetNearestBinFrequency(fsel * i, sampleRate, fftsize);
					var data = QAMath.MagAtFreq(fftdata, df ?? 1, actfreq);
					double udif = 20 * Math.Log10(data);
                    AddAMarker(fmr, actfreq, true);
				}
			}
		}

		private void Addif(ref List<double> frqs, double dval )
		{
			if( dval > 0)
				frqs.Add(dval);
		}

		private double[] MakeHarmonics(double f1, double f2)
		{
			List<double> harmFreqs = new List<double>();
			if( f1 > f2)
			{
				var a = f2;
				f2 = f1;
				f1 = f2;
			}
			Addif(ref harmFreqs, f2 - f1);
			Addif(ref harmFreqs, f2 + f1);
			Addif(ref harmFreqs, 2*f1 - f2);
			Addif(ref harmFreqs, 2*f2 - f1);
			Addif(ref harmFreqs, 3*f1 - 2*f2);
			Addif(ref harmFreqs, 3*f2 - 2*f1);
			Addif(ref harmFreqs, 4 * f1 - 3 * f2);
			Addif(ref harmFreqs, 4 * f2 - 3 * f1);
			Addif(ref harmFreqs, 3 * f2 - f1);
			Addif(ref harmFreqs, 3 * f1 - f2);
			return harmFreqs.ToArray();
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
		private ImdStepChannel ChannelCalculations(double binSize, double generatorAmplitudeDbv, ImdStep step, ImdMeasurementResult msr, bool isRight)
		{
			if (step == null)
				return new();

			uint fundamental1Bin = QaLibrary.GetBinOfFrequency(step.Gen1Freq, binSize);
			uint fundamental2Bin = QaLibrary.GetBinOfFrequency(step.Gen2Freq, binSize);
			var ffts = isRight ? step.fftData?.Right : step.fftData?.Left;
			var times = isRight ? step.timeData?.Right : step.timeData?.Left;
			var lfdata = step.timeData?.Left;
			if(ffts == null || lfdata == null)
				return new();

			double allvolts = Math.Sqrt(lfdata.Select(x => x * x).Sum() / lfdata.Count());  // use the time data for best accuracy gain math

			ImdStepChannel channelData = new()
			{
				Fundamental1_V = ffts[fundamental1Bin],
				Fundamental1_dBV = 20 * Math.Log10(ffts[fundamental1Bin]),
				Fundamental2_V = ffts[fundamental2Bin],
				Fundamental2_dBV = 20 * Math.Log10(ffts[fundamental2Bin]),
				Total_V = allvolts,
				Total_W = allvolts * allvolts / ViewSettings.AmplifierLoad,
				Gain_dB = 20 * Math.Log10(ffts[fundamental1Bin] / Math.Pow(10, generatorAmplitudeDbv / 20)),
			};
			var cd = channelData;
			cd.Fundamentals_V = Math.Sqrt(cd.Fundamental1_V * cd.Fundamental1_V + 
					cd.Fundamental2_V * cd.Fundamental2_V);
			cd.Fundamentals_dBV = 20 * Math.Log10(cd.Fundamentals_V);
			cd.Fundamentals_W = Math.Pow(cd.Fundamentals_V, 2) / ViewSettings.AmplifierLoad;
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
			var vsum = Math.Sqrt(channelData.Fundamental1_V * channelData.Fundamental1_V + channelData.Fundamental2_V * channelData.Fundamental2_V);

			var harmonicFreq = MakeHarmonics(step.Gen1Freq, step.Gen2Freq);
			var maxfreq = binSize * ffts.Count();

			for (int harmonicNumber = 0; harmonicNumber < harmonicFreq.Length; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
            {
                double harmonicFrequency = harmonicFreq[harmonicNumber];
				double amplitude_V = QAMath.MagAtFreq(ffts, binSize, harmonicFrequency);
                double noise_V = channelData.TotalNoiseFloor_V;

				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
                double thd_Percent = (amplitude_V / vsum) * 100;
				var noiseAt = QAMath.MagAtFreq(noiseFlr ?? [], binSize, harmonicFrequency);
				double thdN_Percent = ((amplitude_V - noiseAt) / vsum) * 100;

				HarmonicData harmonic = new()
                {
                    HarmonicNr = harmonicNumber,
                    Frequency = harmonicFrequency,
                    Amplitude_V = amplitude_V,
                    Amplitude_dBV = amplitude_dBV,
                    Thd_Percent = thd_Percent,
					Thd_dB = 20 * Math.Log10(thd_Percent / 100.0),
					Thd_dBN = 20 * Math.Log10(thdN_Percent / 100.0),
					NoiseAmplitude_V = 1e-6
                };

				harmonic.NoiseAmplitude_V = noiseAt;

				if (harmonicNumber >= 6)
                    distortionD6plus += Math.Pow(amplitude_V, 2);

				distortionSqrtTotal += Math.Pow(amplitude_V, 2);
				distortionSqrtTotalN += Math.Pow(amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
            }

			// Calculate THD
			if (distortionSqrtTotal != 0)
			{
				channelData.Thd_Percent = (Math.Sqrt(distortionSqrtTotal) / vsum) * 100;
				channelData.Thd_PercentN = (Math.Sqrt(distortionSqrtTotal) / vsum) * 100;
				channelData.Thd_dB = 20 * Math.Log10(channelData.Thd_Percent / 100.0);
				var Thdn_Percent = (Math.Sqrt(distortionSqrtTotalN) / vsum) * 100;
				channelData.Thd_dBN = 20 * Math.Log10(Thdn_Percent / 100.0);
			}

			// Calculate D6+ (D6 - D12)
			if (distortionD6plus != 0)
            {
                channelData.D6Plus_dBV = 20 * Math.Log10(Math.Sqrt(distortionD6plus));
                channelData.ThdPercent_D6plus = 100 * Math.Sqrt(distortionD6plus) / vsum;
                channelData.ThdDbD6plus = 20 * Math.Log10(channelData.ThdPercent_D6plus / 100.0);
            }

			var powertotal = 10.0;
			if(times != null)
				powertotal = times.Select(x => x * x).Sum() / times.Length;
			double hdtotal = 0;
			// get fundamentals and harmonic distortion
			for(int j=1; j<10; j++)
			{
				var hda = QAMath.MagAtFreq(ffts, binSize, step.Gen1Freq * j);
				hdtotal += hda * hda;
				hda = QAMath.MagAtFreq(ffts, binSize, step.Gen2Freq * j);
				hdtotal += hda * hda;
			}

			var denom = Math.Sqrt(powertotal - distortionSqrtTotal - hdtotal);
			channelData.Snr_dB = -20 * Math.Log10(vsum / denom);

			// If load not zero then calculate load power
			if (ViewSettings.AmplifierLoad != 0)
                channelData.Power_Watt = Math.Pow(vsum, 2) / ViewSettings.AmplifierLoad;

            return channelData;
        }


        /// <summary>
        /// Clear the plot
        /// </summary>
        void ClearPlot()
        {
            fftPlot.ThePlot.Clear();
            fftPlot.Refresh();
        }

		void SetTheTitle(ScottPlot.Plot myPlot)
		{
			var imdVm = MyVModel;
			if( imdVm.IntermodType == "Custom")
				myPlot.Title("Intermodulation Distortion");
			else
			{
				var vsa = imdVm.IntermodType.Split('(').First();
				myPlot.Title(String.Format("{0} Intermodulation Distortion", vsa ));
			}
		}

		/// <summary>
		/// Ititialize the THD % plot
		/// </summary>
		void InitializefftPlot()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			PlotUtil.InitializePctFreqPlot(myPlot);
			var imdVm = MyVModel;

            ImdViewModel thd = MyVModel;
            myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thd.GraphStartFreq)), Math.Log10(MathUtil.ToDouble(thd.GraphEndFreq)), 
				Math.Log10(MathUtil.ToDouble(thd.RangeBottom)) - 0.00000001, Math.Log10(MathUtil.ToDouble(thd.RangeTop)));  // - 0.000001 to force showing label
			SetTheTitle(myPlot);
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("%");

			fftPlot.Refresh();
        }

        /// <summary>
        /// Plot the THD % graph
        /// </summary>
        /// <param name="data"></param>
        void PlotValues(ImdMeasurementResult measurementResult, int measurementNr)
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			myPlot.Clear();

			var imdVm = MyVModel;
			bool useLeft = imdVm.ShowLeft;	// dynamically update these
			bool useRight = imdVm.ShowRight;

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
				if(imdVm.ShowPercent)
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

			var showThick = MyVModel.ShowThickLines;	// so it dynamically updates

			if (useLeft)
			{
				Scatter plotTot_Left = myPlot.Add.Scatter(logFreqX, logHTot_Left_Y);
				plotTot_Left.LineWidth = showThick ? _Thickness : 1;
				plotTot_Left.Color = QaLibrary.BlueColor;  // Blue
				plotTot_Left.MarkerSize = 1;
			}

			if (useRight)
			{
				Scatter plotTot_Right = myPlot.Add.Scatter(logFreqX, logHTot_Right_Y);
				plotTot_Right.LineWidth = showThick ? _Thickness : 1;
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

			var imdVm = MyVModel;

			myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(imdVm.GraphStartFreq, 20)), Math.Log10(MathUtil.ToDouble(imdVm.GraphEndFreq, 20000)),
				MathUtil.ToDouble(imdVm.RangeBottomdB, -20), MathUtil.ToDouble(imdVm.RangeTopdB, 180));
			SetTheTitle(myPlot);
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("dBV");

            fftPlot.Refresh();
        }

		/// <summary>
		///  Start measurement button click
		/// </summary>
		public async void StartMeasurement()
        {
			var imdVm = MyVModel;
			if (!StartAction(imdVm))
				return; 
            ct = new();

			// Clear measurement result
			MeasurementResult = new(imdVm)
			{
				CreateDate = DateTime.Now,
				Show = true,                                      // Show in graph
			};
			Data.Measurements.Clear();

			var msr = MeasurementResult.MeasurementSettings;
			var genType = ToDirection(msr.GenDirection);
			var freq = MathUtil.ToDouble(msr.Gen1Frequency, 1000);
			var freq2 = MathUtil.ToDouble(msr.Gen2Frequency, 1000);
			// calculate the gain curve if we need it
			if (msr.DoAutoAttn || genType != E_GeneratorDirection.INPUT_VOLTAGE)
			{
				// show that we're autoing...
				if (msr.DoAutoAttn)
					imdVm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;
				LRGains = await DetermineGainCurve(true, 1);
			}
			var binSize = QaLibrary.CalcBinSize(msr.SampleRateVal, msr.FftSizeVal);
			// calculate the required attenuation
			if (msr.DoAutoAttn && LRGains != null)
			{
				int[] frqtest = [ToBinNumber(freq, LRGains)];
				int[] frq2test = [ToBinNumber(freq2, LRGains)];
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;

				// find the two input voltages for our testing
				var v1in = msr.ToGenVoltage(msr.Gen1Voltage, frqtest, GEN_INPUT, gains);  // get output voltage
				var v2in = v1in / msr.GenDivisor;  // get second input voltage
																						 // now find the output voltages for this input
				var v1lout = ToGenOutVolts(v1in, frqtest, LRGains.Left);	// left channel output V
				var v2lout = ToGenOutVolts(v2in, frq2test, LRGains.Left);
				var v1rout = ToGenOutVolts(v1in, frqtest, LRGains.Right);	// right channel output V
				var v2rout = ToGenOutVolts(v2in, frq2test, LRGains.Right);
				var vtotal = Math.Max(v1lout*v1lout + v2lout*v2lout, v1rout*v1rout + v2rout*v2rout);	// max sum of squares
				vtotal = Math.Sqrt(vtotal);

				var vdbv = QaLibrary.ConvertVoltage(vtotal, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

				imdVm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);
				msr.Attenuation = imdVm.Attenuation; // update the specVm to update the gui, then this for the steps
			}

			// do the actual measurements
			var rslt = true;
			rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
			var fftsize = msr.FftSize;
			var sampleRate = msr.SampleRate;
			var atten = msr.Attenuation;
			if (rslt)
            {
                await showMessage("Running");
                while (!ct.IsCancellationRequested)
                {
                    if (imdVm.FftSize != fftsize || imdVm.SampleRate != sampleRate || imdVm.Attenuation != atten)
                    {
						fftsize = imdVm.FftSize;
						sampleRate = imdVm.SampleRate;
						atten = imdVm.Attenuation;
						MeasurementResult = new(imdVm)
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
			// Turn the generator off since we leave it on during the loop for settling
			QaUsb.SetOutputSource(OutputSources.Off);

			imdVm.IsRunning = false;
			await showMessage("");
			MyVModel.HasExport = MeasurementResult.FrequencySteps.Count > 0;
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
			ImdViewModel thd = MyVModel;
			var vm = ViewSettings.Singleton.ImdChannelLeft;
			var steps = thd.ShowLeft ? MeasurementResult.FrequencySteps[0].Left : MeasurementResult.FrequencySteps[0].Right;
			vm.CalculateChannelValues(steps, MathUtil.ToDouble( thd.Gen1Frequency), 
						MathUtil.ToDouble(thd.Gen2Frequency), thd.ShowDataPercent);
		}

		public void UpdateGraph(bool settingsChanged)
        {
            fftPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			fftPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			ImdViewModel thd = MyVModel;

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
		}

	}
}