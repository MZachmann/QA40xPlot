using QA40xPlot.Data;

using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

    public class ActSpectrum
    {
        public SpectrumData Data { get; set; }                  // Data used in this form instance
        public bool MeasurementBusy { get; set; }                   // Measurement busy state

        private Views.PlotControl? fftPlot;

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

		/// <summary>
		/// Update the generator voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateGeneratorVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.SpectrumVm;
			vm.Gen1Voltage = QaLibrary.ConvertVoltage(vm.GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)vm.GeneratorUnits).ToString();
        }

		/// <summary>
		/// Update the amplifier output voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateAmpOutputVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.SpectrumVm;
			switch (vm.OutputUnits)
            {
                case 0: // mV
                    vm.OutVoltage = ((int)QaLibrary.ConvertVoltage(vm.AmpOutputAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.MilliVolt));
                    break;
                case 1: // V
					vm.OutVoltage = QaLibrary.ConvertVoltage(vm.AmpOutputAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
                    break;
                case 2: // dB
					vm.OutVoltage = QaLibrary.ConvertVoltage(vm.AmpOutputAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.dBV);
                    break;
            }
        }

		/// <summary>
		/// Generator type changed
		/// </summary>
		public void UpdateGeneratorParameters()
        {
            var vm = ViewSettings.Singleton.SpectrumVm;
			switch (vm.MeasureType)
            {
                case 0: // Input voltage
					vm.ReadVoltage = false;
					vm.ReadOutPower = true;
					vm.ReadOutVoltage = true;
					UpdateGeneratorVoltageDisplay();
					break;
				case 1: // Output voltage
					vm.ReadVoltage = true;
					vm.ReadOutPower = true;
					vm.ReadOutVoltage = false;
					UpdateGeneratorVoltageDisplay();
					break;
				case 2: // Output power
					vm.ReadVoltage = true;
					vm.ReadOutPower = false;
					vm.ReadOutVoltage = true;
					//vm.OutPower = QaLibrary.ParseTextToDouble(txtAmplifierOutputPower.Text, MeasurementSettings.AmpOutputPower);
					//vm.AmpLoad = QaLibrary.ParseTextToDouble(txtOutputLoad.Text, MeasurementSettings.Load);
					//vm.Voltage = Math.Sqrt(MeasurementSettings.AmpOutputPower * MeasurementSettings.Load);      // Expected output DUT amplitude in Volts
					vm.GeneratorUnits = (int)E_VoltageUnit.Volt;                                           // Expected output DUT amplitude in dBV
                    UpdateAmpOutputVoltageDisplay();
                    break;
            }
        }

        private async Task showMessage(String msg, int delay = 0)
        {
            var vm = ViewModels.ViewSettings.Singleton.Main;
            await vm.SetProgressMessage(msg, delay);
		}

		private async Task showProgress(int progress, int delay = 0)
		{
			var vm = ViewModels.ViewSettings.Singleton.Main;
			await vm.SetProgressBar(progress, delay);
		}

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurementSteps(SpectrumMeasurementResult msr, CancellationToken ct)
        {
            // For now clear measurements to allow only one until we have a UI to manage them.
            Data.Measurements.Clear();

            // Add to list
            Data.Measurements.Add(MeasurementResult);

			// Init mini plots
			SpectrumViewModel thd = msr.MeasurementSettings;

            // Check if REST interface is available and device connected
            if (await QaLibrary.CheckDeviceConnected() == false)
                return false;

			var sampleRate = Convert.ToUInt32(thd.SampleRate);
			var fftsize = thd.FftActualSizes.ElementAt(thd.FftSizes.IndexOf(thd.FftSize));

            // ********************************************************************  
            // Load a settings we want
            // ********************************************************************  
            if (msr.FrequencySteps.Count == 0)
            {
				await Qa40x.SetDefaults();
			}
			await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make it turn on or off
			await Qa40x.SetSampleRate(sampleRate);
            await Qa40x.SetBufferSize(fftsize);
			await Qa40x.SetWindowing(thd.WindowingMethod);
            await Qa40x.SetRoundFrequencies(true);
			await Qa40x.SetInputRange((int)thd.Attenuation);

			try
            {
                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;

                // ********************************************************************
                // Calculate frequency steps to do
                // ********************************************************************
                var binSize = QaLibrary.CalcBinSize(Convert.ToUInt32(thd.SampleRate), fftsize);
                // Generate a list of frequencies
                var freq = Convert.ToDouble(thd.Gen1Frequency);
				var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(freq, freq, thd.StepsOctave);
                // Translate the generated list to bin center frequencies
                var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, Convert.ToUInt32(thd.SampleRate), fftsize);
                stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x <= 95500)                // Filter out values that are out of range 
                    .GroupBy(x => x)                                                                    // Filter out duplicates
                    .Select(y => y.First())
                    .ToArray();

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

				// ********************************************************************
				// Step through the list of frequencies
				// ********************************************************************
				for (int f = 0; f < stepBinFrequencies.Length; f++)
                {
                    // now do the step measurement
                    await showMessage($"Measuring step {f + 1} of {stepBinFrequencies.Length}.");
					await showProgress(100*(f + 1)/ stepBinFrequencies.Length);

                    // Set the generator
                    double amplitudeSetpointdBV = QaLibrary.ConvertVoltage(Convert.ToDouble(thd.Gen1Voltage), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
                    await Qa40x.SetGen1(stepBinFrequencies[f], amplitudeSetpointdBV, true);
                    // for the first go around, turn on the generator
                    if( thd.UseGenerator)
                    {
						await Qa40x.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset
					}

					LeftRightSeries lrfs = await QaLibrary.DoAcquisitions(thd.Averages, ct);
                    if (ct.IsCancellationRequested)
                        break;

                    uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
                    if (fundamentalBin >= lrfs.FreqRslt.Left.Length)               // Check in bin within range
                        break;

                    ThdFrequencyStep step = new()
                    {
                        FundamentalFrequency = stepBinFrequencies[f],
                        GeneratorVoltage = QaLibrary.ConvertVoltage(amplitudeSetpointdBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
                        fftData = lrfs.FreqRslt,
                        timeData = lrfs.TimeRslt
                    };
                  
                    step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqRslt.Left, msr.NoiseFloor.FreqRslt.Left, thd.AmpLoad);
                    step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqRslt.Right, msr.NoiseFloor.FreqRslt.Right, thd.AmpLoad);

					// Add step data to list
					if (msr.FrequencySteps.Count > 0)
					{
						msr.FrequencySteps.Clear();
					}
					msr.FrequencySteps.Add(step);

					ClearPlot();
					//ClearCursorTexts();
					UpdateGraph(false);
					//ShowLastMeasurementCursorTexts();

					// Check if cancel button pressed
					if (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Turn the generator off
            await Qa40x.SetOutputSource(OutputSources.Off);

            // Show message
            if( ct.IsCancellationRequested)
				await showMessage($"Measurement cancelled!", 500);
			else
				await showMessage($"Measurement finished!", 500);

            return !ct.IsCancellationRequested;
        }

        private static int toint(string st)
        {
            return Convert.ToInt32(st);
		}

        private void AddAMarker(SpectrumMeasurementResult fmr, double frequency, bool isred = false)
		{
			var vm = ViewSettings.Singleton.SpectrumVm;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			var sampleRate = Convert.ToUInt32(vm.SampleRate);
			var fftsize = vm.FftActualSizes.ElementAt(vm.FftSizes.IndexOf(vm.FftSize));
			int bin = (int)QaLibrary.GetBinOfFrequency(frequency, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
            double markVal = 0;
            ScottPlot.Color markerCol = new ScottPlot.Color();
            if( ! vm.ShowLeft)
            {
				markVal = 20 * Math.Log10(fmr.FrequencySteps[0].fftData.Right[bin]);
                markerCol = isred ? Colors.Green : Colors.DarkGreen;
			}
            else
            {
				markVal = 20 * Math.Log10(fmr.FrequencySteps[0].fftData.Left[bin]);
				markerCol = isred ? Colors.Red : Colors.DarkOrange;
			}
			var mymark = myPlot.Add.Marker(Math.Log10(frequency), markVal,
                MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
			mymark.LegendText = string.Format("{1}: {0:F1}", markVal, (int)frequency);
		}

		private void ShowHarmonicMarkers(SpectrumMeasurementResult fmr)
        {
            var vm = ViewSettings.Singleton.SpectrumVm;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if ( vm.ShowMarkers)
            {
                AddAMarker(fmr, Convert.ToDouble(vm.Gen1Frequency));

                for(int i=0; i<6; i++)
                {
                    var frq = fmr.FrequencySteps[0].Left.Harmonics[i].Frequency;
					AddAMarker(fmr, frq);
				}
			}
		}

		private void ShowPowerMarkers(SpectrumMeasurementResult fmr)
		{
			var vm = ViewSettings.Singleton.SpectrumVm;
            List<int> freqchecks = new List<int> { 50, 60 };
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowPowerMarkers)
			{
				var sampleRate = Convert.ToUInt32(vm.SampleRate);
				var fftsize = vm.FftActualSizes.ElementAt(vm.FftSizes.IndexOf(vm.FftSize));
                var nfloor = MeasurementResult.FrequencySteps[0].Left.Average_NoiseFloor_dBV;   // Average noise floor in dBVolts after the fundamental
				foreach (int freq in freqchecks)
				{
                    // check 5 harmonics of each
                    for(int i=1; i<4; i++)
                    {
                        var actfreq = QaLibrary.GetNearestBinFrequency(freq * i, sampleRate, fftsize);
						int bin = (int)QaLibrary.GetBinOfFrequency(actfreq, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
                        var data = vm.ShowLeft ? fmr.FrequencySteps[0].fftData.Left[bin] : fmr.FrequencySteps[0].fftData.Right[bin];
                        double udif = 20 * Math.Log10(data);
						if ((udif - nfloor) > 20)
                        {
                            AddAMarker(fmr, actfreq, true);
                        }
					}
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
		private ThdFrequencyStepChannel ChannelCalculations(double binSize, double fundamentalFrequency, double generatorAmplitudeDbv, double[] fftData, double[] noiseFloorFftData, double load)
        {
            uint fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFrequency, binSize);

            ThdFrequencyStepChannel channelData = new()
            {
                Fundamental_V = fftData[fundamentalBin],
                Fundamental_dBV = 20 * Math.Log10(fftData[fundamentalBin]),
                Gain_dB = 20 * Math.Log10(fftData[fundamentalBin] / Math.Pow(10, generatorAmplitudeDbv / 20))
            };
            // Calculate average noise floor
            channelData.Average_NoiseFloor_V = noiseFloorFftData.Average();   // Average noise floor in Volts after the fundamental
            var v2 = binSize * noiseFloorFftData.Select(x => x*x).Sum();    // Squared noise floor in Volts after the fundamental
			channelData.TotalNoiseFloor_V = Math.Sqrt(v2);   // Average noise floor in Volts after the fundamental
			channelData.Average_NoiseFloor_dBV = 20 * Math.Log10(channelData.TotalNoiseFloor_V);         // Average noise floor in dBV

			// Reset harmonic distortion variables
			double distortionSqrtTotal = 0;
			double distortionSqrtTotalN = 0;
			double distiortionD6plus = 0;

            // Loop through harmonics up tot the 12th
            for (int harmonicNumber = 2; harmonicNumber <= 12; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
            {
                double harmonicFrequency = fundamentalFrequency * harmonicNumber;
                uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

                if (bin >= fftData.Length) break;                                          // Invalid bin, skip harmonic

                double amplitude_V = fftData[bin];
                double noise_V = channelData.TotalNoiseFloor_V;

				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
                double thd_Percent = 0;
                thd_Percent = (channelData.Fundamental_V > noise_V) ? 0 : (amplitude_V / (channelData.Fundamental_V - noise_V)) * 100;
				double thdN_Percent = (amplitude_V / channelData.Fundamental_V) * 100;

				HarmonicData harmonic = new()
                {
                    HarmonicNr = harmonicNumber,
                    Frequency = harmonicFrequency,
                    Amplitude_V = amplitude_V,
                    Amplitude_dBV = amplitude_dBV,
                    Thd_Percent = thd_Percent,
					Thd_dB = 20 * Math.Log10(thd_Percent / 100.0),
					Thd_dBN = 20 * Math.Log10(thdN_Percent / 100.0),
					NoiseAmplitude_V = noiseFloorFftData[bin]
                };

                if (harmonicNumber >= 6)
                    distiortionD6plus += Math.Pow(amplitude_V, 2);

                distortionSqrtTotal += Math.Pow(Math.Sqrt(Math.Max(0,amplitude_V* amplitude_V - noiseFloorFftData[bin]* noiseFloorFftData[bin])), 2);
				distortionSqrtTotalN += Math.Pow(amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
            }

            // Calculate THD
            if (distortionSqrtTotal != 0)
            {
                channelData.Thd_Percent = (Math.Sqrt(distortionSqrtTotal) / channelData.Fundamental_V) * 100;
                channelData.Thd_dB = 20 * Math.Log10(channelData.Thd_Percent / 100.0);
				var Thdn_Percent = (Math.Sqrt(distortionSqrtTotalN) / channelData.Fundamental_V) * 100;
				channelData.Thd_dBN = 20 * Math.Log10(Thdn_Percent / 100.0);
			}

			// Calculate D6+ (D6 - D12)
			if (distiortionD6plus != 0)
            {
                channelData.D6Plus_dBV = 20 * Math.Log10(Math.Sqrt(distiortionD6plus));
                channelData.ThdPercent_D6plus = Math.Sqrt(distiortionD6plus / Math.Pow(channelData.Fundamental_V, 2)) * 100;
                channelData.ThdDbD6plus = 20 * Math.Log10(channelData.ThdPercent_D6plus / 100.0);
            }

            // If load not zero then calculate load power
            if (load != 0)
                channelData.Power_Watt = Math.Pow(channelData.Fundamental_V, 2) / load;

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

        /// <summary>
        /// Ititialize the THD % plot
        /// </summary>
        void InitializefftPlot()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;

			myPlot.Clear();

			ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenY = new();
            minorTickGenY.Divisions = 10;

            // create a numeric tick generator that uses our custom minor tick generator
            ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
            tickGenY.MinorTickGenerator = minorTickGenY;

            // create a custom tick formatter to set the label text for each tick
            static string LogTickLabelFormatter(double y) => $"{Math.Pow(10, Math.Round(y, 10)):#0.######}";

            // tell our major tick generator to only show major ticks that are whole integers
            tickGenY.IntegerTicksOnly = true;

            // tell our custom tick generator to use our new label formatter
            tickGenY.LabelFormatter = LogTickLabelFormatter;

            // tell the left axis to use our custom tick generator
            myPlot.Axes.Left.TickGenerator = tickGenY;

            // ******* y-ticks ****
            // create a minor tick generator that places log-distributed minor ticks
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();
            minorTickGen.Divisions = 10;

            // create a minor tick generator that places log-distributed minor ticks
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenX = new();

            // create a numeric tick generator that uses our custom minor tick generator
            //ScottPlot.TickGenerators.NumericAutomatic tickGenX = new();
            //tickGenX.MinorTickGenerator = minorTickGenX;

            // create a manual tick generator and add ticks
            ScottPlot.TickGenerators.NumericManual tickGenX = new();

            // add major ticks with their labels
            tickGenX.AddMajor(Math.Log10(1), "1");
            tickGenX.AddMajor(Math.Log10(2), "2");
            tickGenX.AddMajor(Math.Log10(5), "5");
            tickGenX.AddMajor(Math.Log10(10), "10");
            tickGenX.AddMajor(Math.Log10(20), "20");
            tickGenX.AddMajor(Math.Log10(50), "50");
            tickGenX.AddMajor(Math.Log10(100), "100");
            tickGenX.AddMajor(Math.Log10(200), "200");
            tickGenX.AddMajor(Math.Log10(500), "500");
            tickGenX.AddMajor(Math.Log10(1000), "1k");
            tickGenX.AddMajor(Math.Log10(2000), "2k");
            tickGenX.AddMajor(Math.Log10(5000), "5k");
            tickGenX.AddMajor(Math.Log10(10000), "10k");
            tickGenX.AddMajor(Math.Log10(20000), "20k");
            tickGenX.AddMajor(Math.Log10(50000), "50k");
            tickGenX.AddMajor(Math.Log10(100000), "100k");


            // tell our custom tick generator to use our new label formatter
            //    tickGenX.LabelFormatter = LogTickLabelFormatterX;
            myPlot.Axes.Bottom.TickGenerator = tickGenX;


            // show grid lines for minor ticks
            myPlot.Grid.MajorLineColor = Colors.Black.WithOpacity(.35);
            myPlot.Grid.MajorLineWidth = 1;
            myPlot.Grid.MinorLineColor = Colors.Black.WithOpacity(.15);
            myPlot.Grid.MinorLineWidth = 1;


            //myPlot.Axes.AutoScale();
            SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;
            myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(thd.GraphStartFreq)), Math.Log10(Convert.ToInt32(thd.GraphEndFreq)), Math.Log10(Convert.ToDouble(thd.RangeBottom)) - 0.00000001, Math.Log10(Convert.ToDouble(thd.RangeTop)));  // - 0.000001 to force showing label
            myPlot.Title("Spectrum");
            myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			myPlot.XLabel("Frequency (Hz)");
			myPlot.Axes.Bottom.Label.Alignment = Alignment.MiddleCenter;
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.YLabel("dbV");
			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			// configure tick labels
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			myPlot.Legend.IsVisible = true;
            myPlot.Legend.Alignment = ScottPlot.Alignment.UpperRight;
			myPlot.Legend.Orientation = ScottPlot.Orientation.Vertical;
			myPlot.Legend.FontSize = GraphUtil.PtToPixels(PixelSizes.LEGEND_SIZE);
			myPlot.ShowLegend();

            ScottPlot.AxisRules.MaximumBoundary rule = new(
                xAxis: myPlot.Axes.Bottom,
                yAxis: myPlot.Axes.Left,
                limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
                );

            myPlot.Axes.Rules.Clear();
            myPlot.Axes.Rules.Add(rule);

			fftPlot.Refresh();
        }

        /// <summary>
        /// Plot the THD % graph
        /// </summary>
        /// <param name="data"></param>
        void PlotThd(SpectrumMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
        {
			var specVm = ViewSettings.Singleton.SpectrumVm;
			//QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.FrequencySteps[0].fftData, specVm.LeftChannel, specVm.RightChannel);
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			myPlot.Clear();

			var fftData = MeasurementResult.FrequencySteps[0].fftData;
			bool leftChannelEnabled = specVm.LeftChannel;
			bool rightChannelEnabled = specVm.RightChannel;

			List<double> freqX = [];
			List<double> dBV_Left_Y = [];
			List<double> dBV_Right_Y = [];
			double frequency = 0;

			for (int f = 1; f < fftData.Left.Length; f++)   // Skip dc bin
			{
				frequency += fftData.Df;
				freqX.Add(frequency);
				if (leftChannelEnabled)
					dBV_Left_Y.Add(20 * Math.Log10(fftData.Left[f]));
				if (rightChannelEnabled)
					dBV_Right_Y.Add(20 * Math.Log10(fftData.Right[f]));
			}

			// add a scatter plot to the plot
			double[] logFreqX = freqX.Select(Math.Log10).ToArray();
			double[] logHTot_Left_Y = dBV_Left_Y.ToArray();
			double[] logHTot_Right_Y = dBV_Right_Y.ToArray();

			if (leftChannelEnabled)
			{
				Scatter plotTot_Left = myPlot.Add.Scatter(logFreqX, logHTot_Left_Y);
				plotTot_Left.LineWidth = specVm.ShowThickLines ? _Thickness : 1;
				plotTot_Left.Color = new ScottPlot.Color(1, 97, 170, 255);  // Blue
				plotTot_Left.MarkerSize = 1;
			}

			if (rightChannelEnabled)
			{
				Scatter plotTot_Right = myPlot.Add.Scatter(logFreqX, logHTot_Right_Y);
				plotTot_Right.LineWidth = specVm.ShowThickLines ? _Thickness : 1;
				if (leftChannelEnabled)
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 120); // Red transparant
				else
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 255); // Red
				plotTot_Right.MarkerSize = 1;
			}

			var limitY = myPlot.Axes.GetLimits().YRange.Max;
			var max_dBV_left = leftChannelEnabled ? dBV_Left_Y.Max(f => f) : -150;
			var max_dBV_right = rightChannelEnabled ? dBV_Right_Y.Max(f => f) : -150;
			var max_dBV = (max_dBV_left > max_dBV_right) ? max_dBV_left : max_dBV_right;
			if (max_dBV + 10 > limitY)
			{
				limitY += 10;
				myPlot.Axes.SetLimits(Math.Log10(10), Math.Log10(100000), -150, limitY);
			}

			myPlot.Legend.Orientation = ScottPlot.Orientation.Vertical;
			myPlot.Legend.FontSize = GraphUtil.PtToPixels(PixelSizes.LEGEND_SIZE);

			fftPlot.Refresh();
		}



		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			var thdFreq = ViewSettings.Singleton.SpectrumVm;

			myPlot.Clear();
			//myPlot.Axes.Remove(Edge.Right);

			// create a minor tick generator that places log-distributed minor ticks
			//ScottPlot.TickGenerators. minorTickGen = new();
			//minorTickGen.Divisions = 1;

			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);

            ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
            tickGenY.TargetTickCount = 15;
            tickGenY.MinorTickGenerator = minorTickGen;

            // tell the left axis to use our custom tick generator
            myPlot.Axes.Left.TickGenerator = tickGenY;

            // create a minor tick generator that places log-distributed minor ticks
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenX = new();

            // create a numeric tick generator that uses our custom minor tick generator
            //ScottPlot.TickGenerators.NumericAutomatic tickGenX = new();
            //tickGenX.MinorTickGenerator = minorTickGenX;

            // create a manual tick generator and add ticks
            ScottPlot.TickGenerators.NumericManual tickGenX = new();

            // add major ticks with their labels
            tickGenX.AddMajor(Math.Log10(1), "1");
            tickGenX.AddMajor(Math.Log10(2), "2");
            tickGenX.AddMajor(Math.Log10(5), "5");
            tickGenX.AddMajor(Math.Log10(10), "10");
            tickGenX.AddMajor(Math.Log10(20), "20");
            tickGenX.AddMajor(Math.Log10(50), "50");
            tickGenX.AddMajor(Math.Log10(100), "100");
            tickGenX.AddMajor(Math.Log10(200), "200");
            tickGenX.AddMajor(Math.Log10(500), "500");
            tickGenX.AddMajor(Math.Log10(1000), "1k");
            tickGenX.AddMajor(Math.Log10(2000), "2k");
            tickGenX.AddMajor(Math.Log10(5000), "5k");
            tickGenX.AddMajor(Math.Log10(10000), "10k");
            tickGenX.AddMajor(Math.Log10(20000), "20k");
            tickGenX.AddMajor(Math.Log10(50000), "50k");
            tickGenX.AddMajor(Math.Log10(100000), "100k");

            myPlot.Axes.Bottom.TickGenerator = tickGenX;

            // show grid lines for major ticks
            myPlot.Grid.MajorLineColor = Colors.Black.WithOpacity(.35);
            myPlot.Grid.MajorLineWidth = 1;
            myPlot.Grid.MinorLineColor = Colors.Black.WithOpacity(.15);
            myPlot.Grid.MinorLineWidth = 1;


            //myPlot.Axes.AutoScale();
			myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(thdFreq.GraphStartFreq)), Math.Log10(Convert.ToInt32(thdFreq.GraphEndFreq)), thdFreq.RangeBottomdB, thdFreq.RangeTopdB);

            myPlot.Title("Spectrum");
            myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.TITLE_SIZE);


			myPlot.XLabel("Frequency (Hz)");
			myPlot.Axes.Bottom.Label.Alignment = Alignment.MiddleCenter;
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.YLabel("dBV");
			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			// configure tick labels
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
            
            // Legend
            myPlot.Legend.IsVisible = true;
            myPlot.Legend.Alignment = ScottPlot.Alignment.UpperRight;
			myPlot.Legend.Orientation = ScottPlot.Orientation.Vertical;
			myPlot.Legend.FontSize = GraphUtil.PtToPixels(PixelSizes.LEGEND_SIZE);
			myPlot.ShowLegend();

			ScottPlot.AxisRules.MaximumBoundary rule = new(
                xAxis: myPlot.Axes.Bottom,
                yAxis: myPlot.Axes.Left,
                limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
                );

            myPlot.Axes.Rules.Clear();
            myPlot.Axes.Rules.Add(rule);


            fftPlot.Refresh();
        }


        /// <summary>
        /// Plot the magnitude graph
        /// </summary>
        /// <param name="measurementResult">Data to plot</param>
        void PlotMagnitude(SpectrumMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
        {
			var specVm = ViewSettings.Singleton.SpectrumVm;
			//QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.FrequencySteps[0].fftData, specVm.LeftChannel, specVm.RightChannel);
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			myPlot.Clear();
            var fftData = MeasurementResult.FrequencySteps[0].fftData;
            bool leftChannelEnabled = specVm.ShowLeft;
            bool rightChannelEnabled = specVm.ShowRight;

			List<double> freqX = [];
			List<double> dBV_Left_Y = [];
			List<double> dBV_Right_Y = [];
			double frequency = 0;

			for (int f = 1; f < fftData.Left.Length; f++)   // Skip dc bin
			{
				frequency += fftData.Df;
				freqX.Add(frequency);
				if (leftChannelEnabled)
					dBV_Left_Y.Add(20 * Math.Log10(fftData.Left[f]));
				if (rightChannelEnabled)
					dBV_Right_Y.Add(20 * Math.Log10(fftData.Right[f]));
			}

			// add a scatter plot to the plot
			double[] logFreqX = freqX.Select(Math.Log10).ToArray();
			double[] logHTot_Left_Y = dBV_Left_Y.ToArray();
			double[] logHTot_Right_Y = dBV_Right_Y.ToArray();

			if (leftChannelEnabled)
			{
				Scatter plotTot_Left = myPlot.Add.Scatter(logFreqX, logHTot_Left_Y);
				plotTot_Left.LineWidth = specVm.ShowThickLines ? _Thickness : 1;
				plotTot_Left.Color = new ScottPlot.Color(1, 97, 170, 255);  // Blue
				plotTot_Left.MarkerSize = 1;
			}

			if (rightChannelEnabled)
			{
				Scatter plotTot_Right = myPlot.Add.Scatter(logFreqX, logHTot_Right_Y);
				plotTot_Right.LineWidth = specVm.ShowThickLines ? _Thickness : 1;
				if (leftChannelEnabled)
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 120); // Red transparant
				else
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 255); // Red
				plotTot_Right.MarkerSize = 1;
			}

			var limitY = myPlot.Axes.GetLimits().YRange.Max;
			var max_dBV_left = leftChannelEnabled ? dBV_Left_Y.Max(f => f) : -150;
			var max_dBV_right = rightChannelEnabled ? dBV_Right_Y.Max(f => f) : -150;
			var max_dBV = (max_dBV_left > max_dBV_right) ? max_dBV_left : max_dBV_right;
			if (max_dBV + 10 > limitY)
			{
				limitY += 10;
				myPlot.Axes.SetLimits(Math.Log10(10), Math.Log10(100000), -150, limitY);
			}
			fftPlot.Refresh();
		}


        /// <summary>
        ///  Start measurement button click
        /// </summary>
        public async void StartMeasurement()
        {
            if(MeasurementBusy)
			{
                MessageBox.Show("Device is already running");
				return;
			}
			MeasurementBusy = true;
            ct = new();
			var mSets = ViewSettings.Singleton.SpectrumVm;

			// Clear measurement result
			MeasurementResult = new(mSets)
			{
				CreateDate = DateTime.Now,
				Show = true,                                      // Show in graph
			};

            var rslt = true;
			rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
            var fftsize = mSets.FftSize;
            var sampleRate = mSets.SampleRate;
            var atten = mSets.Attenuation;
            if (rslt)
            {
                await showMessage("Running");
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(250);
                    if (ct.IsCancellationRequested)
                        break;
                    if (mSets.FftSize != fftsize || mSets.SampleRate != sampleRate || mSets.Attenuation != atten)
                    {
						fftsize = mSets.FftSize;
						sampleRate = mSets.SampleRate;
						atten = mSets.Attenuation;
						MeasurementResult = new(mSets)
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
			MeasurementBusy = false;
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

        // user entered a new voltage, update the generator amplitude
        public void UpdateGenAmplitude(string value)
        {
			SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;
            var val = QaLibrary.ParseTextToDouble(value, Convert.ToDouble(thd.Gen1Voltage));
			thd.GeneratorAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.GeneratorUnits, E_VoltageUnit.dBV);
		}

		// user entered a new voltage, update the generator amplitude
		public void UpdateAmpAmplitude(string value)
		{
			SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;
			var val = QaLibrary.ParseTextToDouble(value, thd.OutVoltage);
			thd.AmpOutputAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.OutputUnits, E_VoltageUnit.dBV);
		}

        // show the latest step values in the table
        public void DrawChannelInfoTable()
        {
			SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;
			var vm = ViewSettings.Singleton.ChannelLeft;
            vm.FundamentalFrequency = 0;
            vm.CalculateChannelValues(MeasurementResult.FrequencySteps[0].Left, Convert.ToDouble( thd.Gen1Frequency));
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
                    PlotMagnitude(result, resultNr++, thd.LeftChannel, thd.RightChannel);
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
                    PlotThd(result, resultNr++, thd.ShowLeft, thd.ShowRight);
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