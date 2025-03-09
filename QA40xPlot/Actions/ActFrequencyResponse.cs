using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;


namespace QA40xPlot.Actions
{

    public partial class ActFrequencyResponse : ActBase
    {
        public FrequencyResponseData Data { get; set; }       // Data used in this form instance
		private readonly Views.PlotControl frqrsPlot;

		private FrequencyResponseMeasurementResult MeasurementResult;

        CancellationTokenSource ct;                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActFrequencyResponse(ref FrequencyResponseData data, Views.PlotControl graphFreq)
        {
            Data = data;
            frqrsPlot = graphFreq;

			MeasurementResult = new(ViewSettings.Singleton.FreqRespVm); // TODO. Add to list
            ct = new CancellationTokenSource();

            UpdateGraph(true);
            //UpdateGraphChannelSelectors();

            //Program.MainForm.ClearMessage();
            //Program.MainForm.HideProgressBar();
            //AttachPlotMouseEvent();
        }

        public void DoCancel()
        {
            ct.Cancel();
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

		// user entered a new voltage, update the generator amplitude
		public void UpdateGenAmplitude(string value)
		{
			FreqRespViewModel frqrsVm = ViewSettings.Singleton.FreqRespVm;
            var oldv = MathUtil.ParseTextToDouble(frqrsVm.Gen1Voltage, 0.1);    // random default
			var val = MathUtil.ParseTextToDouble(value, oldv);
			frqrsVm.GeneratorAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)frqrsVm.GeneratorUnits, E_VoltageUnit.dBV);
		}

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void StartMeasurement()
		{
			var vm = ViewSettings.Singleton.FreqRespVm;
			vm.HasExport = false;
			vm.IsRunning = true;
			ct = new();
            UpdateGenAmplitude(vm.Gen1Voltage);   // convert gen1voltage to dbv
            UpdateGraph(true);
			await PerformMeasurement(ct.Token, false);
			await showMessage("Finished");
			vm.IsRunning = false;
			vm.HasExport = Data.Measurements.Count > 0;
		}


		/// <summary>
		/// Update the start voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateGeneratorVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.FreqRespVm;
			vm.Gen1Voltage = QaLibrary.ConvertVoltage(vm.GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)vm.GeneratorUnits).ToString();
		}

		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();
			var vf = this.MeasurementResult?.FrequencyResponseData;
            if (vf == null)
                return null;
			var vm = ViewSettings.Singleton.SpectrumVm;
			var sampleRate = MathUtil.ParseTextToUint(vm.SampleRate, 0);
            var fftsize = vf.FreqRslt.Left.Length;
			var binSize = QaLibrary.CalcBinSize(sampleRate, (uint)fftsize);
			db.LeftData = vf.FreqRslt.Left.ToList();
			db.RightData = vf.FreqRslt.Right.ToList();
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
		async Task<bool> PerformMeasurement(CancellationToken ct, bool continuous)
        {
            frqrsPlot.ThePlot.Clear();

			// Clear measurement result
			MeasurementResult = new(ViewSettings.Singleton.FreqRespVm)
            {
                CreateDate = DateTime.Now,
                Show = true,                                      // Show in graph
			};
            var frqrsVm = ViewSettings.Singleton.FreqRespVm;
            var mrs = MeasurementResult.MeasurementSettings;

			// UpdateGraphChannelSelectors();

			// markerIndex = -1;       // Reset marker


			// ********************************************************************
			// Check connection
			if (await QaLibrary.CheckDeviceConnected() == false)
				return false;

			// ********************************************************************
			// Setup the device
			var sampleRate = MathUtil.ParseTextToUint(mrs.SampleRate, 0);
			if (sampleRate == 0 || !frqrsVm.FftSizes.Contains(mrs.FftSize))
			{
				MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = frqrsVm.FftActualSizes.ElementAt(frqrsVm.FftSizes.IndexOf(mrs.FftSize));

			await Qa40x.SetDefaults();
            await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make it turn on or off
            await Qa40x.SetSampleRate(sampleRate);
            await Qa40x.SetBufferSize(fftsize);
            await Qa40x.SetWindowing("Hann");
            await Qa40x.SetRoundFrequencies(true);

            try
            {
                // ********************************************************************
                // Determine input level
                // ********************************************************************
                var attenuation = QaLibrary.MAXIMUM_DEVICE_ATTENUATION;
                double genVoltagedBV = -150;

				E_GeneratorType etp = (E_GeneratorType)mrs.MeasureType;

				if (etp == E_GeneratorType.OUTPUT_VOLTAGE)     // Based on output
                {
                    double amplifierOutputVoltagedBV = mrs.GeneratorAmplitude;
					await showMessage($"Determining generator amplitude to get an output amplitude of {amplifierOutputVoltagedBV:0.00#} dBV.");

                    // Get input voltage based on desired output voltage
                    attenuation = QaLibrary.DetermineAttenuation(amplifierOutputVoltagedBV);
                    double startAmplitude = -40;  // We start a measurement with a 10 mV signal.
                    var result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(startAmplitude, amplifierOutputVoltagedBV, true, mrs.RightChannel, ct);
                    if (ct.IsCancellationRequested)
                        return false;
                    genVoltagedBV = result.Item1;

                    if (genVoltagedBV == -150)
                    {
                        await showMessage($"Could not determine a valid generator amplitude. The amplitude would be {genVoltagedBV:0.00#} dBV.");
                        return false;
                    }

                    // Check if cancel button pressed
                    if (ct.IsCancellationRequested)
                        return false;

                    // Check if amplitude found within the generator range
                    if (genVoltagedBV < 18)
                    {
                        await showMessage($"Found an input amplitude of {genVoltagedBV:0.00#} dBV. Doing second pass.");

                        // 2nd time for extra accuracy
                        result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(genVoltagedBV, amplifierOutputVoltagedBV, true, mrs.RightChannel, ct);
                        if (ct.IsCancellationRequested)
                            return false;
                        genVoltagedBV = result.Item1;
                        if (genVoltagedBV == -150)
                        {
                            await showMessage($"Could not determine a valid generator amplitude. The amplitude would be {genVoltagedBV:0.00#} dBV.");
                            return false;
                        }
                    }

                    await showMessage($"Found an input amplitude of {genVoltagedBV:0.00#} dBV.");
                }
                else
                {
                    genVoltagedBV = frqrsVm.GeneratorAmplitude;
                    await showMessage($"Determining the best input attenuation for a generator voltage of {genVoltagedBV:0.00#} dBV.");

                    // Determine correct input attenuation
                    var result = await QaLibrary.DetermineAttenuationForGeneratorVoltageWithChirp(genVoltagedBV, QaLibrary.MAXIMUM_DEVICE_ATTENUATION, true, frqrsVm.RightChannel, ct);
                    if (ct.IsCancellationRequested)
                        return false;
                    attenuation = result.Item1;

                    await showMessage($"Found correct input attenuation of {attenuation:0} dBV for an amplfier amplitude of {result.Item2:0.00#} dBV.", 500);
                }

                // Set the new input range
                await Qa40x.SetInputRange(attenuation);

                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;

                await Qa40x.SetOutputSource(OutputSources.ExpoChirp);
                //await Qa40x.SetExpoChirpGen(genVoltagedBV, 0, frqrsVm.Smoothing, frqrsVm.RightChannel);
				await Qa40x.SetExpoChirpGen(genVoltagedBV, 0, 24, mrs.RightChannel);

                // If in continous mode we continue sweeping until cancellation requested.
                do
                {
                    await showMessage($"Sweeping...");
                    if (ct.IsCancellationRequested)
                        break;
					var lfrs = await QaLibrary.DoAcquisitions(frqrsVm.Averages, ct, true, true);
                    if (ct.IsCancellationRequested)
						break;

					await showMessage($"Sweeping done");
                    MeasurementResult.FrequencyResponseData = lfrs;
                    MeasurementResult.GainData = CalculateGain(lfrs.FreqRslt, frqrsVm.RightChannel);
                    // just one result to show
                    Data.Measurements.Clear();
                    Data.Measurements.Add(MeasurementResult);
                    UpdateGraph(false);
                    ShowLastMeasurementCursorTexts();

                    bool result = QaLibrary.DetermineAttenuationFromLeftRightSeriesData(true, true, lfrs, out double peak_dBV, out int newAttenuation);
                    if (result && attenuation != newAttenuation)
                    {
                        attenuation = newAttenuation;
                        await Qa40x.SetInputRange(attenuation);
                    }
                } while (continuous && !ct.IsCancellationRequested);
			}
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Turn the generator off
            await Qa40x.SetOutputSource(OutputSources.Off);

            // Show message
            await showMessage($"Measurement finished!");

            UpdateGraph(false);

            return ct.IsCancellationRequested;
        }


        private double[] CalculateGain(LeftRightFrequencySeries data, bool useRight)
        {
            double[] gain = new double[data.Left.Length];
			for (int i = 0; i < data.Left.Length; i ++)
            {
                if(useRight)
                {
					gain[i] = data.Left[i] / (data.Right[i] == 0 ? 0.000000001 : data.Right[i]);
				}
				else
                {
					gain[i] = data.Left[i] / Convert.ToDouble(MeasurementResult.MeasurementSettings.Gen1Voltage);
				}
			}

            return gain;
        }

        void ShowLastMeasurementCursorTexts()
        {
            if (MeasurementResult == null)
                return;

        }

        /// <summary>
        /// Initialize the magnitude plot
        /// </summary>
        void InitializePlot()
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
            InitializeMagFreqPlot(myPlot);

			var frqrsVm = ViewSettings.Singleton.FreqRespVm;

			myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(frqrsVm.GraphStartFreq)), Math.Log10(Convert.ToInt32(frqrsVm.GraphEndFreq)), frqrsVm.RangeBottomdB, frqrsVm.RangeTopdB);
			myPlot.Title("Frequency Response (dBV)");
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("dBV");
			frqrsPlot.Refresh();
        }


        /// <summary>
        /// Plot the magnitude graph
        /// </summary>
        /// <param name="measurementResult">Data to plot</param>
        void PlotGraph(FrequencyResponseMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel, E_FrequencyResponseGraphType graphType)
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var frqrsVm = ViewSettings.Singleton.FreqRespVm;

			if (measurementResult == null || measurementResult.FrequencyResponseData == null || measurementResult.FrequencyResponseData.FreqRslt == null)
                return;

            var freqX = new List<double>();
            var magnY_left = new List<double>();
            var magnY_right = new List<double>();
            var gainY = new List<double>();

            double frequencyStep = measurementResult.FrequencyResponseData.FreqRslt.Df;
            double startFrequency = frequencyStep;


            if (frqrsVm.ShowGain)
            {
                if (measurementResult.GainData == null)
                    return;

                // Gain line only
                foreach (var step in measurementResult.GainData.Skip(1))                                    // Skip first bin (DC)
                {
                    freqX.Add(startFrequency);
                    gainY.Add(QaLibrary.ConvertVoltage(step, E_VoltageUnit.Volt, E_VoltageUnit.dBV));
                    startFrequency += frequencyStep;
                }
            }
            else
            {
                // dBV graph
                foreach (var step in measurementResult.FrequencyResponseData.FreqRslt.Left.Skip(1))        // Skip first bin (DC)
                {
                    freqX.Add(startFrequency);
                    if (showLeftChannel)
                        magnY_left.Add(QaLibrary.ConvertVoltage(step, E_VoltageUnit.Volt, E_VoltageUnit.dBV));

                    startFrequency += frequencyStep;
                }

                if (showRightChannel && measurementResult.MeasurementSettings.RightChannel)
                {
                    foreach (var step in measurementResult.FrequencyResponseData.FreqRslt.Right.Skip(1))
                    {
                        magnY_right.Add(QaLibrary.ConvertVoltage(step, E_VoltageUnit.Volt, E_VoltageUnit.dBV));
                    }
                }
            }

            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;
            float markerSize = frqrsVm.ShowPoints ? lineWidth + 3 : 1;

            var colors = new GraphColors();
            int color = measurementNr * 2;

            void AddPlot(List<double> yValues, string legendText, int colorIndex, LinePattern linePattern)
            {
                if (yValues.Count == 0) 
                    return;
                var logYValues = yValues.ToArray();
                var plot = frqrsPlot.ThePlot.Add.Scatter(logFreqX, logYValues);
                plot.LineWidth = lineWidth;
                plot.Color = colors.GetColor(colorIndex, color);
                plot.MarkerSize = markerSize;
                plot.LegendText = legendText;
                plot.LinePattern = linePattern;
            }

            if (magnY_left.Count > 0)
                AddPlot(magnY_left, showRightChannel ? "Magn-L" : "Magn", 0, LinePattern.Solid);

            if (magnY_right.Count > 0)
                AddPlot(magnY_right, showLeftChannel ? "Magn-R" : "Magn", 3, LinePattern.Solid);

            if (gainY.Count > 0)
                AddPlot(gainY, "Gain", 9, LinePattern.Solid);

            //if (markerIndex != -1)
            //    QaLibrary.PlotCursorMarker(frqrsPlot, lineWidth, LinePattern.Solid, markerDataPoint);

            frqrsPlot.Refresh();
        }



        public void UpdateGraph(bool settingsChanged)
        {
            frqrsPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			var frqsrVm = ViewSettings.Singleton.FreqRespVm;

			int resultNr = 0;

            
            if (settingsChanged)
            {
                InitializePlot();
            }

            foreach (var result in Data.Measurements.Where(m => m.Show))
            {
                PlotGraph(result, resultNr++, frqsrVm.ShowLeft, frqsrVm.ShowRight, E_FrequencyResponseGraphType.DBV);  // frqsrVm.GraphType);
            }

            PlotBandwidthLines();
            frqrsPlot.Refresh();
        }

        private void PlotBandwidthLines()
        {
            // Plot
            //if (GraphSettings.GraphType == E_FrequencyResponseGraphType.DBV)   
                PlotDbVBandwidthLines();
            //else 
            //    PlotGainBandwidthLines();
        }

        void PlotDbVBandwidthLines() 
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			FreqRespViewModel frqrsVm = ViewSettings.Singleton.FreqRespVm;

			// Remove old lines
			myPlot.Remove<VerticalLine>();
			myPlot.Remove<Arrow>();
			myPlot.Remove<Text>();

            if (MeasurementResult != null && MeasurementResult.FrequencyResponseData != null && MeasurementResult.FrequencyResponseData.FreqRslt != null)
            {
                BandwidthData bandwidthData3dB = new BandwidthData();
                BandwidthData bandwidthData1dB = new BandwidthData();
                if (MeasurementResult.MeasurementSettings.LeftChannel)
                {
                    bandwidthData3dB.Left = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqRslt.Left, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                    bandwidthData1dB.Left = CalculateBandwidth(-1, MeasurementResult.FrequencyResponseData.FreqRslt.Left, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                }

                if (MeasurementResult.MeasurementSettings.RightChannel)
                {
                    bandwidthData3dB.Right = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqRslt.Right, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                    bandwidthData1dB.Right = CalculateBandwidth(-1, MeasurementResult.FrequencyResponseData.FreqRslt.Right, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                }

                // Draw bandwidth lines
                var colors = new GraphColors();
                float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;

                if (frqrsVm.ShowLeft && frqrsVm.LeftChannel)
                {
                    if (frqrsVm.Show3dBBandwidth_L)
                    {
                        DrawBandwithLines(3, bandwidthData3dB.Left, 0);
                    }

                    if (frqrsVm.Show1dBBandwidth_L)
                    {
                        DrawBandwithLines(1, bandwidthData1dB.Left, 1);
                    }
                }

                if (frqrsVm.ShowRight && frqrsVm.RightChannel)
                {
                    if (frqrsVm.Show3dBBandwidth_R)
                    {
                        DrawBandwithLines(3, bandwidthData3dB.Right, 2);
                    }

                    if (frqrsVm.Show1dBBandwidth_R)
                    {
                        DrawBandwithLines(1, bandwidthData3dB.Right, 3);
                    }
                }
            }
        }

        void PlotGainBandwidthLines()
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			FreqRespViewModel frqrsVm = ViewSettings.Singleton.FreqRespVm;

			// Remove old lines
			myPlot.Remove<VerticalLine>();
            myPlot.Remove<Arrow>();
            myPlot.Remove<Text>();

            // GAIN
            if (MeasurementResult != null && MeasurementResult.GainData != null)
            {
                // Gain BW
                if (MeasurementResult.MeasurementSettings.LeftChannel)
                {
                    var gainBW3dB = CalculateBandwidth(-3, MeasurementResult.GainData, MeasurementResult.FrequencyResponseData.FreqRslt.Df);        // Volts is gain

                    var gainBW1dB = CalculateBandwidth(-1, MeasurementResult.GainData, MeasurementResult.FrequencyResponseData.FreqRslt.Df);

                    // Draw bandwidth lines
                    var colors = new GraphColors();
                    float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;

                    if (frqrsVm.ShowLeft && frqrsVm.LeftChannel)
                    {
                        if (frqrsVm.Show3dBBandwidth_L)
                        {
                            DrawBandwithLines(3, gainBW3dB, 0);
                        }

                        if (frqrsVm.Show1dBBandwidth_L)
                        {
                            DrawBandwithLines(1, gainBW1dB, 1);
                        }
                    }
                }
            }
        }


        class BandwidthChannelData
        {
            public double LowestAmplitudeVolt;
            public double LowestAmplitudeFreq;

            public double HighestAmplitudeVolt;
            public double HighestAmplitudeFreq;

            public double LowerFreq;
            public double LowerFreqAmplitudeVolt;

            public double UpperFreq;
            public double UpperFreqAmplitudeVolt;

            public double Bandwidth;
        }

        class BandwidthData
        {
            public BandwidthChannelData Left;
            public BandwidthChannelData Right;
        }

        /// <summary>
        /// Calculate bandwidth from equally spaced data.
        /// </summary>
        /// <param name="dB"></param>
        /// <param name="data"></param>
        /// <param name="frequencyResolution"></param>
        /// <returns></returns>
        BandwidthChannelData CalculateBandwidth(double dB, double[] data, double frequencyResolution)
        {
            BandwidthChannelData bandwidthData = new BandwidthChannelData();

            if (data == null)
                return bandwidthData;

            var gainValue = Math.Pow(10, dB / 20);

            bandwidthData.LowestAmplitudeVolt = data.Skip(1).Min(); // Skip dc
            var lowestAmplitude_left_index = data.ToList().IndexOf(bandwidthData.LowestAmplitudeVolt);
            bandwidthData.LowestAmplitudeFreq = frequencyResolution * (lowestAmplitude_left_index + 1);

            // Get highest amplitude
            //bandwidthData.HighestAmplitudeVolt = data.Skip((int)(5 / frequencyResolution)).Max();      // Skip first 5 Hz for now.
            bandwidthData.HighestAmplitudeVolt = data.Skip(1).Max();      // Skip dc.
            var highestAmplitude_left_index = data.ToList().IndexOf(bandwidthData.HighestAmplitudeVolt);
            bandwidthData.HighestAmplitudeFreq = frequencyResolution * highestAmplitude_left_index;

            // Get lower frequency
            //var lowerFreq_left = data.Select((Value, Index) => new { Value, Index }).Where(f => f.Value <= (bandwidthData.HighestAmplitudeVolt * gainValue) && f.Index < highestAmplitude_left_index).LastOrDefault();
            var lowerFreq_left = data.Select((Value, Index) => new { Value, Index })
                .Where(f => f.Index < highestAmplitude_left_index)
                .Select(n => new { n.Value, n.Index, delta = Math.Abs(n.Value - (bandwidthData.HighestAmplitudeVolt * gainValue)) })
                .OrderBy(p => p.delta)
                .FirstOrDefault();

            if (lowerFreq_left != default)
            {
                double lowerFreq_left_index = lowerFreq_left.Index;
                bandwidthData.LowerFreqAmplitudeVolt = lowerFreq_left.Value;
                double lowerFreq_left_amplitude_dBV = 20 * Math.Log10(lowerFreq_left.Value);
                bandwidthData.LowerFreq = (lowerFreq_left_index + 1) * frequencyResolution;
            }
            else
                bandwidthData.LowerFreq = 1;

            // Get upper frequency
            //var upperFreq_left = data.Select((Value, Index) => new { Value, Index }).Where(f => f.Value <= bandwidthData.HighestAmplitudeVolt * gainValue && f.Index > highestAmplitude_left_index).FirstOrDefault();
            var upperFreq_left = data.Select((Value, Index) => new { Value, Index })
                .Where(f => f.Index > highestAmplitude_left_index)
                .Select(n => new { n.Value, n.Index, delta = Math.Abs(n.Value - (bandwidthData.HighestAmplitudeVolt * gainValue)) })
                .OrderBy(p => p.delta)
                .FirstOrDefault();

            if (upperFreq_left != default)
            {
                double upperFreq_left_index = upperFreq_left.Index;
                bandwidthData.UpperFreqAmplitudeVolt = upperFreq_left.Value;
                double upperFreq_left_amplitude_dBV = 20 * Math.Log10(upperFreq_left.Value);
                bandwidthData.UpperFreq = upperFreq_left_index * frequencyResolution;
            }
            else
                bandwidthData.UpperFreq = 100000;

            bandwidthData.Bandwidth = bandwidthData.UpperFreq - bandwidthData.LowerFreq;

            return bandwidthData;
        }

        private string AutoUnitText(double value, string unit, int decimals, int milliDecimals = 0)
        {
            bool isNegative = value < 0;
            string newString = string.Empty;

            value = Math.Abs(value);

            if (value < 1)
                newString = ((int)(value * 1000)).ToString("0." + new string('0', milliDecimals)) + " m" + unit;
            else if (value < 1000)
                newString = value.ToString("0." + new string('0', decimals)) + " " + unit;
            else
                newString = (value / 1000).ToString("0." + new string('0', decimals)) + " k" + unit;

            return (isNegative ? "-" : "") + newString;
        }

        void DrawBandwithLines(int gain, BandwidthChannelData channelData, int colorRange)
        {
			var frqrsVm = ViewSettings.Singleton.FreqRespVm;

			var colors = new GraphColors();
            float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;

            // Low frequency vertical line
            var lowerFreq_dBV_left = Math.Log10(channelData.LowerFreq);
            AddVerticalLine(lowerFreq_dBV_left, 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), colors.GetColor(colorRange, 2), lineWidth);

            // High frequency vertical line
            var upperFreq_dBV_left = Math.Log10(channelData.UpperFreq);
            AddVerticalLine(upperFreq_dBV_left, 20 * Math.Log10(channelData.UpperFreqAmplitudeVolt), colors.GetColor(colorRange, 2), lineWidth);

            // Bandwidht arrow
            AddArrow(lowerFreq_dBV_left, 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), upperFreq_dBV_left, 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), colors.GetColor(colorRange, 4), lineWidth);

            // Bandwitdh text
            var lowerFreq = Math.Log10(channelData.LowerFreq);
            var upperFreq = Math.Log10(channelData.UpperFreq);
            
            var bwText = $"B{gain:0}: {channelData.Bandwidth:0 Hz}";
            if (channelData.Bandwidth > 1000)
                bwText = $"B{gain:0}: {(channelData.Bandwidth / 1000):0.00# kHz}";
            if (channelData.UpperFreq > 96000)
                bwText = $"B{gain:0}: > 96 kHz";
            AddText(bwText, (lowerFreq + ((upperFreq - lowerFreq) / 2)), 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), colors.GetColor(colorRange, 8), -35, -10);

            // Low frequency text
            var bwLowF = $"{channelData.LowerFreq:0 Hz}";
            if (channelData.LowerFreq > 1000)
                bwLowF = $"{(channelData.LowerFreq / 1000):0.00# kHz}";
            AddText(bwLowF, lowerFreq_dBV_left, frqrsPlot.ThePlot.Axes.GetLimits().Bottom, colors.GetColor(colorRange, 8), -20, -30);

            // High frequency text         
            var bwHighF = $"{channelData.UpperFreq:0 Hz}";
            if (channelData.UpperFreq > 1000)
                bwHighF = $"{(channelData.UpperFreq / 1000):0.00# kHz}";
            AddText(bwHighF, upperFreq_dBV_left, frqrsPlot.ThePlot.Axes.GetLimits().Bottom, colors.GetColor(colorRange, 8), -20, -30);
        }


        void AddVerticalLine(double x, double maximum, ScottPlot.Color color, float lineWidth)
        {
            var line = frqrsPlot.ThePlot.Add.VerticalLine(x);
            line.Maximum = maximum;
            line.Color = color;
            line.LineWidth = lineWidth;
            line.LinePattern = LinePattern.DenselyDashed;
        }

        void AddArrow(double x1, double y1, double x2, double y2, ScottPlot.Color color, float lineWidth)
        {
            Coordinates arrowTip = new Coordinates(x1, y1);
            Coordinates arrowBase = new Coordinates(x2, y2);
            var arrow = frqrsPlot.ThePlot.Add.Arrow(arrowTip, arrowBase);
            arrow.ArrowStyle.LineWidth = lineWidth;
            arrow.ArrowStyle.ArrowheadLength = 12;
            arrow.ArrowStyle.ArrowheadWidth = 8;
            arrow.ArrowShape = new ScottPlot.ArrowShapes.DoubleLine();
            arrow.ArrowLineColor = color;
        }

        void AddText(string text, double x, double y, ScottPlot.Color backgroundColor, int offsetX, int offsetY)
        {
            var txt = frqrsPlot.ThePlot.Add.Text(text, x, y);
            txt.LabelFontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			txt.LabelBorderColor = Colors.Black;
            txt.LabelBorderWidth = 1;
            txt.LabelPadding = 2;
            txt.LabelBold = false;
            txt.LabelBackgroundColor = backgroundColor;
            txt.OffsetX = offsetX;
            txt.OffsetY = offsetY;
        }
    }
}
