using FftSharp;
using QA40xPlot.Data;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;

namespace QA40xPlot.Actions
{

    public partial class frmBodePlot : Form
    {
        public BodePlotData Data { get; set; }       // Data used in this form instance
        public bool MeasurementBusy { get; set; }                   // Measurement busy state

        private BodePlotMeasurementSettings MeasurementSettings;
        private BodePlotGraphSettings GraphSettings;
        private BodePlotMeasurementResult MeasurementResult;

        CancellationTokenSource ct;                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public frmBodePlot(ref BodePlotData data)
        {
            Data = data;
            MeasurementResult = new(); // TODO. Add to list

            ct = new CancellationTokenSource();

            InitializeComponent();
            PopulateMeasurementSettingsComboBoxes();
            PopulateGraphSettingsComboBoxes();

            MeasurementSettings = new();
            SetDefaultMeasurementSettings(ref MeasurementSettings);
            MeasurementResult.MeasurementSettings = MeasurementSettings;
            SetDefaultGraphSettings(ref GraphSettings);
            
            SetMeasurementControls(MeasurementSettings);
            SetGraphControls(GraphSettings);

            // TODO: depends on graph settings which graph is shown
            if (data.Measurements.Count > 0 && data.Measurements[0].FrequencySteps.Count > 0)
            {
                UpdateGraph(true);
            }

            Program.MainForm.ClearMessage();
            Program.MainForm.HideProgressBar();
            AttachPlotMouseEvent();
        }


        void PopulateMeasurementSettingsComboBoxes()
        {
            var items = new List<KeyValuePair<double, string>>
            {
                new (0, "Input voltage"),
                new (1, "Output voltage")
            };
            ComboBoxHelper.PopulateComboBox(cmbGeneratorType, items);

            items = new List<KeyValuePair<double, string>>
            {
                new (0, "mV"),
                new (1, "V"),
                new (2, "dBV")
            };
            ComboBoxHelper.PopulateComboBox(cmbGeneratorVoltageUnit, items);
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
        void SetDefaultMeasurementSettings(ref BodePlotMeasurementSettings settings)
        {
            // Initialize with default measurement settings
            settings.StartFrequency = 5;
            settings.EndFrequency = 90000;  
            settings.SampleRate = 192000;
            settings.FftSize = 65536 * 2;
            settings.WindowingFunction = Windowing.Hann;
            settings.GeneratorType = E_GeneratorType.OUTPUT_VOLTAGE;         
            settings.GeneratorAmplitude = 0;
            settings.GeneratorAmplitudeUnit = E_VoltageUnit.dBV;
            settings.StepsPerOctave = 6;
        }

        /// <summary>
        /// Initializer with default graph settings
        /// </summary>
        void SetDefaultGraphSettings(ref BodePlotGraphSettings settings)
        {
            settings = new()
            {
                GainTop = 50,
                GainBottom = -50,

                PhaseTop = 190,
                PhaseBottom = -190,

                FrequencyRange_Start = 5,
                FrequencyRange_End = 100000,
               
                ShowGain = true,
                ShowPhase = true,

                Show1dBBandwidth = false,
                Show3dBBandwidth = true,
            
                ThickLines = true,
                ShowDataPoints = false
            };
        }

        /// <summary>
        /// Set initial control values
        /// </summary>
        void SetMeasurementControls(BodePlotMeasurementSettings settings)
        {
            cmbGeneratorType.SelectedIndex = (int)settings.GeneratorType;                     
            txtGeneratorVoltage.Text = settings.GeneratorAmplitude.ToString("#0.0##");
            cmbGeneratorVoltageUnit.SelectedIndex = (int)settings.GeneratorAmplitudeUnit;
           
            txtStartFrequency.Text = settings.StartFrequency.ToString();
            txtEndFrequency.Text = settings.EndFrequency.ToString();
            udStepsOctave.Value = settings.StepsPerOctave;
        }

        void SetGraphControls(BodePlotGraphSettings graphSettings)
        {
            ud_Graph_Top.Value = (decimal)graphSettings.GainTop;
            ud_Graph_Bottom.Value = (decimal)graphSettings.GainBottom;

            udPhase_Top.Value = (decimal)graphSettings.PhaseTop;
            udPhase_Bottom.Value = (decimal)graphSettings.PhaseBottom;

            SetStartFrequencySelectedIndexByFrequency(graphSettings.FrequencyRange_Start);
            SetEndFrequencySelectedIndexByFrequency(graphSettings.FrequencyRange_End);

            chk1dBBandWidth.Checked = GraphSettings.Show1dBBandwidth;
            chk3dBBandWidth.Checked = GraphSettings.Show3dBBandwidth;
           
            chkThickLines.Checked = graphSettings.ThickLines;
            chkShowDataPoints.Checked = graphSettings.ShowDataPoints;

            chkGraphShowPhase.Checked = graphSettings.ShowPhase;    
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

        /// <summary>
        /// Update the generator voltage in the textbox based on the selected unit.
        /// If the unit changes then the voltage will be converted
        /// </summary>
        void UpdateGeneratorVoltageDisplay()
        { 
            switch (cmbGeneratorVoltageUnit.SelectedIndex)
            {
                case 0: // mV
                    txtGeneratorVoltage.Text = ((int)QaLibrary.ConvertVoltage(MeasurementSettings.GeneratorAmplitude, MeasurementSettings.GeneratorAmplitudeUnit, E_VoltageUnit.MilliVolt)).ToString("###0"); // Whole numbers onyl, so cast to integer
                    break;
                case 1: // V
                    txtGeneratorVoltage.Text = QaLibrary.ConvertVoltage(MeasurementSettings.GeneratorAmplitude, MeasurementSettings.GeneratorAmplitudeUnit, E_VoltageUnit.Volt).ToString("#0.0##");
                    break;
                case 2: // dB
                    txtGeneratorVoltage.Text = QaLibrary.ConvertVoltage(MeasurementSettings.GeneratorAmplitude, MeasurementSettings.GeneratorAmplitudeUnit, E_VoltageUnit.dBV).ToString("#0.0#");
                    break;
            }
        }

       



        /// <summary>
        /// Perform the measurement
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>result. false if cancelled</returns>
        async Task<bool> PerformMeasurement(CancellationToken ct)
        {
            ClearPlot();
            ClearCursorTexts();
                        
            var _measurementSettings = MeasurementSettings.Copy();             // Create snapshot so it is not changed during measuring

            // Clear measurement result
            MeasurementResult = new()
            {
                CreateDate = DateTime.Now,
                Show = true,                                      // Show in graph
                MeasurementSettings = _measurementSettings       // Copy measurment settings to measurement results
            };

            // For now clear measurements to allow only one until we have a UI to manage them.
            Data.Measurements.Clear();

            // Add to list
            Data.Measurements.Add(MeasurementResult);

            markerIndex = -1;       // Reset marker
 
            // Check if webserver available and device connected
            if (await QaLibrary.CheckDeviceConnected() == false)
                return false;
           
            // ********************************************************************  
            // Load a settings we want
            // ********************************************************************  
            await Qa40x.SetDefaults();
            await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make it turn on or off
            await Qa40x.SetSampleRate(_measurementSettings.SampleRate);
            await Qa40x.SetBufferSize(_measurementSettings.FftSize);
            await Qa40x.SetWindowing(_measurementSettings.WindowingFunction);
            await Qa40x.SetRoundFrequencies(true);

            try
            {
                // ********************************************************************
                // Determine input level
                // ********************************************************************
                var attenuation = QaLibrary.MAXIMUM_DEVICE_ATTENUATION;
                double genVoltagedBV = -150;

                if (_measurementSettings.GeneratorType == E_GeneratorType.OUTPUT_VOLTAGE)     // Based on output
                {
                    double amplifierOutputVoltagedBV = QaLibrary.ConvertVoltage(_measurementSettings.GeneratorAmplitude, _measurementSettings.GeneratorAmplitudeUnit, E_VoltageUnit.dBV);

                    await Program.MainForm.ShowMessage($"Determining generator amplitude to get an output amplitude of {amplifierOutputVoltagedBV:0.00#} dBV.");

                    // Get input voltage based on desired output voltage
                    attenuation = QaLibrary.DetermineAttenuation(amplifierOutputVoltagedBV);
                    double startAmplitude = -40;  // We start a measurement with a 10 mV signal.
                    var result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(startAmplitude, amplifierOutputVoltagedBV, true, true, ct);
                    if (ct.IsCancellationRequested)
                        return false;
                    genVoltagedBV = result.Item1;

                    if (genVoltagedBV == -150)
                    {
                        await Program.MainForm.ShowMessage($"Could not determine a valid generator amplitude. The amplitude would be {genVoltagedBV:0.00#} dBV.");
                        return false;
                    }

                    // Check if cancel button pressed
                    if (ct.IsCancellationRequested)
                        return false;

                    // Check if amplitude found within the generator range
                    if (genVoltagedBV < 18)
                    {
                        await Program.MainForm.ShowMessage($"Found an input amplitude of {genVoltagedBV:0.00#} dBV. Doing second pass.");

                        // 2nd time for extra accuracy
                        result = await QaLibrary.DetermineGenAmplitudeByOutputAmplitudeWithChirp(genVoltagedBV, amplifierOutputVoltagedBV, true, true, ct);
                        if (ct.IsCancellationRequested)
                            return false;
                        genVoltagedBV = result.Item1;
                        if (genVoltagedBV == -150)
                        {
                            await Program.MainForm.ShowMessage($"Could not determine a valid generator amplitude. The amplitude would be {genVoltagedBV:0.00#} dBV.");
                            return false;
                        }
                    }

                    await Program.MainForm.ShowMessage($"Found an input amplitude of {genVoltagedBV:0.00#} dBV.");
                }
                else
                {
                    genVoltagedBV = QaLibrary.ConvertVoltage(_measurementSettings.GeneratorAmplitude, _measurementSettings.GeneratorAmplitudeUnit, E_VoltageUnit.dBV);
                    await Program.MainForm.ShowMessage($"Determining the best input attenuation for a generator voltage of {genVoltagedBV:0.00#} dBV.");

                    // Determine correct input attenuation
                    var result = await QaLibrary.DetermineAttenuationForGeneratorVoltageWithChirp(genVoltagedBV, QaLibrary.MAXIMUM_DEVICE_ATTENUATION, true, true, ct);
                    if (ct.IsCancellationRequested)
                        return false;
                    attenuation = result.Item1;

                    await Program.MainForm.ShowMessage($"Found correct input attenuation of {attenuation:0} dBV for an amplfier amplitude of {result.Item2:0.00#} dBV.", 500);
                }



                // Set the new input range
                await Qa40x.SetInputRange(attenuation);

                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;


                // ********************************************************************
                // Calculate frequency steps to do
                // ********************************************************************
                var binSize = QaLibrary.CalcBinSize(_measurementSettings.SampleRate, _measurementSettings.FftSize);
                // Generate a list of frequencies
                var stepFrequencies = QaLibrary.GetLineairSpacedLogarithmicValuesPerOctave(_measurementSettings.StartFrequency, _measurementSettings.EndFrequency, _measurementSettings.StepsPerOctave);
                // Translate the generated list to bin center frequencies
                var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, _measurementSettings.SampleRate, _measurementSettings.FftSize);
                stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x <= 95500)                // Filter out values that are out of range 
                    .GroupBy(x => x)                                                                    // Filter out duplicates
                    .Select(y => y.First())
                    .ToArray();

                Program.MainForm.SetupProgressBar(0, stepBinFrequencies.Length);

                // ********************************************************************
                // Step through the list of frequencies
                // ********************************************************************
                for (int f = 0; f < stepBinFrequencies.Length; f++)
                {
                    // test

                    switch (stepBinFrequencies[f])
                    {
                        case <= 24000:
                            _measurementSettings.SampleRate = 48000;
                            break;
                        case <= 48000:
                            _measurementSettings.SampleRate = 96000;
                            break;
                        default:
                            _measurementSettings.SampleRate = 192000;
                            break;
                    }

                    switch ((double)(_measurementSettings.SampleRate / stepBinFrequencies[f]))
                    {                    
                        case < 2048:
                            _measurementSettings.FftSize = 8192;
                            break;
                        case < 4096:
                            _measurementSettings.FftSize = 16384;
                            break;
                        case < 8192:
                            _measurementSettings.FftSize = 32768;
                            break;
                        case < 16384:
                            _measurementSettings.FftSize = 65536;
                            break;
                        case < 32768:
                            _measurementSettings.FftSize = 131072;
                            break;
                        case < 65536:
                            _measurementSettings.FftSize = 262144;
                            break;
                        default:
                            _measurementSettings.FftSize = 524288;
                            break;
                        
                    }
                    await Qa40x.SetSampleRate(_measurementSettings.SampleRate);
                    await Qa40x.SetBufferSize(_measurementSettings.FftSize);



                    /////
                    await Program.MainForm.ShowMessage($"Measuring step {f + 1} of {stepBinFrequencies.Length}.");
                    Program.MainForm.UpdateProgressBar(f + 1);

                    // Set the generator
                    double amplitudeSetpointdBV = genVoltagedBV;
                    await Qa40x.SetGen1(stepBinFrequencies[f], amplitudeSetpointdBV, true);
                    if (f == 0)
                        await Qa40x.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset

                    await Qa40x.DoAcquisition();
                    if (ct.IsCancellationRequested)
                        return false;

                  
                    var measuredTimeSeries = await Qa40x.GetInputTimeSeries();
                    if (ct.IsCancellationRequested)
                        return false;

                    BodePlotStep step = new()
                    {
                        FundamentalFrequency = stepBinFrequencies[f],
                        GeneratorVoltage = QaLibrary.ConvertVoltage(amplitudeSetpointdBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
                        TimeData = measuredTimeSeries
                    };



                    // Left channel
                    var window = new FftSharp.Windows.Hanning();
                    double[] windowed_measured = window.Apply(measuredTimeSeries.Left);
                    System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);
                    double[] power_measured = FftSharp.FFT.Power(spectrum_measured);

                    double[] windowed_ref = window.Apply(measuredTimeSeries.Right);
                    System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);
                    double[] power_ref = FftSharp.FFT.Power(spectrum_ref);

                    var fundamentalBin = QaLibrary.GetBinOfFrequency(step.FundamentalFrequency, _measurementSettings.SampleRate, (uint)spectrum_ref.Length);
                    var phase = (180 / Math.PI) * (spectrum_measured[fundamentalBin].Phase - spectrum_ref[fundamentalBin].Phase);

                    if (phase < -180)
                        phase = phase + 360;
                    else if (phase > 180)
                        phase = phase - 360;

                    // Set data
                    step.MeasuredAmplitude_dBV = power_measured[fundamentalBin] + 3;   // Add 3 dB
                    step.ReferenceAmplitude_dBV = power_ref[fundamentalBin] + 3;
                    step.Gain = step.MeasuredAmplitude_dBV - step.ReferenceAmplitude_dBV;
                    step.Phase_Degree = phase;

                    // Add step data to list
                    MeasurementResult.FrequencySteps.Add(step);

                    UpdateGraph(false);
                    ShowLastMeasurementCursorTexts();

                    // Check if cancel button pressed
                    if (ct.IsCancellationRequested)
                    {
                        await Qa40x.SetOutputSource(OutputSources.Off);                                             // Be sure to switch gen off
                        return false;
                    }
                }

                // Plot bandwidth lines
                MeasurementBusy = false;
                PlotGainBandwidthLines();
                freqPlot.Refresh();
                phasePlot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Turn the generator off
            await Qa40x.SetOutputSource(OutputSources.Off);

            // Show message
            await Program.MainForm.ShowMessage($"Measurement finished!", 500);

            return true;
        }



        void ShowLastMeasurementCursorTexts()
        {
            if (MeasurementResult == null || MeasurementResult.FrequencySteps.Count == 0)
                return;

            var step = MeasurementResult.FrequencySteps.Last();

            
            // Plot current measurement texts
            WriteCursorTexts(step.FundamentalFrequency
                , step.Gain
                , step.Phase_Degree
                );
                
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
        void WriteCursorTexts(double f, double gain, double phase)
        {
            lblCursor_Frequency.Text = $"F: {f:0.0 Hz}";
            lblCursor_Gain.Text = $"{gain:0.0# dB}";
            lblCursor_Phase.Text = $"{phase:0.0 °}";

            scGraphCursors.Panel2.Refresh();
        }   

        
        void ClearCursorTexts()
        {
            WriteCursorTexts(0, 0, 0);
        }

             

        /// <summary>
        /// Clear the plot
        /// </summary>
        void ClearPlot()
        {
            freqPlot.Plot.Clear();
            freqPlot.Refresh();

            phasePlot.Plot.Clear();
            phasePlot.Refresh();
        }



        /// <summary>
        /// Attach mouse events to the main graph
        /// </summary>
        void AttachPlotMouseEvent()
        {
            // Attach the mouse move event
            freqPlot.MouseMove += (s, e) =>
            {
                SetCursorMarker(s, e, false);
            };

            // Mouse is clicked
            freqPlot.MouseDown += (s, e) =>
            {
                SetCursorMarker(s, e, true);      // Set fixed marker
            };

            phasePlot.MouseMove += (s, e) =>
            {
                SetCursorMarker(s, e, false);
            };

            // Mouse is clicked
            phasePlot.MouseDown += (s, e) =>
            {
                SetCursorMarker(s, e, true);      // Set fixed marker
            };

            
        }


        int markerIndex = -1;
        DataPoint markerDataPoint;

        /// <summary>
        /// Draw a cursor marker and update cursor texts
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="isClick"></param>
        void SetCursorMarker(object s, MouseEventArgs e, bool isClick)
        {

            if (MeasurementBusy)
                return;

            // determine where the mouse is and get the nearest point
            Pixel mousePixel = new(e.Location.X, e.Location.Y);
            Coordinates mouseLocation = freqPlot.Plot.GetCoordinates(mousePixel);
            if (freqPlot.Plot.GetPlottables<Scatter>().Count() == 0)
                return;
            DataPoint nearest1 = freqPlot.Plot.GetPlottables<Scatter>().First().Data.GetNearestX(mouseLocation, freqPlot.Plot.LastRender);

            // place the crosshair over the highlighted point
            if (nearest1.IsReal)
            {
                float lineWidth = (GraphSettings.ThickLines ? 1.6f : 1);
                LinePattern linePattern = LinePattern.DenselyDashed;

                if (isClick)
                {
                    // Mouse click
                    if (nearest1.Index == markerIndex)
                    {
                        // Remove marker
                        markerIndex = -1;
                        freqPlot.Plot.Remove<Crosshair>();
                        return;
                    }
                    else
                    {
                        markerIndex = nearest1.Index;
                        markerDataPoint = nearest1;
                        linePattern = LinePattern.Solid;
                    }
                }
                else
                {
                    // Mouse hoover
                    if (markerIndex != -1)
                        return;                     // Do not show new marker. There is already a clicked marker
                }

                QaLibrary.PlotCursorMarker(freqPlot, lineWidth, linePattern, nearest1);
                QaLibrary.PlotCursorMarker(phasePlot, lineWidth, linePattern, nearest1);


                if (MeasurementResult.FrequencySteps.Count > nearest1.Index)
                {
                    BodePlotStep step = MeasurementResult.FrequencySteps[nearest1.Index];
                    WriteCursorTexts(step.FundamentalFrequency, step.Gain, step.Phase_Degree);
                }

            }
        }



   

        /// <summary>
        /// Amount of steps per octave change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void udStepsOctave_ValueChanged(object sender, EventArgs e)
        {
            MeasurementSettings.StepsPerOctave = Convert.ToUInt16(udStepsOctave.Value);
        }


        /// <summary>
        /// Start frequency key press.
        /// Allow integers only
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtStartFreq_KeyPress(object sender, KeyPressEventArgs e)
        {
            QaLibrary.AllowNumericInput(sender, e, true);
        }

        /// <summary>
        /// Start frequency changed.
        /// Show text in red when value invalid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtStartFreq_TextChanged(object sender, EventArgs e)
        {
            QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_FREQUENCY_HZ, 95500);
            MeasurementSettings.StartFrequency = QaLibrary.ParseTextToUint(txtStartFrequency.Text, MeasurementSettings.StartFrequency);
        }

        /// <summary>
        /// End frequency key press.
        /// Allow integers only
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtEndFreq_KeyPress(object sender, KeyPressEventArgs e)
        {
            QaLibrary.AllowNumericInput(sender, e, true);
        }

        /// <summary>
        /// End frequency changed.
        /// Show text in red when value invalid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtEndFreq_TextChanged(object sender, EventArgs e)
        {
            QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_FREQUENCY_HZ, 95500);
            MeasurementSettings.EndFrequency = QaLibrary.ParseTextToUint(txtEndFrequency.Text, MeasurementSettings.EndFrequency);
        }


        /// <summary>
        /// Set graph x axis to data range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFitGraphX_Click(object sender, EventArgs e)
        {
            
        }

     

        private void UpdateGraph(bool settingsChanged)
        {
            freqPlot.Plot.Remove<Scatter>();             // Remove all current lines
            phasePlot.Plot.Remove<Scatter>();             // Remove all current lines
            int resultNr = 0;


            if (settingsChanged)
            {
                InitializeFrequencyPlot();
                InitializePhasePlot();
            }

            foreach (var result in Data.Measurements.Where(m => m.Show))
            {
                PlotFrequencyGraph(result, resultNr++);
                PlotPhaseGraph(result, resultNr++);
            }

            PlotBandwidthLines();

            freqPlot.Plot.Axes.Link(phasePlot, x: true, y: false);
            phasePlot.Plot.Axes.Link(freqPlot, x: true, y: false);

            freqPlot.Refresh();
            phasePlot.Refresh();
        }


        void InitializeFrequencyPlot()
        {
            freqPlot.Plot.Clear();

            // create a minor tick generator that places log-distributed minor ticks
            //ScottPlot.TickGenerators. minorTickGen = new();
            //minorTickGen.Divisions = 1;

            // create a numeric tick generator that uses our custom minor tick generator
            ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(1);

            ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
            tickGenY.TargetTickCount = 21;
            tickGenY.MinorTickGenerator = minorTickGen;

            // tell the left axis to use our custom tick generator
            freqPlot.Plot.Axes.Left.TickGenerator = tickGenY;

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

            freqPlot.Plot.Axes.Bottom.TickGenerator = tickGenX;

            // show grid lines for major ticks
            freqPlot.Plot.Grid.MajorLineColor = Colors.Black.WithOpacity(.35);
            freqPlot.Plot.Grid.MajorLineWidth = 1;
            freqPlot.Plot.Grid.MinorLineColor = Colors.Black.WithOpacity(.15);
            freqPlot.Plot.Grid.MinorLineWidth = 1;


            //thdPlot.Plot.Axes.AutoScale();
            if (cmbGraph_FreqStart.SelectedIndex > -1 && cmbGraph_FreqEnd.SelectedIndex > -1)
                freqPlot.Plot.Axes.SetLimits(Math.Log10(GraphSettings.FrequencyRange_Start), Math.Log10(Convert.ToDouble(GraphSettings.FrequencyRange_End)), GraphSettings.GainBottom, GraphSettings.GainTop);

            freqPlot.Plot.Title("Gain (dB)");

            freqPlot.Plot.Axes.Title.Label.FontSize = 17;

            freqPlot.Plot.XLabel("Frequency (Hz)");
            freqPlot.Plot.Axes.Bottom.Label.OffsetX = 330;
            freqPlot.Plot.Axes.Bottom.Label.FontSize = 15;
            freqPlot.Plot.Axes.Bottom.Label.Bold = false;


            freqPlot.Plot.Legend.IsVisible = false;
            //freqPlot.Plot.Legend.Orientation = ScottPlot.Orientation.Horizontal;
            //freqPlot.Plot.Legend.Alignment = ScottPlot.Alignment.UpperRight;

            PixelPadding padding = new(50, 20, 20, 40);          // Make the same as the other graph
            freqPlot.Plot.Layout.Fixed(padding);


            ScottPlot.AxisRules.MaximumBoundary rule = new(
                xAxis: freqPlot.Plot.Axes.Bottom,
                yAxis: freqPlot.Plot.Axes.Left,
                limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
                );

            freqPlot.Plot.Axes.Rules.Clear();
            freqPlot.Plot.Axes.Rules.Add(rule);

            //freqPlot.Refresh();
        }


        /// <summary>
        /// Plot the magnitude graph
        /// </summary>
        /// <param name="measurementResult">Data to plot</param>
        void PlotFrequencyGraph(BodePlotMeasurementResult measurementResult, int measurementNr)
        {
            if (measurementResult == null || measurementResult.FrequencySteps == null || !measurementResult.FrequencySteps.Any())
                return;

            var freqX = new List<double>();
            var gainY = new List<double>();

            foreach (var step in measurementResult.FrequencySteps)
            {
                freqX.Add(step.FundamentalFrequency); 
                gainY.Add(step.Gain);
            }


            double[] logFreqX = freqX.Select(Math.Log10).ToArray();

      
            float lineWidth = GraphSettings.ThickLines ? 1.6f : 1;
            float markerSize = GraphSettings.ShowDataPoints ? lineWidth + 3 : 1;

            var colors = new GraphColors();
            int color = measurementNr * 2;

            void AddPlot(List<double> yValues, string legendText, int colorIndex, LinePattern linePattern)
            {
                if (yValues.Count == 0) return;
                var logYValues = yValues.ToArray();
                var plot = freqPlot.Plot.Add.Scatter(logFreqX, logYValues);
                plot.LineWidth = lineWidth;
                plot.Color = colors.GetColor(colorIndex, color);
                plot.MarkerSize = markerSize;
                plot.LegendText = legendText;
                plot.LinePattern = linePattern;
            }
 
            if (gainY.Count > 0)
                AddPlot(gainY, "Gain", 3, LinePattern.Solid);

            if (markerIndex != -1)
                QaLibrary.PlotCursorMarker(freqPlot, lineWidth, LinePattern.Solid, markerDataPoint);

            freqPlot.Refresh();
        }




        void InitializePhasePlot()
        {
            phasePlot.Plot.Clear();
            // create a numeric tick generator that uses our custom minor tick generator
            ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(1);

            ScottPlot.TickGenerators.NumericManual tickGenY = new();
            //tickGenY.TargetTickCount = 21;
            //tickGenY.MinorTickGenerator = minorTickGen;

            // tell the left axis to use our custom tick generator
            tickGenY.AddMajor(30, "30");
            tickGenY.AddMajor(60, "60");
            tickGenY.AddMajor(90, "90");
            tickGenY.AddMajor(120, "120");
            tickGenY.AddMajor(150, "150");
            tickGenY.AddMajor(180, "180");
            tickGenY.AddMajor(0, "0");
            tickGenY.AddMajor(-30, "-30");
            tickGenY.AddMajor(-60, "-60");
            tickGenY.AddMajor(-90, "-90");
            tickGenY.AddMajor(-120, "-120");
            tickGenY.AddMajor(-150, "-150");
            tickGenY.AddMajor(-180, "-180");
            phasePlot.Plot.Axes.Left.TickGenerator = tickGenY;

            // create a minor tick generator that places log-distributed minor ticks
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenX = new();

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

            phasePlot.Plot.Axes.Bottom.TickGenerator = tickGenX;

            // show grid lines for major ticks
            phasePlot.Plot.Grid.MajorLineColor = Colors.Black.WithOpacity(.35);
            phasePlot.Plot.Grid.MajorLineWidth = 1;
            phasePlot.Plot.Grid.MinorLineColor = Colors.Black.WithOpacity(.15);
            phasePlot.Plot.Grid.MinorLineWidth = 1;


            //thdPlot.Plot.Axes.AutoScale();
            if (cmbGraph_FreqStart.SelectedIndex > -1 && cmbGraph_FreqEnd.SelectedIndex > -1)
                phasePlot.Plot.Axes.SetLimits(Math.Log10(GraphSettings.FrequencyRange_Start), Math.Log10(Convert.ToDouble(GraphSettings.FrequencyRange_End)), GraphSettings.PhaseBottom, GraphSettings.PhaseTop);


            phasePlot.Plot.Title("Phase (degrees)");

            phasePlot.Plot.Axes.Title.Label.FontSize = 17;

            phasePlot.Plot.XLabel("Frequency (Hz)");
            phasePlot.Plot.Axes.Bottom.Label.OffsetX = 330;
            phasePlot.Plot.Axes.Bottom.Label.FontSize = 15;
            phasePlot.Plot.Axes.Bottom.Label.Bold = false;
       

            phasePlot.Plot.Legend.IsVisible = false;
            //phasePlot.Plot.Legend.Orientation = ScottPlot.Orientation.Horizontal;
            //phasePlot.Plot.Legend.Alignment = ScottPlot.Alignment.UpperRight;

            PixelPadding padding = new(50, 20, 30, 50);          // Make the same as the other graph
            phasePlot.Plot.Layout.Fixed(padding);


            ScottPlot.AxisRules.MaximumBoundary rule = new(
                xAxis: phasePlot.Plot.Axes.Bottom,
                yAxis: phasePlot.Plot.Axes.Left,
                limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 200)
                );

            phasePlot.Plot.Axes.Rules.Clear();
            phasePlot.Plot.Axes.Rules.Add(rule);

            //phasePlot.Refresh();
        }



        /// <summary>
        /// Plot the magnitude graph
        /// </summary>
        /// <param name="measurementResult">Data to plot</param>
        void PlotPhaseGraph(BodePlotMeasurementResult measurementResult, int measurementNr)
        {
            if (measurementResult == null || measurementResult.FrequencySteps == null || !measurementResult.FrequencySteps.Any())
                return;

            var freqX = new List<double>();
            var phase_l = new List<double>();
            var phase_r = new List<double>();

            foreach (var step in measurementResult.FrequencySteps)
            {
                freqX.Add(step.FundamentalFrequency);
                phase_l.Add(step.Phase_Degree);
            }


            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            float lineWidth = GraphSettings.ThickLines ? 1.6f : 1;
            float markerSize = GraphSettings.ShowDataPoints ? lineWidth + 3 : 1;

            var colors = new GraphColors();
            int color = measurementNr * 2;

            void AddPlot(List<double> yValues, string legendText, int colorIndex, LinePattern linePattern)
            {
                if (yValues.Count == 0) return;
                var logYValues = yValues.ToArray();
                var plot = phasePlot.Plot.Add.Scatter(logFreqX, logYValues);
                plot.LineWidth = lineWidth;
                plot.Color = colors.GetColor(colorIndex, color);
                plot.MarkerSize = markerSize;
                plot.LegendText = legendText;
                plot.LinePattern = linePattern;
            }
    
            AddPlot(phase_l, "Phase", 0, LinePattern.Solid);

           
            if (markerIndex != -1)
                QaLibrary.PlotCursorMarker(phasePlot, lineWidth, LinePattern.Solid, markerDataPoint);

            phasePlot.Refresh();
        }



        private void PlotBandwidthLines()
        {
            // Plot
            PlotGainBandwidthLines();
        }


        void PlotGainBandwidthLines()
        {
            // Remove old lines
            freqPlot.Plot.Remove<VerticalLine>();
            freqPlot.Plot.Remove<Arrow>();
            freqPlot.Plot.Remove<Text>();

            // GAIN
            if (MeasurementResult != null && MeasurementResult.FrequencySteps != null)
            {

                // Interpolation to more points
                if (MeasurementResult.FrequencySteps.Count > 5 && !MeasurementBusy)
                {
                    // Gain BW
                    var gainBW3dB = CalculateGainBandwidth(-3, MeasurementResult.FrequencySteps);        // Volts is gain
                    lblMeas_BW3_L.Text = AutoUnitText(gainBW3dB.Bandwidth, "Hz", 1);
                    lblMeas_BW3_low_L.Text = AutoUnitText(gainBW3dB.LowerFreq, "Hz", 1);
                    lblMeas_BW3_high_L.Text = AutoUnitText(gainBW3dB.UpperFreq, "Hz", 1);
                    lblMeas_Gain_L.Text = gainBW3dB.HighestGain.ToString("0.00") + " x";
                    lblMeas_Gain_dB_L.Text = gainBW3dB.HighestGain.ToString("0." + new string('0', 2)) + " dB";
                    lblMeas_Highest_Freq_L.Text = AutoUnitText(gainBW3dB.HighestGainFreq, "Hz", 1);

                    var gainBW1dB = CalculateGainBandwidth(-1, MeasurementResult.FrequencySteps);
                    lblMeas_BW1_L.Text = AutoUnitText(gainBW1dB.Bandwidth, "Hz", 1);
                    lblMeas_BW1_low_L.Text = AutoUnitText(gainBW1dB.LowerFreq, "Hz", 1);
                    lblMeas_BW1_high_L.Text = AutoUnitText(gainBW1dB.UpperFreq, "Hz", 1);

                    // Draw bandwidth lines
                    var colors = new GraphColors();
                    float lineWidth = GraphSettings.ThickLines ? 1.6f : 1;


                    if (GraphSettings.Show3dBBandwidth)
                        DrawBandwithLines(3, gainBW3dB, 0);


                    if (GraphSettings.Show1dBBandwidth)
                        DrawBandwithLines(1, gainBW1dB, 1);
                }
            }
        }

    

        class BandwidthData
        {
            public double LowestGain;
            public double LowestGainFreq;

            public double HighestGain;
            public double HighestGainFreq;

            public double LowerFreq;
            public double LowerFreqGain;

            public double UpperFreq;
            public double UpperFreqGain;

            public double Bandwidth;
        }

        BandwidthData CalculateGainBandwidth(double dB, List<BodePlotStep> data)
        {
            BandwidthData bandwidthData = new BandwidthData();

            if (data == null)
                return bandwidthData;

            BodePlotStep lowestGainStep = data.OrderBy(v => v.Gain).First();
            bandwidthData.LowestGain = lowestGainStep.Gain;
            bandwidthData.LowestGainFreq = lowestGainStep.FundamentalFrequency;

            // Get highest amplitude
            BodePlotStep highestGainStep = data.OrderBy(v => v.Gain).Last();
            bandwidthData.HighestGain = highestGainStep.Gain;
            bandwidthData.HighestGainFreq = highestGainStep.FundamentalFrequency;

            // Get lower frequency
            var lowerFreq_left = data.Where(d => d.FundamentalFrequency < bandwidthData.HighestGainFreq)
                .Select(n => new { n.Gain, n.FundamentalFrequency, delta = Math.Abs(n.Gain - (bandwidthData.HighestGain + dB)) })
                .OrderBy(p => p.delta)
                .FirstOrDefault();


            if (lowerFreq_left != default)
            {

                bandwidthData.LowerFreqGain = lowerFreq_left.Gain;
                bandwidthData.LowerFreq = lowerFreq_left.FundamentalFrequency;
            }
            else
            {
                bandwidthData.LowerFreqGain = data.First().Gain;
                bandwidthData.LowerFreq = data.First().FundamentalFrequency;
            }

            // Get upper frequency
            var upperFreq_left = data.Where(d => d.FundamentalFrequency > bandwidthData.HighestGainFreq)
                .Select(n => new { n.Gain, n.FundamentalFrequency, delta = Math.Abs(n.Gain - (bandwidthData.HighestGain + dB)) })
                .OrderBy(p => p.delta)
                .FirstOrDefault();

            if (upperFreq_left != default)
            {
                bandwidthData.UpperFreqGain = upperFreq_left.Gain;
                bandwidthData.UpperFreq = upperFreq_left.FundamentalFrequency;
            }
            else
            {
                bandwidthData.UpperFreqGain = data.Last().Gain;
                bandwidthData.UpperFreq = data.Last().FundamentalFrequency;
            }

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

        void DrawBandwithLines(int gain, BandwidthData bwData, int colorRange)
        {
            var colors = new GraphColors();
            float lineWidth = GraphSettings.ThickLines ? 1.6f : 1;

            // Low frequency vertical line
            var lowerFreq_dB_left = Math.Log10(bwData.LowerFreq);
            AddVerticalLine(lowerFreq_dB_left, bwData.LowerFreqGain, colors.GetColor(colorRange, 2), lineWidth);

            // High frequency vertical line
            var upperFreq_dB_left = Math.Log10(bwData.UpperFreq);
            AddVerticalLine(upperFreq_dB_left, bwData.UpperFreqGain, colors.GetColor(colorRange, 2), lineWidth);

            // Bandwidht arrow
            AddArrow(lowerFreq_dB_left, bwData.UpperFreqGain, upperFreq_dB_left, bwData.UpperFreqGain, colors.GetColor(colorRange, 4), lineWidth);

            // Bandwitdh text
            var lowerFreq = Math.Log10(bwData.LowerFreq);
            var upperFreq = Math.Log10(bwData.UpperFreq);

            var bwText = $"B{gain:0}: {bwData.Bandwidth:0 Hz}";
            if (bwData.Bandwidth > 1000)
                bwText = $"B{gain:0}: {(bwData.Bandwidth / 1000):0.00# kHz}";
            if (bwData.UpperFreq > 96000)
                bwText = $"B{gain:0}: > 96 kHz";
            AddText(bwText, (lowerFreq + ((upperFreq - lowerFreq) / 2)), bwData.UpperFreqGain, colors.GetColor(colorRange, 8), -35, -10);

            // Low frequency text
            var bwLowF = $"{bwData.LowerFreq:0 Hz}";
            if (bwData.LowerFreq > 1000)
                bwLowF = $"{(bwData.LowerFreq / 1000):0.00# kHz}";
            AddText(bwLowF, lowerFreq_dB_left, freqPlot.Plot.Axes.GetLimits().Bottom, colors.GetColor(colorRange, 8), -20, -30);

            // High frequency text         
            var bwHighF = $"{bwData.UpperFreq:0 Hz}";
            if (bwData.UpperFreq > 1000)
                bwHighF = $"{(bwData.UpperFreq / 1000):0.00# kHz}";
            AddText(bwHighF, upperFreq_dB_left, freqPlot.Plot.Axes.GetLimits().Bottom, colors.GetColor(colorRange, 8), -20, -30);
        }


        void AddVerticalLine(double x, double maximum, ScottPlot.Color color, float lineWidth)
        {
            var line = freqPlot.Plot.Add.VerticalLine(x);
            line.Maximum = maximum;
            line.Color = color;
            line.LineWidth = lineWidth;
            line.LinePattern = LinePattern.DenselyDashed;
        }

        void AddArrow(double x1, double y1, double x2, double y2, ScottPlot.Color color, float lineWidth)
        {
            Coordinates arrowTip = new Coordinates(x1, y1);
            Coordinates arrowBase = new Coordinates(x2, y2);
            var arrow = freqPlot.Plot.Add.Arrow(arrowTip, arrowBase);
            arrow.ArrowStyle.LineWidth = lineWidth;
            arrow.ArrowStyle.ArrowheadLength = 12;
            arrow.ArrowStyle.ArrowheadWidth = 8;
            arrow.ArrowShape = new ScottPlot.ArrowShapes.DoubleLine();
            arrow.ArrowLineColor = color;
        }

        void AddText(string text, double x, double y, ScottPlot.Color backgroundColor, int offsetX, int offsetY)
        {
            var txt = freqPlot.Plot.Add.Text(text, x, y);
            txt.LabelFontSize = 12;
            txt.LabelBorderColor = Colors.Black;
            txt.LabelBorderWidth = 1;
            txt.LabelPadding = 2;
            txt.LabelBold = false;
            txt.LabelBackgroundColor = backgroundColor;
            txt.OffsetX = offsetX;
            txt.OffsetY = offsetY;
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


        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void scGraphCursors_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }



        private void cmbGeneratorType_SelectedIndexChanged(object sender, EventArgs e)
        {
            MeasurementSettings.GeneratorType = (E_GeneratorType)cmbGeneratorType.SelectedIndex;
            ValidateGeneratorAmplitude(txtGeneratorVoltage);
        }

        private void txtGeneratorVoltage_TextChanged(object sender, EventArgs e)
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

        private void cmbGeneratorVoltageUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateGeneratorVoltageDisplay();
        }

        private void chk1dBBandWidth_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.Show1dBBandwidth = chk1dBBandWidth.Checked;
            UpdateGraph(false);
        }

        private void chk3dBBandWidth_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.Show3dBBandwidth = chk3dBBandWidth.Checked;
            UpdateGraph(false);
        }

        private void ud_Graph_Top_ValueChanged(object sender, EventArgs e)
        {
            // Prevent top lower than bottom
            if (ud_Graph_Top.Value - ud_Graph_Bottom.Value < 1)
            {
                ud_Graph_Top.Value = ud_Graph_Bottom.Value + 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.GainTop = (double)ud_Graph_Top.Value;
            if (GraphSettings.GainTop - GraphSettings.GainBottom < 20)
            {
                ud_Graph_Top.Increment = 1;
                ud_Graph_Bottom.Increment = 1;
            }
            else if (GraphSettings.GainTop - GraphSettings.GainBottom < 50)
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

        private void ud_Graph_Bottom_ValueChanged(object sender, EventArgs e)
        {
            // Prevent bottom higher than top
            if (ud_Graph_Top.Value - ud_Graph_Bottom.Value < 1)
            {
                ud_Graph_Bottom.Value = ud_Graph_Top.Value - 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.GainBottom = (double)ud_Graph_Bottom.Value;
            if (GraphSettings.GainTop - GraphSettings.GainBottom < 20)
            {
                ud_Graph_Top.Increment = 1;
                ud_Graph_Bottom.Increment = 1;
            }
            else if (GraphSettings.GainTop - GraphSettings.GainBottom < 50)
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

        /// <summary>
        /// Resize the graphs when their parent panel is resized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void scGraphSettings_Panel1_Resize(object sender, EventArgs e)
        {
            ResizeGraphs();
        }


        void ResizeGraphs()
        {
            if (GraphSettings == null)
                return;

            if (GraphSettings.ShowGain && GraphSettings.ShowPhase)
            {
                freqPlot.Width = scGraphSettings.Panel1.Width;
                freqPlot.Height = (int)((scGraphSettings.Panel1.Height - 10) / 1.6);
                freqPlot.Top = 0;
                freqPlot.Left = 0;
                freqPlot.Visible = true;

                phasePlot.Width = freqPlot.Width;
                phasePlot.Height = scGraphSettings.Panel1.Height - freqPlot.Height - 5;
                phasePlot.Top = freqPlot.Height + 5;
                phasePlot.Left = 0;
                phasePlot.Visible = true;
            }
            else if (GraphSettings.ShowGain)
            {
                freqPlot.Width = scGraphSettings.Panel1.Width;
                freqPlot.Height = scGraphSettings.Panel1.Height;
                freqPlot.Top = 0;
                freqPlot.Left = 0;
                freqPlot.Visible = true;
                phasePlot.Visible = false;
            }
            else
            {
                phasePlot.Width = scGraphSettings.Panel1.Width;
                phasePlot.Height = scGraphSettings.Panel1.Height;
                phasePlot.Top = 0;
                phasePlot.Left = 0;
                freqPlot.Visible = false;
                phasePlot.Visible = true;
            }
        }


        private async void btnStartThdMeasurement_Click(object sender, EventArgs e)
        {
            MeasurementBusy = true;
            btnStopThdMeasurement.Enabled = true;
            btnStopThdMeasurement.ForeColor = System.Drawing.Color.Black;
            btnStartThdMeasurement.Enabled = false;
            btnStartThdMeasurement.ForeColor = System.Drawing.Color.DimGray;
            ct = new();
            await PerformMeasurement(ct.Token);
            Program.MainForm.ClearMessage();
            Program.MainForm.HideProgressBar();
            MeasurementBusy = false;
            btnStopThdMeasurement.Enabled = false;
            btnStopThdMeasurement.ForeColor = System.Drawing.Color.DimGray;
            btnStartThdMeasurement.Enabled = true;
            btnStartThdMeasurement.ForeColor = System.Drawing.Color.Black;
        }

        private void btnStopThdMeasurement_Click_1(object sender, EventArgs e)
        {
            ct.Cancel();
        }

        private void chkGraphShowPhase_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowPhase = chkGraphShowPhase.Checked;
            UpdateGraph(true);
            ResizeGraphs();
        }

        private void chkGraphShowGain_CheckedChanged(object sender, EventArgs e)
        {
            GraphSettings.ShowGain = chkGraphShowGain.Checked;
            UpdateGraph(true);
            ResizeGraphs();
        }

        private void btnFitGraphX_Click_1(object sender, EventArgs e)
        {
            SetStartFrequencySelectedIndexByFrequency(MeasurementSettings.StartFrequency);
            SetEndFrequencySelectedIndexByFrequency(MeasurementSettings.EndFrequency);
        }

        private void btnD_FitGraphY_Click(object sender, EventArgs e)
        {
            if (MeasurementResult.FrequencySteps.Count == 0)
                return;

            // Determine top Y
            decimal maxGain = (int)MeasurementResult.FrequencySteps.Max(d => d.Gain);
            maxGain = ((int)Math.Ceiling(maxGain / 2) * 2) + 2;
            ud_Graph_Top.Value = maxGain;

            // Bottom y. Try 1 dB per division
            decimal minGain = (int)MeasurementResult.FrequencySteps.Min(d => d.Gain);
            if ((maxGain - minGain) < 15)
                ud_Graph_Bottom.Value = maxGain - 10;
            else
                ud_Graph_Bottom.Value = ((int)Math.Floor(minGain / 10) * 10) - 5;
        }

   
        private void btnPhase_FitGraph_Click(object sender, EventArgs e)
        {
            if (MeasurementResult.FrequencySteps.Count == 0)
                return;

            // Determine top Y
            double maxPhase = (int)MeasurementResult.FrequencySteps.Max(d => d.Phase_Degree);
            udPhase_Top.Value = (int)Math.Ceiling(maxPhase / 10) * 10 + 10;

            // Bottom y. Try 1 dB per division
            double minPhase = MeasurementResult.FrequencySteps.Min(d => d.Phase_Degree);
            udPhase_Bottom.Value = (int)Math.Floor(minPhase / 10) * 10 - 10;
        }

        private void btnPhase180_Click(object sender, EventArgs e)
        {
            udPhase_Top.Value = 190;
            GraphSettings.PhaseTop = 190;

            udPhase_Bottom.Value = -190;
            GraphSettings.PhaseBottom = -190;
            UpdateGraph(true);
        }


        private void ud_Graph_Phase_Top_ValueChanged(object sender, EventArgs e)
        {
            // Prevent top lower than bottom
            if (udPhase_Top.Value - udPhase_Bottom.Value < 1)
            {
                udPhase_Top.Value = udPhase_Bottom.Value + 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.PhaseTop = (double)udPhase_Top.Value;
            if (GraphSettings.PhaseTop - GraphSettings.PhaseBottom < 20)
            {
                udPhase_Top.Increment = 1;
                udPhase_Bottom.Increment = 1;
            }
            else if (GraphSettings.PhaseTop - GraphSettings.PhaseBottom < 50)
            {
                udPhase_Top.Increment = 5;
                udPhase_Bottom.Increment = 5;
            }
            else
            {
                udPhase_Top.Increment = 10;
                udPhase_Bottom.Increment = 10;
            }
            UpdateGraph(true);
        }
        private void ud_Graph_Phase_Bottom_ValueChanged(object sender, EventArgs e)
        {
            // Prevent bottom higher than top
            if (udPhase_Top.Value - udPhase_Bottom.Value < 1)
            {
                udPhase_Bottom.Value = udPhase_Top.Value - 1;
            }

            // Change increment when top and bottom closer together
            GraphSettings.PhaseBottom = (double)udPhase_Bottom.Value;
            if (GraphSettings.PhaseTop - GraphSettings.PhaseBottom < 20)
            {
                udPhase_Top.Increment = 1;
                ud_Graph_Bottom.Increment = 1;
            }
            else if (GraphSettings.PhaseTop - GraphSettings.PhaseBottom < 50)
            {
                udPhase_Top.Increment = 5;
                ud_Graph_Bottom.Increment = 5;
            }
            else
            {
                udPhase_Top.Increment = 10;
                ud_Graph_Bottom.Increment = 10;
            }
            UpdateGraph(true);
        }
    }
}