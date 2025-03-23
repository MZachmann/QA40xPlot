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
            QaLibrary.InitMiniFftPlot(fftPlot, MathUtil.ToDouble(thd.StartFreq, 10),
                MathUtil.ToDouble(thd.EndFreq, 20000), -150, 20);
            QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);
            MeasurementResult = new(thd);

            // TODO: depends on graph settings which graph is shown
            UpdateGraph(true);
        }

        /// <summary>
        /// Generator type changed
        /// </summary>
        public void UpdateGeneratorParameters()
        {
            var vm = ViewSettings.Singleton.ThdFreq;
            var tt = vm.ToDirection(vm.GenDirection);
            switch (tt)
            {
                case E_GeneratorDirection.INPUT_VOLTAGE: // Input voltage
				case E_GeneratorDirection.OUTPUT_VOLTAGE: // Output voltage
					vm.ReadVoltage = true;
                    vm.ReadPower = false;
                    break;
                case E_GeneratorDirection.OUTPUT_POWER: // Output power
                    vm.ReadVoltage = false;
                    vm.ReadPower = true;
                    break;
            }
        }

        public void DoCancel()
        {
            ct.Cancel();
        }

        private ThdColumn? MakeColumn(ThdFrequencyStepChannel chan)
        {
            if (chan == null)
                return null;
            var cl = new ThdColumn();
            cl.Mag = chan.Fundamental_dBV;
            cl.THD = chan.Thd_dB;
            cl.D2 = cl.D3 = cl.D4 = cl.D5 = -180;
            try
            {
				cl.D2 = chan.Harmonics[0].Thd_dB;
				cl.D3 = chan.Harmonics[1].Thd_dB;
				cl.D4 = chan.Harmonics[2].Thd_dB;
				cl.D5 = chan.Harmonics[3].Thd_dB;
			}
            catch { }


			cl.D6P = chan.D6Plus_dBV;
            cl.Noise = chan.Average_NoiseFloor_dBV;
            //
            cl.Amplitude = chan.Fundamental_dBV;
            return cl;
        }

        private void AddColumn(ThdFrequencyStep step)
        {
            var f = MeasurementResult.LeftColumns;
            if (f != null)
            {
                var cl = MakeColumn(step.Left);
                if (cl != null)
                {
                    cl.Freq = step.FundamentalFrequency;
                    MeasurementResult.LeftColumns.Add(cl);
                }
            }
            f = MeasurementResult.RightColumns;
            if (f != null)
            {
                var cl = MakeColumn(step.Right);
                if (cl != null)
                {
                    cl.Freq = step.FundamentalFrequency;
                    MeasurementResult.RightColumns.Add(cl);
                }
            }
        }

		public ValueTuple<ThdColumn?, ThdColumn?> LookupX(double freq)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			var vf = vm.ShowLeft ? MeasurementResult.LeftColumns : MeasurementResult.RightColumns;
			if (vf == null || vf.Count == 0)
			{
				return ValueTuple.Create((ThdColumn?)null, (ThdColumn?)null);
			}

			// find nearest amplitude (both left and right will be identical here if scanned)
			var bin = vf.Count(x => x.Freq < freq) - 1;    // find first freq less than me
			if (bin == -1)
				bin = 0;
			var anearest = vf[bin].Freq;
			if (bin < (vf.Count - 1) && Math.Abs(freq - anearest) > Math.Abs(freq - vf[bin + 1].Freq))
			{
				bin++;
			}

			ThdColumn? mf1 = null;
			ThdColumn? mf2 = null;

			if (vm.ShowLeft)
				mf1 = MeasurementResult.LeftColumns?.ElementAt(bin);
			if (vm.ShowRight)
				mf2 = MeasurementResult.RightColumns?.ElementAt(bin);

			return ValueTuple.Create(mf1, mf2);
		}


        /// <summary>
        /// Perform the measurement
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>result. false if cancelled</returns>
        async Task<bool> PerformMeasurementSteps(CancellationToken ct)
        {
            bool rslt = false;
            try
            {
				rslt = await PerformSteps(ct);
			}
            catch (Exception )
            {

            }
			// Turn the generator off no matter what, since there are random returns in the perform code
			await Qa40x.SetOutputSource(OutputSources.Off);
			return rslt;
		}

		async Task<bool> PerformSteps(CancellationToken ct)
        { 
            ClearPlot();
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
            QaLibrary.InitMiniFftPlot(fftPlot, MathUtil.ToDouble(thd.StartFreq, 10),
                MathUtil.ToDouble(thd.EndFreq, 20000), -150, 20);
            QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			if (false == await QaLibrary.InitializeDevice(thd.SampleRate, thd.FftSize, thd.WindowingMethod, QaLibrary.DEVICE_MAX_ATTENUATION, true))
			{
				return false;
			}

            try
            {
                // ********************************************************************
                // Determine input level
                // ********************************************************************
                double testFrequency = QaLibrary.GetNearestBinFrequency(1000, thd.SampleRate, thd.FftSize);
                E_GeneratorDirection etp = thd.ToDirection(thd.GenDirection);
                if (etp == E_GeneratorDirection.OUTPUT_VOLTAGE || etp == E_GeneratorDirection.OUTPUT_POWER)     // Based on output
                {
                    double amplifierOutputVoltagedBV = QaLibrary.ConvertVoltage(MathUtil.ToDouble(thd.OutVoltage), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
                    if (etp == E_GeneratorDirection.OUTPUT_VOLTAGE)
                        await showMessage($"Determining generator amplitude to get an output amplitude of {amplifierOutputVoltagedBV:0.00#} dBV.");
                    else
                        await showMessage($"Determining generator amplitude to get an output power of {thd.OutPower} W.");

                    // Get input voltage based on desired output voltage
                    thd.InputRange = QaLibrary.DetermineAttenuation(amplifierOutputVoltagedBV);
                    double startAmplitude = -40;  // We start a measurement with a 10 mV signal.
                    var result = await QaLibrary.DetermineGenAmplitudeWithChirp(startAmplitude, amplifierOutputVoltagedBV, thd.LeftChannel, thd.RightChannel, ct);
                    if (ct.IsCancellationRequested)
                        return false;
                    var generatorAmp = result.Item1;
                    QaLibrary.PlotMiniFftGraph(fftPlot, result.Item2?.FreqRslt, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);                                             // Plot fft data in mini graph
                    QaLibrary.PlotMiniTimeGraph(timePlot, result.Item2?.TimeRslt, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);                                      // Plot time data in mini graph
                    if (generatorAmp == -150)
                    {
                        await showMessage($"Could not determine a valid generator amplitude. The amplitude would be {generatorAmp:0.00#} dBV.");
                        return false;
                    }

                    // Check if cancel button pressed
                    if (ct.IsCancellationRequested)
                        return false;

                    // Check if amplitude found within the generator range
                    if (generatorAmp < 18)
                    {
                        await showMessage($"Found an input amplitude of {generatorAmp:0.00#} dBV. Doing second pass.");

                        // 2nd time for extra accuracy
                        result = await QaLibrary.DetermineGenAmplitudeWithChirp(generatorAmp, amplifierOutputVoltagedBV, thd.LeftChannel, thd.RightChannel, ct);
                        if (ct.IsCancellationRequested)
                            return false;
						generatorAmp = result.Item1;
                        QaLibrary.PlotMiniFftGraph(fftPlot, result.Item2?.FreqRslt, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight);                                             // Plot fft data in mini graph
                        QaLibrary.PlotMiniTimeGraph(timePlot, result.Item2?.TimeRslt, testFrequency, thd.LeftChannel && thd.ShowLeft, thd.RightChannel && thd.ShowRight, true);                                      // Plot time data in mini graph
                        if (generatorAmp == -150)
                        {
                            await showMessage($"Could not determine a valid generator amplitude. The amplitude would be {generatorAmp:0.00#} dBV.");
                            return false;
                        }
                    }

					thd.GenVoltage = QaLibrary.ConvertVoltage( generatorAmp, E_VoltageUnit.dBV, E_VoltageUnit.Volt).ToString();
					//UpdateGeneratorVoltageDisplay();

					await showMessage($"Found an input amplitude of {generatorAmp:0.00#} dBV.");
                }
                else if (etp == E_GeneratorDirection.INPUT_VOLTAGE)                         // Based on input voltage
                {
                    double genVoltagedBV = QaLibrary.ConvertVoltage(MathUtil.ToDouble(thd.GenVoltage), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
                    await showMessage($"Determining the best input attenuation for a generator voltage of {genVoltagedBV:0.00#} dBV.");

                    // Determine correct input attenuation
                    var result = await QaLibrary.DetermineAttenuationWithChirp(genVoltagedBV, QaLibrary.DEVICE_MAX_ATTENUATION, ct);
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
                var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(
                    MathUtil.ToDouble(thd.StartFreq, 10), MathUtil.ToDouble(thd.EndFreq, 10000), thd.StepsOctave);
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

                // Set the generator
                double amplitudeSetpointdBV = QaLibrary.ConvertVoltage(MathUtil.ToDouble(thd.GenVoltage), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
                await Qa40x.SetGen1(stepBinFrequencies[0], amplitudeSetpointdBV, true);

                await Qa40x.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset

                // ********************************************************************
                // Step through the list of frequencies
                // ********************************************************************
                for (int f = 0; f < stepBinFrequencies.Length; f++)
                {
                    await showMessage($"Measuring step {f + 1} of {stepBinFrequencies.Length}.");
                    await showProgress(100 * (f + 1) / stepBinFrequencies.Length);
                    await Qa40x.SetGen1(stepBinFrequencies[f], amplitudeSetpointdBV, true);

                    LeftRightSeries lrfs = await QaLibrary.DoAcquisitions(thd.Averages, ct);
                    if (ct.IsCancellationRequested)
                        break;

                    uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
                    if (lrfs.FreqRslt == null || fundamentalBin >= lrfs.FreqRslt.Left.Length)               // Check in bin within range
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

                    step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqRslt.Left, MeasurementResult.NoiseFloor?.FreqRslt?.Left, thd.AmpLoad);
                    step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, amplitudeSetpointdBV, lrfs.FreqRslt.Right, MeasurementResult.NoiseFloor?.FreqRslt?.Right, thd.AmpLoad);

                    // Add step data to list
                    MeasurementResult.FrequencySteps.Add(step);
                    AddColumn(step);

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
            await showMessage(ct.IsCancellationRequested ? $"Measurement cancelled!" : $"Measurement finished!");

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
        private ThdFrequencyStepChannel ChannelCalculations(double binSize, double fundamentalFrequency, double generatorAmplitudeDbv, double[] fftData, double[]? noiseFloorFftData, double load)
        {
            uint fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFrequency, binSize);

            ThdFrequencyStepChannel channelData = new()
            {
                Fundamental_V = fftData[fundamentalBin],
                Fundamental_dBV = 20 * Math.Log10(fftData[fundamentalBin]),
                Gain_dB = 20 * Math.Log10(fftData[fundamentalBin] / Math.Pow(10, generatorAmplitudeDbv / 20))
            };
            // Calculate average noise floor
            if (noiseFloorFftData != null)
            {
				channelData.Average_NoiseFloor_V = noiseFloorFftData.Skip((int)fundamentalBin + 1).Average();   // Average noise floor in Volts after the fundamental
				channelData.Average_NoiseFloor_dBV = 20 * Math.Log10(channelData.Average_NoiseFloor_V);         // Average noise floor in dBV
			}
            else
            {
				channelData.Average_NoiseFloor_V = 1e-4;    // Average noise floor in Volts after the fundamental
				channelData.Average_NoiseFloor_dBV = -100;  // Average noise floor in dBV
			}

			// Reset harmonic distortion variables
			double distortionSqrtTotal = 0;
            double distortionD6plus = 0;

            // Loop through harmonics up tot the 12th
            for (int harmonicNumber = 2; harmonicNumber <= 12; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
            {
                double harmonicFrequency = fundamentalFrequency * harmonicNumber;
                uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

                if (bin >= fftData.Length)
                    break;                                          // Invalid bin, skip harmonic

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
                    NoiseAmplitude_V = 0
                };
                if (noiseFloorFftData != null)
					harmonic.NoiseAmplitude_V = noiseFloorFftData[bin];

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

            myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thdFreq.GraphStartFreq)), Math.Log10(MathUtil.ToDouble(thdFreq.GraphEndFreq)),
                Math.Log10(MathUtil.ToDouble(thdFreq.RangeBottom)) - 0.00000001, Math.Log10(MathUtil.ToDouble(thdFreq.RangeTop)));  // - 0.000001 to force showing label
            myPlot.Title("Distortion vs Frequency (%)");
            myPlot.XLabel("Frequency (Hz)");
            myPlot.YLabel("Distortion (%)");
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
            myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thdFreq.GraphStartFreq)), Math.Log10(MathUtil.ToDouble(thdFreq.GraphEndFreq)),
                MathUtil.ToDouble(thdFreq.RangeBottomdB), MathUtil.ToDouble(thdFreq.RangeTopdB));
            myPlot.Title("Distortion vs Frequency (dB)");
            myPlot.XLabel("Frequency (Hz)");
            myPlot.YLabel("Distortion (dB)");
            thdPlot.Refresh();
        }


        /// <summary>
        /// Plot the  THD magnitude (dB) data
        /// </summary>
        /// <param name="data">The data to plot</param>
        private void PlotValues(ThdFrequencyMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
        {
            if (!showLeftChannel && !showRightChannel)
                return;

            var thdFreq = ViewSettings.Singleton.ThdFreq;
            float lineWidth = thdFreq.ShowThickLines ? 1.6f : 1;
            float markerSize = thdFreq.ShowPoints ? lineWidth + 3 : 1;

            var colors = new GraphColors();
            int color = measurementNr * 2;

            // here Y values are in dBV
            void AddPlot(double[] xValues, List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
            {
                if (yValues.Count == 0) return;
                Scatter? plot = null;
                if (thdFreq.ShowPercent)
                {
                    var vals = yValues.Select(x => 2 + x / 20).ToArray();       // convert to volts then 100 then back to log10
                    plot = thdPlot.ThePlot.Add.Scatter(xValues, vals);
                }
                else
                {
                    plot = thdPlot.ThePlot.Add.Scatter(xValues, yValues.ToArray());
                }
                plot.LineWidth = lineWidth;
                plot.Color = colors.GetColor(colorIndex, color);
                plot.MarkerSize = markerSize;
                plot.LegendText = legendText;
                plot.LinePattern = linePattern;
            }

            // which columns are we displaying? left, right or both
            List<ThdColumn>[] columns;
            if (showLeftChannel && showRightChannel)
            {
                columns = [MeasurementResult.LeftColumns, MeasurementResult.RightColumns];
            }
            else if (!showRightChannel)
            {
                columns = [MeasurementResult.LeftColumns];
            }
            else
            {
                columns = [MeasurementResult.RightColumns];
            }

            string suffix = string.Empty;
            var lp = LinePattern.Solid;
            if (showRightChannel && showLeftChannel)
                suffix = "-L";

            // copy the vector of columns into vectors of values
            foreach (var col in columns)
            {
                var freq = col.Select(x => Math.Log10(x.Freq)).ToArray();
                if (thdFreq.ShowMagnitude)
                    AddPlot(freq, col.Select(x => x.Mag).ToList(), 9, "Mag" + suffix, LinePattern.DenselyDashed);
                if (thdFreq.ShowTHD)
                    AddPlot(freq, col.Select(x => x.THD).ToList(), 8, "THD" + suffix, lp);
                if (thdFreq.ShowD2)
                    AddPlot(freq, col.Select(x => x.D2).ToList(), 0, "D2" + suffix, lp);
                if (thdFreq.ShowD3)
                    AddPlot(freq, col.Select(x => x.D3).ToList(), 1, "D3" + suffix, lp);
                if (thdFreq.ShowD4)
                    AddPlot(freq, col.Select(x => x.D4).ToList(), 2, "D4" + suffix, lp);
                if (thdFreq.ShowD5)
                    AddPlot(freq, col.Select(x => x.D5).ToList(), 3, "D5" + suffix, lp);
                if (thdFreq.ShowD6)
                    AddPlot(freq, col.Select(x => x.D6P).ToList(), 3, "D6+" + suffix, lp);
                if (thdFreq.ShowNoiseFloor)
                    AddPlot(freq, col.Select(x => x.Noise).ToList(), 3, "Noise" + suffix, LinePattern.Dotted);
                suffix = "-R";          // second pass iff there are both channels
                lp = LinePattern.DenselyDashed;
            }

            thdPlot.Refresh();
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
                    PlotValues(result, resultNr++, thd.ShowLeft, thd.ShowRight);
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
                    PlotValues(result, resultNr++, thd.ShowLeft, thd.ShowRight);
                }
            }
        }
    

            /// <summary>
        ///  Start measurement button click
        /// </summary>
        public async void StartMeasurement()
        {
            ThdFreqViewModel thd = ViewSettings.Singleton.ThdFreq;
			if (!await StartAction(thd))
				return; 
			ct = new();
            await PerformMeasurementSteps(ct.Token);
            await showMessage("Finished");
            thd.IsRunning = false;
        }
    }
}