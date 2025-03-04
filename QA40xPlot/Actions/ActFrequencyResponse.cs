using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;


namespace QA40xPlot.Actions
{

    public partial class ActFrequencyResponse
    {
        public FrequencyResponseData Data { get; set; }       // Data used in this form instance
        public bool MeasurementBusy { get; set; }                   // Measurement busy state
		private Views.PlotControl? frqrsPlot;

		//private FrequencyResponseMeasurementSettings MeasurementSettings;
		//private FrequencyResponseGraphSettings GraphSettings;
		private FrequencyResponseMeasurementResult MeasurementResult;

        CancellationTokenSource ct;                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActFrequencyResponse(ref FrequencyResponseData data, Views.PlotControl graphFreq)
        {
            Data = data;
            frqrsPlot = graphFreq;

			MeasurementResult = new(); // TODO. Add to list
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
			var val = QaLibrary.ParseTextToDouble(value, Convert.ToDouble(frqrsVm.Gen1Voltage));
			frqrsVm.GeneratorAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)frqrsVm.GeneratorUnits, E_VoltageUnit.dBV);
		}

#if false
        void PopulateMeasurementSettingsComboBoxes()
        { 
            var items = new List<KeyValuePair<double, string>>
            {
                new (0, "Input voltage"),
                new (1, "Output voltage")
            };
            ComboBoxHelper.PopulateComboBox(cmbGeneratorType, items);

            items =
            [
                new (48000, "24000"),
                new (96000, "48000"),
                new (192000, "96000")
            ];
            ComboBoxHelper.PopulateComboBox(cmbFrequencySpan, items);

            items =
            [
                new (0, "mV"),
                new (1, "V"),
                new (2, "dBV")
            ];
            ComboBoxHelper.PopulateComboBox(cmbGeneratorVoltageUnit, items);


            items =
            [
                new (0, "None"),
                new (69, "1/69 (min)"),
                new (48, "1/48"),
                new (24, "1/24"),
                new (12, "1/12"),
                new (6, "1/6"),
                new (3, "1/3 (max)")
            ];
            ComboBoxHelper.PopulateComboBox(cmbSmoothing, items);

            items =
            [
                new (1, "1.46"),
                new (2, "0.73"),
                new (4, "0.37")
            ];
            ComboBoxHelper.PopulateComboBox(cmbLowFrequencyAccuracy, items);
        }

        void PopulateGraphSettingsComboBoxes()
        {

            var items = new List<KeyValuePair<double, string>>
            {
                new (1, "1"),
                new (2, "2"),
                new (5, "5"),
                new (10, "10"),
                new (20, "20"),
                new (50, "50"),
                new (200, "200"),
                new (100, "100"),
                new (500, "500")
            };
            ComboBoxHelper.PopulateComboBox(cmbGraph_FreqStart, items);

            items = new List<KeyValuePair<double, string>>
            {
                new (1000, "1000"),
                new (2000, "2000"),
                new (5000, "5000"),
                new (10000, "10000"),
                new (20000, "20000"),
                new (50000, "50000"),
                new (100000, "100000")
            };
            ComboBoxHelper.PopulateComboBox(cmbGraph_FreqEnd, items);

        }


        /// <summary>
        /// Initialize Settings with default settings
        /// </summary>
        void SetDefaultMeasurementSettings(ref FrequencyResponseMeasurementSettings settings)
        {
            // Initialize with default measurement settings
            settings.SampleRate = 192000;
            settings.FftSize = 65536 * 2;
            settings.WindowingFunction = Windowing.Hann;
            settings.SmoothDenominator = 3;
            settings.RightChannelIsReference = true;
            settings.GeneratorType = E_GeneratorType.OUTPUT_VOLTAGE;
            settings.GeneratorAmplitude = 0;
            settings.GeneratorAmplitudeUnit = E_VoltageUnit.dBV;
            settings.EnableLeftChannel = true;
            settings.EnableRightChannel = false;
            settings.SmoothDenominator = 24;
            settings.FftResolution = 1;
        }

        /// <summary>
        /// Initializer with default graph settings
        /// </summary>
        void SetDefaultGraphSettings(ref FrequencyResponseGraphSettings settings)
        {
            settings = new()
            {
                YRangeTop = 50,
                YRangeBottom = -20,

                FrequencyRange_Start = 1,
                FrequencyRange_End = 100000,

                GraphType = E_FrequencyResponseGraphType.DBV,

                Show1dBBandwidth_L = false,
                Show3dBBandwidth_L = true,
                Show1dBBandwidth_R = false,
                Show3dBBandwidth_R = true,

                ThickLines = true,
                ShowDataPoints = false,

                ShowLeftChannel = true,
                ShowRightChannel = true
            };
        }


        /// <summary>
        /// Set initial control values
        /// </summary>
        void SetMeasurementControls(FrequencyResponseMeasurementSettings settings)
        {
            cmbGeneratorType.SelectedIndex = (int)settings.GeneratorType;
            txtGeneratorVoltage.Text = settings.GeneratorAmplitude.ToString("#0.0##");
            cmbGeneratorVoltageUnit.SelectedIndex = (int)settings.GeneratorAmplitudeUnit;
            ComboBoxHelper.SelectNearestValue(cmbFrequencySpan, settings.SampleRate);
            
            chkRightIsReference.Checked = settings.RightChannelIsReference;
            ComboBoxHelper.SelectNearestValue(cmbSmoothing, settings.SmoothDenominator);
            udAverages.Value = settings.Averages;

            ComboBoxHelper.SelectNearestValue(cmbLowFrequencyAccuracy, settings.FftResolution);
            chkEnableLeftChannel.Checked = settings.EnableLeftChannel;
            chkEnableRightChannel.Checked = settings.EnableRightChannel;
            
        }


        void SetGraphControls(FrequencyResponseGraphSettings graphSettings)
        {
            ud_Graph_Top.Value = (decimal)graphSettings.YRangeTop;
            ud_Graph_Bottom.Value = (decimal)graphSettings.YRangeBottom;

            SetStartFrequencySelectedIndexByFrequency(graphSettings.FrequencyRange_Start);
            SetEndFrequencySelectedIndexByFrequency(graphSettings.FrequencyRange_End);

            chk1dBBandWidth_L.Checked = GraphSettings.Show1dBBandwidth_L;
            chk3dBBandWidth_L.Checked = GraphSettings.Show3dBBandwidth_L;
            chk1dBBandWidth_R.Checked = GraphSettings.Show1dBBandwidth_R;
            chk3dBBandWidth_R.Checked = GraphSettings.Show3dBBandwidth_R;

            chkThickLines.Checked = graphSettings.ThickLines;
            chkShowDataPoints.Checked = graphSettings.ShowDataPoints;

            chkGraphShowLeftChannel.Checked = graphSettings.ShowLeftChannel;
            chkGraphShowLeftChannel.Visible = chkEnableLeftChannel.Checked;
            chkGraphShowRightChannel.Checked = graphSettings.ShowRightChannel;
            chkGraphShowRightChannel.Visible = chkEnableRightChannel.Checked;
        }


        private void SetStartFrequencySelectedIndexByFrequency(uint startfrequency)
        {
            ComboBoxHelper.SelectNearestValue(cmbGraph_FreqStart, startfrequency);
            GraphSettings.FrequencyRange_Start = (uint)((KeyValuePair<double, string>)cmbGraph_FreqStart.SelectedItem).Key;
        }

        private void SetEndFrequencySelectedIndexByFrequency(uint endfrequency)
        {
            ComboBoxHelper.SelectNearestValue(cmbGraph_FreqEnd, endfrequency);
            GraphSettings.FrequencyRange_End = (uint)((KeyValuePair<double, string>)cmbGraph_FreqEnd.SelectedItem).Key;
        }
#endif

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void StartMeasurement()
		{
			var vm = ViewSettings.Singleton.FreqRespVm;
			MeasurementBusy = true;
			ct = new();
            UpdateGenAmplitude(vm.Gen1Voltage);   // convert gen1voltage to dbv
            UpdateGraph(true);
			await PerformMeasurement(ct.Token, false);
			await showMessage("Finished");
			MeasurementBusy = false;
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

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurement(CancellationToken ct, bool continuous)
        {
            frqrsPlot.ThePlot.Clear();
			var frqrsVm = ViewSettings.Singleton.FreqRespVm;

			// Clear measurement result
			MeasurementResult = new()
            {
                CreateDate = DateTime.Now,
                Show = true,                                      // Show in graph
                MeasurementSettings = frqrsVm       // Copy measurment settings to measurement results
			};

            // For now clear measurements to allow only one until we have a UI to manage them.
            Data.Measurements.Clear();

            // Add to list
            Data.Measurements.Add(MeasurementResult);

            // UpdateGraphChannelSelectors();

            // markerIndex = -1;       // Reset marker


			// ********************************************************************
			// Check connection
			if (await QaLibrary.CheckDeviceConnected() == false)
				return false;

			// ********************************************************************
			// Setup the device
			var sampleRate = Convert.ToUInt32(frqrsVm.SampleRate);
			var fftsize = frqrsVm.FftActualSizes.ElementAt(frqrsVm.FftSizes.IndexOf(frqrsVm.FftSize));

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

				E_GeneratorType etp = (E_GeneratorType)frqrsVm.MeasureType;

				if (etp == E_GeneratorType.OUTPUT_VOLTAGE)     // Based on output
                {
                    double amplifierOutputVoltagedBV = frqrsVm.GeneratorAmplitude;
					await showMessage($"Determining generator amplitude to get an output amplitude of {amplifierOutputVoltagedBV:0.00#} dBV.");

                    // Get input voltage based on desired output voltage
                    attenuation = QaLibrary.DetermineAttenuation(amplifierOutputVoltagedBV);
                    double startAmplitude = -40;  // We start a measurement with a 10 mV signal.
                    var result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(startAmplitude, amplifierOutputVoltagedBV, true, frqrsVm.RightChannel, ct);
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
                        result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(genVoltagedBV, amplifierOutputVoltagedBV, true, frqrsVm.RightChannel, ct);
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
				await Qa40x.SetExpoChirpGen(genVoltagedBV, 0, 24, frqrsVm.RightChannel);

                // If in continous mode we continue sweeping until cancellation requested.
                do
                {
                    await showMessage($"Sweeping...");
                    if (ct.IsCancellationRequested)
                        break;
					var lfrs = await QaLibrary.DoAcquisitions(frqrsVm.Averages, ct, true, true);
                    if (ct.IsCancellationRequested)
						break;

					await showMessage($"Sweeping done", 200);
                    MeasurementResult.FrequencyResponseData = lfrs;
                    MeasurementResult.GainData = CalculateGain(lfrs.FreqInput);
                    UpdateGraph(false);
                    ShowLastMeasurementCursorTexts();

                    bool result = QaLibrary.DetermineAttenuationFromLeftRightSeriesData(true, frqrsVm.RightChannel, lfrs, out double peak_dBV, out int newAttenuation);
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
            await showMessage($"Measurement finished!", 500);

            UpdateGraph(false);

            return ct.IsCancellationRequested;
        }


        private double[] CalculateGain(LeftRightFrequencySeries data)
        {
            double[] gain = new double[data.Left.Length];
            for (int i = 0; i < data.Left.Length; i ++)
            {
                gain[i] = data.Left[i] / (data.Right[i] == 0 ? 0.000000001 : data.Right[i]);
            }

            //double[] gain = data.Left.Zip(data.Right, (left, right) => left / (right == 0 ? 0.000000001 : right)).ToArray();
            return gain;
        }

        void ShowLastMeasurementCursorTexts()
        {
            if (MeasurementResult == null)
                return;

        }

#if false
        void ClearCursorTexts()
        {
            lblCursor_Frequency.Text = "F: 0.00 Hz";
            lblCursor_Amplitude_dBV_L.Text = "0.00 dBV";
            lblCursor_Amplitude_dBV_R.Text = "0.00 dBV";
            lblCursor_Amplitude_V_L.Text = "0.00 V";
            lblCursor_Amplitude_V_R.Text = "0.00 V";
            lblCursor_Gain_times_L.Text = "0.00 x";
            lblCursor_Gain_dB_L.Text = "0.00 dB";
        }

        /// <summary>
        /// Clear the plot
        /// </summary>
        void ClearPlot()
        {
            frqrsPlot.Plot.Clear();
            frqrsPlot.Refresh();
        }
#endif

        /// <summary>
        /// Initialize the magnitude plot
        /// </summary>
        void InitializePlot()
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var frqrsVm = ViewSettings.Singleton.FreqRespVm;

			myPlot.Clear();

            // create a minor tick generator that places log-distributed minor ticks
            //ScottPlot.TickGenerators. minorTickGen = new();
            //minorTickGen.Divisions = 1;

            // create a numeric tick generator that uses our custom minor tick generator
            ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);

            ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
            tickGenY.TargetTickCount = 21;
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


            //thdPlot.Plot.Axes.AutoScale();
			myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(frqrsVm.GraphStartFreq)), Math.Log10(Convert.ToInt32(frqrsVm.GraphEndFreq)), frqrsVm.RangeBottomdB, frqrsVm.RangeTopdB);

			//if (GraphSettings.GraphType == E_FrequencyResponseGraphType.DBV)
			myPlot.Title("Frequency Response (dBV)");
            //else
            //    myPlot.Title("Gain (dB)");

            myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			myPlot.XLabel("Frequency (Hz)");
            myPlot.Axes.Bottom.Label.OffsetX = 330;
            myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Bottom.Label.Bold = false;

			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			// configure tick labels
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			myPlot.Legend.IsVisible = true;
            myPlot.Legend.Orientation = ScottPlot.Orientation.Horizontal;
            myPlot.Legend.Alignment = ScottPlot.Alignment.UpperRight;

            ScottPlot.AxisRules.MaximumBoundary rule = new(
                xAxis: myPlot.Axes.Bottom,
                yAxis: myPlot.Axes.Left,
                limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
                );

            myPlot.Axes.Rules.Clear();
            myPlot.Axes.Rules.Add(rule);

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

			if (measurementResult == null || measurementResult.FrequencyResponseData == null || measurementResult.FrequencyResponseData.FreqInput == null)
                return;

            var freqX = new List<double>();
            var magnY_left = new List<double>();
            var magnY_right = new List<double>();
            var gainY = new List<double>();

            double frequencyStep = measurementResult.FrequencyResponseData.FreqInput.Df;
            double startFrequency = frequencyStep;


            if (graphType == E_FrequencyResponseGraphType.GAIN)
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
                foreach (var step in measurementResult.FrequencyResponseData.FreqInput.Left.Skip(1))        // Skip first bin (DC)
                {
                    freqX.Add(startFrequency);
                    if (showLeftChannel)
                        magnY_left.Add(QaLibrary.ConvertVoltage(step, E_VoltageUnit.Volt, E_VoltageUnit.dBV));

                    startFrequency += frequencyStep;
                }

                if (showRightChannel && measurementResult.MeasurementSettings.RightChannel)
                {
                    foreach (var step in measurementResult.FrequencyResponseData.FreqInput.Right.Skip(1))
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


#if false
        void UpdateGraphChannelSelectors()
        {
            if (MeasurementSettings.RightChannelIsReference)
            {
                chkGraphShowLeftChannel.Checked = true;
                chkGraphShowRightChannel.Checked = false;
                GraphSettings.ShowLeftChannel = true;
                GraphSettings.ShowRightChannel = false;
                chkGraphShowLeftChannel.Visible = false;
                chkGraphShowRightChannel.Visible = false;
                chkEnableLeftChannel.Checked = true;
                chkEnableLeftChannel.Visible = false;
                chkEnableRightChannel.Checked = false;
                chkEnableRightChannel.Visible = false;
                btnGraph_Gain.Visible = true;
            }
            else
            {
                chkGraphShowLeftChannel.Visible = true;
                chkGraphShowRightChannel.Visible = true;
                chkEnableLeftChannel.Visible = true;
                chkEnableRightChannel.Visible = true;
                
                btnGraph_Gain.Visible = false;
                GraphSettings.GraphType = E_FrequencyResponseGraphType.DBV;
                
                
                if (MeasurementSettings.EnableLeftChannel && !MeasurementSettings.EnableRightChannel)
                {
                    // Only single channel. Enable
                    chkGraphShowLeftChannel.Checked = true;
                    chkGraphShowRightChannel.Checked = false;
                    GraphSettings.ShowLeftChannel = true;
                    GraphSettings.ShowRightChannel = false;
                }
                if (MeasurementSettings.EnableRightChannel && !MeasurementSettings.EnableLeftChannel)
                {
                    chkGraphShowLeftChannel.Checked = false;
                    chkGraphShowRightChannel.Checked = true;
                    GraphSettings.ShowLeftChannel = false;
                    GraphSettings.ShowRightChannel = true;
                }
            }
            chkGraphShowLeftChannel.Enabled = MeasurementSettings.EnableLeftChannel && MeasurementSettings.EnableRightChannel;
            chkGraphShowRightChannel.Enabled = MeasurementSettings.EnableLeftChannel && MeasurementSettings.EnableRightChannel;
            pnlCursorsLeft.Visible = GraphSettings.ShowLeftChannel;
            pnlCursorsRight.Visible = GraphSettings.ShowRightChannel;

            grpMeasurements_L.Visible = MeasurementSettings.EnableLeftChannel;
            grpMeasurements_R.Visible = MeasurementSettings.EnableRightChannel && GraphSettings.GraphType == E_FrequencyResponseGraphType.DBV;
            if (GraphSettings.GraphType == E_FrequencyResponseGraphType.DBV)
            {
                grpMeasurements_L.Text = "Measurements left channel";
                pnlCursorsRight.Visible = true;
                lblCursor_LeftChannel.Visible = true;
                lblCursor_RightChannel.Visible = true;
                lblCursorAmplitude.Visible = true;
                lblCursor_Amplitude_dBV_L.Visible = true;
                lblCursor_Amplitude_V_L.Visible = true;

                lblCursorGain.Visible = false;
                lblCursor_Gain_dB_L.Visible = false;
                lblCursor_Gain_times_L.Visible = false;

                lblMeas_MaxAmplitude_L.Text = "Max. amplitude";
                lblMeas_HighestGainFreq.Text = "Highest amplitude freq.";
                gbDbV_Range.Text = "dBV";
            }
            else
            { 
                grpMeasurements_L.Text = "Measurements";
                pnlCursorsRight.Visible = false;
                lblCursor_LeftChannel.Visible = false;
                lblCursor_RightChannel.Visible = false;
                lblCursorAmplitude.Visible = false;
                lblCursor_Amplitude_dBV_L.Visible = false;
                lblCursor_Amplitude_V_L.Visible = false;

                lblCursorGain.Visible = true;
                lblCursor_Gain_dB_L.Visible = true;
                lblCursor_Gain_times_L.Visible = true;

                lblMeas_MaxAmplitude_L.Text = "Max. gain";
                lblMeas_HighestGainFreq.Text = "Highest gain freq.";
                gbDbV_Range.Text = "dB";
            }


            chk1dBBandWidth_L.Enabled = MeasurementSettings.EnableLeftChannel && GraphSettings.ShowLeftChannel;
            chk3dBBandWidth_L.Enabled = MeasurementSettings.EnableLeftChannel && GraphSettings.ShowLeftChannel;
            chk1dBBandWidth_R.Enabled = MeasurementSettings.EnableRightChannel && GraphSettings.ShowRightChannel;
            chk3dBBandWidth_R.Enabled = MeasurementSettings.EnableRightChannel && GraphSettings.ShowRightChannel;
            chk1dBBandWidth_L.Checked = GraphSettings.Show1dBBandwidth_L;
            chk3dBBandWidth_L.Checked = GraphSettings.Show3dBBandwidth_L;
            chk1dBBandWidth_R.Checked = GraphSettings.Show1dBBandwidth_R;
            chk3dBBandWidth_R.Checked = GraphSettings.Show3dBBandwidth_R;

            
        }

        /// <summary>
        /// Attach mouse events to the main graph
        /// </summary>
        void AttachPlotMouseEvent()
        {
            // Attach the mouse move event
            frqrsPlot.MouseMove += (s, e) =>
            {
                SetCursorMarker(s, e, false);
            };

            // Mouse is clicked
            frqrsPlot.MouseDown += (s, e) =>
            {
                SetCursorMarker(s, e, true);      // Set persistent marker
            };

            // Mouse is leaving the graph
            frqrsPlot.SKControl.MouseLeave += (s, e) =>
            {
                if (markerIndex == -1)
                {
                    frqrsPlot.Plot.Remove<Crosshair>();
                    frqrsPlot.Refresh();
                }
            };

        }


        int markerIndex = -1;
        DataPoint markerDataPoint;

        /// <summary>
        /// Set a cursor marker
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="isClick"></param>
        void SetCursorMarker(object s, MouseEventArgs e, bool isClick)
        {
            //if (MeasurementBusy)            // Do not show marker when measurement is still busy
            //    return;

            // determine where the mouse is and get the nearest point
            Pixel mousePixel = new(e.Location.X, e.Location.Y);
            Coordinates mouseLocation = frqrsPlot.Plot.GetCoordinates(mousePixel);
            if (frqrsPlot.Plot.GetPlottables<Scatter>().Count() == 0)
                return;                     // Nothing plotted

            // Get nearest x-location in plotr
            DataPoint nearest1 = frqrsPlot.Plot.GetPlottables<Scatter>().First().Data.GetNearestX(mouseLocation, frqrsPlot.Plot.LastRender);

            // place the crosshair over the highlighted point
            if (nearest1.IsReal)
            {
                float lineWidth = (GraphSettings.ThickLines ? 1.6f : 1);
                LinePattern linePattern = LinePattern.DenselyDashed;

                if (isClick)
                {
                    // Mouse click
                    if (nearest1.Index == markerIndex)          // Clicked point is currently marked
                    {
                        // Remove marker
                        markerIndex = -1;
                        frqrsPlot.Plot.Remove<Crosshair>();
                        return;
                    }
                    else
                    {
                        // Add solid marker line
                        markerIndex = nearest1.Index;           // Remember marker
                        markerDataPoint = nearest1;
                        linePattern = LinePattern.Solid;
                    }
                }
                else
                {
                    // Mouse hoover
                    if (markerIndex != -1)
                        return;                                 // Do not show new marker. There is already a clicked marker
                }

                QaLibrary.PlotCursorMarker(frqrsPlot, lineWidth, linePattern, nearest1);

                // Check if index in StepData array
                if (MeasurementResult.FrequencyResponseData.FreqInput.Left.Count() > nearest1.Index)
                {
                    // Write cursor texts based in plot type
                    if (GraphSettings.GraphType == E_FrequencyResponseGraphType.DBV)
                    {
                        WriteCursorTexts_dBV((uint)nearest1.Index);
                    }
                    else
                    {
                        WriteCursorTexts_Gain((uint)nearest1.Index);
                    }
                }

            }
        }


        void WriteCursorTexts_dBV(uint index)
        {
            if (MeasurementResult == null || MeasurementResult.MeasurementSettings == null || MeasurementResult.FrequencyResponseData == null || index > (MeasurementResult.FrequencyResponseData.FreqInput.Left.Length - 1))
                return;

            var binSize = QaLibrary.CalcBinSize(MeasurementResult.MeasurementSettings.SampleRate, MeasurementResult.MeasurementSettings.FftSize);
            double frequency = QaLibrary.GetBinFrequency(index + 1, binSize);
            lblCursor_Frequency.Text = $"F: {frequency:0.0 Hz}";
            lblCursor_Frequency.Refresh();

            if (GraphSettings.ShowLeftChannel)
            {
                var amplitudeV_L = MeasurementResult.FrequencyResponseData.FreqInput.Left[index + 1];
                var amplitudeDbV_L = QaLibrary.ConvertVoltage(amplitudeV_L, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

                lblCursor_Amplitude_V_L.Text = $"{amplitudeV_L:0.0# dBV}";
                lblCursor_Amplitude_dBV_L.Text = $"{amplitudeDbV_L:0.0# dBV}";
            }

            if (GraphSettings.ShowRightChannel)
            {
                var amplitudeV_R = MeasurementResult.FrequencyResponseData.FreqInput.Right[index + 1];
                var amplitudeDbV_R = QaLibrary.ConvertVoltage(amplitudeV_R, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

                lblCursor_Amplitude_V_R.Text = $"{amplitudeV_R:0.0# dBV}";
                lblCursor_Amplitude_dBV_R.Text = $"{amplitudeDbV_R:0.0# dBV}";
            }
            pnlCursorsLeft.Refresh();
            pnlCursorsRight.Refresh();
        }

        void WriteCursorTexts_Gain(uint index)
        {
            if (MeasurementResult == null || MeasurementResult.MeasurementSettings == null || MeasurementResult.GainData == null || index > (MeasurementResult.GainData.Length - 1))
                return;

            var binSize = QaLibrary.CalcBinSize(MeasurementResult.MeasurementSettings.SampleRate, MeasurementResult.MeasurementSettings.FftSize);
            double frequency = QaLibrary.GetBinFrequency(index + 1, binSize);
            lblCursor_Frequency.Text = $"F: {frequency:0.0 Hz}";
            lblCursor_Frequency.Refresh();

            var gain = MeasurementResult.GainData[index + 1];
            var gainDb = QaLibrary.ConvertVoltage(gain, E_VoltageUnit.Volt, E_VoltageUnit.dBV); 
            lblCursor_Gain_times_L.Text = $"{gain:0.0#} x";
            lblCursor_Gain_dB_L.Text = $"{gainDb:0.0# dB}";
            pnlCursorsLeft.Refresh();
        }


        /// <summary>
        /// Start measurement button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnRun_Click(object sender, EventArgs e)
        {
            MeasurementBusy = true;
            btnStopThdMeasurement.Enabled = true;
            btnStopThdMeasurement.ForeColor = System.Drawing.Color.Black;
            btnSingle.Enabled = false;
            btnSingle.ForeColor = System.Drawing.Color.DimGray;
            btnRun.Enabled = false;
            btnRun.ForeColor = System.Drawing.Color.DimGray;
            ct = new();
            await PerformMeasurement(ct.Token, true);
            Program.MainForm.ClearMessage();
            Program.MainForm.HideProgressBar();
            MeasurementBusy = false;
            btnStopThdMeasurement.Enabled = false;
            btnStopThdMeasurement.ForeColor = System.Drawing.Color.DimGray;
            btnSingle.Enabled = true;
            btnSingle.ForeColor = System.Drawing.Color.Black;
            btnRun.Enabled = true;
            btnRun.ForeColor = System.Drawing.Color.Black;
        }


        private async void btnSingle_Click(object sender, EventArgs e)
        {
            MeasurementBusy = true;
            btnSingle.Enabled = false;
            btnSingle.ForeColor = System.Drawing.Color.DimGray;
            btnStopThdMeasurement.Enabled = true;
            btnStopThdMeasurement.ForeColor = System.Drawing.Color.Black;
            btnRun.Enabled = false;
            btnRun.ForeColor = System.Drawing.Color.DimGray;
            ct = new();
            await PerformMeasurement(ct.Token, false);
            Program.MainForm.ClearMessage();
            Program.MainForm.HideProgressBar();
            MeasurementBusy = false;
            btnStopThdMeasurement.Enabled = false;
            btnStopThdMeasurement.ForeColor = System.Drawing.Color.DimGray;
            btnRun.Enabled = true;
            btnRun.ForeColor = System.Drawing.Color.Black;
            btnSingle.Enabled = true;
            btnSingle.ForeColor = System.Drawing.Color.Black;
        }

        /// <summary>
        /// Start voltage unit is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbGeneratorAmplitudeUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateGeneratorVoltageDisplay();
        }



        private void txtGeneratorAmplitude_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Text has been changed by user typing. Remember unit
            MeasurementSettings.GeneratorAmplitudeUnit = (E_VoltageUnit)cmbGeneratorVoltageUnit.SelectedIndex;
            QaLibrary.AllowNumericInput(sender, e, false);
        }



        private void txtGeneratorAmplitude_TextChanged(object sender, EventArgs e)
        {
            if (txtGeneratorVoltage.Focused)
            {
                MeasurementSettings.GeneratorAmplitude = QaLibrary.ParseTextToDouble(txtGeneratorVoltage.Text, MeasurementSettings.GeneratorAmplitude);
                MeasurementSettings.GeneratorAmplitudeUnit = (E_VoltageUnit)cmbGeneratorVoltageUnit.SelectedIndex;
            }
            ValidateGeneratorAmplitude(sender);
        }


        /// <summary>
        /// Chcek if the start voltage is lower then the end voltage and within the device measurement range
        /// </summary>
        /// <param name="sender"></param>
        private void ValidateGeneratorAmplitude(object sender)
        {
            if (cmbGeneratorVoltageUnit.SelectedIndex == (int)E_VoltageUnit.MilliVolt)
            {
                QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_MV, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_MV);        // mV
            }
            else if (cmbGeneratorVoltageUnit.SelectedIndex == (int)E_VoltageUnit.Volt)
            {
                QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_V, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_V);
            }
            else if (cmbGeneratorVoltageUnit.SelectedIndex == (int)E_VoltageUnit.dBV)
            {
                QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_DBV, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_DBV);
            }
        }


        private void udAverages_ValueChanged(object sender, EventArgs e)
        {
            MeasurementSettings.Averages = Convert.ToUInt16(udAverages.Value);
        }


        private void btnGraph_dBV_Click(object sender, EventArgs e)
        {
            GraphSettings.GraphType = E_FrequencyResponseGraphType.DBV;
            ClearCursorTexts();
            UpdateGraphChannelSelectors();
            btnFitGraphY_Click(sender, e);
        }

        private void btnGraph_Gain_Click(object sender, EventArgs e)
        {
            GraphSettings.GraphType = E_FrequencyResponseGraphType.GAIN;
            ClearCursorTexts();
            UpdateGraphChannelSelectors();
            btnFitGraphY_Click(sender, e);
        }
#endif


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

            if (MeasurementResult != null && MeasurementResult.FrequencyResponseData != null && MeasurementResult.FrequencyResponseData.FreqInput != null)
            {
                BandwidthData bandwidthData3dB = new BandwidthData();
                BandwidthData bandwidthData1dB = new BandwidthData();
                if (MeasurementResult.MeasurementSettings.LeftChannel)
                {
                    bandwidthData3dB.Left = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqInput.Left, MeasurementResult.FrequencyResponseData.FreqInput.Df);
                    bandwidthData1dB.Left = CalculateBandwidth(-1, MeasurementResult.FrequencyResponseData.FreqInput.Left, MeasurementResult.FrequencyResponseData.FreqInput.Df);
                }

                if (MeasurementResult.MeasurementSettings.RightChannel)
                {
                    bandwidthData3dB.Right = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqInput.Right, MeasurementResult.FrequencyResponseData.FreqInput.Df);
                    bandwidthData1dB.Right = CalculateBandwidth(-1, MeasurementResult.FrequencyResponseData.FreqInput.Right, MeasurementResult.FrequencyResponseData.FreqInput.Df);
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
                    var gainBW3dB = CalculateBandwidth(-3, MeasurementResult.GainData, MeasurementResult.FrequencyResponseData.FreqInput.Df);        // Volts is gain

                    var gainBW1dB = CalculateBandwidth(-1, MeasurementResult.GainData, MeasurementResult.FrequencyResponseData.FreqInput.Df);

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



#if false
        /// <summary>
        /// Measurement cancel button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStopThdMeasurement_Click(object sender, EventArgs e)
        {
            ct.Cancel();
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

        private void chkGraphShowLeftChannel_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowLeftChannel = chkGraphShowLeftChannel.Checked;
            UpdateGraphChannelSelectors();
            UpdateGraph(true);
        }

        private void chkGraphShowRightChannel_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowRightChannel = chkGraphShowRightChannel.Checked;
            UpdateGraphChannelSelectors();
            UpdateGraph(true);
        }

        private void chkEnableLeftChannel_CheckedChanged(object sender, EventArgs e)
        {
            MeasurementSettings.EnableLeftChannel = chkEnableLeftChannel.Checked;
            UpdateGraphChannelSelectors();
            UpdateGraph(true);
        }

        private void chkEnableRightChannel_CheckedChanged(object sender, EventArgs e)
        {
            MeasurementSettings.EnableRightChannel = chkEnableRightChannel.Checked;
            UpdateGraphChannelSelectors();
            UpdateGraph(true);
        }



        private void ud_dBV_Graph_Top_ValueChanged(object sender, EventArgs e)
        {
            // Prevent top lower than bottom
            if (ud_Graph_Top.Value - ud_Graph_Bottom.Value < 1)
            {
                ud_Graph_Top.Value = ud_Graph_Bottom.Value + 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.YRangeTop = (double)ud_Graph_Top.Value;
            if (GraphSettings.YRangeTop - GraphSettings.YRangeBottom < 20)
            {
                ud_Graph_Top.Increment = 1;
                ud_Graph_Bottom.Increment = 1;
            }
            else if (GraphSettings.YRangeTop - GraphSettings.YRangeBottom < 50)
            {
                ud_Graph_Top.Increment = 5;
                ud_Graph_Bottom.Increment = 5;
            }
            else
            {
                ud_Graph_Top.Increment = 10;
                ud_Graph_Bottom.Increment = 10;
            }
            UpdateGraph(true);
        }

        private void ud_dBV_Graph_Bottom_ValueChanged(object sender, EventArgs e)
        {
            // Prevent bottom higher than top
            if (ud_Graph_Top.Value - ud_Graph_Bottom.Value < 1)
            {
                ud_Graph_Bottom.Value = ud_Graph_Top.Value - 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.YRangeBottom = (double)ud_Graph_Bottom.Value;
            if (GraphSettings.YRangeTop - GraphSettings.YRangeBottom < 20)
            {
                ud_Graph_Top.Increment = 1;
                ud_Graph_Bottom.Increment = 1;
            }
            else if (GraphSettings.YRangeTop - GraphSettings.YRangeBottom < 50)
            {
                ud_Graph_Top.Increment = 5;
                ud_Graph_Bottom.Increment = 5;
            }
            else
            {
                ud_Graph_Top.Increment = 10;
                ud_Graph_Bottom.Increment = 10;
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


        private void cmbGeneratorType_SelectedIndexChanged(object sender, EventArgs e)
        {
            MeasurementSettings.GeneratorType = (E_GeneratorType)cmbGeneratorType.SelectedIndex;
            ValidateGeneratorAmplitude(txtGeneratorVoltage);
        }

  
        private void cmbSmoothing_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSmoothing.SelectedIndex != -1)
                MeasurementSettings.SmoothDenominator = (int)((KeyValuePair<double, string>)cmbSmoothing.SelectedItem).Key;
        }



        private void chk1dBBandWidth_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.Show1dBBandwidth_L = chk1dBBandWidth_L.Checked;
            UpdateGraph(false);
        }

        private void chk3dBBandWidth_L_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.Show3dBBandwidth_L = chk3dBBandWidth_L.Checked;
            UpdateGraph(false);
        }

        private void chk1dBBandWidth_R_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.Show1dBBandwidth_R = chk1dBBandWidth_R.Checked;
            UpdateGraph(false);
        }

        private void chk3dBBandWidth_R_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.Show3dBBandwidth_R = chk3dBBandWidth_R.Checked;
            UpdateGraph(false);
        }

        private void cmbFrequencySpan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSmoothing.SelectedIndex != -1)
                MeasurementSettings.SampleRate = (uint)((KeyValuePair<double, string>)cmbFrequencySpan.SelectedItem).Key;
        }

        private void btnFitGraphX_Click(object sender, EventArgs e)
        {
            cmbGraph_FreqStart.SelectedIndex = 0;
            switch (MeasurementResult.MeasurementSettings.SampleRate)
            {
                case 48000:
                    ComboBoxHelper.SelectNearestValue(cmbGraph_FreqEnd, 50000);
                    break;
                case 96000:
                    ComboBoxHelper.SelectNearestValue(cmbGraph_FreqEnd, 50000);
                    break;
                case 192000:
                    ComboBoxHelper.SelectNearestValue(cmbGraph_FreqEnd, 100000);
                    break;
            }
           
        }

        private void btnFitGraphY_Click(object sender, EventArgs e)
        {
            BandwidthData bw = new BandwidthData();
            if (GraphSettings.GraphType == E_FrequencyResponseGraphType.GAIN)
            {
                if (MeasurementResult == null || MeasurementResult.GainData == null)
                    return;

                bw.Left = CalculateBandwidth(-3, MeasurementResult.GainData, MeasurementResult.FrequencyResponseData.FreqInput.Df);
                var maxGain = 20 * Math.Log10(bw.Left.HighestAmplitudeVolt);
                var maxValue = (int)Math.Round((maxGain + 2) / 2, MidpointRounding.AwayFromZero) * 2;
                ud_Graph_Top.Value = maxValue;

                var minGain = 20 * Math.Log10(bw.Left.LowestAmplitudeVolt);
                if ((maxValue - minGain) < 10)
                    ud_Graph_Bottom.Value = maxValue - 10;
                else
                    ud_Graph_Bottom.Value = (int)minGain;
            }
            else
            {
                if (MeasurementResult == null || MeasurementResult.FrequencyResponseData == null || MeasurementResult.FrequencyResponseData.FreqInput == null || MeasurementResult.FrequencyResponseData.FreqInput.Left == null)
                    return;

                bw.Left = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqInput.Left, MeasurementResult.FrequencyResponseData.FreqInput.Df);
                bw.Right = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqInput.Right, MeasurementResult.FrequencyResponseData.FreqInput.Df);

                var maxGain = 20 * Math.Log10(Math.Max(bw.Left.HighestAmplitudeVolt, (GraphSettings.ShowRightChannel ? bw.Right.HighestAmplitudeVolt : 0)));
                var maxValue = (int)Math.Round((maxGain + 2) / 2, MidpointRounding.AwayFromZero) * 2;
                ud_Graph_Top.Value = maxValue;

                var minGain = 20 * Math.Log10(Math.Min(bw.Left.LowestAmplitudeVolt, (GraphSettings.ShowRightChannel ? bw.Right.LowestAmplitudeVolt : 100)));
                ud_Graph_Bottom.Value = maxValue - 10;      // 1 dB per tick
                //if ((maxValue - minGain) < 10)
                //    ud_Graph_Bottom.Value = maxValue - 10;      // Mimimum range is 10
                //else 
                //    ud_Graph_Bottom.Value = (int)minGain;

            }
        }

        private void chkRightIsReference_CheckedChanged(object sender, EventArgs e)
        {
            MeasurementSettings.RightChannelIsReference = chkRightIsReference.Checked;
            UpdateGraphChannelSelectors();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/QuantAsylum/QA40x/wiki/Frequency-Response-with-Right-Channel-as-Reference");
        }

        private void cmbLowFrequencyAccuracy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLowFrequencyAccuracy.SelectedIndex != -1)
                MeasurementSettings.FftResolution = (uint)((KeyValuePair<double, string>)cmbLowFrequencyAccuracy.SelectedItem).Key;
        }
#endif
    }
}
