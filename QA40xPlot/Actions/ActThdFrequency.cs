using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using static QA40xPlot.ViewModels.BaseViewModel;


// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{
	using MyDataTab = DataTab<ThdFreqViewModel>;

	public class ActThdFrequency : ActBase
    {
        private readonly Views.PlotControl thdPlot;
        private readonly Views.PlotControl fftPlot;
        private readonly Views.PlotControl timePlot;

		public MyDataTab PageData { get; private set; } // Data used in this form instance
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document

		private float _Thickness = 2.0f;

		private static ThdFreqViewModel MyVModel { get => ViewSettings.Singleton.ThdFreq; }
		CancellationTokenSource ct;                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActThdFrequency(Views.PlotControl graphThd, Views.PlotControl graphFft, Views.PlotControl graphTime)
        {
            fftPlot = graphFft;
            timePlot = graphTime;
            thdPlot = graphThd;

			ct = new CancellationTokenSource();
			PageData = new(MyVModel, new LeftRightTimeSeries());

			// Show empty graphs
			ThdFreqViewModel thd = MyVModel;
            QaLibrary.InitMiniFftPlot(fftPlot, MathUtil.ToDouble(thd.StartFreq, 10),
                MathUtil.ToDouble(thd.EndFreq, 20000), -150, 20);
            QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

            UpdateGraph(true);
        }

        public void DoCancel()
        {
            ct.Cancel();
        }

		public void UpdatePlotTitle()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			var title = "Distortion vs Frequency";
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
			else
				myPlot.Title(title);
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
            rrc.Y = steps.Min(x => Math.Min(Math.Min(x.THD, x.D2),x.D5));      // min magnitude will be min value shown
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
			if(u != null)
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
			if(OtherTabs.Count() > 0)
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
			return Util.SaveToFile<ThdFreqViewModel>(PageData, fileName);
		}

		public async Task LoadFromFile(string fileName, bool doLoad)
		{
			var page = Util.LoadFile<ThdFreqViewModel>(PageData, fileName);
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
			return Util.LoadFile<ThdFreqViewModel>(page, fileName);
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
				page.ViewModel.CopyPropertiesTo<ThdFreqViewModel>(ViewSettings.Singleton.ThdFreq);    // retract the gui

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
		///  Start measurement button click
		/// </summary>
		public async Task DoMeasurement()
		{
			var freqVm = MyVModel;          // the active viewmodel
			if (!await StartAction(freqVm))
				return;

			try
			{
				ct = new();
				await RunAcquisition();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			PageData.TimeRslt = new();
			PageData.FreqRslt = null;

			// Show message
			await showMessage(ct.IsCancellationRequested ? $"Measurement cancelled!" : $"Measurement finished!");

			await EndAction(freqVm);

			await showMessage("Finished");
		}

		public async Task<bool> RunAcquisition()
		{
			var freqVm = MyVModel;          // the active viewmodel
			LeftRightTimeSeries lrts = new();
			MyDataTab NextPage = new(freqVm, lrts);
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			PageData = NextPage;    // set the current page to the loaded one
            var page = PageData;    // alias
			var vm = page.ViewModel;
			if (vm == null)
				return false;
			var genType = ToDirection(vm.GenDirection);

			// Init mini plots
			var startFreq = MathUtil.ToDouble(vm.StartFreq, 2);
			var endFreq = MathUtil.ToDouble(vm.EndFreq, 20000);

			QaLibrary.InitMiniFftPlot(fftPlot, Math.Max(10,startFreq), endFreq, -150, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			// ********************************************************************
			// Determine input level / attenuation
			// ********************************************************************
			await showMessage("Calculating DUT gain");
			LRGains = await DetermineGainCurve(true, 1);   // read the gain curve
			if (LRGains == null)
				return false;

			var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
			int[] frqtest = [ToBinNumber(startFreq, LRGains), ToBinNumber(endFreq, LRGains)];
			var genVolt = vm.ToGenVoltage(vm.GenVoltage, frqtest, GEN_INPUT, gains);   // input voltage for request
            page.Definition.GeneratorVoltage = genVolt;   // set the generator voltage in the definition

			try
			{
				// ********************************************************************
				// Determine input level for attenuation
				// ********************************************************************
				var genOut = ToGenOutVolts(genVolt, frqtest, gains);   // output voltage for request
				double amplifierOutputVoltagedBV = QaLibrary.ConvertVoltage(genOut, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

				// Get input voltage based on desired output voltage
				var attenuation = QaLibrary.DetermineAttenuation(amplifierOutputVoltagedBV);
				await showMessage($"Setting attenuation to {attenuation:0}", 200);

				// Set the new input range
				MyVModel.Attenuation = attenuation;   // visible display
                vm.Attenuation = attenuation;         // my setting
				await QaComm.SetInputRange(attenuation);

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
					return false;

				// ********************************************************************
				// Calculate frequency steps to do
				// ********************************************************************
				var binSize = QaLibrary.CalcBinSize(vm.SampleRateVal, vm.FftSizeVal);
				// Generate a list of frequencies
				var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(
					MathUtil.ToDouble(vm.StartFreq, 10), MathUtil.ToDouble(vm.EndFreq, 10000), vm.StepsOctave);
				var maxBinFreq = vm.SampleRateVal / 4;	// nyquist over 2 since we're looking at distortion
				// Translate the generated list to bin center frequencies
				var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, vm.SampleRateVal, vm.FftSizeVal);
				stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x < maxBinFreq)                // Filter out values that are out of range 
					.GroupBy(x => x)                                                                    // Filter out duplicates
					.Select(y => y.First())
					.ToArray();

				// ********************************************************************  
				// Load a settings we want since we're done autoscaling
				// ********************************************************************  
				if (true != await QaComm.InitializeDevice(vm.SampleRateVal, vm.FftSizeVal, vm.WindowingMethod, attenuation))
					return false;

				// ********************************************************************
				// Do noise floor measurement
				// ********************************************************************
				var noisy = await MeasureNoise(ct.Token);
				if (ct.IsCancellationRequested)
					return false;
				page.NoiseFloor = new LeftRightPair();
				page.NoiseFloor.Right = QaCompute.CalculateNoise(noisy.FreqRslt, true);
				page.NoiseFloor.Left = QaCompute.CalculateNoise(noisy.FreqRslt, false);

				// ********************************************************************
				// Step through the list of frequencies
				// ********************************************************************
				for (int f = 0; f < stepBinFrequencies.Length; f++)
				{
					var freqy = stepBinFrequencies[f];
					await showMessage($"Measuring {freqy:0.#} Hz at {genVolt:G3} V.");
					await showProgress(100 * (f + 1) / stepBinFrequencies.Length);

					var wave = BuildWave(page, freqy);   // also update the waveform variables
					var lrfs = await QaComm.DoAcquireUser(vm.Averages, ct.Token, wave, wave, true);

					if (ct.IsCancellationRequested)
						break;

					page.TimeRslt = lrfs.TimeRslt ?? new();
					page.FreqRslt = lrfs.FreqRslt;

					uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
					if (page.TimeRslt.Left.Length == 0 || lrfs.FreqRslt == null || fundamentalBin >= lrfs.FreqRslt.Left.Length)               // Check in bin within range
						break;

					// Plot the mini graphs
					QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);
					QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, freqy, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);

					var work = CalculateColumn(page, freqy, ct.Token); // do the math for the columns
					if(work.Item1 != null && work.Item2 != null)
						AddColumn(page, freqy, work.Item1, work.Item2);

					MyVModel.LinkAbout(PageData.Definition); 
					RawToThdColumns(page);
					UpdateGraph(false);

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
			return !ct.IsCancellationRequested;
		}

		/// <summary>
		/// do post processing, just a bunch of easy math and moving stuff into viewmodels
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		private (ThdColumn?,ThdColumn?) CalculateColumn(MyDataTab msr, double dFreq, CancellationToken ct)
		{
			if (msr.FreqRslt == null)
			{
				return (null,null);
			}

			// left and right channels summary info to fill in
			var left = new ThdColumn();
			var right = new ThdColumn();
			ThdColumn[] steps = [left, right];
			ThdFreqViewModel vm = msr.ViewModel;

			var lrfs = msr.FreqRslt;    // frequency response

			var maxf = msr.FreqRslt.Df * msr.FreqRslt.Left.Length;
			//LeftRightPair snrdb = QaCompute.GetSnrDb(lrfs, dFreq, 20.0, maxf);
			LeftRightPair thds = QaCompute.GetThdDb(lrfs, dFreq, 20.0, Math.Min(20000, maxf));
			//LeftRightPair thdN = QaCompute.GetThdnDb(lrfs, dFreq, 20.0, maxf);

			var frq = msr.FreqRslt.Left;	// start with left
			foreach (var step in steps)
			{ 
				step.Freq = dFreq;
				step.Mag = QaMath.MagAtFreq(frq, msr.FreqRslt.Df, dFreq);
				step.D2 = (maxf > (2 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 2*dFreq) : 1e-10;
				step.D3 = (maxf > (3 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 3*dFreq) : step.D2;
				step.D4 = (maxf > (4 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 4*dFreq) : step.D3;
				step.D5 = (maxf > (5 * dFreq)) ? QaMath.MagAtFreq(frq, msr.FreqRslt.Df, 5*dFreq) : step.D4;
				step.D6P = 0;
				if(maxf > (6 * dFreq))
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
			left.THD = left.Mag * Math.Pow(10,thds.Left/20);	// in volts from dB relative to mag
			right.THD = right.Mag * Math.Pow(10, thds.Right / 20); ;
			left.Noise = Math.Max(1e-10, msr.NoiseFloor.Left);
			right.Noise = Math.Max(1e-10, msr.NoiseFloor.Right);

			return (left, right);
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
		/// Clear the plot
		/// </summary>
		void ClearPlot()
        {
            thdPlot.ThePlot.Clear();
            thdPlot.Refresh();
        }

		private double ToD(string stri)
		{
			return MathUtil.ToDouble(stri);
		}

		/// <summary>
		/// Ititialize the THD % plot
		/// </summary>
		void InitializeThdPlot(string plotFormat = "%")
        {
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			var thd = MyVModel;
			myPlot.Axes.SetLimits(Math.Log10(ToD(thd.GraphStartFreq)), Math.Log10(ToD(thd.GraphEndFreq)),
				Math.Log10(ToD(thd.RangeBottom)) - 0.00000001, Math.Log10(ToD(thd.RangeTop)));  // - 0.000001 to force showing label
			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			thdPlot.Refresh();
        }

        /// <summary>
        /// Initialize the magnitude plot
        /// </summary>
        void InitializeMagnitudePlot(string plotFormat = "dBV")
        {
			var thdFreq = MyVModel;
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			myPlot.Axes.SetLimitsX(Math.Log10(ToD(thdFreq.GraphStartFreq)),
				Math.Log10(ToD(thdFreq.GraphEndFreq)), myPlot.Axes.Bottom);

			myPlot.Axes.SetLimitsY(ToD(thdFreq.RangeBottomdB), ToD(thdFreq.RangeTopdB), myPlot.Axes.Left);

			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
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
			var thdFreq = MyVModel;
			var showLeft = isMain ? thdFreq.ShowLeft : thdFreq.ShowOtherLeft;
			var showRight = isMain ? thdFreq.ShowRight : thdFreq.ShowOtherRight;
			if (!showLeft && !showRight)
				return;

			float lineWidth = thdFreq.ShowThickLines ? 1.6f : 1;
            float markerSize = thdFreq.ShowPoints ? lineWidth + 3 : 1;

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
			List<ThdColumn[]> columns;
			ThdColumn[] leftCol = page.GetProperty("Left") as ThdColumn[] ?? [];
			ThdColumn[] rightCol = page.GetProperty("Right") as ThdColumn[] ?? [];
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
			// scaling by X.mag since it's all relative to the fundamental
			foreach (var col in columns)
            {
                var freq = col.Select(x => Math.Log10(x.Freq)).ToArray();
                if (thdFreq.ShowMagnitude)
                    AddPlot(freq, col.Select(x => FormVal(x.Mag, x.Mag)).ToList(), 9, "Mag" + suffix, LinePattern.DenselyDashed);
                if (thdFreq.ShowTHD)
                    AddPlot(freq, col.Select(x => FormVal(x.THD, x.Mag)).ToList(), 8, "THD" + suffix, lp);
                if (thdFreq.ShowD2)
                    AddPlot(freq, col.Select(x => FormVal(x.D2, x.Mag)).ToList(), 0, "D2" + suffix, lp);
                if (thdFreq.ShowD3)
                    AddPlot(freq, col.Select(x => FormVal(x.D3, x.Mag)).ToList(), 1, "D3" + suffix, lp);
                if (thdFreq.ShowD4)
                    AddPlot(freq, col.Select(x => FormVal(x.D4, x.Mag)).ToList(), 2, "D4" + suffix, lp);
                if (thdFreq.ShowD5)
                    AddPlot(freq, col.Select(x => FormVal(x.D5, x.Mag)).ToList(), 3, "D5" + suffix, lp);
                if (thdFreq.ShowD6)
                    AddPlot(freq, col.Select(x => FormVal(x.D6P, x.Mag)).ToList(), 3, "D6+" + suffix, lp);
                if (thdFreq.ShowNoiseFloor)
                    AddPlot(freq, col.Select(x => FormVal(x.Noise, x.Mag)).ToList(), 3, "Noise" + suffix, LinePattern.Dotted);
                suffix = "-R";          // second pass iff there are both channels
                lp = isMain ? LinePattern.DenselyDashed : LinePattern.Dotted;
            }

            thdPlot.Refresh();
        }

        public void UpdateGraph(bool settingsChanged)
        {
            thdPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
            int resultNr = 0;
            ThdFreqViewModel thd = MyVModel;

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