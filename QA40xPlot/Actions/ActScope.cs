using QA40x_BareMetal;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Data;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

	public class ActScope : ActBase
    {
        public ScopeData Data { get; set; }                  // Data used in this form instance

        private readonly Views.PlotControl timePlot;

        private ScopeMeasurementResult MeasurementResult;

        private float _Thickness = 2.0f;
		private static ScopeViewModel MyVModel { get => ViewSettings.Singleton.ScopeVm; }

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActScope(ref ScopeData data, Views.PlotControl graphFft)
        {
            Data = data;
            
			timePlot = graphFft;

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
		async Task<bool> PerformMeasurementSteps(ScopeMeasurementResult msr, CancellationToken ct)
        {
			// Setup
			ScopeViewModel thd = msr.MeasurementSettings;
			var scopeVm = MyVModel;

			var freq = MathUtil.ToDouble(msr.MeasurementSettings.Gen1Frequency, 0);
			var freq2 = MathUtil.ToDouble(msr.MeasurementSettings.Gen2Frequency, 0);
			var sampleRate = msr.MeasurementSettings.SampleRateVal;
			if (freq == 0 || sampleRate == 0 || !ScopeViewModel.FftSizes.Contains(msr.MeasurementSettings.FftSize))
            {
                MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = thd.FftSizeVal;

			// ********************************************************************  
			// Load a settings we want
			// ********************************************************************  
			if (true != QaUsb.InitializeDevice(sampleRate, fftsize, msr.MeasurementSettings.WindowingMethod, (int)msr.MeasurementSettings.Attenuation))
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
				double[] stepFrequencies = [freq, freq2];
                // Translate the generated list to bin center frequencies
                var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize).ToArray();

                if(msr.NoiseFloor == null)
                {
					msr.NoiseFloor = await MeasureNoise(ct);
					if (ct.IsCancellationRequested)
						return false;
				}
				var gains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				var genVolt = scopeVm.ToGenVoltage(msr.MeasurementSettings.Gen1Voltage, [], GEN_INPUT, gains) ;
				var genVolt2 = scopeVm.ToGenVoltage(msr.MeasurementSettings.Gen2Voltage, [], GEN_INPUT, gains);
				if (genVolt > 5)
				{
					await showMessage($"Requesting input voltage of {genVolt} volts, check connection and settings");
					genVolt = 0.01;
				}
				double amplitudeSetpointdBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

				// ********************************************************************
				// Do a spectral sweep once
				// ********************************************************************
				while(true)
				{
					// now do the step measurement
					await showMessage($"Measuring spectrum with input of {genVolt:G3}V.");
					await showProgress(0);

					// for the first go around, turn on the generator
					LeftRightSeries? lrfs;
					if (thd.UseGenerator1 || thd.UseGenerator2)
					{
						// Set the generators via a usermode
						var gw1 = new GenWaveform()
						{
							Frequency = stepBinFrequencies[0],
							Voltage = genVolt,
							Name = msr.MeasurementSettings.Gen1Waveform
						};
						var gw2 = new GenWaveform()
						{
							Frequency = stepBinFrequencies[1],
							Voltage = genVolt2,
							Name = msr.MeasurementSettings.Gen2Waveform
						};
						var gws = new GenWaveSample()
						{
							SampleRate = (int)sampleRate,
							SampleSize = (int)fftsize
						};
						GenWaveform[] gwho = [];
						if(thd.UseGenerator1 && thd.UseGenerator2)
							gwho = [gw1, gw2];
						else if(thd.UseGenerator1)
							gwho = [gw1];
						else
							gwho = [gw2];
						var wave = QAMath.CalculateWaveform(gwho, gws);
						lrfs = await QaUsb.DoAcquireUser(1, ct, wave.ToArray(), wave.ToArray(), true);
					}
					else
					{
						QaUsb.SetOutputSource(OutputSources.Off);            // We need to call this to make the averages reset
						lrfs = await QaUsb.DoAcquisitions(1, ct);
					}
					if (lrfs == null)
						break;

					uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[0], binSize);
                    if (fundamentalBin >= (lrfs.FreqRslt?.Left.Length ?? -1))               // Check in bin within range
                        break;

                    ThdFrequencyStep step = new()
                    {
                        FundamentalFrequency = stepBinFrequencies[0],
                        GeneratorVoltage = genVolt,
                        fftData = lrfs.FreqRslt,
                        timeData = lrfs.TimeRslt
                    };

					step.Left = ChannelCalculations(binSize, genVolt, step, msr, false);
					step.Right = ChannelCalculations(binSize, genVolt, step, msr, true);

					// Calculate the THD
					{
						var maxf = 20000;   // the app seems to use 20,000 so not sampleRate/ 2.0;
						if(lrfs != null && lrfs.FreqRslt != null)
						{
							LeftRightPair snrdb = QaCompute.GetSnrDb(lrfs.FreqRslt, stepBinFrequencies[0], 20.0, maxf);
							LeftRightPair thds = QaCompute.GetThdDb(lrfs.FreqRslt, stepBinFrequencies[0], 20.0, maxf);
							LeftRightPair thdN = QaCompute.GetThdnDb(lrfs.FreqRslt, stepBinFrequencies[0], 20.0, maxf);

							step.Left.Thd_dBN = thdN.Left;
							step.Right.Thd_dBN = thdN.Right;
							step.Left.Thd_dB = thds.Left;
							step.Right.Thd_dB = thds.Right;
							step.Left.Snr_dB = snrdb.Left;
							step.Right.Snr_dB = snrdb.Right;
							step.Left.Thd_Percent = 100 * Math.Pow(10, thds.Left / 20);
							step.Right.Thd_Percent = 100 * Math.Pow(10, thds.Right / 20);
							step.Left.Thd_PercentN = 100 * Math.Pow(10, thdN.Left / 20);
							step.Right.Thd_PercentN = 100 * Math.Pow(10, thdN.Right / 20);
						}
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

        private void AddAMarker(ScopeMeasurementResult fmr, double frequency, bool isred = false)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = timePlot.ThePlot;
			var sampleRate = fmr.MeasurementSettings.SampleRateVal;
			var fftsize = fmr.MeasurementSettings.FftSizeVal;
			int bin = (int)QaLibrary.GetBinOfFrequency(frequency, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
			var leftData = fmr.FrequencySteps[0].fftData?.Left;
			var rightData = fmr.FrequencySteps[0].fftData?.Right;

			double markVal = 0;
			if (rightData != null && !vm.ShowLeft)
			{
				markVal = 20 * Math.Log10(rightData[bin]);
			}
			else if(leftData != null )
			{
				markVal = 20 * Math.Log10(leftData[bin]);
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
			var mymark = myPlot.Add.Marker(Math.Log10(frequency), markVal,
				MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
			mymark.LegendText = string.Format("{1}: {0:F1}", markVal, (int)frequency);
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
		private ThdFrequencyStepChannel ChannelCalculations(double binSize, double generatorV, ThdFrequencyStep step, ScopeMeasurementResult msr, bool isRight)
		{
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(step.FundamentalFrequency, binSize);
			var ffts = isRight ? step.fftData?.Right : step.fftData?.Left;
			var ltdata = step.timeData?.Left;

			// this should never happen
			if (ffts == null || ltdata == null)
				return new();

			double allvolts = Math.Sqrt(ltdata.Select(x => x * x ).Sum() / ltdata.Count()); // use the time data for best accuracy gain math

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
			channelData.TotalNoiseFloor_V = QaCompute.CalculateNoise(msr.NoiseFloor?.FreqRslt, !isRight);


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
			if(vmr == null || vmr.timeData == null)
				return Rect.Empty;
			var scopeVm = MyVModel;     // current settings

			Rect rrc = new Rect(0, 0, 0, 0);
			rrc.X = 0;
			double maxY = 0;
			if(scopeVm.ShowLeft)
			{
				rrc.Y = vmr.timeData.Left.Min();
				maxY = vmr.timeData.Left.Max();
				if (scopeVm.ShowRight)
				{
					rrc.Y = Math.Min(rrc.Y, vmr.timeData.Right.Min());
					maxY = Math.Max(maxY, vmr.timeData.Right.Max());
				}
			}
			else if (scopeVm.ShowRight)
			{
				rrc.Y = vmr.timeData.Right.Min();
				maxY = vmr.timeData.Right.Max();
			}

			rrc.Width = 1000 * vmr.timeData.Left.Length * vmr.timeData.dt - rrc.X;       // max ms
			rrc.Height = maxY - rrc.Y;      // max voltage absolute

			return rrc;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="atime">time on chart</param>
		/// <param name="posnV">Y of mouse in plot</param>
		/// <param name="useRight">which channel</param>
		/// <returns>a tuple of value, value in pct</returns>
		public ValueTuple<double,double> LookupXY(double atime, double posnV, bool useRight)
		{
			var steps = MeasurementResult.FrequencySteps;
			if (atime <= 0 || steps == null || steps.Count == 0)
				return ValueTuple.Create(0.0,0.0);
			var step = steps.First();
			try
			{
				// get the data to look through
				var timedata = step.timeData;
				var ffs = useRight ? timedata?.Right : timedata?.Left;
				if (timedata != null && ffs != null && ffs.Length > 0 && atime > 0)
				{
					int abin = (int)(atime / (1000*timedata.dt));       // approximate bin in mS

					var vm = MyVModel;
					if ( abin < ffs.Length)
					{
						return ValueTuple.Create(1000*abin * timedata.dt, ffs[abin]);
					}
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
            timePlot.ThePlot.Clear();
            timePlot.Refresh();
        }

        /// <summary>
        /// Plot the THD % graph
        /// </summary>
        /// <param name="data"></param>
        void PlotValues(ScopeMeasurementResult measurementResult, int measurementNr)
        {
			ScottPlot.Plot myPlot = timePlot.ThePlot;
			myPlot.Clear();

			var scopeVm = MyVModel;
			bool leftChannelEnabled = scopeVm.ShowLeft;	// dynamically update these
			bool rightChannelEnabled = scopeVm.ShowRight;

			var timeData = MeasurementResult.FrequencySteps[0].timeData;
			if (timeData == null)
				return;

			double maxleft = timeData.Left.Max();
			double maxright = timeData.Right.Max();

			var timeX = Enumerable.Range(0, timeData.Left.Length).Select(x => x * 1000 * timeData.dt).ToArray(); // in ms
			var showThick = MyVModel.ShowThickLines;	// so it dynamically updates
			var markerSize = scopeVm.ShowPoints ? (showThick ? _Thickness : 1) + 3 : 1;
			if (leftChannelEnabled)
			{
				Scatter pLeft = myPlot.Add.Scatter(timeX, timeData.Left);
				pLeft.LineWidth = showThick ? _Thickness : 1;
				pLeft.Color = QaLibrary.BlueColor;  // Blue
				pLeft.MarkerSize = markerSize;
			}

			if (rightChannelEnabled)
			{
				Scatter pRight = myPlot.Add.Scatter(timeX, timeData.Right);
				pRight.LineWidth = showThick ? _Thickness : 1;
				if (leftChannelEnabled)
					pRight.Color = QaLibrary.RedXColor; // Red transparant
				else
					pRight.Color = QaLibrary.RedColor; // Red
				pRight.MarkerSize = markerSize;
			}

			timePlot.Refresh();
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot()
        {
			ScottPlot.Plot myPlot = timePlot.ThePlot;
            PlotUtil.InitializeMagTimePlot(myPlot);

			var thdFreq = MyVModel;

			myPlot.Axes.SetLimits(MathUtil.ToDouble(thdFreq.GraphStartTime), MathUtil.ToDouble(thdFreq.GraphEndTime), 
				MathUtil.ToDouble(thdFreq.RangeBottom), MathUtil.ToDouble(thdFreq.RangeTop));

            myPlot.Title("Scope");
			myPlot.XLabel("Time (mS)");
			myPlot.YLabel("Voltage");

            timePlot.Refresh();
        }

        /// <summary>
        ///  Start measurement button click
        /// </summary>
        public async void StartMeasurement()
        {
			var scopeVm = MyVModel;
			if (!StartAction(scopeVm))
				return;

			ct = new();
			// Clear measurement result
			MeasurementResult = new(scopeVm)
			{
				CreateDate = DateTime.Now,
				Show = true,                                      // Show in graph
			};
			Data.Measurements.Clear();

			var genType = ToDirection(scopeVm.GenDirection);
			var freq = MathUtil.ToDouble(scopeVm.Gen1Frequency, 1000);
			var binSize = QaLibrary.CalcBinSize(scopeVm.SampleRateVal, scopeVm.FftSizeVal);
			// if we're doing adjusting here
			if (scopeVm.DoAutoAttn || genType != E_GeneratorDirection.INPUT_VOLTAGE)
			{
				// show that we're autoing...
				if (scopeVm.DoAutoAttn)
					scopeVm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;
				await showMessage("Calculating DUT gain"); 
				LRGains = await DetermineGainAtFreq(freq, true, 1);
			}

			if (scopeVm.DoAutoAttn && LRGains != null)
			{
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
				var vinL = scopeVm.ToGenVoltage(scopeVm.Gen1Voltage, [], GEN_INPUT, gains); // get primary input voltage
				var vinL2 = scopeVm.ToGenVoltage(scopeVm.Gen2Voltage, [], GEN_INPUT, gains); // get primary input voltage
				vinL = Math.Max(vinL, vinL2);
				double voutL = ToGenOutVolts(vinL, [], LRGains.Left);	// what is that as output voltage?
				double voutR = ToGenOutVolts(vinL, [], LRGains.Right);	// for both channels
				var vdbv = QaLibrary.ConvertVoltage( Math.Max(voutL, voutR), E_VoltageUnit.Volt, E_VoltageUnit.dBV );
				scopeVm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);				// find attenuation for both
				MeasurementResult.MeasurementSettings.Attenuation = scopeVm.Attenuation;	// update the scopeVm to update the gui, then this for the steps
			}

			bool rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
            var fftsize = scopeVm.FftSize;
            var sampleRate = scopeVm.SampleRate;
            var atten = scopeVm.Attenuation;
            if (rslt)
            {
                await showMessage("Running");
                while (!ct.IsCancellationRequested)
                {
					var msrSet = MeasurementResult.MeasurementSettings;
                    await Task.Delay(250);
                    if (ct.IsCancellationRequested)
                        break;
					if( scopeVm.Gen1Frequency != msrSet.Gen1Frequency || msrSet.GenDirection != scopeVm.GenDirection)
					{
						msrSet.Gen1Frequency = scopeVm.Gen1Frequency;
						msrSet.GenDirection = scopeVm.GenDirection;
						var genoType = ToDirection(msrSet.GenDirection);
						if (LRGains != null && genoType == E_GeneratorDirection.OUTPUT_VOLTAGE)
						{
							await showMessage("Calculating DUT gain");
							LRGains = await DetermineGainAtFreq(MathUtil.ToDouble(msrSet.Gen1Frequency, 1000), false, 1);
						}
					}
                    if (scopeVm.FftSize != fftsize || scopeVm.SampleRate != sampleRate || scopeVm.Attenuation != atten)
                    {
						fftsize = scopeVm.FftSize;
						sampleRate = scopeVm.SampleRate;
						atten = scopeVm.Attenuation;
						MeasurementResult = new(scopeVm)
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

			scopeVm.IsRunning = false;
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
			var thd = MyVModel;
			var vm = ViewSettings.Singleton.ScopeChanLeft;
            vm.FundamentalFrequency = 0;
            vm.CalculateChannelValues(MathUtil.ToDouble( thd.Gen1Frequency), false);
		}

		public void UpdateGraph(bool settingsChanged)
        {
            timePlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			timePlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			var thd = MyVModel;

            if (settingsChanged)
            {
                InitializeMagnitudePlot();
            }

            foreach (var result in Data.Measurements.Where(m => m.Show))
            {
				ScopeViewModel mvs = result.MeasurementSettings;
				PlotValues(result, resultNr++);
            }

            if( MeasurementResult.FrequencySteps.Count > 0)
            {
				DrawChannelInfoTable();
			}

			timePlot.Refresh();
		}
	}
}