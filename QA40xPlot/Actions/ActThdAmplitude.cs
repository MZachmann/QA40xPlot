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

	using MyDataTab = DataTab<ThdAmpViewModel>;

	public partial class ActThdAmplitude : ActBase
	{
		private readonly Views.PlotControl thdPlot;
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;

		public MyDataTab PageData { get; private set; } // Data used in this form instance
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document

		private float _Thickness = 2.0f;

		private static ThdAmpViewModel MyVModel { get => ViewSettings.Singleton.ThdAmp; }
		CancellationTokenSource ct;                                  // Measurement cancelation token

		/// <summary>
		/// Constructor
		/// </summary>
		public ActThdAmplitude(ref ThdAmplitudeData data, Views.PlotControl graphThd, Views.PlotControl graphFft, Views.PlotControl graphTime)
		{
			ct = new CancellationTokenSource();
			PageData = new(MyVModel, new LeftRightTimeSeries());

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

		private double[] ColumnToArray(ThdColumn col)
		{
			return new double[] { col.Freq, col.Mag, col.THD, col.Noise, col.D2, col.D3, col.D4, col.D5, col.D6P, col.GenVolts };
		}

		private ThdColumn ArrayToColumn(double[] rawData, uint startIdx)
		{
			ThdColumn col = new();
			col.Freq = rawData[startIdx];
			col.Mag = rawData[startIdx + 1];
			col.THD = rawData[startIdx + 2];
			col.Noise = rawData[startIdx + 3];
			col.D2 = rawData[startIdx + 4];
			col.D3 = rawData[startIdx + 5];
			col.D4 = rawData[startIdx + 6];
			col.D5 = rawData[startIdx + 7];
			col.D6P = rawData[startIdx + 8];
			col.GenVolts = rawData[startIdx + 9];
			return col;
		}

		private ThdColumn[] RawToColumn(double[] raw)
		{
			if (raw.Length == 0)
				return [];
			List<ThdColumn> left = new();
			for (int i = 0; i < raw.Length; i += 10)
			{
				var col = ArrayToColumn(raw, (uint)i);
				left.Add(col);
			}
			return left.ToArray();
		}

		// convert the raw data into columns of data
		private void RawToThdColumns(MyDataTab page)
		{
			var u = RawToColumn(page.Sweep.RawLeft);
			var v = RawToColumn(page.Sweep.RawRight);
			page.SetProperty("Left", u);
			page.SetProperty("Right", v);
		}

		/// <summary>
		/// add a column to the data table columns
		/// </summary>
		/// <param name="x"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		private void AddColumn(MyDataTab page, double x, ThdColumn left, ThdColumn right)
		{
			page.Sweep.X = page.Sweep.X.Append(x).ToArray();
			page.Sweep.RawLeft = page.Sweep.RawLeft.Concat(ColumnToArray(left)).ToArray();
			page.Sweep.RawRight = page.Sweep.RawRight.Concat(ColumnToArray(right)).ToArray();
		}

		private double ConvertToInputVoltage(double outV, double[] gains)
		{
			return outV * gains[0];
		}

		public Rect GetDataBounds()
		{
			var msr = PageData.ViewModel;    // measurement settings
			var Xvalues = PageData.Sweep.X;
			if (msr == null || Xvalues.Length == 0)
				return Rect.Empty;
			var specVm = MyVModel;     // current settings

			ThdColumn[] steps = (ThdColumn[])((msr.ShowLeft ? PageData.GetProperty("Left") : PageData.GetProperty("Right")) ?? new());
			if (steps.Length == 0)
				return Rect.Empty;

			Rect rrc = new Rect(0, 0, 0, 0);
			rrc.X = Xvalues.Min();              // min X/Freq value
			rrc.Width = Xvalues.Max() - rrc.X;  // max frequency

			double maxY = 0;
			rrc.Y = steps.Min(x => Math.Min(Math.Min(x.THD, x.D2), x.D5));      // min magnitude will be min value shown
			maxY = steps.Max(x => Math.Max(x.Mag, x.THD));      // maximum magnitude will be max value shown
			rrc.Height = maxY - rrc.Y;      // max voltage absolute

			return rrc;
		}

		private (ThdColumn, ThdColumn) LookupColumn(MyDataTab page, double freq)
		{
			var vm = MyVModel;
			var vf = page.Sweep.X;
			if (vf.Length == 0)
			{
				return (new ThdColumn(), new ThdColumn());
			}
			// find nearest amplitude (both left and right will be identical here if scanned)
			var bin = vf.Count(x => x < freq) - 1;    // find first freq less than me
			if (bin == -1)
				bin = 0;
			var anearest = vf[bin];
			if (bin < (vf.Length - 1) && Math.Abs(freq - anearest) > Math.Abs(freq - vf[bin + 1]))
			{
				bin++;
			}
			ThdColumn? mf1 = new ThdColumn();
			ThdColumn? mf2 = new ThdColumn();
			var u = page.GetProperty("Left");
			if (u != null)
				mf1 = ((ThdColumn[])u)[bin];    // get the left channel
			u = page.GetProperty("Right");
			if (u != null)
				mf2 = ((ThdColumn[])u)[bin];    // get the left channel
			return (mf1, mf2);
		}

		public ThdColumn[] LookupX(double freq)
		{
			var vm = MyVModel;
			List<ThdColumn> myset = new();
			var all = LookupColumn(PageData, freq); // lookup the columns
			if (vm.ShowLeft)
				myset.Add(all.Item1);
			if (vm.ShowRight)
				myset.Add(all.Item2);
			if (OtherTabs.Count() > 0)
			{
				var all2 = LookupColumn(OtherTabs.First(), freq); // lookup the columns
				if (vm.ShowOtherLeft)
					myset.Add(all2.Item1);
				if (vm.ShowOtherRight)
					myset.Add(all2.Item2);
			}
			return myset.ToArray();
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<ThdAmpViewModel>(PageData, fileName);
		}

		public async Task LoadFromFile(string fileName, bool doLoad)
		{
			var page = Util.LoadFile<ThdAmpViewModel>(PageData, fileName);
			RawToThdColumns(page);
			await FinishLoad(page, doLoad);
		}

		/// <summary>
		/// Load a file into a DataTab
		/// </summary>
		/// <param name="fileName">full path name</param>
		/// <returns>a datatab with no frequency info</returns>
		public MyDataTab LoadFile(MyDataTab page, string fileName)
		{
			return Util.LoadFile<ThdAmpViewModel>(page, fileName);
		}

		/// <summary>
		/// given a datatab, integrate it into the gui as the current datatab
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public async Task FinishLoad(MyDataTab page, bool doLoad)
		{
			// now recalculate everything
			await PostProcess(page, ct.Token);
			if (doLoad)
			{
				PageData = page;    // set the current page to the loaded one
									// we can't overwrite the viewmodel since it links to the display proper
									// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				page.ViewModel.CopyPropertiesTo<ThdAmpViewModel>(ViewSettings.Singleton.ThdAmp);    // retract the gui

				// relink to the new definition
				MyVModel.LinkAbout(page.Definition);
			}
			else
			{
				OtherTabs.Clear(); // clear the other tabs
				OtherTabs.Add(page); // add the new one
			}

			UpdateGraph(true);
		}

		// build a sinewave for the sweep
		private static double[] BuildWave(MyDataTab page, double dFreq)
		{
			var vm = page.ViewModel;

			// for the first go around, turn on the generator
			// Set the generators via a usermode
			var waveForm = new GenWaveform()
			{
				Frequency = dFreq,
				Voltage = page.Definition.GeneratorVoltage,
				Name = "Sine"
			};
			var waveSample = new GenWaveSample()
			{
				SampleRate = (int)vm.SampleRateVal,
				SampleSize = (int)vm.FftSizeVal
			};

			double[] wave = QaMath.CalculateWaveform([waveForm], waveSample).ToArray();
			return wave;
		}

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async Task DoMeasurement()
		{
			var thdAmp = MyVModel;
			if (!await StartAction(thdAmp))
				return;
			try
			{
				await RunMeasurement();
			}
			catch (Exception ex)
			{
				await showMessage(ex.Message.ToString());
			}
			await EndAction(thdAmp);
		}

		public async Task RunMeasurement()
		{ 
			var thdAmp = MyVModel;

			ct = new();
			LeftRightTimeSeries lrts = new();
			MyDataTab NextPage = new(thdAmp, lrts);
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			PageData = NextPage;    // set the current page to the loaded one
			var page = PageData;    // alias
			var vm = page.ViewModel;
			if (vm == null)
				return;
			var genType = ToDirection(vm.GenDirection);

			// Init mini plots
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 100000, -150, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			double testFreq = MathUtil.ToDouble(vm.TestFreq, 1000);
			double testFrequency = QaLibrary.GetNearestBinFrequency(testFreq, vm.SampleRateVal, vm.FftSizeVal);

			await showMessage("Calculating DUT gain");
			LRGains = await DetermineGainAtFreq(testFrequency, true, 2);
			if (LRGains == null)
				return;

			// ********************************************************************
			// Determine voltage sequences
			// ********************************************************************
			// specified voltages boundaries
			var startV = MathUtil.ToDouble(vm.StartVoltage, 1);
			var endV = MathUtil.ToDouble(vm.EndVoltage, 1);
			var stepVoltages = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(startV, endV, vm.StepsOctave);
			// now convert all of the step voltages to input voltages
			var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
			var stepInVoltages = stepVoltages.Select(x => vm.ToGenVoltage(x.ToString(), [], GEN_INPUT, gains)).ToArray();
			// get output values for left and right so we can attenuate
			var stepOutLVoltages = stepInVoltages.Select(x => ToGenOutVolts(x, [], LRGains.Left)).ToArray();
			var stepOutRVoltages = stepInVoltages.Select(x => ToGenOutVolts(x, [], LRGains.Right)).ToArray();

			if (ct.IsCancellationRequested)
				return;

			// ********************************************************************
			// Do noise floor measurement
			// ********************************************************************
			if (true != await QaComm.InitializeDevice(vm.SampleRateVal, vm.FftSizeVal, vm.WindowingMethod, 12))
			{
				return;
			}

			// ********************************************************************
			// Do noise floor measurement
			// ********************************************************************
			var noisy = await MeasureNoise(ct.Token);
			if (ct.IsCancellationRequested)
				return;
			page.NoiseFloor = new LeftRightPair();
			page.NoiseFloor.Right = QaCompute.CalculateNoise(noisy.FreqRslt, true);
			page.NoiseFloor.Left = QaCompute.CalculateNoise(noisy.FreqRslt, false);

			if (ct.IsCancellationRequested)
				return;

			QaLibrary.PlotMiniFftGraph(fftPlot, noisy.FreqRslt, vm.ShowLeft, vm.ShowRight);
			QaLibrary.PlotMiniTimeGraph(timePlot, noisy.TimeRslt, testFrequency, vm.ShowLeft, vm.ShowRight);

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
				page.Definition.GeneratorVoltage = generatorVoltageV;

				// Set generator
				await QaComm.SetInputRange(attenuate);
				thdAmp.Attenuation = attenuate;	// update the GUI
				vm.Attenuation = attenuate; // update the model data

				LeftRightSeries? lrfs = null;
				try
				{
					var wave = BuildWave(page, testFrequency);   // also update the waveform variables
					lrfs = await QaComm.DoAcquireUser(vm.Averages, ct.Token, wave, wave, true);
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
				page.TimeRslt = lrfs.TimeRslt ?? new();
				page.FreqRslt = lrfs.FreqRslt;

				if (page.TimeRslt.Left.Length == 0 || lrfs.FreqRslt == null )               // Check in bin within range
					break;

				// Plot the mini graphs
				QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, vm.ShowLeft, vm.ShowRight);
				QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, testFrequency, vm.ShowLeft, vm.ShowRight);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				var work = CalculateColumn(page, testFrequency, generatorVoltageV, ct.Token); // do the math for the columns
				if (work.Item1 != null && work.Item2 != null)
					AddColumn(page, generatorVoltageV, work.Item1, work.Item2);

				MyVModel.LinkAbout(PageData.Definition);
				RawToThdColumns(page);
				UpdateGraph(false);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
				{
					break;
				}

				// Get maximum signal for attenuation prediction of next step
				//prevInputAmplitudedBV = 20 * Math.Log10(lrfs.FreqRslt.Left.Max());
				//prevInputAmplitudedBV = Math.Max(prevInputAmplitudedBV, 20 * Math.Log10(lrfs.FreqRslt.Left.Max()));
				if (!vm.IsTracking)
				{
					vm.RaiseMouseTracked("track");
				}

			}

			PageData.TimeRslt = new();
			PageData.FreqRslt = null;

			// Show message
			await showMessage(ct.IsCancellationRequested ? $"Measurement cancelled!" : $"Measurement finished!");

			await showMessage("Finished");
		}

		private double SafeLog(double? din)
		{
			if (din == null || din == 0)
				return -9;
			return Math.Log10((double)din);
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
		/// do post processing, just a bunch of easy math and moving stuff into viewmodels
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		private async Task<bool> PostProcess(MyDataTab msr, CancellationToken ct)
		{
			await Task.Delay(1); // allow the UI to update
			return true;
		}

		/// <summary>
		/// do post processing, just a bunch of easy math and moving stuff into viewmodels
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		private (ThdColumn?, ThdColumn?) CalculateColumn(MyDataTab msr, double dFreq, double dVolts, CancellationToken ct)
		{
			if (msr.FreqRslt == null)
			{
				return (null, null);
			}

			// left and right channels summary info to fill in
			var left = new ThdColumn();
			var right = new ThdColumn();
			ThdColumn[] steps = [left, right];
			ThdAmpViewModel vm = msr.ViewModel;

			var lrfs = msr.FreqRslt;    // frequency response

			var maxf = msr.FreqRslt.Df * msr.FreqRslt.Left.Length;
			//LeftRightPair snrdb = QaCompute.GetSnrDb(lrfs, dFreq, 20.0, maxf);
			LeftRightPair thds = QaCompute.GetThdDb(lrfs, dFreq, 20.0, Math.Min(20000, maxf));
			//LeftRightPair thdN = QaCompute.GetThdnDb(lrfs, dFreq, 20.0, maxf);

			var frq = msr.FreqRslt.Left;    // start with left
			foreach (var step in steps)
			{
				step.GenVolts = dVolts;
				step.Freq = dFreq;
				step.Mag = QaMath.MagAtFreq(frq, msr.FreqRslt.Df, dFreq);
				step.D2 = (maxf > (2 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 2 * dFreq) : 1e-10;
				step.D3 = (maxf > (3 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 3 * dFreq) : step.D2;
				step.D4 = (maxf > (4 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 4 * dFreq) : step.D3;
				step.D5 = (maxf > (5 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 5 * dFreq) : step.D4;
				step.D6P = 0;
				if (maxf > (6 * dFreq))
				{
					for (int i = 6; i < 12; i++)
					{
						step.D6P += (maxf > (i * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, i * dFreq) : 0;
					}
				}
				else
				{
					step.D6P = step.D5;
				}
				frq = msr.FreqRslt.Right;   // now right
			}
			left.THD = left.Mag * Math.Pow(10, thds.Left / 20); // in volts from dB relative to mag
			right.THD = right.Mag * Math.Pow(10, thds.Right / 20); ;
			left.Noise = Math.Max(1e-10, msr.NoiseFloor.Left);
			right.Noise = Math.Max(1e-10, msr.NoiseFloor.Right);

			return (left, right);
		}


		private double ToD(string stri)
		{
			return MathUtil.ToDouble(stri);
		}

		/// <summary>
		/// Initialize the THD % plot
		/// </summary>
		void InitializeThdPlot(string plotFormat = "%")
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			PlotUtil.InitializeMagAmpPlot(myPlot, plotFormat);

			var thdFreq = MyVModel;
			myPlot.Axes.SetLimits(Math.Log10(ToD(thdFreq.GraphStartVolts)), Math.Log10(ToD(thdFreq.GraphEndVolts)),
				Math.Log10(ToD(thdFreq.RangeBottom)), Math.Log10(ToD(thdFreq.RangeTop)));
			SetPlotLabels();
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			thdPlot.Refresh();
		}

		/// <summary>
		/// Initialize the THD magnitude (dB) plot
		/// </summary>
		void InitializeMagnitudePlot(string plotFormat = "dBV")
		{
			var thdFreq = MyVModel;
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			PlotUtil.InitializeMagAmpPlot(myPlot, plotFormat);

			myPlot.Axes.SetLimits(Math.Log10(ToD(thdFreq.GraphStartVolts)), Math.Log10(ToD(thdFreq.GraphEndVolts)),
				ToD(thdFreq.RangeBottomdB), ToD(thdFreq.RangeTopdB));
			SetPlotLabels();
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			thdPlot.Refresh();
		}

		// this always uses the 'global' format so others work too
		private double FormVal(double d1, double dMax)
		{
			var x = GraphUtil.ReformatValue(MyVModel.PlotFormat, d1, dMax);
			return GraphUtil.IsPlotFormatLog(MyVModel.PlotFormat) ? x : Math.Log10(x);
		}

		/// <summary>
		/// Plot the  THD magnitude (dB) data
		/// </summary>
		/// <param name="data">The data to plot</param>
		private void PlotValues(MyDataTab page, int measurementNr, bool isMain)
		{
			var thdAmp = MyVModel;
			ThdColumn[] leftCol = page.GetProperty("Left") as ThdColumn[] ?? [];
			ThdColumn[] rightCol = page.GetProperty("Right") as ThdColumn[] ?? [];
			if (leftCol.Length == 0)
				return;

			var showLeft = isMain ? thdAmp.ShowLeft : thdAmp.ShowOtherLeft;
			var showRight = isMain ? thdAmp.ShowRight : thdAmp.ShowOtherRight;
			if (!showLeft && !showRight)
				return;

			float lineWidth = thdAmp.ShowThickLines ? 1.6f : 1;
			float markerSize = thdAmp.ShowPoints ? lineWidth + 3 : 1;

			var colors = new GraphColors();
			int color = measurementNr * 2;

			// here Y values are in dBV
			void AddPlot(double[] xValues, List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
			{
				var u = measurementNr;
				if (yValues.Count == 0) return;
				Scatter? plot = null;
				plot = thdPlot.ThePlot.Add.Scatter(xValues, yValues.ToArray());
				plot.LineWidth = lineWidth;
				plot.Color = colors.GetColor(colorIndex, color);
				plot.MarkerSize = markerSize;
				plot.LegendText = legendText;
				plot.LinePattern = linePattern;
			}

			// which columns are we displaying? left, right or both
			// which columns are we displaying? left, right or both
			List<ThdColumn[]> columns;
			if (showLeft && showRight)
			{
				columns = [leftCol, rightCol];
			}
			else if (!showRight)
			{
				columns = [leftCol];
			}
			else
			{
				columns = [rightCol];
			}

			string suffix = string.Empty;
			var lp = isMain ? LinePattern.Solid : LinePattern.Dashed;
			if (showRight && showLeft)
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
					AddPlot(amps, col.Select(x => FormVal(x.Mag, x.Mag)).ToList(), 9, "Mag" + suffix, LinePattern.DenselyDashed);
				if (thdAmp.ShowTHD)
					AddPlot(amps, col.Select(x => FormVal(x.THD, x.Mag)).ToList(), 8, "THD" + suffix, lp);
				if (thdAmp.ShowD2)
					AddPlot(amps, col.Select(x => FormVal(x.D2, x.Mag)).ToList(), 0, "D2" + suffix, lp);
				if (thdAmp.ShowD3)
					AddPlot(amps, col.Select(x => FormVal(x.D3, x.Mag)).ToList(), 1, "D3" + suffix, lp);
				if (thdAmp.ShowD4)
					AddPlot(amps, col.Select(x => FormVal(x.D4, x.Mag)).ToList(), 2, "D4" + suffix, lp);
				if (thdAmp.ShowD5)
					AddPlot(amps, col.Select(x => FormVal(x.D5, x.Mag)).ToList(), 3, "D5" + suffix, lp);
				if (thdAmp.ShowD6)
					AddPlot(amps, col.Select(x => FormVal(x.D6P, x.Mag)).ToList(), 3, "D6+" + suffix, lp);
				if (thdAmp.ShowNoiseFloor)
					AddPlot(amps, col.Select(x => FormVal(x.Noise, x.Mag)).ToList(), 3, "Noise" + suffix, LinePattern.Dotted);
				suffix = "-R";          // second pass iff there are both channels
				lp = isMain ? LinePattern.DenselyDashed : LinePattern.Dotted;
			}

			thdPlot.Refresh();
		}


		public void UpdateGraph(bool settingsChanged)
		{
			thdPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			int resultNr = 0;
			ThdAmpViewModel thd = MyVModel;

			if (GraphUtil.IsPlotFormatLog(thd.PlotFormat))
			{
				if (settingsChanged)
				{
					InitializeMagnitudePlot(thd.PlotFormat);
				}
			}
			else
			{
				if (settingsChanged)
				{
					InitializeThdPlot(thd.PlotFormat);
				}
			}
			PlotValues(PageData, resultNr++, true);
			if (thd.ShowOtherLeft || thd.ShowOtherRight)
			{
				if (OtherTabs.Count > 0)
				{
					foreach (var other in OtherTabs)
					{
						if (other != null)
							PlotValues(other, resultNr++, false);
					}
				}
			}
		}

	}
}
