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

    public class ActThdFrequency : ActBase
    {
        public ThdFrequencyData Data { get; set; }                  // Data used in this form instance

        private readonly Views.PlotControl thdPlot;
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;

        private ThdFrequencyMeasurementResult MeasurementResult;


		CancellationTokenSource ct;                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActThdFrequency(ref ThdFrequencyData data, Views.PlotControl graphThd, Views.PlotControl graphFft, Views.PlotControl graphTime)
        {
            Data = data;
            
            fftPlot = graphFft;
			timePlot = graphTime;
			thdPlot = graphThd;

			ct = new CancellationTokenSource();

            // Show empty graphs
            ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
			QaLibrary.InitMiniFftPlot(fftPlot, thd.StartFreq, thd.EndFreq, -150, 20);
            QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

            // TODO: depends on graph settings which graph is shown
            UpdateGraph(true);
        }

		/// <summary>
		/// Update the generator voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateGeneratorVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.ThdFreq;
			vm.GenVoltage = QaLibrary.ConvertVoltage(vm.GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)vm.GeneratorUnits);
        }

		/// <summary>
		/// Update the amplifier output voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateAmpOutputVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.ThdFreq;
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
            var vm = ViewSettings.Singleton.ThdFreq;
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
					//vm.OutPower = MathUtil.ParseTextToDouble(txtAmplifierOutputPower.Text, MeasurementSettings.AmpOutputPower);
					//vm.AmpLoad = MathUtil.ParseTextToDouble(txtOutputLoad.Text, MeasurementSettings.Load);
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

		public void DoCancel()
		{
			ct.Cancel();
		}

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurementSteps(CancellationToken ct)
        {
            ClearPlot();
            //ClearCursorTexts();
                        
            // Clear measurement result
            MeasurementResult = new(ViewSettings.Singleton.ThdFreq)
            {
                CreateDate = DateTime.Now,
                Show = true,                                      // Show in graph
			};
            var thd = MeasurementResult.MeasurementSettings;

			// For now clear measurements to allow only one until we have a UI to manage them.
			Data.Measurements.Clear();

            // Add to list
            Data.Measurements.Add(MeasurementResult);

			// Init mini plots
			QaLibrary.InitMiniFftPlot(fftPlot, thd.StartFreq, thd.EndFreq, -150, 20);
            QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

            // Check if REST interface is available and device connected
            if (await QaLibrary.CheckDeviceConnected() == false)
                return false;
           
            // ********************************************************************  
            // Load a settings we want
            // ********************************************************************  
            await Qa40x.SetDefaults();
            await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make it turn on or off
            await Qa40x.SetSampleRate(thd.SampleRate);
            await Qa40x.SetBufferSize(thd.FftSize);
            await Qa40x.SetWindowing(thd.WindowingMethod.ToString());
            await Qa40x.SetRoundFrequencies(true);

            try
            {
                // ********************************************************************
                // Determine input level
                // ********************************************************************
                double testFrequency = QaLibrary.GetNearestBinFrequency(1000, thd.SampleRate, thd.FftSize);
				E_GeneratorType etp = (E_GeneratorType)thd.MeasureType;
				if (etp == E_GeneratorType.OUTPUT_VOLTAGE || etp == E_GeneratorType.OUTPUT_POWER)     // Based on output
                {
                    double amplifierOutputVoltagedBV = QaLibrary.ConvertVoltage(thd.OutVoltage, (E_VoltageUnit)thd.OutputUnits, E_VoltageUnit.dBV);
                    if (etp == E_GeneratorType.OUTPUT_VOLTAGE)
                        await showMessage($"Determining generator amplitude to get an output amplitude of {amplifierOutputVoltagedBV:0.00#} dBV.");
                    else
                       await showMessage($"Determining generator amplitude to get an output power of {thd.OutPower:0.00#} W.");

                    // Get input voltage based on desired output voltage
                    thd.InputRange = QaLibrary.DetermineAttenuation(amplifierOutputVoltagedBV);
                    double startAmplitude = -40;  // We start a measurement with a 10 mV signal.
                    var result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(startAmplitude, amplifierOutputVoltagedBV, thd.LeftChannel, thd.RightChannel, ct);
                    if (ct.IsCancellationRequested)
                        return false;
                    thd.GeneratorAmplitude = result.Item1;
                    QaLibrary.PlotMiniFftGraph(fftPlot, result.Item2.FreqRslt, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);                                             // Plot fft data in mini graph
                    QaLibrary.PlotMiniTimeGraph(timePlot, result.Item2.TimeRslt, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);                                      // Plot time data in mini graph
                    if (thd.GeneratorAmplitude == -150)
                    {
                        await showMessage($"Could not determine a valid generator amplitude. The amplitude would be {thd.GeneratorAmplitude:0.00#} dBV.");
                        return false;
                    }

                    // Check if cancel button pressed
                    if (ct.IsCancellationRequested)
                        return false;

                    // Check if amplitude found within the generator range
                    if (thd.GeneratorAmplitude < 18)
                    {
                        await showMessage($"Found an input amplitude of {thd.GeneratorAmplitude:0.00#} dBV. Doing second pass.");

                        // 2nd time for extra accuracy
                        result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(thd.GeneratorAmplitude, amplifierOutputVoltagedBV, thd.LeftChannel, thd.RightChannel, ct);
                        if (ct.IsCancellationRequested)
                            return false;
                        thd.GeneratorAmplitude = result.Item1;
                        QaLibrary.PlotMiniFftGraph(fftPlot, result.Item2.FreqRslt, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);                                             // Plot fft data in mini graph
                        QaLibrary.PlotMiniTimeGraph(timePlot, result.Item2.TimeRslt, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);                                      // Plot time data in mini graph
                        if (thd.GeneratorAmplitude == -150)
                        {
                            await showMessage($"Could not determine a valid generator amplitude. The amplitude would be {thd.GeneratorAmplitude:0.00#} dBV.");
                            return false;
                        }
                    }

                    //UpdateGeneratorVoltageDisplay();

                    await showMessage($"Found an input amplitude of {thd.GeneratorAmplitude:0.00#} dBV.");
                }
                else if (etp == E_GeneratorType.INPUT_VOLTAGE)                         // Based on input voltage
                {
                    double genVoltagedBV = QaLibrary.ConvertVoltage(thd.GeneratorAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.dBV);
                    await showMessage($"Determining the best input attenuation for a generator voltage of {genVoltagedBV:0.00#} dBV.");

                    // Determine correct input attenuation
                    var result = await QaLibrary.DetermineAttenuationForGeneratorVoltageWithChirp(genVoltagedBV, QaLibrary.MAXIMUM_DEVICE_ATTENUATION, thd.LeftChannel, thd.RightChannel, ct);
                    if (ct.IsCancellationRequested)
                        return false;
                    thd.InputRange = result.Item1;
                    QaLibrary.PlotMiniFftGraph(fftPlot, result.Item3.FreqRslt, thd.LeftChannel, thd.RightChannel);
                    QaLibrary.PlotMiniTimeGraph(timePlot, result.Item3.TimeRslt, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);

                    await showMessage($"Found correct input attenuation of {thd.InputRange:0} dBV for an amplfier amplitude of {result.Item2:0.00#} dBV.", 500);
                }

                // Set the new input range
                await Qa40x.SetInputRange(thd.InputRange);

                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;

                // ********************************************************************
                // Calculate frequency steps to do
                // ********************************************************************
                var binSize = QaLibrary.CalcBinSize(thd.SampleRate, thd.FftSize);
                // Generate a list of frequencies
                var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(thd.StartFreq, thd.EndFreq, thd.StepsOctave);
                // Translate the generated list to bin center frequencies
                var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, thd.SampleRate, thd.FftSize);
                stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x <= 95500)                // Filter out values that are out of range 
                    .GroupBy(x => x)                                                                    // Filter out duplicates
                    .Select(y => y.First())
                    .ToArray();

                // ********************************************************************
                // Do noise floor measurement
                // ********************************************************************
                await showMessage($"Determining noise floor.");
                await Qa40x.SetOutputSource(OutputSources.Off);
				await Qa40x.DoAcquisition();    // do a single acquisition for settling
				MeasurementResult.NoiseFloor = await QaLibrary.DoAcquisitions(thd.Averages, ct);
                if (ct.IsCancellationRequested)
                    return false;

                // ********************************************************************
                // Step through the list of frequencies
                // ********************************************************************
                for (int f = 0; f < stepBinFrequencies.Length; f++)
                {
                    await showMessage($"Measuring step {f + 1} of {stepBinFrequencies.Length}.");
					await showProgress(100*(f + 1)/ stepBinFrequencies.Length);

                    // Set the generator
                    double amplitudeSetpointdBV = QaLibrary.ConvertVoltage(thd.GeneratorAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.dBV);
                    await Qa40x.SetGen1(stepBinFrequencies[f], amplitudeSetpointdBV, true);
                    if (f == 0)
                        await Qa40x.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset

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
                  
                    // Plot the mini graphs
                    QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);           
                    QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, step.FundamentalFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);

                    step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqRslt.Left, MeasurementResult.NoiseFloor.FreqRslt.Left, thd.AmpLoad);
                    step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqRslt.Right, MeasurementResult.NoiseFloor.FreqRslt.Right, thd.AmpLoad);

                    // Add step data to list
                    MeasurementResult.FrequencySteps.Add(step);

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
            await showMessage( ct.IsCancellationRequested ? $"Measurement cancelled!" : $"Measurement finished!");

            return true;
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
            channelData.Average_NoiseFloor_V = noiseFloorFftData.Skip((int)fundamentalBin + 1).Average();   // Average noise floor in Volts after the fundamental
            channelData.Average_NoiseFloor_dBV = 20 * Math.Log10(channelData.Average_NoiseFloor_V);         // Average noise floor in dBV


            // Reset harmonic distortion variables
            double distortionSqrtTotal = 0;
            double distortionD6plus = 0;

            // Loop through harmonics up tot the 12th
            for (int harmonicNumber = 2; harmonicNumber <= 12; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
            {
                double harmonicFrequency = fundamentalFrequency * harmonicNumber;
                uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

                if (bin >= fftData.Length) break;                                          // Invalid bin, skip harmonic

                double amplitude_V = fftData[bin];
                double amplitude_dBV = 20 * Math.Log10(amplitude_V);
                double thd_Percent = (amplitude_V / channelData.Fundamental_V) * 100;

                HarmonicData harmonic = new()
                {
                    HarmonicNr = harmonicNumber,
                    Frequency = harmonicFrequency,
                    Amplitude_V = amplitude_V,
                    Amplitude_dBV = amplitude_dBV,
                    Thd_Percent = thd_Percent,
                    Thd_dB = 20 * Math.Log10(thd_Percent / 100.0),
                    NoiseAmplitude_V = noiseFloorFftData[bin]
                };

                if (harmonicNumber >= 6)
                    distortionD6plus += Math.Pow(amplitude_V, 2);

                distortionSqrtTotal += Math.Pow(amplitude_V, 2);
                channelData.Harmonics.Add(harmonic);
            }

            // Calculate THD
            if (distortionSqrtTotal != 0)
            {
                channelData.Thd_Percent = (Math.Sqrt(distortionSqrtTotal) / channelData.Fundamental_V) * 100;
                channelData.Thd_dB = 20 * Math.Log10(channelData.Thd_Percent / 100.0);
            }

            // Calculate D6+ (D6 - D12)
            if (distortionD6plus != 0)
            {
                channelData.D6Plus_dBV = 20 * Math.Log10(Math.Sqrt(distortionD6plus));
                channelData.ThdPercent_D6plus = Math.Sqrt(distortionD6plus / Math.Pow(channelData.Fundamental_V, 2)) * 100;
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
            thdPlot.ThePlot.Clear();
            thdPlot.Refresh();
        }

        /// <summary>
        /// Ititialize the THD % plot
        /// </summary>
        void InitializeThdPlot()
        {
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			InitializePctFreqPlot(myPlot);

			var thdFreq = ViewSettings.Singleton.ThdFreq;

            myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(thdFreq.GraphStartFreq)), Math.Log10(Convert.ToInt32(thdFreq.GraphEndFreq)), 
                Math.Log10(Convert.ToDouble(thdFreq.RangeBottom)) - 0.00000001, Math.Log10(Convert.ToDouble(thdFreq.RangeTop)));  // - 0.000001 to force showing label
            myPlot.Title("Distortion vs Frequency (%)");
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("Distortion (%)");
            thdPlot.Refresh();
        }

        /// <summary>
        /// Plot the THD % graph
        /// </summary>
        /// <param name="data"></param>
        void PlotThd(ThdFrequencyMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
        {
            var freqX = new List<double>();
            var hTotY_left = new List<double>();
            var h2Y_left = new List<double>();
            var h3Y_left = new List<double>();
            var h4Y_left = new List<double>();
            var h5Y_left = new List<double>();
            var h6Y_left = new List<double>();
            var noiseY_left = new List<double>();

            var hTotY_right = new List<double>();
            var h2Y_right = new List<double>();
            var h3Y_right = new List<double>();
            var h4Y_right = new List<double>();
            var h5Y_right = new List<double>();
            var h6Y_right = new List<double>();
            var noiseY_right = new List<double>();

			ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;

			foreach (var step in measurementResult.FrequencySteps)
            {
                freqX.Add(step.FundamentalFrequency);

				if (showLeftChannel && thd.LeftChannel)
                {
                    if (step.Left.Harmonics.Count > 0 && thd.ShowTHD)
                        hTotY_left.Add(step.Left.Thd_Percent);
                    if (step.Left.Harmonics.Count > 0 && thd.ShowD2)
                        h2Y_left.Add(step.Left.Harmonics[0].Thd_Percent);
                    if (step.Left.Harmonics.Count > 1 && thd.ShowD3)
                        h3Y_left.Add(   step.Left.Harmonics[1].Thd_Percent);
                    if (step.Left.Harmonics.Count > 2 && thd.ShowD4)
                        h4Y_left.Add(step.Left.Harmonics[2].Thd_Percent);
                    if (step.Left.Harmonics.Count > 3 && thd.ShowD5)
                        h5Y_left.Add(step.Left.Harmonics[3].Thd_Percent);
                    if (step.Left.Harmonics.Count > 4 && step.Left.ThdPercent_D6plus != 0 && thd.ShowD6)
                        h6Y_left.Add(step.Left.ThdPercent_D6plus);        // D6+
                    if (thd.ShowNoiseFloor)
                        noiseY_left.Add((step.Left.Average_NoiseFloor_V / step.Left.Fundamental_V) * 100);
                }

                if (showRightChannel && thd.RightChannel)
                {
                    if (step.Right.Harmonics.Count > 0 && thd.ShowTHD)
                        hTotY_right.Add(step.Right.Thd_Percent);
                    if (step.Right.Harmonics.Count > 0 && thd.ShowD2)
                        h2Y_right.Add(step.Right.Harmonics[0].Thd_Percent);
                    if (step.Right.Harmonics.Count > 1 && thd.ShowD3)
                        h3Y_right.Add(step.Right.Harmonics[1].Thd_Percent);
                    if (step.Right.Harmonics.Count > 2 && thd.ShowD4)
                        h4Y_right.Add(step.Right.Harmonics[2].Thd_Percent);
                    if (step.Right.Harmonics.Count > 3 && thd.ShowD5)
                        h5Y_right.Add(step.Right.Harmonics[3].Thd_Percent);
                    if (step.Right.Harmonics.Count > 4 && step.Right.ThdPercent_D6plus != 0 && thd.ShowD6)
                        h6Y_right.Add(step.Right.ThdPercent_D6plus);        // D6+
                    if (thd.ShowNoiseFloor)
                        noiseY_right.Add((step.Right.Average_NoiseFloor_V / step.Right.Fundamental_V) * 100);
                }
            }

            var colors = new GraphColors();
            float lineWidth = thd.ShowThickLines ? 1.6f : 1;
            float markerSize = thd.ShowPoints ? lineWidth + 3 : 1;
            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            int color = measurementNr * 2;

            void AddPlot(List<double> yValues, string legendText, int colorIndex, LinePattern linePattern)
            {
                if (yValues.Count == 0) return;
                var logYValues = yValues.Select(Math.Log10).ToArray();
                var plot = thdPlot.ThePlot.Add.Scatter(logFreqX, logYValues);
                plot.LineWidth = lineWidth;
                plot.Color = colors.GetColor(colorIndex, color);
                plot.MarkerSize = markerSize;
                plot.LegendText = legendText;
                plot.LinePattern = linePattern;
            }

            if (showLeftChannel)
            {
                AddPlot(hTotY_left, showRightChannel ? "THD-L" : "THD", 8, LinePattern.Solid);
                AddPlot(h2Y_left, showRightChannel ? "D2-L" : "D2", 0, LinePattern.Solid);
                AddPlot(h3Y_left, showRightChannel ? "D3-L" : "D3", 1, LinePattern.Solid);
                AddPlot(h4Y_left, showRightChannel ? "D4-L" : "D4", 2, LinePattern.Solid);
                AddPlot(h5Y_left, showRightChannel ? "D5-L" : "D5", 3, LinePattern.Solid);
                AddPlot(h6Y_left, showRightChannel ? "D6+-L" : "D6+", 4, LinePattern.Solid);
                AddPlot(noiseY_left, showRightChannel ? "Noise-L" : "Noise", 9, showRightChannel ? LinePattern.Solid : LinePattern.Dotted);
            }

            if (showRightChannel)
            {
                AddPlot(hTotY_right, showLeftChannel ? "THD-R" : "THD", 8, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h2Y_right, showLeftChannel ? "D2-R" : "D2", 0, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h3Y_right, showLeftChannel ? "D3-R" : "D3", 1, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h4Y_right, showLeftChannel ? "D4-R" : "D4", 2, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h5Y_right, showLeftChannel ? "D5-R" : "D5", 3, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h6Y_right, showLeftChannel ? "D6+-R" : "D6+", 4, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(noiseY_right, showLeftChannel ? "Noise-R" : "Noise", 9, LinePattern.Dotted);
            }

            thdPlot.Refresh();
        }



        /// <summary>
        /// Initialize the magnitude plot
        /// </summary>
        void InitializeMagnitudePlot()
        {
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
            InitializeMagFreqPlot(myPlot);

			var thdFreq = ViewSettings.Singleton.ThdFreq;

			myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(thdFreq.GraphStartFreq)), Math.Log10(Convert.ToInt32(thdFreq.GraphEndFreq)), 
                thdFreq.RangeBottomdB, thdFreq.RangeTopdB);
            myPlot.Title("Distortion vs Frequency (dB)");
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("Distortion (dB)");

            thdPlot.Refresh();
        }


        /// <summary>
        /// Plot the magnitude graph
        /// </summary>
        /// <param name="measurementResult">Data to plot</param>
        void PlotMagnitude(ThdFrequencyMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
        {
			var thdFreq = ViewSettings.Singleton.ThdFreq;
			var freqX = new List<double>();
         
            var magnY_left = new List<double>();
            var hTotY_left = new List<double>();
            var h2Y_left = new List<double>();
            var h3Y_left = new List<double>();
            var h4Y_left = new List<double>();
            var h5Y_left = new List<double>();
            var h6Y_left = new List<double>();
            var noiseY_left = new List<double>();

            var magnY_right = new List<double>();
            var hTotY_right = new List<double>();
            var h2Y_right = new List<double>();
            var h3Y_right = new List<double>();
            var h4Y_right = new List<double>();
            var h5Y_right = new List<double>();
            var h6Y_right = new List<double>();
            var noiseY_right = new List<double>();

            foreach (var step in measurementResult.FrequencySteps)
            {
                freqX.Add(step.FundamentalFrequency);
               
                if (showLeftChannel && thdFreq.LeftChannel)
                {
                    if (thdFreq.ShowMagnitude)
                        magnY_left.Add(step.Left.Gain_dB);

                    if (step.Left.Harmonics.Count > 0 && thdFreq.ShowTHD)
                        hTotY_left.Add(step.Left.Thd_dB + step.Left.Gain_dB);
                    if (step.Left.Harmonics.Count > 0 && thdFreq.ShowD2)
                        h2Y_left.Add(step.Left.Harmonics[0].Amplitude_dBV - step.Left.Fundamental_dBV + step.Right.Gain_dB);
                    if (step.Left.Harmonics.Count > 1 && thdFreq.ShowD3)
                        h3Y_left.Add(step.Left.Harmonics[1].Amplitude_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
                    if (step.Left.Harmonics.Count > 2 && thdFreq.ShowD4)
                        h4Y_left.Add(step.Left.Harmonics[2].Amplitude_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
                    if (step.Left.Harmonics.Count > 3 && thdFreq.ShowD5)
                        h5Y_left.Add(step.Left.Harmonics[3].Amplitude_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
                    if (step.Left.D6Plus_dBV != 0 && step.Left.Harmonics.Count > 4 && thdFreq.ShowD6)
                        h6Y_left.Add(step.Left.D6Plus_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
                    if (thdFreq.ShowNoiseFloor)
                        noiseY_left.Add(step.Left.Average_NoiseFloor_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
                }

                if (showRightChannel && thdFreq.RightChannel)
                {
                    if (thdFreq.ShowMagnitude)
                        magnY_right.Add(step.Right.Gain_dB);

                    if (step.Right.Harmonics.Count > 0 && thdFreq.ShowTHD)
                        hTotY_right.Add(step.Right.Thd_dB + step.Right.Gain_dB);
                    if (step.Right.Harmonics.Count > 0 && thdFreq.ShowD2)
                        h2Y_right.Add(step.Right.Harmonics[0].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
                    if (step.Right.Harmonics.Count > 1 && thdFreq.ShowD3)
                        h3Y_right.Add(step.Right.Harmonics[1].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
                    if (step.Right.Harmonics.Count > 2 && thdFreq.ShowD4)
                        h4Y_right.Add(step.Right.Harmonics[2].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
                    if (step.Right.Harmonics.Count > 3 && thdFreq.ShowD5)
                        h5Y_right.Add(step.Right.Harmonics[3].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
                    if (step.Right.D6Plus_dBV != 0 && step.Right.Harmonics.Count > 4 && thdFreq.ShowD6)
                        h6Y_right.Add(step.Right.D6Plus_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
                    if (thdFreq.ShowNoiseFloor)
                        noiseY_right.Add(step.Right.Average_NoiseFloor_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
                }
            }

            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            float lineWidth = thdFreq.ShowThickLines ? 1.6f : 1;
            float markerSize = thdFreq.ShowPoints ? lineWidth + 3 : 1;

            var colors = new GraphColors();
            int color = measurementNr * 2;

            void AddPlot(List<double> yValues, string legendText, int colorIndex, LinePattern linePattern)
            {
                if (yValues.Count == 0) return;
                var logYValues = yValues.ToArray();
                var plot = thdPlot.ThePlot.Add.Scatter(logFreqX, logYValues);
                plot.LineWidth = lineWidth;
                plot.Color = colors.GetColor(colorIndex, color);
                plot.MarkerSize = markerSize;
                plot.LegendText = legendText;
                plot.LinePattern = linePattern;
            }

            if (showLeftChannel)
            {
                AddPlot(magnY_left, showRightChannel ? "Magn-L" : "Magn", 9, showRightChannel ? LinePattern.Solid : LinePattern.DenselyDashed);
                AddPlot(hTotY_left, showRightChannel ? "THD-L" : "THD", 8, LinePattern.Solid);
                AddPlot(h2Y_left, showRightChannel ? "H2-L" : "H2", 0, LinePattern.Solid);
                AddPlot(h3Y_left, showRightChannel ? "H3-L" : "H3", 1, LinePattern.Solid);
                AddPlot(h4Y_left, showRightChannel ? "H4-L" : "H4", 2, LinePattern.Solid);
                AddPlot(h5Y_left, showRightChannel ? "H5-L" : "H5", 3, LinePattern.Solid);
                AddPlot(h6Y_left, showRightChannel ? "H6+-L" : "H6+", 4, LinePattern.Solid);
                AddPlot(noiseY_left, showRightChannel ? "Noise-L" : "Noise", 9, showRightChannel ? LinePattern.Solid : LinePattern.Dotted);
            }

            if (showRightChannel)
            {
                AddPlot(magnY_right, showLeftChannel ? "Magn-R" : "Magn", 9, showLeftChannel ? LinePattern.Dotted : LinePattern.DenselyDashed);
                AddPlot(hTotY_right, showLeftChannel ? "THD-R" : "THD", 8, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h2Y_right, showLeftChannel ? "H2-R" : "H2", 0, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h3Y_right, showLeftChannel ? "H3-R" : "H3", 1, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h4Y_right, showLeftChannel ? "H4-R" : "H4", 2, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h5Y_right, showLeftChannel ? "H5-R" : "H5", 3, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(h6Y_right, showLeftChannel ? "H6+-R" : "H6+", 4, showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
                AddPlot(noiseY_right, showLeftChannel ? "Noise-R" : "Noise", 9, LinePattern.Dotted);
            }

            thdPlot.Refresh();
        }


        /// <summary>
        ///  Start measurement button click
        /// </summary>
        public async void StartMeasurement()
        {
			ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
			thd.IsRunning = true;
            ct = new();
            await PerformMeasurementSteps(ct.Token);
            await showMessage("Finished");
            thd.IsRunning = false;
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
			ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
            var val = MathUtil.ParseTextToDouble(value, thd.GenVoltage);
			thd.GeneratorAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.GeneratorUnits, E_VoltageUnit.dBV);
		}

		// user entered a new voltage, update the generator amplitude
		public void UpdateAmpAmplitude(string value)
		{
			ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
			var val = MathUtil.ParseTextToDouble(value, thd.OutVoltage);
			thd.AmpOutputAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.OutputUnits, E_VoltageUnit.dBV);
		}

		public void UpdateGraph(bool settingsChanged)
        {
            thdPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
            int resultNr = 0;
            ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;

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
                    InitializeThdPlot();
                }
          
                foreach (var result in Data.Measurements.Where(m => m.Show))
                {
                    PlotThd(result, resultNr++, thd.ShowLeft, thd.ShowRight);
                }
            }
        }

    }
}