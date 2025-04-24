using QA40x_BareMetal;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Net.Http;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

namespace QA40xPlot.Actions
{

	public partial class ActThdAmplitude : ActBase
	{
		public ThdAmplitudeData Data { get; set; }       // Data used in this form instance
		private readonly Views.PlotControl thdPlot;
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;

		private ThdAmplitudeMeasurementResult MeasurementResult;
		private static ThdAmpViewModel MyVModel { get => ViewSettings.Singleton.ThdAmp; }


		CancellationTokenSource ct;                                 // Measurement cancelation token

		/// <summary>
		/// Constructor
		/// </summary>
		public ActThdAmplitude(ref ThdAmplitudeData data, Views.PlotControl graphThd, Views.PlotControl graphFft, Views.PlotControl graphTime)
		{
			Data = data;
			ThdAmpViewModel thd = MyVModel;
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

		public void DoCancel()
		{
			ct.Cancel();
		}

		private double ConvertToInputVoltage(double outV, double[] gains)
		{
			return outV * gains[0];
		}

		public Rect GetDataBounds()
		{
			var msr = MeasurementResult.MeasurementSettings;    // measurement settings
			if (msr == null || MeasurementResult.AmplitudeSteps.Count == 0)
				return Rect.Empty;
			var vmr = MeasurementResult.AmplitudeSteps.First(); // test data
			if (vmr == null || vmr.fftData == null)
				return Rect.Empty;
			var specVm = MyVModel;     // current settings

			Rect rrc = new Rect(0, 0, 0, 0);
			var steps = MeasurementResult.AmplitudeSteps;
			rrc.X = steps.First().GeneratorVoltage;
			rrc.Width = steps.Last().GeneratorVoltage - rrc.X;       // max frequency
			rrc.Y = 1e-15;
			rrc.Height = 1 - rrc.Y;
			double maxY = 1;
			if( specVm.ShowLeft)
			{
				rrc.Y = steps.Select(x => x.fftData?.Left.Min()).Min() ?? 1e-12;
				maxY = steps.Select(x => x.fftData?.Left.Max()).Max() ?? 1;
				if (specVm.ShowRight)
				{
					rrc.Y = Math.Min(rrc.Y, steps.Select(x => x.fftData?.Right.Min()).Min() ?? rrc.Y);
					maxY = Math.Max(maxY, steps.Select(x => x.fftData?.Left.Max()).Max() ?? maxY);
				}
			}
			else if (specVm.ShowRight)
			{
				rrc.Y = steps.Select(x => x.fftData?.Right.Min()).Min() ?? 1e-12;
				maxY = steps.Select(x => x.fftData?.Right.Max()).Max() ?? 1;
			}
			rrc.Height = maxY - rrc.Y;      // max voltage absolute

			var start = rrc.X;
			var end = rrc.X + rrc.Width;  // our input voltage boundary
			var ttype = ToDirection(specVm.GenDirection);
			var freq = MathUtil.ToDouble(msr.TestFreq);
			if (ttype == E_GeneratorDirection.OUTPUT_POWER || ttype == E_GeneratorDirection.OUTPUT_VOLTAGE)
			{
				var lrGains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				if(lrGains != null)
				{
					var startOut = ToGenOutVolts(start, [], lrGains);
					var endOut = ToGenOutVolts(end, [], lrGains);
					if (ttype == E_GeneratorDirection.OUTPUT_VOLTAGE)
					{
						rrc.X = startOut;
						rrc.Width = endOut - startOut;
					}
					else
					{
						rrc.X = startOut * startOut / ViewSettings.AmplifierLoad;
						rrc.Width = endOut * endOut / ViewSettings.AmplifierLoad - rrc.X;
					}
				}
			}
			else
			{
				rrc.X = start;
				rrc.Width = end - start;
			}
			return rrc;
		}

		public ValueTuple<ThdColumn?, ThdColumn?> LookupX(double amp)
		{
			var vm = MyVModel;
			var vf = vm.ShowLeft ? MeasurementResult.LeftColumns : MeasurementResult.RightColumns;
			if (vf == null || vf.Count == 0)
			{
				return ValueTuple.Create((ThdColumn?)null, (ThdColumn?)null);
			}
			var freq = vf[0].Freq;
			var vinp = vm.ToGenVoltage(amp.ToString(), [(int)Math.Floor(freq / (LRGains?.Df ?? 1))], GEN_INPUT, LRGains?.Left);

			// find nearest amplitude (both left and right will be identical here if scanned)
			// determine amp as an input voltage
			var bin = vf.Count(x => x.GenVolts < vinp) - 1;    // find first freq less than me
			if (bin == -1)
				bin = 0;
			var anearest = vf[bin].GenVolts;
			if (bin < (vf.Count - 1) && Math.Abs(vinp - anearest) > Math.Abs(vinp - vf[bin + 1].GenVolts))
			{
				bin++;
			}

			ThdColumn? mf1 = null;
			ThdColumn? mf2 = null;

			if( vm.ShowLeft)
				mf1 = MeasurementResult.LeftColumns?.ElementAt(bin);
			if (vm.ShowRight)
				mf2 = MeasurementResult.RightColumns?.ElementAt(bin);

			return ValueTuple.Create(mf1, mf2);
		}

		private ThdColumn? MakeColumn(ThdFrequencyStepChannel chan)
		{
			if (chan == null)
				return null;
			var cl = new ThdColumn();
			cl.Mag = chan.Fundamental_V;
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
			cl.Freq = MathUtil.ToDouble(MeasurementResult.MeasurementSettings.TestFreq, 10);
			cl.GenVolts = chan.Fundamental_V;
			return cl;
		}

		private void AddColumn(double inputVolt, ThdAmplitudeStep step)
		{
			var f = MeasurementResult.LeftColumns;
			if (f != null)
			{
				var cl = MakeColumn(step.Left);
				if (cl != null)
				{
					cl.GenVolts = inputVolt;
					MeasurementResult.LeftColumns.Add(cl);
				}
			}
			f = MeasurementResult.RightColumns;
			if (f != null)
			{
				var cl = MakeColumn(step.Right);
				if (cl != null)
				{
					cl.GenVolts = inputVolt;
					MeasurementResult.RightColumns.Add(cl);
				}
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
			MeasurementResult = new(MyVModel)
			{
				CreateDate = DateTime.Now,
				Show = true,                       // Show in graph
			};
			var msr = MeasurementResult.MeasurementSettings;
			var thdaVm = MyVModel;

			// For now clear measurements to allow only one until we have a UI to manage them.
			Data.Measurements.Clear();

			// Add to list
			Data.Measurements.Add(MeasurementResult);

			// Init mini plots
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 100000, -150, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			double testFreq = MathUtil.ToDouble(msr.TestFreq, 1000);
			double testFrequency = QaLibrary.GetNearestBinFrequency(testFreq, msr.SampleRateVal, msr.FftSizeVal);

			await showMessage("Calculating attenuation");
			LRGains = await DetermineGainAtFreq(testFrequency, true, 2);
			if (LRGains == null)
				return false;

			// ********************************************************************
			// Determine voltage sequences
			// ********************************************************************
			// specified voltages boundaries
			var startV = MathUtil.ToDouble(msr.StartVoltage, 1);
			var endV = MathUtil.ToDouble(msr.EndVoltage, 1);
			var stepVoltages = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(startV, endV, msr.StepsOctave);
			// now convert all of the step voltages to input voltages
			var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
			var stepInVoltages = stepVoltages.Select(x => msr.ToGenVoltage(x.ToString(), [], GEN_INPUT, gains)).ToArray();
			// get output values for left and right so we can attenuate
			var stepOutLVoltages = stepInVoltages.Select(x => ToGenOutVolts(x, [], LRGains.Left)).ToArray();
			var stepOutRVoltages = stepInVoltages.Select(x => ToGenOutVolts(x, [], LRGains.Right)).ToArray();

			if (ct.IsCancellationRequested)
				return false;

			// ********************************************************************
			// Do noise floor measurement
			// ********************************************************************
			if (true != QaUsb.InitializeDevice(msr.SampleRateVal, msr.FftSizeVal, msr.WindowingMethod, 12, false))
			{
				return false;
			}
			await showMessage($"Determining noise floor.");
			QaUsb.SetOutputSource(OutputSources.Off);
			MeasurementResult.NoiseFloor = await QaUsb.DoAcquisitions(1, ct);
			if (ct.IsCancellationRequested || MeasurementResult.NoiseFloor == null)
				return false;
			QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.NoiseFloor.FreqRslt, msr.ShowLeft, msr.ShowRight);
			QaLibrary.PlotMiniTimeGraph(timePlot, MeasurementResult.NoiseFloor.TimeRslt, testFrequency, msr.ShowLeft, msr.ShowRight);

			var binSize = QaLibrary.CalcBinSize(msr.SampleRateVal, msr.FftSizeVal);
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(testFrequency, binSize);
			QaUsb.SetOutputSource(OutputSources.Sine);                // We need to call this before all the testing

			// ********************************************************************
			// Step through the list of voltages
			// ********************************************************************
			for (int i = 0; i < stepInVoltages.Length; i++)
			{
				// attenuate for both channels
				var voutLdbv = QaLibrary.ConvertVoltage(stepOutLVoltages[i], E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				var voutRdbv = QaLibrary.ConvertVoltage(stepOutRVoltages[i], E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				var attenuate = QaLibrary.DetermineAttenuation(Math.Max(voutLdbv, voutRdbv));
				await showMessage($"Measuring step {i + 1} at {stepInVoltages[i]:0.###}V with attenuation {attenuate}.");
				await showProgress(100 * (i + 1) / (stepInVoltages.Length));

				// Convert generator voltage from V to dBV
				var generatorVoltageV = stepInVoltages[i];
				var generatorVoltagedBV = QaLibrary.ConvertVoltage(generatorVoltageV, E_VoltageUnit.Volt, E_VoltageUnit.dBV);   // Convert to dBV

				// Set generator
				QaUsb.SetGen1(testFrequency, generatorVoltagedBV, true);      // Set the generator in dBV
				QaUsb.SetInputRange(attenuate);
				thdaVm.Attenuation = attenuate;	// update the GUI

				LeftRightSeries? lrfs = null;
				do
				{
					try
					{
						lrfs = await QaUsb.DoAcquisitions(msr.Averages, ct);  // Do acquisitions
					}
					catch (HttpRequestException ex)
					{
						if (ex.Message.Contains("400 (Acquisition Overload)"))
						{
							await showMessage(ex.Message.ToString());
						}
						else
						{
							await showMessage(ex.Message.ToString());
						}
						break;
					}
					catch (Exception ex2)
					{
						await showMessage(ex2.Message.ToString());
						break;
					}
					if (ct.IsCancellationRequested)
						break;
				} while (lrfs == null);     // Loop until we have an acquisition result

				if (lrfs == null || lrfs.FreqRslt == null || MeasurementResult.NoiseFloor?.FreqRslt == null)
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
				QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, msr.ShowLeft, msr.ShowRight);
				QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, step.FundamentalFrequency, msr.ShowLeft, msr.ShowRight);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				step.Left = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltageV, 
					lrfs.FreqRslt.Left, MeasurementResult.NoiseFloor.FreqRslt.Left, ViewSettings.AmplifierLoad);
				step.Right = ChannelCalculations(binSize, step.FundamentalFrequency, generatorVoltageV, 
					lrfs.FreqRslt.Right, MeasurementResult.NoiseFloor.FreqRslt.Right, ViewSettings.AmplifierLoad);

				// Add step data to list
				MeasurementResult.AmplitudeSteps.Add(step);
				AddColumn(generatorVoltageV, step);

				UpdateGraph(false);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				// Get maximum signal for attenuation prediction of next step
				//prevInputAmplitudedBV = 20 * Math.Log10(lrfs.FreqRslt.Left.Max());
				//prevInputAmplitudedBV = Math.Max(prevInputAmplitudedBV, 20 * Math.Log10(lrfs.FreqRslt.Left.Max()));
				if (!msr.IsTracking)
				{
					msr.RaiseMouseTracked("track");
				}

			}

			EndAction();

			// Show message
			await showMessage(ct.IsCancellationRequested ? $"Measurement cancelled!" : $"Measurement finished!");

			return true;
		}


		private double SafeLog(double? din)
		{
			if (din == null || din == 0)
				return -9;
			return Math.Log10((double)din);
		}

		private ThdFrequencyStepChannel ChannelCalculations(double binSize, double fundamentalFrequency, double generatorAmplitudeV, double[] fftData, double[] noiseFloorFftData, double load)
		{
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFrequency, binSize);
			int binmin = (int)Math.Max(0, fundamentalBin - 2);
			int bintrack = (int)(Math.Min(fftData.Length, fundamentalBin + 2) - binmin);

			// the amplitude is max of a small area
			var fftVal = fftData.Skip(binmin).Take(bintrack).Max();

			// Get and store step data
			ThdFrequencyStepChannel channelData = new()
			{
				Fundamental_V = fftVal,
				Fundamental_dBV = QaLibrary.ConvertVoltage(fftVal, E_VoltageUnit.Volt, E_VoltageUnit.dBV),
				Gain_dB = QaLibrary.ConvertVoltage((fftVal / generatorAmplitudeV), E_VoltageUnit.Volt, E_VoltageUnit.dBV)
			};

			// Calculate average noise floor
			channelData.Average_NoiseFloor_V = noiseFloorFftData
				.Skip((int)fundamentalBin + 1)
				.Average();
			channelData.Average_NoiseFloor_dBV = 20 * SafeLog(channelData.Average_NoiseFloor_V);

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
				};
				if( bin < fftData.Length)
				{
					harmonic.Amplitude_V = fftData[bin];
					harmonic.NoiseAmplitude_V = noiseFloorFftData[bin];
					harmonic.Amplitude_dBV = 20 * Math.Log10(fftData[bin]);
					harmonic.Thd_Percent = (fftData[bin] / channelData.Fundamental_V) * 100;
					harmonic.Thd_dB = 20 * Math.Log10(fftData[bin] / channelData.Fundamental_V);
				}
				else
				{
					harmonic.Amplitude_V = 0;
					harmonic.NoiseAmplitude_V = 0;
					harmonic.Amplitude_dBV = -180;
					harmonic.Thd_Percent = 0;
					harmonic.Thd_dB = -180;
				}

				if (h >= 6)
					distiortionD6plus += Math.Pow(harmonic.Amplitude_V, 2);

				distortionSqrtTotal += Math.Pow(harmonic.Amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
			}

			// Calculate THD of current step
			channelData.Thd_Percent = (Math.Sqrt(distortionSqrtTotal) / channelData.Fundamental_V) * 100;
			channelData.Thd_dB = 20 * SafeLog(channelData.Thd_Percent / 100.0);

			// Calculate D6+ (D6 - D12)
			if (distiortionD6plus != 0)
			{
				channelData.D6Plus_dBV = 20 * SafeLog(Math.Sqrt(distiortionD6plus));
				channelData.ThdPercent_D6plus = Math.Sqrt(distiortionD6plus / Math.Pow(channelData.Fundamental_V, 2)) * 100;
				channelData.ThdDbD6plus = 20 * SafeLog(channelData.ThdPercent_D6plus / 100.0);
			}

			// If load not zero then calculate load power
			if (load == 0)
				load = 8;
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
			var thdAmp = MyVModel;
			var tt = ToDirection(thdAmp.GenDirection);
			if (tt == E_GeneratorDirection.INPUT_VOLTAGE)
			{
				myPlot.XLabel("Input voltage (Vrms)");
				myPlot.Title("Distortion vs Voltage");
			}
			else if (tt == E_GeneratorDirection.OUTPUT_VOLTAGE)
			{
				myPlot.XLabel("Output voltage (Vrms)");
				myPlot.Title("Distortion vs Voltage");
			}
			else if (tt == E_GeneratorDirection.OUTPUT_POWER)
			{
				myPlot.XLabel("Output power (W)");
				myPlot.Title("Distortion vs Power");
			}
		}

		/// <summary>
		/// Initialize the THD % plot
		/// </summary>
		void InitializeThdPlot()
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			PlotUtil.InitializePctAmpPlot(myPlot);
			var thdAmp = MyVModel;
			myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thdAmp.GraphStartVolts)), Math.Log10(MathUtil.ToDouble(thdAmp.GraphEndVolts)),
				Math.Log10(MathUtil.ToDouble(thdAmp.RangeBottom)), Math.Log10(MathUtil.ToDouble(thdAmp.RangeTop)));
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
			PlotUtil.InitializeMagAmpPlot(myPlot);
			var thdAmp = MyVModel;
			myPlot.Axes.SetLimits(Math.Log10(MathUtil.ToDouble(thdAmp.GraphStartVolts)), Math.Log10(MathUtil.ToDouble(thdAmp.GraphEndVolts)),
				MathUtil.ToDouble(thdAmp.RangeBottomdB), MathUtil.ToDouble(thdAmp.RangeTopdB));
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

			var thdAmp = MyVModel;
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
			var ttype = ToDirection(thdAmp.GenDirection);
			double[] amps = [];
			foreach (var col in columns)
			{
				switch(ttype)
				{
					case E_GeneratorDirection.INPUT_VOLTAGE:
						amps = col.Select(x => x.GenVolts).ToArray();
						break;
					case E_GeneratorDirection.OUTPUT_VOLTAGE:
						amps = col.Select(x => x.Mag).ToArray();
						break;
					case E_GeneratorDirection.OUTPUT_POWER:
						amps = col.Select(x => (x.Mag * x.Mag / ViewSettings.AmplifierLoad)).ToArray();
						break;
				}
				amps = amps.Select(x => Math.Log10(x)).ToArray();
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
			var thdAmp = MyVModel;
			if (!StartAction(thdAmp))
				return;

			ct = new();
			await PerformMeasurementSteps(ct.Token);
			await showMessage("Finished");
			thdAmp.IsRunning = false;
		}


		public void UpdateGraph(bool settingsChanged)
		{
			thdPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			var thdAmp = MyVModel;
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
