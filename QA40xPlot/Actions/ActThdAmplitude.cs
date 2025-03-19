using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Windows;
using static FreqRespViewModel;

namespace QA40xPlot.Actions
{

	public partial class ActThdAmplitude : ActBase
	{
		public ThdAmplitudeData Data { get; set; }       // Data used in this form instance
		private readonly Views.PlotControl thdPlot;
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;

		private ThdAmplitudeMeasurementResult MeasurementResult;

		CancellationTokenSource ct;                                 // Measurement cancelation token

		/// <summary>
		/// Constructor
		/// </summary>
		public ActThdAmplitude(ref ThdAmplitudeData data, Views.PlotControl graphThd, Views.PlotControl graphFft, Views.PlotControl graphTime)
		{
			Data = data;
			ThdAmpViewModel thd = ViewSettings.Singleton.ThdAmp;
			MeasurementResult = new(thd); // TODO. Add to list

			ct = new CancellationTokenSource();

			// Show empty graphs

			thdPlot = graphThd;
			fftPlot = graphFft;
			timePlot = graphTime;

			// Show empty graphs
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 100000, -150, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			// TODO: depends on graph settings which graph is shown
			UpdateGraph(true);
		}

		// user entered a new voltage, update the generator amplitude
		public void UpdateStartAmplitude(string value)
		{
			ThdAmpViewModel thd = ViewSettings.Singleton.ThdAmp;
			var val = MathUtil.ParseTextToDouble(value, thd.StartVoltage);
			thd.StartAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.StartVoltageUnits, E_VoltageUnit.dBV);
		}

		// user entered a new voltage, update the generator amplitude
		public void UpdateEndAmplitude(string value)
		{
			ThdAmpViewModel thd = ViewSettings.Singleton.ThdAmp;
			var val = MathUtil.ParseTextToDouble(value, thd.EndVoltage);
			thd.EndAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.EndVoltageUnits, E_VoltageUnit.dBV);
		}

		/// <summary>
		/// Update the start voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateStartVoltageDisplay()
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			vm.StartVoltage = QaLibrary.ConvertVoltage(vm.StartAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)vm.StartVoltageUnits);
		}

		/// <summary>
		/// Update the end voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateEndVoltageDisplay()
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			vm.EndVoltage = QaLibrary.ConvertVoltage(vm.EndAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)vm.EndVoltageUnits);
		}

		public void DoCancel()
		{
			ct.Cancel();
		}

		public Tuple<ThdColumn?, ThdColumn?> LookupX(double amp)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			var vf = vm.ShowLeft ? MeasurementResult?.LeftColumns : MeasurementResult?.RightColumns;
			if (vf == null || vf.Count == 0)
			{
				return Tuple.Create((ThdColumn?)null, (ThdColumn?)null);
			}

			// find nearest amplitude (both left and right will be identical here if scanned)
			var ampl = 20 * Math.Log10(amp);
			var bin = vf.Count(x => x.Amplitude < ampl) - 1;    // find first freq less than me
			if (bin == -1)
				bin = 0;
			var anearest = vf[bin].Amplitude;
			if (bin < (vf.Count - 1) && Math.Abs(ampl - anearest) > Math.Abs(ampl - vf[bin + 1].Amplitude))
			{
				bin++;
			}

			ThdColumn? mf1 = null;
			ThdColumn? mf2 = null;

			if( vm.ShowLeft)
				mf1 = MeasurementResult?.LeftColumns?.ElementAt(bin);
			if (vm.ShowRight)
				mf2 = MeasurementResult?.RightColumns?.ElementAt(bin);

			return Tuple.Create(mf1, mf2);
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
			cl.Freq = MathUtil.ParseTextToDouble(MeasurementResult.MeasurementSettings.TestFreq, 10);
			cl.Amplitude = chan.Fundamental_dBV;
			return cl;
		}

		private void AddColumn(ThdAmplitudeStep step)
		{
			var f = MeasurementResult?.LeftColumns;
			if (f != null)
			{
				var cl = MakeColumn(step.Left);
				if (cl != null)
					MeasurementResult.LeftColumns.Add(cl);
			}
			f = MeasurementResult?.RightColumns;
			if (f != null)
			{
				var cl = MakeColumn(step.Right);
				if (cl != null)
					MeasurementResult.RightColumns.Add(cl);
			}
		}


		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurementSteps(CancellationToken ct)
		{
			ClearPlot();
			// Clear measurement result
			MeasurementResult = new(ViewSettings.Singleton.ThdAmp)
			{
				CreateDate = DateTime.Now,
				Show = true,                       // Show in graph
			};
			var thdAmp = MeasurementResult.MeasurementSettings;

			// For now clear measurements to allow only one until we have a UI to manage them.
			Data.Measurements.Clear();

			// Add to list
			Data.Measurements.Add(MeasurementResult);

			// Init mini plots
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 100000, -150, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			// Check if webserver available and device connected
			if (await QaLibrary.CheckDeviceConnected() == false)
				return false;

			// ********************************************************************
			// Check connection
			// Load a settings file with the particulars we want
			await Qa40x.SetDefaults();
			await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make it turn on or off
			await Qa40x.SetSampleRate(thdAmp.SampleRate);
			await Qa40x.SetBufferSize(thdAmp.FftSize);
			await Qa40x.SetWindowing(thdAmp.WindowingMethod);
			await Qa40x.SetRoundFrequencies(true);

			// ********************************************************************
			// Determine attenuation level
			// ********************************************************************
			double generatorAmplitudedBV = Math.Max(thdAmp.EndAmplitude, thdAmp.StartAmplitude);	// use the largest amplitude
			await showMessage($"Determining the best input attenuation for a generator voltage of {generatorAmplitudedBV:0.00#} dBV.");

			double testFrequency = QaLibrary.GetNearestBinFrequency(MathUtil.ParseTextToDouble(thdAmp.TestFreq, 10), thdAmp.SampleRate, thdAmp.FftSize);
			// Determine correct input attenuation
			var attenuation = 42;
			{
				// check for allowed attenuation
				var result = await QaLibrary.DetermineAttenuationForGeneratorVoltageWithChirp(generatorAmplitudedBV, attenuation, true, true, ct);
				if (ct.IsCancellationRequested)
					return false;
				QaLibrary.PlotMiniFftGraph(fftPlot, result.Item3.FreqRslt, thdAmp.ShowLeft, thdAmp.ShowRight);
				QaLibrary.PlotMiniTimeGraph(timePlot, result.Item3.TimeRslt, testFrequency, thdAmp.ShowLeft, thdAmp.ShowRight, true);
				// var prevInputAmplitudedBV = result.Item2; // maximum reading
				// Set attenuation
				attenuation = result.Item1;
				await showMessage($"Found correct input attenuation of {result.Item1:0.00#} dBV for an input of {result.Item2:0.00#} dBV.", 500);
			}
			await Qa40x.SetInputRange(attenuation);

			if (ct.IsCancellationRequested)
				return false;

			// ********************************************************************
			// Generate a list of voltages evenly spaced in log scale
			// ********************************************************************
			double[] stepVoltages;
			{
				var startAmplitudeV = QaLibrary.ConvertVoltage(thdAmp.StartAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
				var prevGeneratorVoltagedBV = thdAmp.StartAmplitude;
				var endAmplitudeV = QaLibrary.ConvertVoltage(thdAmp.EndAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
				stepVoltages = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(startAmplitudeV, endAmplitudeV, thdAmp.StepsOctave);
			}

			// ********************************************************************
			// Do noise floor measurement
			// ********************************************************************
			await showMessage($"Determining noise floor.");
			await Qa40x.SetOutputSource(OutputSources.Off);
			MeasurementResult.NoiseFloor = await QaLibrary.DoAcquisitions(thdAmp.Averages, ct);
			if (ct.IsCancellationRequested)
				return false;
			QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.NoiseFloor.FreqRslt, thdAmp.ShowLeft, thdAmp.ShowRight);
			QaLibrary.PlotMiniTimeGraph(timePlot, MeasurementResult.NoiseFloor.TimeRslt, testFrequency, thdAmp.ShowLeft, thdAmp.ShowRight);

			var binSize = QaLibrary.CalcBinSize(thdAmp.SampleRate, thdAmp.FftSize);
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(testFrequency, binSize);
			await Qa40x.SetOutputSource(OutputSources.Sine);                // We need to call this before all the testing

			// ********************************************************************
			// Step through the list of voltages
			// ********************************************************************
			for (int i = 0; i < stepVoltages.Length; i++)
			{
				await showMessage($"Measuring step {i + 1} of {stepVoltages.Length}.");
				await showProgress(100 * (i + 1) / (stepVoltages.Length));

				// Convert generator voltage from V to dBV
				var generatorVoltageV = stepVoltages[i];
				var generatorVoltagedBV = QaLibrary.ConvertVoltage(generatorVoltageV, E_VoltageUnit.Volt, E_VoltageUnit.dBV);   // Convert to dBV

				// Set generator
				await Qa40x.SetGen1(testFrequency, generatorVoltagedBV, true);      // Set the generator in dBV

				LeftRightSeries? lrfs = null;
				do
				{
					try
					{
						lrfs = await QaLibrary.DoAcquisitions(thdAmp.Averages, ct);  // Do acquisitions
					}
					catch (HttpRequestException ex)
					{
						if (ex.Message.Contains("400 (Acquisition Overload)"))
						{
							// Detected overload. Increase attenuation to next step.
							var newAttenuation = 6 + attenuation;
							if (newAttenuation > 42)
							{
								MessageBox.Show($"Maximum attenuation reached.\nMeasurements are stopped", "Maximum attenuation reached", MessageBoxButton.OK, MessageBoxImage.Information);
								await Qa40x.SetOutputSource(OutputSources.Off);
								return false;
							}
							attenuation = newAttenuation;
							await Qa40x.SetInputRange(attenuation);

							if (attenuation == 24)
							{
								// Attenuation changed. Get new noise floor
								await showMessage($"Attenuation changed. Measuring new noise floor.");
								await Qa40x.SetOutputSource(OutputSources.Off);
								MeasurementResult.NoiseFloor = await QaLibrary.DoAcquisitions(thdAmp.Averages, ct);
								if (ct.IsCancellationRequested)
									return false;

								await Qa40x.SetOutputSource(OutputSources.Sine);
							}
						}
					}
					if (ct.IsCancellationRequested)
						break;
				} while (lrfs == null);     // Loop until we have an acquisition result

				if (lrfs == null)
					break;

				if (fundamentalBin >= lrfs.FreqRslt.Left.Length)                   // Check if bin within array bounds
					break;

				ThdAmplitudeStep step = new()
				{
					FundamentalFrequency = testFrequency,
					GeneratorVoltage = generatorVoltageV,
					fftData = lrfs.FreqRslt,
					timeData = lrfs.TimeRslt
				};

				// Plot the mini graphs
				QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, thdAmp.ShowLeft, thdAmp.ShowRight);
				QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, step.FundamentalFrequency, thdAmp.ShowLeft, thdAmp.ShowRight);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltagedBV, lrfs.FreqRslt.Left, MeasurementResult.NoiseFloor.FreqRslt.Left, thdAmp.AmpLoad);
				step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltagedBV, lrfs.FreqRslt.Right, MeasurementResult.NoiseFloor.FreqRslt.Right, thdAmp.AmpLoad);

				// Add step data to list
				MeasurementResult.AmplitudeSteps.Add(step);
				AddColumn(step);

				UpdateGraph(false);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				// Get maximum signal for attenuation prediction of next step
				//prevInputAmplitudedBV = 20 * Math.Log10(lrfs.FreqRslt.Left.Max());
				//prevInputAmplitudedBV = Math.Max(prevInputAmplitudedBV, 20 * Math.Log10(lrfs.FreqRslt.Left.Max()));
				if (!thdAmp.IsTracking)
				{
					thdAmp.RaiseMouseTracked("track");
				}

			}

			// Turn the generator off
			await Qa40x.SetOutputSource(OutputSources.Off);

			// Show message
			await showMessage(ct.IsCancellationRequested ? $"Measurement cancelled!" : $"Measurement finished!");

			return true;
		}



		private ThdFrequencyStepChannel ChannelCalculations(double binSize, double fundamentalFrequency, double generatorAmplitudeDbv, double[] fftData, double[] noiseFloorFftData, double load)
		{
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFrequency, binSize);

			// Get and store step data
			ThdFrequencyStepChannel channelData = new()
			{
				Fundamental_V = fftData[fundamentalBin],
				Fundamental_dBV = 20 * Math.Log10(fftData[fundamentalBin]),
				Gain_dB = 20 * Math.Log10(fftData[fundamentalBin] / Math.Pow(10, generatorAmplitudeDbv / 20))
			};

			// Calculate average noise floor
			channelData.Average_NoiseFloor_V = noiseFloorFftData
				.Skip((int)fundamentalBin + 1)
				.Average();
			channelData.Average_NoiseFloor_dBV = 20 * Math.Log10(channelData.Average_NoiseFloor_V);

			// Reset harmonic distortion variables
			double distortionSqrtTotal = 0;
			double distiortionD6plus = 0;

			// Loop through harmonics up to the 12th
			for (int h = 2; h <= 12; h++)
			{
				var harmonicFrequency = fundamentalFrequency * h;
				uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);

				if (bin >= fftData.Length)
					break;

				HarmonicData harmonic = new()
				{
					HarmonicNr = h,
					Frequency = harmonicFrequency,
					NoiseAmplitude_V = noiseFloorFftData[bin],
					Amplitude_V = fftData[bin],
					Amplitude_dBV = 20 * Math.Log10(fftData[bin]),
					Thd_Percent = (fftData[bin] / channelData.Fundamental_V) * 100,
					Thd_dB = 20 * Math.Log10(fftData[bin] / channelData.Fundamental_V)
				};

				if (h >= 6)
					distiortionD6plus += Math.Pow(harmonic.Amplitude_V, 2);

				distortionSqrtTotal += Math.Pow(harmonic.Amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
			}

			// Calculate THD of current step
			if (distortionSqrtTotal != 0)
			{
				channelData.Thd_Percent = (Math.Sqrt(distortionSqrtTotal) / channelData.Fundamental_V) * 100;
				channelData.Thd_dB = 20 * Math.Log10(channelData.Thd_Percent / 100.0);
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
			thdPlot.ThePlot.Clear();
			thdPlot.Refresh();
		}

		private void SetPlotLabels()
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			var thdAmp = ViewSettings.Singleton.ThdAmp;
			if (thdAmp.XAxisType == (int)E_X_AxisType.INPUT_VOLTAGE)
				myPlot.XLabel("Input voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_VOLTAGE)
				myPlot.XLabel("Output voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_POWER)
				myPlot.XLabel("Output power (W)");
			myPlot.Title("Distortion vs Amplitude");
		}

		/// <summary>
		/// Initialize the THD % plot
		/// </summary>
		void InitializeThdPlot()
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			InitializePctAmpPlot(myPlot);
			var thdAmp = ViewSettings.Singleton.ThdAmp;
			myPlot.Axes.SetLimits(Math.Log10(Convert.ToDouble(thdAmp.GraphStartVolts)), Math.Log10(Convert.ToDouble(thdAmp.GraphEndVolts)),
				Math.Log10(Convert.ToDouble(thdAmp.RangeBottom)), Math.Log10(Convert.ToDouble(thdAmp.RangeTop)));
			SetPlotLabels();
			myPlot.YLabel("Distortion (%)");
			thdPlot.Refresh();
		}

		/// <summary>
		/// Initialize the THD magnitude (dB) plot
		/// </summary>
		void InitializeMagnitudePlot()
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			InitializeMagAmpPlot(myPlot);
			var thdAmp = ViewSettings.Singleton.ThdAmp;
			myPlot.Axes.SetLimits(Math.Log10(Convert.ToDouble(thdAmp.GraphStartVolts)), Math.Log10(Convert.ToDouble(thdAmp.GraphEndVolts)),
				Convert.ToDouble(thdAmp.RangeBottomdB), Convert.ToDouble(thdAmp.RangeTopdB));
			SetPlotLabels();
			myPlot.YLabel("Distortion (dB)");
			thdPlot.Refresh();
		}


		/// <summary>
		/// Plot the  THD magnitude (dB) data
		/// </summary>
		/// <param name="data">The data to plot</param>
		private void PlotValues(ThdAmplitudeMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
		{
			if (!showLeftChannel && !showRightChannel)
				return;

			var thdAmp = ViewSettings.Singleton.ThdAmp;
			float lineWidth = thdAmp.ShowThickLines ? 1.6f : 1;
			float markerSize = thdAmp.ShowPoints ? lineWidth + 3 : 1;

			var colors = new GraphColors();
			int color = measurementNr * 2;

			// here Y values are in dBV
			void AddPlot(double[] xValues, List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
			{
				if (yValues.Count == 0) return;
				Scatter? plot = null;
				if( thdAmp.ShowPercent)
				{
					var vals = yValues.Select(x => 2 + x / 20).ToArray();		// convert to volts then 100 then back to log10
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
			if(showLeftChannel && showRightChannel)
			{
				columns = [MeasurementResult.LeftColumns, MeasurementResult.RightColumns];
			}
			else if(!showRightChannel)
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
				var amps = col.Select(x => x.Amplitude/20).ToArray();
				if (thdAmp.ShowMagnitude)
					AddPlot(amps, col.Select(x => x.Mag).ToList(), 9, "Mag" + suffix, LinePattern.DenselyDashed);
				if (thdAmp.ShowTHD)
					AddPlot(amps, col.Select(x => x.THD).ToList(), 8, "THD" + suffix, lp);
				if (thdAmp.ShowD2)
					AddPlot(amps, col.Select(x => x.D2).ToList(), 0, "D2" + suffix, lp);
				if (thdAmp.ShowD3)
					AddPlot(amps, col.Select(x => x.D3).ToList(), 1, "D3" + suffix, lp);
				if (thdAmp.ShowD4)
					AddPlot(amps, col.Select(x => x.D4).ToList(), 2, "D4" + suffix, lp);
				if (thdAmp.ShowD5)
					AddPlot(amps, col.Select(x => x.D5).ToList(), 3, "D5" + suffix, lp);
				if (thdAmp.ShowD6)
					AddPlot(amps, col.Select(x => x.D6P).ToList(), 3, "D6+" + suffix, lp);
				if (thdAmp.ShowNoiseFloor)
					AddPlot(amps, col.Select(x => x.Noise).ToList(), 3, "Noise" + suffix, LinePattern.Dotted);
				suffix = "-R";			// second pass iff there are both channels
				lp = LinePattern.DenselyDashed;
			}

			thdPlot.Refresh();
		}

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void StartMeasurement()
		{
			var thdAmp = ViewSettings.Singleton.ThdAmp;

			thdAmp.IsRunning = true;
			ct = new();
			await PerformMeasurementSteps(ct.Token);
			await showMessage("Finished");
			thdAmp.IsRunning = false;
		}


		public void UpdateGraph(bool settingsChanged)
		{
			thdPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			var thdAmp = ViewSettings.Singleton.ThdAmp;
			int resultNr = 0;

			if (!thdAmp.ShowPercent)
			{
				if (settingsChanged)
				{
					InitializeMagnitudePlot();
				}

				foreach (var result in Data.Measurements.Where(m => m.Show))
				{
					PlotValues(result, resultNr++, thdAmp.ShowLeft, thdAmp.ShowRight);
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
					PlotValues(result, resultNr++, thdAmp.ShowLeft, thdAmp.ShowRight);
				}
			}
		}

	}
}
