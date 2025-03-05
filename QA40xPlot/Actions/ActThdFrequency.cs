using QA40xPlot.Data;

using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

    public class ActThdFrequency
    {
        public ThdFrequencyData Data { get; set; }                  // Data used in this form instance
        public bool MeasurementBusy { get; set; }                   // Measurement busy state

        private Views.PlotControl? thdPlot;
		private Views.PlotControl? fftPlot;
		private Views.PlotControl? timePlot;
		private int markerIndex = -1;
		private DataPoint markerDataPoint;

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

            AttachPlotMouseEvent();
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
		async Task<bool> PerformMeasurementSteps(CancellationToken ct)
        {
            ClearPlot();
            //ClearCursorTexts();
                        
            var mSets = ViewSettings.Singleton.ThdFreq;

            // Clear measurement result
            MeasurementResult = new()
            {
                CreateDate = DateTime.Now,
                Show = true,                                      // Show in graph
                MeasurementSettings = mSets       // Copy measurment settings to measurement results
			};

            // For now clear measurements to allow only one until we have a UI to manage them.
            Data.Measurements.Clear();

            // Add to list
            Data.Measurements.Add(MeasurementResult);

            UpdateGraphChannelSelectors();

            markerIndex = -1;       // Reset marker

			// Init mini plots
			ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
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
                    QaLibrary.PlotMiniFftGraph(fftPlot, result.Item2.FreqInput, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);                                             // Plot fft data in mini graph
                    QaLibrary.PlotMiniTimeGraph(timePlot, result.Item2.TimeInput, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);                                      // Plot time data in mini graph
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
                        QaLibrary.PlotMiniFftGraph(fftPlot, result.Item2.FreqInput, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);                                             // Plot fft data in mini graph
                        QaLibrary.PlotMiniTimeGraph(timePlot, result.Item2.TimeInput, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);                                      // Plot time data in mini graph
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
                    QaLibrary.PlotMiniFftGraph(fftPlot, result.Item3.FreqInput, thd.LeftChannel, thd.RightChannel);
                    QaLibrary.PlotMiniTimeGraph(timePlot, result.Item3.TimeInput, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);

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
                await Qa40x.DoAcquisition();
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
                        return false;

                    uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
                    if (fundamentalBin >= lrfs.FreqInput.Left.Length)               // Check in bin within range
                        break;


                    ThdFrequencyStep step = new()
                    {
                        FundamentalFrequency = stepBinFrequencies[f],
                        GeneratorVoltage = QaLibrary.ConvertVoltage(amplitudeSetpointdBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
                        fftData = lrfs.FreqInput,
                        timeData = lrfs.TimeInput
                    };
                  
                    // Plot the mini graphs
                    QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqInput, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);           
                    QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeInput, step.FundamentalFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);

                    step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqInput.Left, MeasurementResult.NoiseFloor.FreqInput.Left, thd.AmpLoad);
                    step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqInput.Right, MeasurementResult.NoiseFloor.FreqInput.Right, thd.AmpLoad);

                    // Add step data to list
                    MeasurementResult.FrequencySteps.Add(step);

                    UpdateGraph(false);
                    //ShowLastMeasurementCursorTexts();

                    // Check if cancel button pressed
                    if (ct.IsCancellationRequested)
                    {
                        await Qa40x.SetOutputSource(OutputSources.Off);                                             // Be sure to switch gen off
                        return false;
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
            await showMessage($"Measurement finished!", 500);

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




#if FALSE
        void ShowLastMeasurementCursorTexts()
        {
            if (MeasurementResult == null || MeasurementResult.FrequencySteps.Count == 0) 
                return;

            ThdFrequencyStep step = MeasurementResult.FrequencySteps.Last();

            if (GraphSettings.GraphType == E_ThdFreq_GraphType.DB)
            {
                // Plot current measurement texts
                WriteCursorTexts_dB_L(step.FundamentalFrequency
                    , step.Left.Gain_dB
                    , step.Left.Thd_dB - step.Left.Fundamental_dBV
                    , (step.Left.Harmonics.Count > 0 ? step.Left.Harmonics[0].Amplitude_dBV - step.Left.Fundamental_dBV : 0)   // 2nd harmonic
                    , (step.Left.Harmonics.Count > 1 ? step.Left.Harmonics[1].Amplitude_dBV - step.Left.Fundamental_dBV : 0)
                    , (step.Left.Harmonics.Count > 3 ? step.Left.Harmonics[2].Amplitude_dBV - step.Left.Fundamental_dBV : 0)
                    , (step.Left.Harmonics.Count > 4 ? step.Left.Harmonics[3].Amplitude_dBV - step.Left.Fundamental_dBV : 0)
                    , (step.Left.Harmonics.Count > 5 ? step.Left.D6Plus_dBV - step.Left.Fundamental_dBV : 0)                   // 6+ harmonics
                    , step.Left.Power_Watt
                    , step.Left.Average_NoiseFloor_dBV - step.Left.Fundamental_dBV
                    , MeasurementResult.MeasurementSettings.Load
                    );

                WriteCursorTexts_dB_R(step.FundamentalFrequency
                    , step.Right.Gain_dB
                    , step.Right.Thd_dB - step.Right.Fundamental_dBV
                    , (step.Right.Harmonics.Count > 0 ? step.Right.Harmonics[0].Amplitude_dBV - step.Right.Fundamental_dBV : 0)   // 2nd harmonic
                    , (step.Right.Harmonics.Count > 1 ? step.Right.Harmonics[1].Amplitude_dBV - step.Right.Fundamental_dBV : 0)
                    , (step.Right.Harmonics.Count > 3 ? step.Right.Harmonics[2].Amplitude_dBV - step.Right.Fundamental_dBV : 0)
                    , (step.Right.Harmonics.Count > 4 ? step.Right.Harmonics[3].Amplitude_dBV - step.Right.Fundamental_dBV : 0)
                    , (step.Right.Harmonics.Count > 5 ? step.Right.D6Plus_dBV - step.Right.Fundamental_dBV : 0)                   // 6+ harmonics
                    , step.Right.Power_Watt
                    , step.Right.Average_NoiseFloor_dBV - step.Right.Fundamental_dBV
                    , MeasurementResult.MeasurementSettings.Load
                    );
            }
            else
            {
                // Plot current measurement texts
                WriteCursorTexts_Dpercent_L(step.FundamentalFrequency
                    , step.Left.Gain_dB
                    , step.Left.Thd_Percent
                    , (step.Left.Harmonics.Count > 0 ? step.Left.Harmonics[0].Thd_Percent : 0)                         // 2nd harmonic
                    , (step.Left.Harmonics.Count > 1 ? step.Left.Harmonics[1].Thd_Percent : 0)
                    , (step.Left.Harmonics.Count > 3 ? step.Left.Harmonics[2].Thd_Percent : 0)
                    , (step.Left.Harmonics.Count > 4 ? step.Left.Harmonics[3].Thd_Percent : 0)
                    , (step.Left.Harmonics.Count > 5 ? step.Left.ThdPercent_D6plus : 0)                                // 6+ harmonics
                    , step.Left.Power_Watt
                    , (step.Left.Average_NoiseFloor_V / step.Left.Fundamental_V) * 100
                    , MeasurementResult.MeasurementSettings.Load
                    );

                WriteCursorTexts_Dpercent_R(step.FundamentalFrequency
                    , step.Right.Gain_dB
                    , step.Right.Thd_Percent
                    , (step.Right.Harmonics.Count > 0 ? step.Right.Harmonics[0].Thd_Percent : 0)                         // 2nd harmonic
                    , (step.Right.Harmonics.Count > 1 ? step.Right.Harmonics[1].Thd_Percent : 0)
                    , (step.Right.Harmonics.Count > 3 ? step.Right.Harmonics[2].Thd_Percent : 0)
                    , (step.Right.Harmonics.Count > 4 ? step.Right.Harmonics[3].Thd_Percent : 0)
                    , (step.Right.Harmonics.Count > 5 ? step.Right.ThdPercent_D6plus : 0)                                // 6+ harmonics
                    , step.Right.Power_Watt
                    , (step.Right.Average_NoiseFloor_V / step.Right.Fundamental_V) * 100
                    , MeasurementResult.MeasurementSettings.Load
                    );
            }
        }


        /// <summary>
        /// Write thd percent cursor values to labels
        /// </summary>
        /// <param name="f">Fundamental frequency</param>
        /// <param name="magnitude">Magnitude</param>
        /// <param name="thd">Total harmonic distortion</param>
        /// <param name="D2">Distortion of 2nd harmonic</param>
        /// <param name="D3">Distortion of 3rd harmonic</param>
        /// <param name="D4">Distortion of 4th harmonic</param>
        /// <param name="D5">Distortion of 5th harmonic</param>
        /// <param name="D6">Distortion of 6th harmonic</param>
        /// <param name="dc">The dc component</param>
        /// <param name="power">Amount of power in Watt</param>
        /// <param name="noiseFloor">The noise floor in dB</param>
        /// <param name="load">The amplifier load</param>
        void WriteCursorTexts_Dpercent_L(double f, double magnitude, double thd, double D2, double D3, double D4, double D5, double D6, double power, double noiseFloor, double load)
        {
            lblCursor_Frequency.Text = $"F: {f:0.0 Hz}";
            lblCursor_Magnitude_L.Text = $"{magnitude:0.0# dB}";
            lblCursor_THD_L.Text = $"{thd:0.0000 \\%}";

            lblCursor_D2_L.Text = $"{D2:0.0000 \\%}";
            lblCursor_D3_L.Text = $"{D3:0.0000 \\%}";
            lblCursor_D4_L.Text = $"{D4:0.0000 \\%}";
            lblCursor_D5_L.Text = $"{D5:0.0000 \\%}";
            lblCursor_D6_L.Text = $"{D6:0.0000 \\%}";

            if (power < 1)
                lblCursor_Power_L.Text = $"{power * 1000:0 mW} ({load:0.##} Ω)";
            else
                lblCursor_Power_L.Text = $"{power:0.00# W} ({load:0.##} Ω)";

            lblCursor_NoiseFloor_L.Text = $"{noiseFloor:0.000000 \\%}";

            lblCursor_Frequency.Refresh();
            pnlCursorsLeft.Refresh();
        }

        void WriteCursorTexts_Dpercent_R(double f, double magnitude, double thd, double D2, double D3, double D4, double D5, double D6, double power, double noiseFloor, double load)
        {
            lblCursor_Frequency.Text = $"F: {f:0.0 Hz}";
            lblCursor_Magnitude_R.Text = $"{magnitude:0.0# dB}";
            lblCursor_THD_R.Text = $"{thd:0.0000 \\%}";

            lblCursor_D2_R.Text = $"{D2:0.0000 \\%}";
            lblCursor_D3_R.Text = $"{D3:0.0000 \\%}";
            lblCursor_D4_R.Text = $"{D4:0.0000 \\%}";
            lblCursor_D5_R.Text = $"{D5:0.0000 \\%}";
            lblCursor_D6_R.Text = $"{D6:0.0000 \\%}";

            if (power < 1)
                lblCursor_Power_R.Text = $"{power * 1000:0 mW} ({load:0.##} Ω)";
            else
                lblCursor_Power_R.Text = $"{power:0.00# W} ({load:0.##} Ω)";

            lblCursor_NoiseFloor_R.Text = $"{noiseFloor:0.000000 \\%}";

            lblCursor_Frequency.Refresh();
            pnlCursorsRight.Refresh();
        }

        /// <summary>
        /// Write thd dB cursor values to labels
        /// </summary>
        /// <param name="f">Fundamental frequency</param>
        /// <param name="magnitude">Magnitude</param>
        /// <param name="thd">Total harmonic distortion</param>
        /// <param name="D2">Distortion of 2nd harmonic</param>
        /// <param name="D3">Distortion of 3rd harmonic</param>
        /// <param name="D4">Distortion of 4th harmonic</param>
        /// <param name="D5">Distortion of 5th harmonic</param>
        /// <param name="D6">Distortion of 6th harmonic</param>
        /// <param name="dc">The dc component</param>
        /// <param name="power">Amount of power in Watt</param>
        /// <param name="noiseFloor">The noise floor in dB</param>
        /// <param name="load">The amplifier load</param>
        void WriteCursorTexts_dB_L(double f, double magnitude, double thd, double D2, double D3, double D4, double D5, double D6, double power, double noiseFloor, double load)
        {
            lblCursor_Frequency.Text = $"F: {f:0.0 Hz}";
            lblCursor_Magnitude_L.Text = $"{magnitude:0.0# dB}";
            lblCursor_THD_L.Text = $"{thd:0.0# dB}";

            lblCursor_D2_L.Text = $"{D2:0.0# dB}";
            lblCursor_D3_L.Text = $"{D3:0.0# dB}";
            lblCursor_D4_L.Text = $"{D4:0.0# dB}";
            lblCursor_D5_L.Text = $"{D5:0.0# dB}";
            lblCursor_D6_L.Text = $"{D6:0.0# dB}";

            if (power < 1)
                lblCursor_Power_L.Text = $"{power * 1000:0 mW} ({load:0.##} Ω)";
            else
                lblCursor_Power_L.Text = $"{power:0.00# W} ({load:0.##} Ω)";

            lblCursor_NoiseFloor_L.Text = $"{noiseFloor:0.0# dB}";

            lblCursor_Frequency.Refresh();
            pnlCursorsLeft.Refresh();
        }

        void WriteCursorTexts_dB_R(double f, double magnitude, double thd, double D2, double D3, double D4, double D5, double D6, double power, double noiseFloor, double load)
        {
            lblCursor_Frequency.Text = $"F: {f:0.0 Hz}";
            lblCursor_Magnitude_R.Text = $"{magnitude:0.0# dB}";
            lblCursor_THD_R.Text = $"{thd:0.0# dB}";

            lblCursor_D2_R.Text = $"{D2:0.0# dB}";
            lblCursor_D3_R.Text = $"{D3:0.0# dB}";
            lblCursor_D4_R.Text = $"{D4:0.0# dB}";
            lblCursor_D5_R.Text = $"{D5:0.0# dB}";
            lblCursor_D6_R.Text = $"{D6:0.0# dB}";

            if (power < 1)
                lblCursor_Power_R.Text = $"{power * 1000:0 mW} ({load:0.##} Ω)";
            else
                lblCursor_Power_R.Text = $"{power:0.00# W} ({load:0.##} Ω)";

            lblCursor_NoiseFloor_R.Text = $"{noiseFloor:0.0# dB}";

            lblCursor_Frequency.Refresh();
            pnlCursorsRight.Refresh();
        }

        void ClearCursorTexts()
        {
            if (GraphSettings.GraphType == E_ThdFreq_GraphType.DB)
            {
                WriteCursorTexts_dB_L(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                WriteCursorTexts_dB_R(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
            else
            {
                WriteCursorTexts_Dpercent_L(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                WriteCursorTexts_Dpercent_R(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
        }
#endif
             

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
            ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
            myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(thd.GraphStartFreq)), Math.Log10(Convert.ToInt32(thd.GraphEndFreq)), Math.Log10(Convert.ToDouble(thd.RangeBottom)) - 0.00000001, Math.Log10(Convert.ToDouble(thd.RangeTop)));  // - 0.000001 to force showing label
            myPlot.Title("Distortion vs Frequency (%)");
            myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			myPlot.XLabel("Frequency (Hz)");
			myPlot.Axes.Bottom.Label.Alignment = Alignment.MiddleCenter;
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.YLabel("Distortion (%)");
			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			// configure tick labels
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			myPlot.Legend.IsVisible = true;
            myPlot.Legend.Orientation = ScottPlot.Orientation.Horizontal;
            myPlot.Legend.Alignment = ScottPlot.Alignment.UpperRight;
            myPlot.ShowLegend();

            ScottPlot.AxisRules.MaximumBoundary rule = new(
                xAxis: myPlot.Axes.Bottom,
                yAxis: myPlot.Axes.Left,
                limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
                );

            myPlot.Axes.Rules.Clear();
            myPlot.Axes.Rules.Add(rule);

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

            if (markerIndex != -1)
                QaLibrary.PlotCursorMarker(thdPlot, lineWidth, LinePattern.Solid, markerDataPoint);

            thdPlot.Refresh();
        }



        /// <summary>
        /// Initialize the magnitude plot
        /// </summary>
        void InitializeMagnitudePlot()
        {
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			var thdFreq = ViewSettings.Singleton.ThdFreq;

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

            myPlot.Title("Distortion vs Frequency (dB)");
            myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.TITLE_SIZE);


			myPlot.XLabel("Frequency (Hz)");
			myPlot.Axes.Bottom.Label.Alignment = Alignment.MiddleCenter;
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.YLabel("Distortion (dB)");
			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			// configure tick labels
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
            
            // Legend
            myPlot.Legend.IsVisible = true;
            myPlot.Legend.Orientation = ScottPlot.Orientation.Horizontal;
            myPlot.Legend.Alignment = ScottPlot.Alignment.UpperRight;
			myPlot.ShowLegend();

			ScottPlot.AxisRules.MaximumBoundary rule = new(
                xAxis: myPlot.Axes.Bottom,
                yAxis: myPlot.Axes.Left,
                limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
                );

            myPlot.Axes.Rules.Clear();
            myPlot.Axes.Rules.Add(rule);


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

            //if (thdPlot!= null && markerIndex != -1)
            //    QaLibrary.PlotCursorMarker(thdPlot, lineWidth, LinePattern.Solid, markerDataPoint);

            thdPlot.Refresh();
        }

        void UpdateGraphChannelSelectors()
        {
            //if (MeasurementSettings.EnableLeftChannel && !MeasurementSettings.EnableRightChannel)
            //{
            //    // Only single channel. Enable
            //    chkGraphShowLeftChannel.Checked = true;
            //    chkGraphShowRightChannel.Checked = false;
            //    thd.ShowLeft = true;
            //    thd.ShowRight = false;
            //}
            //if (MeasurementSettings.EnableRightChannel && !MeasurementSettings.EnableLeftChannel)
            //{
            //    chkGraphShowLeftChannel.Checked = false;
            //    chkGraphShowRightChannel.Checked = true;
            //    thd.ShowLeft = false;
            //    thd.ShowRight = true;
            //}
            //chkGraphShowLeftChannel.Enabled = MeasurementSettings.EnableLeftChannel && MeasurementSettings.EnableRightChannel;
            //chkGraphShowRightChannel.Enabled = MeasurementSettings.EnableLeftChannel && MeasurementSettings.EnableRightChannel;
            //pnlCursorsLeft.Visible = thd.ShowLeft;
            //pnlCursorsRight.Visible = thd.ShowRight;
        }

        /// <summary>
        /// Attach mouse events to the main graph
        /// </summary>
        void AttachPlotMouseEvent()
        {
			ScottPlot.Plot myPlot = thdPlot.ThePlot;

			// Attach the mouse move event
			//myPlot.MouseMove += (s, e) =>
   //         {
   //             ShowCursorMiniGraphs(s, e);
   //             SetCursorMarker(s, e, false);
   //         };

			//// Mouse is clicked
			//myPlot.MouseDown += (s, e) =>
   //         {
   //             SetCursorMarker(s, e, true);      // Set fixed marker
   //         };

			// Mouse is leaving the graph
			//myPlot.SKControl.MouseLeave += (s, e) =>
   //         {
   //             // If persistent marker set then show mini plots of that marker
   //             if (markerIndex >= 0)
   //             {
   //                 QaLibrary.PlotMiniFftGraph(graphFft, MeasurementResult.FrequencySteps[markerIndex].fftData, MeasurementResult.MeasurementSettings.EnableLeftChannel && thd.ShowLeft, MeasurementResult.MeasurementSettings.EnableRightChannel && thd.ShowRight);
   //                 QaLibrary.PlotMiniTimeGraph(graphTime, MeasurementResult.FrequencySteps[markerIndex].timeData, MeasurementResult.FrequencySteps[markerIndex].FundamentalFrequency, MeasurementResult.MeasurementSettings.EnableLeftChannel && thd.ShowLeft, MeasurementResult.MeasurementSettings.EnableRightChannel && thd.ShowRight);
   //             }
   //         };
        }

        /// <summary>
        /// Show the mini graphs for the frequency where the mouse cursor is pointing to
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        //     void ShowCursorMiniGraphs(object s, MouseEventArgs e)
        //     {
        //ScottPlot.Plot myPlot = thdPlot.ThePlot;

        //if (MeasurementBusy)
        //             return;         // Still busy

        //         // determine where the mouse is and get the nearest point
        //         Pixel mousePixel = new(e.Location.X, e.Location.Y);
        //         Coordinates mouseLocation = myPlot.GetCoordinates(mousePixel);
        //         if (myPlot.GetPlottables<Scatter>().Count() == 0)
        //             return;

        //         DataPoint nearest1 = myPlot.GetPlottables<Scatter>().First().Data.GetNearestX(mouseLocation, myPlot.LastRender);

        //         // place the crosshair over the highlighted point
        //         if (nearest1.IsReal && MeasurementResult.FrequencySteps.Count > nearest1.Index)
        //         {
        //             QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.FrequencySteps[nearest1.Index].fftData, MeasurementResult.MeasurementSettings.EnableLeftChannel && thd.ShowLeft, MeasurementResult.MeasurementSettings.EnableRightChannel && thd.ShowRight);
        //             QaLibrary.PlotMiniTimeGraph(timePlot, MeasurementResult.FrequencySteps[nearest1.Index].timeData, MeasurementResult.FrequencySteps[nearest1.Index].FundamentalFrequency, MeasurementResult.MeasurementSettings.EnableLeftChannel && thd.ShowLeft, MeasurementResult.MeasurementSettings.EnableRightChannel && thd.ShowRight);
        //         }
        //     }

        /// <summary>
        /// Draw a cursor marker and update cursor texts
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="isClick"></param>
        //     void SetCursorMarker(object s, MouseEventArgs e, bool isClick)
        //     {
        //ScottPlot.Plot myPlot = thdPlot.ThePlot;

        //if (MeasurementBusy)
        //             return;

        //         // determine where the mouse is and get the nearest point
        //         Pixel mousePixel = new(e.Location.X, e.Location.Y);
        //         Coordinates mouseLocation = myPlot.GetCoordinates(mousePixel);
        //         if (myPlot.GetPlottables<Scatter>().Count() == 0)
        //             return;
        //         DataPoint nearest1 = myPlot.GetPlottables<Scatter>().First().Data.GetNearestX(mouseLocation, myPlot.LastRender);

        //         // place the crosshair over the highlighted point
        //         if (nearest1.IsReal)
        //         {
        //             float lineWidth = (GraphSettings.ThickLines ? 1.6f : 1);
        //             LinePattern linePattern = LinePattern.DenselyDashed;

        //             if (isClick)
        //             {
        //                 // Mouse click
        //                 if (nearest1.Index == markerIndex)
        //                 {
        //                     // Remove marker
        //                     markerIndex = -1;
        //                     myPlot.Remove<Crosshair>();
        //                     return;
        //                 }
        //                 else
        //                 {
        //                     markerIndex = nearest1.Index;
        //                     markerDataPoint = nearest1;
        //                     linePattern = LinePattern.Solid;
        //                 }
        //             }
        //             else
        //             {
        //                 // Mouse hoover
        //                 if (markerIndex != -1)
        //                     return;                     // Do not show new marker. There is already a clicked marker
        //             }

        //             QaLibrary.PlotCursorMarker(thdPlot, lineWidth, linePattern, nearest1);


        //             if (MeasurementResult.FrequencySteps.Count > nearest1.Index)
        //             {
        //                 ThdFrequencyStep step = MeasurementResult.FrequencySteps[nearest1.Index];

        //                 if (GraphSettings.GraphType == E_ThdFreq_GraphType.DB)
        //                 {
        //                     WriteCursorTexts_dB_L(step.FundamentalFrequency
        //                     , step.Left.Gain_dB
        //                     , step.Left.Thd_dB - step.Left.Fundamental_dBV
        //                     , (step.Left.Harmonics.Count > 0 ? step.Left.Harmonics[0].Amplitude_dBV - step.Left.Fundamental_dBV : 0)   // 2nd harmonic
        //                     , (step.Left.Harmonics.Count > 1 ? step.Left.Harmonics[1].Amplitude_dBV - step.Left.Fundamental_dBV : 0)
        //                     , (step.Left.Harmonics.Count > 2 ? step.Left.Harmonics[2].Amplitude_dBV - step.Left.Fundamental_dBV : 0)
        //                     , (step.Left.Harmonics.Count > 3 ? step.Left.Harmonics[3].Amplitude_dBV - step.Left.Fundamental_dBV : 0)
        //                     , (step.Left.Harmonics.Count > 4 ? step.Left.D6Plus_dBV - step.Left.Fundamental_dBV : 0)                   // 6+ harmonics
        //                     , step.Left.Power_Watt
        //                     , step.Left.Average_NoiseFloor_dBV - step.Left.Fundamental_dBV
        //                     , MeasurementResult.MeasurementSettings.Load
        //                     );

        //                     WriteCursorTexts_dB_R(step.FundamentalFrequency
        //                     , step.Right.Gain_dB
        //                     , step.Right.Thd_dB - step.Right.Fundamental_dBV
        //                     , (step.Right.Harmonics.Count > 0 ? step.Right.Harmonics[0].Amplitude_dBV - step.Right.Fundamental_dBV : 0)   // 2nd harmonic
        //                     , (step.Right.Harmonics.Count > 1 ? step.Right.Harmonics[1].Amplitude_dBV - step.Right.Fundamental_dBV : 0)
        //                     , (step.Right.Harmonics.Count > 2 ? step.Right.Harmonics[2].Amplitude_dBV - step.Right.Fundamental_dBV : 0)
        //                     , (step.Right.Harmonics.Count > 3 ? step.Right.Harmonics[3].Amplitude_dBV - step.Right.Fundamental_dBV : 0)
        //                     , (step.Right.Harmonics.Count > 4 ? step.Right.D6Plus_dBV - step.Right.Fundamental_dBV : 0)                   // 6+ harmonics
        //                     , step.Right.Power_Watt
        //                     , step.Right.Average_NoiseFloor_dBV - step.Right.Fundamental_dBV
        //                     , MeasurementResult.MeasurementSettings.Load
        //                     );
        //                 }
        //                 else
        //                 {
        //                     WriteCursorTexts_Dpercent_L(step.FundamentalFrequency
        //                     , step.Left.Gain_dB
        //                     , step.Left.Thd_Percent
        //                     , (step.Left.Harmonics.Count > 0 ? step.Left.Harmonics[0].Thd_Percent : 0)     // 2nd harmonic
        //                     , (step.Left.Harmonics.Count > 1 ? step.Left.Harmonics[1].Thd_Percent : 0)
        //                     , (step.Left.Harmonics.Count > 2 ? step.Left.Harmonics[2].Thd_Percent : 0)
        //                     , (step.Left.Harmonics.Count > 3 ? step.Left.Harmonics[3].Thd_Percent : 0)
        //                     , (step.Left.Harmonics.Count > 4 ? step.Left.ThdPercent_D6plus : 0)                   // 6+ harmonics
        //                     , step.Left.Power_Watt
        //                     , (step.Left.Average_NoiseFloor_V / step.Left.Fundamental_V) * 100
        //                     , MeasurementResult.MeasurementSettings.Load
        //                     );

        //                     WriteCursorTexts_Dpercent_R(step.FundamentalFrequency
        //                     , step.Right.Gain_dB
        //                     , step.Right.Thd_Percent
        //                     , (step.Right.Harmonics.Count > 0 ? step.Right.Harmonics[0].Thd_Percent : 0)     // 2nd harmonic
        //                     , (step.Right.Harmonics.Count > 1 ? step.Right.Harmonics[1].Thd_Percent : 0)
        //                     , (step.Right.Harmonics.Count > 2 ? step.Right.Harmonics[2].Thd_Percent : 0)
        //                     , (step.Right.Harmonics.Count > 3 ? step.Right.Harmonics[3].Thd_Percent : 0)
        //                     , (step.Right.Harmonics.Count > 4 ? step.Right.ThdPercent_D6plus : 0)                   // 6+ harmonics
        //                     , step.Right.Power_Watt
        //                     , (step.Right.Average_NoiseFloor_V / step.Right.Fundamental_V) * 100
        //                     , MeasurementResult.MeasurementSettings.Load
        //                     );
        //                 }


        //             }

        //         }
        //     }


        /// <summary>
        ///  Start measurement button click
        /// </summary>
        public async void StartMeasurement()
        {
            MeasurementBusy = true;
            ct = new();
            await PerformMeasurementSteps(ct.Token);
            await showMessage("Finished");
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
			ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
            var val = QaLibrary.ParseTextToDouble(value, thd.GenVoltage);
			thd.GeneratorAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.GeneratorUnits, E_VoltageUnit.dBV);
		}

		// user entered a new voltage, update the generator amplitude
		public void UpdateAmpAmplitude(string value)
		{
			ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
			var val = QaLibrary.ParseTextToDouble(value, thd.OutVoltage);
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
                    //gbdB_Range.Visible = true;
                    //btnGraph_dB.BackColor = System.Drawing.Color.Cornsilk;
                    //gbD_Range.Visible = false;
                    //btnGraph_D_Percent.BackColor = System.Drawing.Color.WhiteSmoke;
                    //chkShowMagnitude.Enabled = true;
                    //lblCursorMagnitude.Visible = true;
                    //lblCursor_Magnitude_L.Visible = true;
                    //lblCursor_Magnitude_R.Visible = true;

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
                    //gbdB_Range.Visible = false;
                    //btnGraph_dB.BackColor = System.Drawing.Color.WhiteSmoke;
                    //gbD_Range.Visible = true;
                    //btnGraph_D_Percent.BackColor = System.Drawing.Color.Cornsilk;
                    //chkShowMagnitude.Enabled = false;
                    //lblCursorMagnitude.Visible = false;
                    //lblCursor_Magnitude_L.Visible = false;
                    //lblCursor_Magnitude_R.Visible = false;

                    InitializeThdPlot();
                }
          
                foreach (var result in Data.Measurements.Where(m => m.Show))
                {
                    PlotThd(result, resultNr++, thd.ShowLeft, thd.ShowRight);
                }
            }
        }

#if FALSE
        /// <summary>
        /// THD % y axis fit click request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFitDGraphY_Click(object sender, EventArgs e)
        {
            if (MeasurementResult.FrequencySteps.Count == 0)
                return;

            // Determine top Y
            double maxThd_left = MeasurementResult.FrequencySteps.Max(d => d.Left.Thd_Percent);
            double maxThd_right = MeasurementResult.FrequencySteps.Max(d => d.Right.Thd_Percent);
            double max = Math.Max(maxThd_left, maxThd_right);

            if (max <= 1)
                cmbD_Graph_Top.SelectedIndex = 2;
            else if (max <= 10)
                cmbD_Graph_Top.SelectedIndex = 1;
            else
                cmbD_Graph_Top.SelectedIndex = 0;


            // Determine bottom Y
            double minThd_left = MeasurementResult.FrequencySteps.Min(d => (d.Left.Average_NoiseFloor_V / d.Left.Fundamental_V) * 100);
            double minD2_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 1).Min(d => (d.Left.Harmonics[0].Amplitude_V / d.Left.Fundamental_V) * 100);
            double minD3_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 2).Min(d => (d.Left.Harmonics[1].Amplitude_V / d.Left.Fundamental_V) * 100);
            double minD4_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 3).Min(d => (d.Left.Harmonics[2].Amplitude_V / d.Left.Fundamental_V) * 100);
            double minD5_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 4).Min(d => (d.Left.Harmonics[3].Amplitude_V / d.Left.Fundamental_V) * 100);
            double minD6_left = MeasurementResult.FrequencySteps.Min(d => d.Left.ThdPercent_D6plus);

            double min = Math.Min(minThd_left, minD2_left);
            min = Math.Min(min, minD3_left);
            min = Math.Min(min, minD4_left);
            min = Math.Min(min, minD5_left);

            double minThd_right = MeasurementResult.FrequencySteps.Min(d => (d.Right.Average_NoiseFloor_V / d.Right.Fundamental_V) * 100);
            double minD2_right = MeasurementResult.FrequencySteps.Where(d => d.Right.Harmonics.Count >= 1).Min(d => (d.Right.Harmonics[0].Amplitude_V / d.Right.Fundamental_V) * 100);
            double minD3_right = MeasurementResult.FrequencySteps.Where(d => d.Right.Harmonics.Count >= 2).Min(d => (d.Right.Harmonics[1].Amplitude_V / d.Right.Fundamental_V) * 100);
            double minD4_right = MeasurementResult.FrequencySteps.Where(d => d.Right.Harmonics.Count >= 3).Min(d => (d.Right.Harmonics[2].Amplitude_V / d.Right.Fundamental_V) * 100);
            double minD5_right = MeasurementResult.FrequencySteps.Where(d => d.Right.Harmonics.Count >= 4).Min(d => (d.Right.Harmonics[3].Amplitude_V / d.Right.Fundamental_V) * 100);
            double minD6_right = MeasurementResult.FrequencySteps.Min(d => d.Right.ThdPercent_D6plus);

            min = Math.Min(min, minThd_right);
            min = Math.Min(min, minD2_right);
            min = Math.Min(min, minD3_right);
            min = Math.Min(min, minD4_right);
            min = Math.Min(min, minD5_right);

            if (min > 0.1)
                cmbD_Graph_Bottom.SelectedIndex = 0;
            else if (min > 0.01)
                cmbD_Graph_Bottom.SelectedIndex = 1;
            else if (min > 0.001)
                cmbD_Graph_Bottom.SelectedIndex = 2;
            else if (min > 0.0001)
                cmbD_Graph_Bottom.SelectedIndex = 3;
            else if (min > 0.00001)
                cmbD_Graph_Bottom.SelectedIndex = 4;
            else
                cmbD_Graph_Bottom.SelectedIndex = 5;
        }

        /// <summary>
        /// Magnitude y axis fit click request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFitDbGraphY_Click(object sender, EventArgs e)
        {

            if (MeasurementResult.FrequencySteps.Count == 0)
                return;

            // Determine top Y
            double maxGain_left = MeasurementResult.FrequencySteps.Max(d => d.Left.Gain_dB);
            double maxGain_right = MeasurementResult.FrequencySteps.Max(d => d.Right.Gain_dB);
            double max = Math.Max(maxGain_left, maxGain_right);
            ud_dB_Graph_Top.Value = (Math.Ceiling((decimal)((int)max) / 10) * 10) + 20;


            // Determine bottom Y
            double minDb_left = MeasurementResult.FrequencySteps.Min(d => d.Left.Average_NoiseFloor_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD2_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 1).Min(d => d.Left.Harmonics[0].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD3_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 2).Min(d => d.Left.Harmonics[1].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD4_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 3).Min(d => d.Left.Harmonics[2].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD5_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 4).Min(d => d.Left.Harmonics[3].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD6_left = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 5).Min(d => d.Left.Harmonics[4].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            
            double min = Math.Min(minDb_left, minD2_left);
            min = Math.Min(min, minD3_left);
            min = Math.Min(min, minD4_left);
            min = Math.Min(min, minD5_left);

            double minDb_right = MeasurementResult.FrequencySteps.Min(d => d.Left.Average_NoiseFloor_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD2_right = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 1).Min(d => d.Left.Harmonics[0].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD3_right = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 2).Min(d => d.Left.Harmonics[1].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD4_right = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 3).Min(d => d.Left.Harmonics[2].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD5_right = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 4).Min(d => d.Left.Harmonics[3].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double minD6_right = MeasurementResult.FrequencySteps.Where(d => d.Left.Harmonics.Count >= 5).Min(d => d.Left.Harmonics[4].Amplitude_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;

            min = Math.Min(min, minDb_right);
            min = Math.Min(min, minD2_right);
            min = Math.Min(min, minD3_right);
            min = Math.Min(min, minD4_right);
            min = Math.Min(min, minD5_right);

            ud_dB_Graph_Bottom.Value = Math.Ceiling((decimal)((int)(min / 10) - 1) * 10);        // Round to 10, subtract 10
            
            /*
            double min_left = MeasurementResult.FrequencySteps.Min(d => d.Left.Average_NoiseFloor_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Left.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Left.Gain_dB;
            double min_right = MeasurementResult.FrequencySteps.Min(d => d.Left.Average_NoiseFloor_dBV) - MeasurementResult.FrequencySteps.Average(d => d.Right.Fundamental_dBV) + MeasurementResult.FrequencySteps[0].Right.Gain_dB;
            double min = Math.Min(min_left, min_right);
            ud_dB_Graph_Bottom.Value = Math.Ceiling(( (decimal)(int)(min / 10) - 1) * 10);        // Round to 10, subtract 10
            */
        }

        /// <summary>
        ///  Cancel measurement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStopMeasurement_Click(object sender, EventArgs e)
        {
            ct.Cancel();
        }

        private void lblCursor_Power_Click(object sender, EventArgs e)
        {

        }

        private void chkEnableLeftChannel_CheckedChanged(object sender, EventArgs e)
        {
            MeasurementSettings.EnableLeftChannel = chkEnableLeftChannel.Checked;
        }

        private void chkEnableRightChannel_CheckedChanged(object sender, EventArgs e)
        {
            MeasurementSettings.EnableRightChannel = chkEnableRightChannel.Checked;
        }

        private void cmbD_Graph_Top_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbD_Graph_Top.SelectedIndex != -1)
                GraphSettings.D_PercentTop = (double)((KeyValuePair<double, string>)cmbD_Graph_Top.SelectedItem).Key;
            UpdateGraph(true);
        }

        private void cmbD_Graph_Bottom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbD_Graph_Bottom.SelectedIndex != -1)
                GraphSettings.D_PercentBottom = (double)((KeyValuePair<double, string>)cmbD_Graph_Bottom.SelectedItem).Key;
            UpdateGraph(true);
        }

        private void ud_dB_Graph_Top_ValueChanged(object sender, EventArgs e)
        {
            // Prevent top lower than bottom
            if (ud_dB_Graph_Top.Value - ud_dB_Graph_Bottom.Value < 1)
            {
                ud_dB_Graph_Top.Value = ud_dB_Graph_Bottom.Value + 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.DbRangeTop = (double)ud_dB_Graph_Top.Value;
            if (GraphSettings.DbRangeTop - GraphSettings.DbRangeBottom < 20)
            {
                ud_dB_Graph_Top.Increment = 1;
                ud_dB_Graph_Bottom.Increment = 1;
            }
            else if (GraphSettings.DbRangeTop - GraphSettings.DbRangeBottom < 50)
            {
                ud_dB_Graph_Top.Increment = 5;
                ud_dB_Graph_Bottom.Increment = 5;
            }
            else
            {
                ud_dB_Graph_Top.Increment = 10;
                ud_dB_Graph_Bottom.Increment = 10;
            }
            UpdateGraph(true);
        }

        private void ud_dB_Graph_Bottom_ValueChanged(object sender, EventArgs e)
        {
            // Prevent bottom higher than top
            if (ud_dB_Graph_Top.Value - ud_dB_Graph_Bottom.Value < 1)
            {
                ud_dB_Graph_Bottom.Value = ud_dB_Graph_Top.Value - 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.DbRangeBottom = (double)ud_dB_Graph_Bottom.Value;
            if (GraphSettings.DbRangeTop - GraphSettings.DbRangeBottom < 20)
            {
                ud_dB_Graph_Top.Increment = 1;
                ud_dB_Graph_Bottom.Increment = 1;
            }
            else if (GraphSettings.DbRangeTop - GraphSettings.DbRangeBottom < 50)
            {
                ud_dB_Graph_Top.Increment = 5;
                ud_dB_Graph_Bottom.Increment = 5;
            }
            else
            {
                ud_dB_Graph_Top.Increment = 10;
                ud_dB_Graph_Bottom.Increment = 10;
            }
            UpdateGraph(true);
        }

        private void cmbGraph_FreqStart_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbGraph_FreqStart.SelectedIndex != -1)
                GraphSettings.FrequencyRange_Start = (uint)((KeyValuePair<double, string>)cmbGraph_FreqStart.SelectedItem).Key;
            UpdateGraph(true);
        }

        private void cmbGraph_FreqEnd_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbGraph_FreqEnd.SelectedIndex != -1)
                GraphSettings.FrequencyRange_End = (uint)((KeyValuePair<double, string>)cmbGraph_FreqEnd.SelectedItem).Key;
            UpdateGraph(true);
        }

        private void chkShowMagnitude_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowMagnitude = chkShowMagnitude.Checked;
            UpdateGraph(true);
        }

        private void chkShowThd_CheckedChanged_1(object sender, EventArgs e)
        {
            GraphSettings.ShowTHD = chkShowThd.Checked;
            UpdateGraph(true);
        }

        private void chkShowD2_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowD2 = chkShowD2.Checked;
            UpdateGraph(true);
        }

        private void chkShowD3_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowD3 = chkShowD3.Checked;
            UpdateGraph(true);
        }

        private void chkShowD4_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowD4 = chkShowD4.Checked;
            UpdateGraph(true);
        }

        private void chkShowD5_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowD5 = chkShowD5.Checked;
            UpdateGraph(true);
        }

        private void chkShowD6_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowD6 = chkShowD6.Checked;
            UpdateGraph(true);
        }

        private void chkShowNoiseFloor_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowNoiseFloor = chkShowNoiseFloor.Checked;
            UpdateGraph(true);
        }

        private void chkThickLines_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ThickLines = chkThickLines.Checked;
            UpdateGraph(true);
        }

        private void chkShowDataPoints_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowDataPoints = chkShowDataPoints.Checked;
            UpdateGraph(true);
        }

        /// <summary>
        /// Magnitude graph button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGraph_dB_Click(object sender, EventArgs e)
        {
            GraphSettings.GraphType = E_ThdFreq_GraphType.DB;
            ClearCursorTexts();
            UpdateGraph(true);
        }

        /// <summary>
        /// THD % graph button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGraph_D_Click(object sender, EventArgs e)
        {
            GraphSettings.GraphType = E_ThdFreq_GraphType.D_PERCENT;
            ClearCursorTexts();
            UpdateGraph(true);
        }

        private void chkGraphShowLeftChannel_CheckedChanged(object sender, EventArgs e)
        {
            thd.ShowLeft = chkGraphShowLeftChannel.Checked;
            UpdateGraphChannelSelectors();
            UpdateGraph(true);
        }

        private void chkGraphShowRightChannel_CheckedChanged(object sender, EventArgs e)
        {
            thd.ShowRight = chkGraphShowRightChannel.Checked;
            UpdateGraphChannelSelectors();
            UpdateGraph(true);
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void scGraphCursors_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
#endif
    }
}