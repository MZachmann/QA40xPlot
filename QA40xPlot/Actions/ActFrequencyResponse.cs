using FftSharp;
using Newtonsoft.Json.Linq;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.AxisRules;
using ScottPlot.Plottables;
using System.Data;
using System.Numerics;
using System.Windows;
using static FreqRespViewModel;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace QA40xPlot.Actions
{

    public partial class ActFrequencyResponse : ActBase
    {
        public FrequencyResponseData Data { get; set; }       // Data used in this form instance
		private readonly Views.PlotControl frqrsPlot;
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;

		private FrequencyResponseMeasurementResult MeasurementResult;

        CancellationTokenSource ct;                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActFrequencyResponse(ref FrequencyResponseData data, Views.PlotControl graphFreq, Views.PlotControl graphFft, Views.PlotControl graphTime)
		{
            Data = data;
            frqrsPlot = graphFreq;
			fftPlot = graphFft;
			timePlot = graphTime;

			// Show empty graphs
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 100000, -180, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -2, 2);

			MeasurementResult = new(ViewSettings.Singleton.FreqRespVm); // TODO. Add to list
            ct = new CancellationTokenSource();

            UpdateGraph(true);
        }

        public void DoCancel()
        {
            ct.Cancel();
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
												  // Show empty graphs
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 40000, -180, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -2, 2); 
            
            UpdateGraph(true);

			await PerformMeasurement(ct.Token, false);
			await showMessage("Finished");
			vm.IsRunning = false;
            vm.HasExport = MeasurementResult.GainFrequencies.Count > 0;
		}


		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();
            var frsqVm = ViewSettings.Singleton.FreqRespVm;
			var freqs = this.MeasurementResult?.GainFrequencies;
            if (freqs == null || freqs.Count == 0)
                return null;

            db.FreqData = freqs;        // test frequencies
            var ttype = GetTestingType(MeasurementResult.MeasurementSettings.TestType);
            switch( ttype)
            {
                case TestingType.Response:
					if (frsqVm.ShowRight && !frsqVm.ShowLeft)
					{
						db.LeftData = MeasurementResult.GainData.Select(x => x.Imaginary).ToList();
					}
					else
					{
						db.LeftData = MeasurementResult.GainData.Select(x => x.Real).ToList();
					}
					break;
                case TestingType.Gain:
					db.LeftData = MeasurementResult.GainData.Select(x => x.Magnitude).ToList();
					db.PhaseData = MeasurementResult.GainData.Select(x => x.Phase).ToList();
					break;
				case TestingType.Impedance:
                    {
						double rref = MathUtil.ToDouble(MeasurementResult.MeasurementSettings.ZReference);
						db.LeftData = MeasurementResult.GainData.Select(x => rref * ToImpedance(x).Magnitude).ToList();
						// YValues = gainY.Select(x => rref * x.Magnitude/(1-x.Magnitude)).ToArray();
						db.PhaseData = MeasurementResult.GainData.Select(x => 180 * ToImpedance(x).Phase / Math.PI).ToList();
					}
					break;
            }
			return db;
		}

        // run a capture to get complex gain at a frequency
        async Task<Complex> GetGain(double showfreq, FreqRespViewModel msr, TestingType ttype)
        {
			if (ct.Token.IsCancellationRequested)
				return new();
			var lfrs = await QaLibrary.DoAcquisitions(1, ct.Token, false, true);
            if (lfrs == null)
                return new();
			MeasurementResult.FrequencyResponseData = lfrs;
			var ga = CalculateGain(showfreq, lfrs, ttype == TestingType.Response); // gain,phase or gain1,gain2
            return ga;
		}

		public TestingType GetTestingType(string type)
		{
            return (TestingType)TestTypes.IndexOf(type);
		}

        public Tuple<double, double, double> LookupX(double freq)
        {
			var freqs = MeasurementResult.GainFrequencies;
			Tuple<double, double, double> tup = Tuple.Create(1.0,1.0,1.0);
			if (freqs != null && freqs.Count > 0)
            {
                var values = MeasurementResult.GainData;
                // find nearest frequency from list
                var bin = freqs.Count(x => x < freq)-1;    // find first freq less than me
                if (bin == -1)
                    bin = 0;
                var fnearest = freqs[bin];
                if (bin < (freqs.Count-1) && Math.Abs(freq - fnearest) > Math.Abs(freq - freqs[bin + 1]))
                {
                    bin++;
                }

                var frsqVm = ViewSettings.Singleton.FreqRespVm;
                var ttype = GetTestingType(frsqVm.TestType);
                switch(ttype)
                {
                    case TestingType.Response:
						// send freq, gain, gain2
						tup = Tuple.Create(freqs[bin], values[bin].Real, values[bin].Imaginary);
                        break;
                    case TestingType.Impedance:
                        {   // send freq, ohms, phasedeg
							double rref = MathUtil.ToDouble(MeasurementResult.MeasurementSettings.ZReference);
							var ohms = rref * ToImpedance(MeasurementResult.GainData[bin]).Magnitude;
							tup = Tuple.Create(freqs[bin], ohms, 180 * values[bin].Phase / Math.PI);
						}
						break;
                    case TestingType.Gain:
						    // send freq, gain, phasedeg
							tup = Tuple.Create(freqs[bin], values[bin].Magnitude, 180 * values[bin].Phase / Math.PI);
						break;
                }
			}
			return tup;
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

			// ********************************************************************
			// Check connection
			if (await QaLibrary.CheckDeviceConnected() == false)
				return false;

			// ********************************************************************
			// Setup the device
			var sampleRate = MathUtil.ParseTextToUint(mrs.SampleRate, 0);
			if (sampleRate == 0 || !FreqRespViewModel.FftSizes.Contains(mrs.FftSize))
			{
				MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = FreqRespViewModel.FftActualSizes.ElementAt(FreqRespViewModel.FftSizes.IndexOf(mrs.FftSize));

			await Qa40x.SetDefaults();
            await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make it turn on or off
            await Qa40x.SetSampleRate(sampleRate);
            await Qa40x.SetBufferSize(fftsize);
            await Qa40x.SetWindowing("Hann");
            await Qa40x.SetRoundFrequencies(true);

			// ********************************************************************
			// Calculate frequency steps to do
			// ********************************************************************
			var binSize = QaLibrary.CalcBinSize(sampleRate, fftsize);
			// Generate a list of frequencies
			var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(MathUtil.ToDouble(mrs.StartFreq), MathUtil.ToDouble(mrs.EndFreq), mrs.StepsOctave);
			// Translate the generated list to bin center frequencies
			var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize);
			stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x <= 95500)                // Filter out values that are out of range 
				.GroupBy(x => x)                                                                    // Filter out duplicates
				.Select(y => y.First())
				.ToArray();

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
                    double amplifierOutputVoltagedBV = QaLibrary.ConvertVoltage(MathUtil.ToDouble(mrs.Gen1Voltage), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
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
                    genVoltagedBV = QaLibrary.ConvertVoltage(MathUtil.ToDouble(mrs.Gen1Voltage), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
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

                await Qa40x.SetOutputSource(OutputSources.Sine);

                // If in continous mode we continue sweeping until cancellation requested.
                MeasurementResult.GainData = new List<Complex>();
                MeasurementResult.GainFrequencies = new List<double>();
				// just one result to show
				Data.Measurements.Clear();
				Data.Measurements.Add(MeasurementResult);

                var ttype = GetTestingType(mrs.TestType);

				do
				{
                    if (ct.IsCancellationRequested)
                        break;
                    var voltagedBV = 20*Math.Log10(MathUtil.ToDouble(frqrsVm.Gen1Voltage));
                    for( int steps = 0; steps < stepBinFrequencies.Count(); steps++)
                    {
                        if (ct.IsCancellationRequested)
                            break;
                        var dfreq = stepBinFrequencies[steps];
                        if(dfreq > 0)
						    await Qa40x.SetGen1(dfreq, voltagedBV, true);
                        else
							await Qa40x.SetGen1(1, voltagedBV, false);

                        if( mrs.Averages > 0)
                        {
							List<Complex> readings = new();
							for (int j = 0; j < mrs.Averages; j++)
							{
								await showMessage(string.Format("Checking + {0:0}.{1}", dfreq, j));   // need a delay to actually see it
								var ga = await GetGain(dfreq, mrs, ttype);
								readings.Add(ga);
							}
							var total = new Complex(0, 0);
							foreach (var f in readings)
                            {
                                total += f;
                            }
							MeasurementResult.GainData.Add(total / mrs.Averages);
						}
						else
                        {
							await showMessage(string.Format("Checking + {0:0}", dfreq));   // need a delay to actually see it
							var ga = await GetGain(dfreq, mrs, ttype);
							MeasurementResult.GainData.Add(ga);
						}
                        if(MeasurementResult.FrequencyResponseData.FreqRslt != null)
                        {
							QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.FrequencyResponseData.FreqRslt, true, false);
							QaLibrary.PlotMiniTimeGraph(timePlot, MeasurementResult.FrequencyResponseData.TimeRslt, dfreq, true, false);
						}
						MeasurementResult.GainFrequencies.Add(dfreq);
                        UpdateGraph(false);
						if (!frqrsVm.IsTracking)
						{
							frqrsVm.RaiseMouseTracked("track");
						}
					}

					UpdateGraph(false);
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
			ViewSettings.Singleton.FreqRespVm.HasExport = true;

			return ct.IsCancellationRequested;
        }


        private Complex CalculateGain(double dFreq, LeftRightSeries data, bool showBoth)
        {
            Complex gain = new();
            if(showBoth)
				gain = QAMath.CalculateDualGain(dFreq, data);
            else
				gain = QAMath.CalculateGainPhase(dFreq, data);
			return gain;
        }

        /// <summary>
        /// Initialize the magnitude plot
        /// </summary>
        void InitializePlot()
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var frqrsVm = ViewSettings.Singleton.FreqRespVm;

			InitializeMagFreqPlot(myPlot);

            myPlot.Axes.SetLimitsX(Math.Log10(MathUtil.ToDouble(frqrsVm.GraphStartFreq, 20.0)), Math.Log10(MathUtil.ToDouble(frqrsVm.GraphEndFreq, 20000)), myPlot.Axes.Bottom);
			myPlot.Axes.SetLimitsY(MathUtil.ToDouble(frqrsVm.RangeBottomdB, -20), MathUtil.ToDouble(frqrsVm.RangeTopdB, 180), myPlot.Axes.Left);
            myPlot.Axes.SetLimitsY(-360, 360, myPlot.Axes.Right);

            var ttype = GetTestingType(frqrsVm.TestType);
            switch( ttype)
            {
                case TestingType.Response:
					myPlot.Title("Frequency Response (dBV)");
					myPlot.YLabel("dBV");
					myPlot.Axes.Right.Label.Text = string.Empty;
					break;
				case TestingType.Gain:
					AddPhasePlot(myPlot);
					myPlot.Title("Gain");
					myPlot.YLabel("dB");
					myPlot.Axes.Right.Label.Text = "Phase (Deg)";
					break;
				case TestingType.Impedance:
					AddPhasePlot(myPlot);
					myPlot.Title("Impedance");
					myPlot.YLabel("|Z| Ohms");
					myPlot.Axes.Right.Label.Text = "Phase (Deg)";
					break;
			}
			myPlot.XLabel("Frequency (Hz)");
			frqrsPlot.Refresh();
        }

        private Complex ToImpedance(Complex z)
        {
			var xtest = z / ((new Complex(1, 0)) - z);  // do the math
            return xtest;
		}


		/// <summary>
		/// Plot the magnitude graph
		/// </summary>
		/// <param name="measurementResult">Data to plot</param>
		void PlotGraph(FrequencyResponseMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel, E_FrequencyResponseGraphType graphType)
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var frqrsVm = ViewSettings.Singleton.FreqRespVm;

			if (measurementResult == null || measurementResult.GainData == null || measurementResult.GainFrequencies == null)
                return;

            var freqX = measurementResult.GainFrequencies;
            var gainY = measurementResult.GainData;
			if (gainY.Count == 0 || freqX.Count == 0)
				return;

			if (freqX[0] == 0)
                freqX[0] = 1e-6;    // so can log10

            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;
            float markerSize = frqrsVm.ShowPoints ? lineWidth + 3 : 1;

            var colors = new GraphColors();
            int color = measurementNr * 2;
            var ttype = GetTestingType(frqrsVm.TestType);

            // var magValues = gainY.Select(x => x.Magnitude).ToArray();
            double[] YValues = [];
            double[] phaseValues = [];
            double rref = MathUtil.ToDouble(frqrsVm.ZReference);
            string legendname = string.Empty;
            switch(ttype)
            {
                case TestingType.Gain:
					YValues = gainY.Select(x => 20 * Math.Log10(x.Magnitude)).ToArray();
					phaseValues = gainY.Select(x => 180 * x.Phase / Math.PI).ToArray();
                    legendname = "Gain";
					break;
				case TestingType.Response:
					YValues = gainY.Select(x => 20 * Math.Log10(x.Real)).ToArray(); // real is the left gain
					phaseValues = gainY.Select(x => 20 * Math.Log10(x.Imaginary)).ToArray();
					legendname = "Left dBV";
					break;
				case TestingType.Impedance:
					YValues = gainY.Select(x => rref * ToImpedance(x).Magnitude).ToArray();
					// YValues = gainY.Select(x => rref * x.Magnitude/(1-x.Magnitude)).ToArray();
					phaseValues = gainY.Select(x => 180 * ToImpedance(x).Phase / Math.PI).ToArray();
					legendname = "|Z| Ohms";
                    if (myPlot.Axes.Rules.Count > 0)
                    {
                        var rule = myPlot.Axes.Rules.First();
                        if (rule is MaximumBoundary)
                        {
                            // change to an impedance set of limits
                            var myrule = ((MaximumBoundary)rule);
                            var oldlimit = myrule.Limits;
                            AxisLimits axs = new AxisLimits(oldlimit.Left, oldlimit.Right, 0, 2000);
                            myrule.Limits = axs;
                        }
                    }
					if (myPlot.Axes.Rules.Count > 1)
					{
						var rule = myPlot.Axes.Rules.Last();
						if (rule is MaximumBoundary)
						{
							// change to an impedance set of limits
							var myrule = ((MaximumBoundary)rule);
							var oldlimit = myrule.Limits;
							AxisLimits axs = new AxisLimits(oldlimit.Left, oldlimit.Right, -360, 360);
							myrule.Limits = axs;
						}
					}

					break;
			}
			//SetMagFreqRule(myPlot);
			var plot = myPlot.Add.Scatter(logFreqX, YValues);
			plot.LineWidth = lineWidth;
			plot.Color = colors.GetColor(0, color);
			plot.MarkerSize = markerSize;
            plot.LegendText = legendname;
			plot.LinePattern = LinePattern.Solid;
            if( ttype != TestingType.Response || frqrsVm.ShowRight)
            {
				plot = myPlot.Add.Scatter(logFreqX, phaseValues);
                if(ttype == TestingType.Gain || ttype == TestingType.Impedance)
                {
					plot.Axes.YAxis = myPlot.Axes.Right;
					plot.LegendText = "Phase (Deg)";
				}
                else
                {
					plot.LegendText = "Right dBV";
				}
				plot.LineWidth = lineWidth;
				plot.Color = colors.GetColor(3, color);
				plot.MarkerSize = markerSize;
				plot.LinePattern = LinePattern.Solid;
			}

			frqrsPlot.Refresh();
        }

		public void UpdateGraph(bool settingsChanged)
        {
            frqrsPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			var frqsrVm = ViewSettings.Singleton.FreqRespVm;

			int resultNr = 0;

            switch(GetTestingType(frqsrVm.TestType))
            {
                case TestingType.Response:
					frqsrVm.GraphUnit = "dBV";
                    break;
				case TestingType.Impedance:
					frqsrVm.GraphUnit = "Ohms";
					break;
				case TestingType.Gain:
					frqsrVm.GraphUnit = "dB";
					break;
			}

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
                    var gainBW3dB = CalculateBandwidth(-3, MeasurementResult.GainData.Select(x => x.Magnitude).ToArray(), MeasurementResult.FrequencyResponseData.FreqRslt.Df);        // Volts is gain

                    var gainBW1dB = CalculateBandwidth(-1, MeasurementResult.GainData.Select(x => x.Magnitude).ToArray(), MeasurementResult.FrequencyResponseData.FreqRslt.Df);

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
