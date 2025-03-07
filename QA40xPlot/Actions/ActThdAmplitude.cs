using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Net.Http;
using System.Windows;

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

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurementSteps(CancellationToken ct)
		{

			ClearPlot();
			//ClearCursorTexts();

			//var _measurementSettings = MeasurementSettings.Copy();             // Create snapshot so it is not changed during measuring

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

			//UpdateGraphChannelSelectors();

			//markerIndex = -1;       // Reset marker

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
			await Qa40x.SetWindowing(thdAmp.WindowingMethod.ToString());
			await Qa40x.SetRoundFrequencies(true);


			// ********************************************************************
			// Determine attenuation level
			// ********************************************************************
			double generatorAmplitudedBV = thdAmp.StartAmplitude;
			await showMessage($"Determining the best input attenuation for a generator voltage of {generatorAmplitudedBV:0.00#} dBV.");

			double testFrequency = QaLibrary.GetNearestBinFrequency(thdAmp.TestFreq, thdAmp.SampleRate, thdAmp.FftSize);
			// Determine correct input attenuation
			var result = await QaLibrary.DetermineAttenuationForGeneratorVoltageWithChirp(generatorAmplitudedBV, 42, thdAmp.LeftChannel, thdAmp.RightChannel, ct);
			if (ct.IsCancellationRequested)
				return false;
			QaLibrary.PlotMiniFftGraph(fftPlot, result.Item3.FreqRslt, thdAmp.LeftChannel, thdAmp.RightChannel);
			QaLibrary.PlotMiniTimeGraph(timePlot, result.Item3.TimeRslt, testFrequency, thdAmp.LeftChannel, thdAmp.RightChannel, true);
			var prevInputAmplitudedBV = result.Item2;


			// Set attenuation
			await Qa40x.SetInputRange(result.Item1);

			await showMessage($"Found correct input attenuation of {result.Item1:0} dBV for an amplifier amplitude of {result.Item2:0.00#} dBV.", 500);

			if (ct.IsCancellationRequested)
				return false;

			// ********************************************************************
			// Generate a list of voltages evenly spaced in log scale
			// ********************************************************************
			var startAmplitudeV = QaLibrary.ConvertVoltage(thdAmp.StartAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			var prevGeneratorVoltagedBV = thdAmp.StartAmplitude;
			var endAmplitudeV = QaLibrary.ConvertVoltage(thdAmp.EndAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			var stepVoltages = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(startAmplitudeV, endAmplitudeV, thdAmp.StepsOctave);

			// ********************************************************************
			// Do noise floor measurement
			// ********************************************************************
			await showMessage($"Determining noise floor.");
			await Qa40x.SetOutputSource(OutputSources.Off);
			MeasurementResult.NoiseFloor = await QaLibrary.DoAcquisitions(thdAmp.Averages, ct);
			if (ct.IsCancellationRequested)
				return false;
			QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.NoiseFloor.FreqRslt, thdAmp.LeftChannel, thdAmp.RightChannel);
			QaLibrary.PlotMiniTimeGraph(timePlot, MeasurementResult.NoiseFloor.TimeRslt, testFrequency, thdAmp.LeftChannel && thdAmp.ShowLeft, thdAmp.RightChannel && thdAmp.ShowRight);

			var binSize = QaLibrary.CalcBinSize(thdAmp.SampleRate, thdAmp.FftSize);
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(testFrequency, binSize);

			var newAttenuation = 0;
			var prevAttenuation = 0;
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

				// Determine attanuation needed
				var voltageDiffdBV = generatorVoltagedBV - prevGeneratorVoltagedBV;             // Calculate voltage rise of amplifier output
				var predictedAttenuation = QaLibrary.DetermineAttenuation(prevInputAmplitudedBV + voltageDiffdBV); // Predict attenuation
				newAttenuation = predictedAttenuation > newAttenuation ? predictedAttenuation : newAttenuation;
				await Qa40x.SetInputRange(newAttenuation);                          // Set attenuation
				prevGeneratorVoltagedBV = generatorVoltagedBV;
				if (newAttenuation > prevAttenuation && newAttenuation == 24)
				{
					// Attenuation changed. Get new noise floor
					await showMessage($"Attenuation changed. Measuring new noise floor.");
					await Qa40x.SetOutputSource(OutputSources.Off);
					MeasurementResult.NoiseFloor = await QaLibrary.DoAcquisitions(thdAmp.Averages, ct);
					if (ct.IsCancellationRequested)
						return false;
					QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.NoiseFloor.FreqRslt, thdAmp.LeftChannel, thdAmp.RightChannel);
					QaLibrary.PlotMiniTimeGraph(timePlot, MeasurementResult.NoiseFloor.TimeRslt, testFrequency, thdAmp.LeftChannel && thdAmp.ShowLeft, thdAmp.RightChannel && thdAmp.ShowRight);
					await Qa40x.SetOutputSource(OutputSources.Sine);
				}
				prevAttenuation = newAttenuation;

				// Set generator
				await Qa40x.SetGen1(testFrequency, generatorVoltagedBV, true);      // Set the generator in dBV
				if (i == 0)
					await Qa40x.SetOutputSource(OutputSources.Sine);                // We need to call this the first time

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
							newAttenuation += 6;
							if (newAttenuation > 42)
							{
								MessageBox.Show($"Maximum attenuation reached.\nMeasurements are stopped", "Maximum attenuation reached", MessageBoxButton.OK, MessageBoxImage.Information);
								await Qa40x.SetOutputSource(OutputSources.Off);
								return false;
							}
							await Qa40x.SetInputRange(newAttenuation);

							if (newAttenuation == 24)
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
				QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, thdAmp.LeftChannel && thdAmp.ShowLeft, thdAmp.RightChannel && thdAmp.ShowRight);
				QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, step.FundamentalFrequency, thdAmp.LeftChannel && thdAmp.ShowLeft, thdAmp.RightChannel && thdAmp.ShowRight);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltagedBV, lrfs.FreqRslt.Left, MeasurementResult.NoiseFloor.FreqRslt.Left, thdAmp.AmpLoad);
				step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltagedBV, lrfs.FreqRslt.Right, MeasurementResult.NoiseFloor.FreqRslt.Right, thdAmp.AmpLoad);

				// Add step data to list
				MeasurementResult.AmplitudeSteps.Add(step);

				UpdateGraph(false);
				//ShowLastMeasurementCursorTexts();

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				// Get maximum signal for attenuation prediction of next step
				prevInputAmplitudedBV = 20 * Math.Log10(lrfs.FreqRslt.Left.Max());
				prevInputAmplitudedBV = Math.Max(prevInputAmplitudedBV, 20 * Math.Log10(lrfs.FreqRslt.Left.Max()));
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
			myPlot.Title("Distortion vs Amplitude (%)");

			if (thdAmp.XAxisType == (int)E_X_AxisType.INPUT_VOLTAGE)
				myPlot.XLabel("Input voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_VOLTAGE)
				myPlot.XLabel("Output voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_POWER)
				myPlot.XLabel("Output power (W)");
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
				thdAmp.RangeBottomdB, thdAmp.RangeTopdB);

			myPlot.Title("Distortion vs Amplitude (dB)");

			if (thdAmp.XAxisType == (int)E_X_AxisType.INPUT_VOLTAGE)
				myPlot.XLabel("Input voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_VOLTAGE)
				myPlot.XLabel("Output voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_POWER)
				myPlot.XLabel("Output power (W)");

			myPlot.YLabel("Distortion (dB)");

			thdPlot.Refresh();
		}


		/// <summary>
		/// Plot the  THD magnitude (dB) data
		/// </summary>
		/// <param name="data">The data to plot</param>
		void PlotValues(ThdAmplitudeMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
		{
			var thdAmp = ViewSettings.Singleton.ThdAmp;
			// Create lists for line data
			var amplitudeX_L = new List<double>();
			var amplitudeX_R = new List<double>();
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

			// Add data to the line lists
			foreach (var step in measurementResult.AmplitudeSteps)
			{
				double xValue_L = (E_X_AxisType)thdAmp.XAxisType switch
				{
					E_X_AxisType.OUTPUT_VOLTAGE => step.Left.Fundamental_V,
					E_X_AxisType.OUTPUT_POWER => step.Left.Power_Watt,
					_ => step.GeneratorVoltage
				};
				amplitudeX_L.Add(xValue_L);

				double xValue_R = (E_X_AxisType)thdAmp.XAxisType switch
				{
					E_X_AxisType.OUTPUT_VOLTAGE => step.Right.Fundamental_V,
					E_X_AxisType.OUTPUT_POWER => step.Right.Power_Watt,
					_ => step.GeneratorVoltage
				};
				amplitudeX_R.Add(xValue_R);

				if (showLeftChannel && measurementResult.MeasurementSettings.LeftChannel)
				{
					if (thdAmp.ShowMagnitude)
						magnY_left.Add(step.Left.Gain_dB);

					if (step.Left.Harmonics.Count > 0)
					{
						hTotY_left.Add(step.Left.Thd_dB + step.Left.Gain_dB);
						h2Y_left.Add(step.Left.Harmonics[0].Amplitude_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
					}
					if (step.Left.Harmonics.Count > 1)
						h3Y_left.Add(step.Left.Harmonics[1].Amplitude_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
					if (step.Left.Harmonics.Count > 2)
						h4Y_left.Add(step.Left.Harmonics[2].Amplitude_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
					if (step.Left.Harmonics.Count > 3)
						h5Y_left.Add(step.Left.Harmonics[3].Amplitude_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
					if (step.Left.D6Plus_dBV != 0 && step.Left.Harmonics.Count > 4 && thdAmp.ShowD6)
						h6Y_left.Add(step.Left.D6Plus_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
					if (thdAmp.ShowNoiseFloor)
						noiseY_left.Add(step.Left.Average_NoiseFloor_dBV - step.Left.Fundamental_dBV + step.Left.Gain_dB);
				}

				if (showRightChannel && measurementResult.MeasurementSettings.RightChannel)
				{
					if (thdAmp.ShowMagnitude)
						magnY_right.Add(step.Right.Gain_dB);

					if (step.Right.Harmonics.Count > 0)
					{
						hTotY_right.Add(step.Right.Thd_dB + step.Right.Gain_dB);
						h2Y_right.Add(step.Right.Harmonics[0].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
					}
					if (step.Right.Harmonics.Count > 1)
						h3Y_right.Add(step.Right.Harmonics[1].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
					if (step.Right.Harmonics.Count > 2)
						h4Y_right.Add(step.Right.Harmonics[2].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
					if (step.Right.Harmonics.Count > 3)
						h5Y_right.Add(step.Right.Harmonics[3].Amplitude_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
					if (step.Right.D6Plus_dBV != 0 && step.Right.Harmonics.Count > 4 && thdAmp.ShowD6)
						h6Y_right.Add(step.Right.D6Plus_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
					if (thdAmp.ShowNoiseFloor)
						noiseY_right.Add(step.Right.Average_NoiseFloor_dBV - step.Right.Fundamental_dBV + step.Right.Gain_dB);
				}
			}

			// add a scatter plot to the plot
			double[] logAmplitudeX_L = amplitudeX_L.Select(Math.Log10).ToArray();
			double[] logAmplitudeX_R = amplitudeX_R.Select(Math.Log10).ToArray();
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

			if (showLeftChannel)
			{
				if (thdAmp.ShowMagnitude) AddPlot(logAmplitudeX_L, magnY_left, 9, showRightChannel ? "Magn-L" : "Magn", LinePattern.DenselyDashed);
				if (thdAmp.ShowTHD) AddPlot(logAmplitudeX_L, hTotY_left, 8, showRightChannel ? "THD-L" : "THD", LinePattern.Solid);
				if (thdAmp.ShowD2) AddPlot(logAmplitudeX_L, h2Y_left, 0, showRightChannel ? "H2-L" : "H2", LinePattern.Solid);
				if (thdAmp.ShowD3) AddPlot(logAmplitudeX_L, h3Y_left, 1, showRightChannel ? "H3-L" : "H3", LinePattern.Solid);
				if (thdAmp.ShowD4) AddPlot(logAmplitudeX_L, h4Y_left, 2, showRightChannel ? "H4-L" : "H4", LinePattern.Solid);
				if (thdAmp.ShowD5) AddPlot(logAmplitudeX_L, h5Y_left, 3, showRightChannel ? "H5-L" : "H5", LinePattern.Solid);
				if (thdAmp.ShowD6) AddPlot(logAmplitudeX_L, h6Y_left, 4, showRightChannel ? "H6+-L" : "H6+", LinePattern.Solid);
				if (thdAmp.ShowNoiseFloor) AddPlot(logAmplitudeX_L, noiseY_left, 9, showRightChannel ? "Noise-L" : "Noise", showRightChannel ? LinePattern.Solid : LinePattern.Dotted);
			}

			if (showRightChannel)
			{
				if (thdAmp.ShowMagnitude) AddPlot(logAmplitudeX_R, magnY_right, 9, showLeftChannel ? "Magn-R" : "Magn", showLeftChannel ? LinePattern.Dotted : LinePattern.DenselyDashed);
				if (thdAmp.ShowTHD) AddPlot(logAmplitudeX_R, hTotY_right, 8, showLeftChannel ? "THD-R" : "THD", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD2) AddPlot(logAmplitudeX_R, h2Y_right, 0, showLeftChannel ? "H2-R" : "H2", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD3) AddPlot(logAmplitudeX_R, h3Y_right, 1, showLeftChannel ? "H3-R" : "H3", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD4) AddPlot(logAmplitudeX_R, h4Y_right, 2, showLeftChannel ? "H4-R" : "H4", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD5) AddPlot(logAmplitudeX_R, h5Y_right, 3, showLeftChannel ? "H5-R" : "H5", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD6) AddPlot(logAmplitudeX_R, h6Y_right, 4, showLeftChannel ? "H6+-R" : "H6+", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowNoiseFloor) AddPlot(logAmplitudeX_R, noiseY_right, 9, showLeftChannel ? "Noise-R" : "Noise", LinePattern.Dotted);
			}

			// If marker selected draw marker line
			//if (markerIndex != -1)
			//    QaLibrary.PlotCursorMarker(thdPlot, lineWidth, LinePattern.Solid, markerDataPoint);

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
