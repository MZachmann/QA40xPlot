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

    public class ActSpectrum
    {
        public SpectrumData Data { get; set; }                  // Data used in this form instance
        public bool MeasurementBusy { get; set; }                   // Measurement busy state

        private Views.PlotControl? fftPlot;
		private int markerIndex = -1;
		private DataPoint markerDataPoint;

        private SpectrumMeasurementResult MeasurementResult;

        private float _Thickness = 2.0f;

		public CancellationTokenSource ct { private set; get; }                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActSpectrum(ref SpectrumData data, Views.PlotControl graphFft)
        {
            Data = data;
            
			fftPlot = graphFft;

			ct = new CancellationTokenSource();

			// TODO: depends on graph settings which graph is shown
			MeasurementResult = new();
			UpdateGraph(true);


			AttachPlotMouseEvent();
        }

		/// <summary>
		/// Update the generator voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateGeneratorVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.SpectrumVm;
			vm.GenVoltage = QaLibrary.ConvertVoltage(vm.GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)vm.GeneratorUnits);
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

            markerIndex = -1;       // Reset marker

			// Init mini plots
			SpectrumViewModel thd = ViewSettings.Singleton.SpectrumVm;

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
			Windowing wValue = (Windowing)Enum.Parse(typeof(Windowing), thd.WindowingMethod);
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
					// Do noise floor measurement
					// ********************************************************************
					await showMessage($"Determining noise floor.");
					await Qa40x.SetOutputSource(OutputSources.Off);
					await Qa40x.DoAcquisition();
					msr.NoiseFloor = await QaLibrary.DoAcquisitions(thd.Averages, ct);
					if (ct.IsCancellationRequested)

						return false;
				}

				// ********************************************************************
				// Step through the list of frequencies
				// ********************************************************************
				for (int f = 0; f < stepBinFrequencies.Length; f++)
                {
                    await showMessage($"Measuring step {f + 1} of {stepBinFrequencies.Length}.");
					await showProgress(100*(f + 1)/ stepBinFrequencies.Length);

                    // Set the generator
                    double amplitudeSetpointdBV = QaLibrary.ConvertVoltage(Convert.ToDouble(thd.Gen1Voltage), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
                    await Qa40x.SetGen1(stepBinFrequencies[f], amplitudeSetpointdBV, true);
                    if (f == 0)
                        await Qa40x.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset

                    LeftRightSeries lrfs = await QaLibrary.DoAcquisitions(thd.Averages, ct);
                    if (ct.IsCancellationRequested)
                        break;

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
                  
                    step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqInput.Left, msr.NoiseFloor.FreqInput.Left, thd.AmpLoad);
                    step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqInput.Right, msr.NoiseFloor.FreqInput.Right, thd.AmpLoad);

					// Add step data to list
					if (msr.FrequencySteps.Count > 0)
					{
						msr.FrequencySteps.Clear();
					}
					msr.FrequencySteps.Add(step);

					ClearPlot();
					//ClearCursorTexts();
					UpdateGraph(false);
                    DrawChannelInfoTable();
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
			double distortionSqrtTotalN = 0;
			double distiortionD6plus = 0;

            // Loop through harmonics up tot the 12th
            for (int harmonicNumber = 2; harmonicNumber <= 12; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
            {
                double harmonicFrequency = fundamentalFrequency * harmonicNumber;
                uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

                if (bin >= fftData.Length) break;                                          // Invalid bin, skip harmonic

                double amplitude_V = fftData[bin];
                double noise_V = noiseFloorFftData[bin];

				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
                double thd_Percent = (amplitude_V / (channelData.Fundamental_V - noise_V)) * 100;
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
            ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(1);

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

            fftPlot.Refresh();
		}

        /// <summary>
        /// Attach mouse events to the main graph
        /// </summary>
        void AttachPlotMouseEvent()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;

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
        //ScottPlot.Plot myPlot = fftPlot.ThePlot;

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
        //ScottPlot.Plot myPlot = fftPlot.ThePlot;

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

        //             QaLibrary.PlotCursorMarker(fftPlot, lineWidth, linePattern, nearest1);


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
			var mSets = ViewSettings.Singleton.SpectrumVm;

			// Clear measurement result
			MeasurementResult = new()
			{
				CreateDate = DateTime.Now,
				Show = true,                                      // Show in graph
				MeasurementSettings = mSets       // Copy measurment settings to measurement results
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
						MeasurementResult = new()
                        {
                            CreateDate = DateTime.Now,
                            Show = true,                                      // Show in graph
                            MeasurementSettings = mSets       // Copy measurment settings to measurement results
                        };
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
            var val = QaLibrary.ParseTextToDouble(value, thd.GenVoltage);
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

#endif
    }
}