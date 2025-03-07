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
		public bool MeasurementBusy { get; set; }                   // Measurement busy state
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
			switch (vm.EndVoltageUnits)
			{
				case 0: // mV
					vm.EndVoltage = ((int)QaLibrary.ConvertVoltage(vm.EndAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.MilliVolt)); // Whole numbers onyl, so cast to integer
					break;
				case 1: // V
					break;
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
						if (ct.IsCancellationRequested)
							return false;
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
				} while (lrfs == null);     // Loop until we have an acquisition result

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

				step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltagedBV, lrfs.FreqRslt.Left, MeasurementResult.NoiseFloor.FreqRslt.Left, thdAmp.AmpLoad);
				step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltagedBV, lrfs.FreqRslt.Right, MeasurementResult.NoiseFloor.FreqRslt.Right, thdAmp.AmpLoad);

				// Add step data to list
				MeasurementResult.AmplitudeSteps.Add(step);

				UpdateGraph(false);
				//ShowLastMeasurementCursorTexts();

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					await Qa40x.SetOutputSource(OutputSources.Off);                                             // Be sure to switch gen off
					return false;
				}

				// Get maximum signal for attenuation prediction of next step
				prevInputAmplitudedBV = 20 * Math.Log10(lrfs.FreqRslt.Left.Max());
				prevInputAmplitudedBV = Math.Max(prevInputAmplitudedBV, 20 * Math.Log10(lrfs.FreqRslt.Left.Max()));
			}

			// Turn the generator off
			await Qa40x.SetOutputSource(OutputSources.Off);

			// Show message
			await showMessage($"Measurement finished!", 500);

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
			var thdAmp = ViewSettings.Singleton.ThdAmp;

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
			ScottPlot.TickGenerators.NumericAutomatic tickGenX = new();

			minorTickGenX.Divisions = 10;
			tickGenX.MinorTickGenerator = minorTickGenX;

			tickGenX.TargetTickCount = 25;
			// tell our major tick generator to only show major ticks that are whole integers
			tickGenX.IntegerTicksOnly = true;
			// tell our custom tick generator to use our new label formatter
			tickGenX.LabelFormatter = LogTickLabelFormatter;
			myPlot.Axes.Bottom.TickGenerator = tickGenX;

			// configure tick labels
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			// show grid lines for minor ticks
			myPlot.Grid.MajorLineColor = Colors.Black.WithOpacity(.25);
			myPlot.Grid.MinorLineColor = Colors.Black.WithOpacity(.10);
			myPlot.Grid.MinorLineWidth = 1;


			//myPlot.Axes.AutoScale();
			myPlot.Axes.SetLimits(Math.Log10(Convert.ToDouble(thdAmp.GraphStartVolts)), Math.Log10(Convert.ToDouble(thdAmp.GraphEndVolts)),
				Math.Log10(Convert.ToDouble(thdAmp.RangeBottom)), Math.Log10(Convert.ToDouble(thdAmp.RangeTop)));
			myPlot.Title("Distortion vs Amplitude (%)");
			myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.TITLE_SIZE);

			if (thdAmp.XAxisType == (int)E_X_AxisType.INPUT_VOLTAGE)
				myPlot.XLabel("Input voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_VOLTAGE)
				myPlot.XLabel("Output voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_POWER)
				myPlot.XLabel("Output power (W)");
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			myPlot.YLabel("Distortion (%)");
			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			SetupLegend(myPlot);

			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(0), Math.Log10(100), -200, 100)
				);

			myPlot.Axes.Rules.Clear();
			myPlot.Axes.Rules.Add(rule);

			thdPlot.Refresh();


		}

		/// <summary>
		/// Plot the THD % data
		/// </summary>
		/// <param name="data"></param>
		void PlotThd(ThdAmplitudeMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			var thdAmp = ViewSettings.Singleton.ThdAmp;
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

			foreach (var step in measurementResult.AmplitudeSteps)
			{
				double xValue = thdAmp.XAxisType switch
				{
					(int)E_X_AxisType.OUTPUT_VOLTAGE => step.Left.Fundamental_V,
					(int)E_X_AxisType.OUTPUT_POWER => step.Left.Power_Watt,
					_ => step.GeneratorVoltage
				};
				freqX.Add(xValue);

				if (showLeftChannel && measurementResult.MeasurementSettings.LeftChannel)
				{
					if (step.Left.Harmonics.Count > 0 && thdAmp.ShowTHD)
						hTotY_left.Add(step.Left.Thd_Percent);
					if (step.Left.Harmonics.Count > 0 && thdAmp.ShowD2)
						h2Y_left.Add(step.Left.Harmonics[0].Thd_Percent);
					if (step.Left.Harmonics.Count > 1 && thdAmp.ShowD3)
						h3Y_left.Add(step.Left.Harmonics[1].Thd_Percent);
					if (step.Left.Harmonics.Count > 2 && thdAmp.ShowD4)
						h4Y_left.Add(step.Left.Harmonics[2].Thd_Percent);
					if (step.Left.Harmonics.Count > 3 && thdAmp.ShowD5)
						h5Y_left.Add(step.Left.Harmonics[3].Thd_Percent);
					if (step.Left.Harmonics.Count > 4 && step.Left.ThdPercent_D6plus != 0 && thdAmp.ShowD6)
						h6Y_left.Add(step.Left.ThdPercent_D6plus);        // D6+
					if (thdAmp.ShowNoiseFloor)
						noiseY_left.Add((step.Left.Average_NoiseFloor_V / step.Left.Fundamental_V) * 100);
				}

				if (showRightChannel && measurementResult.MeasurementSettings.RightChannel)
				{
					if (step.Right.Harmonics.Count > 0 && thdAmp.ShowTHD)
						hTotY_right.Add(step.Right.Thd_Percent);
					if (step.Right.Harmonics.Count > 0 && thdAmp.ShowD2)
						h2Y_right.Add(step.Right.Harmonics[0].Thd_Percent);
					if (step.Right.Harmonics.Count > 1 && thdAmp.ShowD3)
						h3Y_right.Add(step.Right.Harmonics[1].Thd_Percent);
					if (step.Right.Harmonics.Count > 2 && thdAmp.ShowD4)
						h4Y_right.Add(step.Right.Harmonics[2].Thd_Percent);
					if (step.Right.Harmonics.Count > 3 && thdAmp.ShowD5)
						h5Y_right.Add(step.Right.Harmonics[3].Thd_Percent);
					if (step.Right.Harmonics.Count > 4 && step.Right.ThdPercent_D6plus != 0 && thdAmp.ShowD6)
						h6Y_right.Add(step.Right.ThdPercent_D6plus);        // D6+
					if (thdAmp.ShowNoiseFloor)
						noiseY_right.Add((step.Right.Average_NoiseFloor_V / step.Right.Fundamental_V) * 100);
				}
			}

			var colors = new GraphColors();
			float lineWidth = thdAmp.ShowThickLines ? 1.6f : 1;
			float markerSize = thdAmp.ShowPoints ? lineWidth + 3 : 1;

			double[] logFreqX = freqX.Select(Math.Log10).ToArray();
			int color = measurementNr * 2;

			void AddPlot(List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
			{
				if (yValues.Count == 0)
					return;
				double[] logYValues = yValues.Select(Math.Log10).ToArray();
				var plot = myPlot.Add.Scatter(logFreqX, logYValues);
				plot.LineWidth = lineWidth;
				plot.Color = colors.GetColor(colorIndex, color);
				plot.MarkerSize = markerSize;
				plot.LegendText = legendText;
				plot.LinePattern = linePattern;
			}

			if (showLeftChannel)
			{
				if (thdAmp.ShowTHD) AddPlot(hTotY_left, 8, showRightChannel ? "THD-L" : "THD", LinePattern.Solid);
				if (thdAmp.ShowD2) AddPlot(h2Y_left, 0, showRightChannel ? "D2-L" : "D2", LinePattern.Solid);
				if (thdAmp.ShowD3) AddPlot(h3Y_left, 1, showRightChannel ? "D3-L" : "D3", LinePattern.Solid);
				if (thdAmp.ShowD4) AddPlot(h4Y_left, 2, showRightChannel ? "D4-L" : "D4", LinePattern.Solid);
				if (thdAmp.ShowD5) AddPlot(h5Y_left, 3, showRightChannel ? "D5-L" : "D5", LinePattern.Solid);
				if (thdAmp.ShowD6) AddPlot(h6Y_left, 4, showRightChannel ? "D6+-L" : "D6+", LinePattern.Solid);
				if (thdAmp.ShowNoiseFloor) AddPlot(noiseY_left, 9, showRightChannel ? "Noise-L" : "Noise", showRightChannel ? LinePattern.Solid : LinePattern.Dotted);
			}

			if (showRightChannel)
			{
				if (thdAmp.ShowTHD) AddPlot(hTotY_right, 8, showLeftChannel ? "THD-R" : "THD", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD2) AddPlot(h2Y_right, 0, showLeftChannel ? "D2-R" : "D2", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD3) AddPlot(h3Y_right, 1, showLeftChannel ? "D3-R" : "D3", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD4) AddPlot(h4Y_right, 2, showLeftChannel ? "D4-R" : "D4", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD5) AddPlot(h5Y_right, 3, showLeftChannel ? "D5-R" : "D5", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowD6) AddPlot(h6Y_right, 4, showLeftChannel ? "D6+-R" : "D6+", showLeftChannel ? LinePattern.DenselyDashed : LinePattern.Solid);
				if (thdAmp.ShowNoiseFloor) AddPlot(noiseY_right, 9, showLeftChannel ? "Noise-R" : "Noise", LinePattern.Dotted);
			}

			//if (markerIndex != -1)
			//    QaLibrary.PlotCursorMarker(thdPlot, lineWidth, LinePattern.Solid, markerDataPoint);

			thdPlot.Refresh();
		}


		/// <summary>
		/// Initialize the THD magnitude (dB) plot
		/// </summary>
		void InitializeMagnitudePlot()
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			var thdAmp = ViewSettings.Singleton.ThdAmp;
			myPlot.Clear();

			// Y - axis
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
			tickGenY.TargetTickCount = 15;
			tickGenY.MinorTickGenerator = minorTickGen;

			// tell the left axis to use our custom tick generator
			myPlot.Axes.Left.TickGenerator = tickGenY;

			// X - axis
			// create a minor tick generator that places log-distributed minor ticks
			ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenX = new();
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.NumericAutomatic tickGenX = new();
			minorTickGenX.Divisions = 10;
			tickGenX.MinorTickGenerator = minorTickGenX;
			tickGenX.TargetTickCount = 25;
			// tell our major tick generator to only show major ticks that are whole integers
			tickGenX.IntegerTicksOnly = true;
			// tell our custom tick generator to use our new label formatter
			static string LogTickLabelFormatter(double y) => $"{Math.Pow(10, Math.Round(y, 10)):#0.######}";
			tickGenX.LabelFormatter = LogTickLabelFormatter;

			myPlot.Axes.Bottom.TickGenerator = tickGenX;


			// show grid lines for minor ticks
			myPlot.Grid.MajorLineColor = Colors.Black.WithOpacity(.25);
			myPlot.Grid.MajorLineWidth = 1;
			myPlot.Grid.MinorLineColor = Colors.Black.WithOpacity(.10);
			myPlot.Grid.MinorLineWidth = 1;

			myPlot.Axes.SetLimits(Math.Log10(Convert.ToDouble(thdAmp.GraphStartVolts)), Math.Log10(Convert.ToDouble(thdAmp.GraphEndVolts)), thdAmp.RangeBottomdB, thdAmp.RangeTopdB);

			myPlot.Title("Distortion vs Amplitude (dB)");
			myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.TITLE_SIZE);

			if (thdAmp.XAxisType == (int)E_X_AxisType.INPUT_VOLTAGE)
				myPlot.XLabel("Input voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_VOLTAGE)
				myPlot.XLabel("Output voltage (Vrms)");
			else if (thdAmp.XAxisType == (int)E_X_AxisType.OUTPUT_POWER)
				myPlot.XLabel("Output power (W)");
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			myPlot.YLabel("Distortion (dB)");
			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);

			// configure tick labels
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			// Legend
			SetupLegend(myPlot);

			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(0.0001), Math.Log10(100), -200, 100)
				);

			myPlot.Axes.Rules.Clear();
			myPlot.Axes.Rules.Add(rule);

			thdPlot.Refresh();
		}


		/// <summary>
		/// Plot the  THD magnitude (dB) data
		/// </summary>
		/// <param name="data">The data to plot</param>
		void PlotMagnitude(ThdAmplitudeMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
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

			void AddPlot(double[] xValues, List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
			{
				if (yValues.Count == 0) return;

				var plot = thdPlot.ThePlot.Add.Scatter(xValues, yValues.ToArray());
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
			MeasurementBusy = true;
			ct = new();
			await PerformMeasurementSteps(ct.Token);
			await showMessage("Finished");
			MeasurementBusy = false;
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
					PlotMagnitude(result, resultNr++, thdAmp.ShowLeft, thdAmp.ShowRight);
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
					PlotThd(result, resultNr++, thdAmp.ShowLeft, thdAmp.ShowRight);
				}
			}
		}

	}
}
