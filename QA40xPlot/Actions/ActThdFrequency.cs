using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;
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

		private static ThdFreqViewModel MyVModel { get => new(); }
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
			QaLibrary.InitMiniFftPlot(fftPlot, ToD(thd.StartFreq, 10),
				ToD(thd.EndFreq, 20000), -150, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			UpdateGraph(true);
		}

		// here param is the id of the tab to remove from the othertab list
		public void DeleteTab(int id)
		{
			OtherTabs.RemoveAll(item => item.Id == id);
			MyVModel.ForceGraphUpdate(); // force a graph update
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
			return new double[] { col.Freq, col.Mag, col.THD, col.Noise, col.D2, col.D3, col.D4, col.D5, col.D6P, col.GenVolts, col.THDN };
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
			col.THDN = rawData[startIdx + 10];
			return col;
		}

		private ThdColumn[] RawToColumn(double[] raw)
		{
			if (raw.Length == 0)
				return [];
			List<ThdColumn> left = new();
			for (int i = 0; i < raw.Length; i += ThdColumn.ThdColumnCount)
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

		private double AllMax(List<ThdColumn[]> steps, Func<ThdColumn, double> fn)
		{
			return steps.Max(y => y.Max(fn));
		}
		private double AllMin(List<ThdColumn[]> steps, Func<ThdColumn, double> fn)
		{
			return steps.Min(y => y.Min(fn));
		}

		public override Rect GetDataBounds()
		{
			Rect rrc = new Rect(0, 0, 0, 0);
			try
			{
				var vm = PageData.ViewModel;    // measurement settings cloned to not shift...
				var Xvalues = PageData.Sweep.X;
				if (vm == null || Xvalues.Length == 0)
					return Rect.Empty;

				var specVm = MyVModel;     // current settings

				List<ThdColumn[]> steps = new();
				if (vm.ShowLeft)
				{
					var a1 = PageData.GetProperty("Left");
					if (a1 != null)
						steps.Add((ThdColumn[])a1);
				}
				if (specVm.ShowRight)
				{
					var a1 = PageData.GetProperty("Right");
					if (a1 != null)
						steps.Add((ThdColumn[])a1);
				}
				var seen = DataUtil.FindShownInfo<ThdFreqViewModel, ThdColumn[]>(OtherTabs);
				foreach (var s in seen)
				{
					if (s != null)
						steps.Add(s);
				}

				if (steps.Count == 0)
					return Rect.Empty;

				rrc.X = Xvalues.Min();              // min X/Freq value
				rrc.Width = Xvalues.Max() - rrc.X;  // max frequency

				double maxY = 0;
				double minY = double.MaxValue;
				if (specVm.ShowMagnitude)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.Mag));
					minY = Math.Min(minY, AllMin(steps, x => x.Mag));
				}
				if (specVm.ShowTHD)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.THD));
					minY = Math.Min(minY, AllMin(steps, x => x.THD));
				}
				if (specVm.ShowTHDN)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.THDN));
					minY = Math.Min(minY, AllMin(steps, x => x.THDN));
				}
				if (specVm.ShowNoiseFloor)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.Noise));
					minY = Math.Min(minY, AllMin(steps, x => x.Noise));
				}
				if(specVm.ShowD2)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.D2));
					minY = Math.Min(minY, AllMin(steps, x => x.D2));
				}
				if (specVm.ShowD3)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.D3));
					minY = Math.Min(minY, AllMin(steps, x => x.D3));
				}
				if (specVm.ShowD4)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.D4));
					minY = Math.Min(minY, AllMin(steps, x => x.D4));
				}
				if (specVm.ShowD5)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.D5));
					minY = Math.Min(minY, AllMin(steps, x => x.D5));
				}
				if (specVm.ShowD6)
				{
					maxY = Math.Max(maxY, AllMax(steps, x => x.D6P));
					minY = Math.Min(minY, AllMin(steps, x => x.D6P));
				}

				rrc.Y = minY;      // min magnitude will be min value shown
				rrc.Height = maxY - rrc.Y;      // max voltage absolute
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
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
			if (myset.Count < 2 && OtherTabs.Count > 0)
			{
				foreach (var o in OtherTabs)
				{
					var all2 = LookupColumn(o, freq); // lookup the columns
					if (o.Definition.IsOnL)
						myset.Add(all2.Item1);
					if (myset.Count == 2)
						break;
					if (o.Definition.IsOnR)
						myset.Add(all2.Item2);
					if (myset.Count == 2)
						break;
				}
			}
			return myset.ToArray();
		}

		public void PinGraphRange(string who)
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			var vm = MyVModel;
			PinGraphRanges(myPlot, vm, who);
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<ThdFreqViewModel>(PageData, MyVModel, fileName);
		}

		public override async Task LoadFromFile(string fileName, bool doLoad)
		{
			var page = Util.LoadFile<ThdFreqViewModel>(PageData, fileName);
			if (page != null)
			{
				RawToThdColumns(page);
				await FinishLoad(page, doLoad, fileName);
			}
		}

		/// <summary>
		/// Load a file into a DataTab
		/// </summary>
		/// <param name="fileName">full path name</param>
		/// <returns>a datatab with no frequency info</returns>
		public MyDataTab? LoadFile(MyDataTab page, string fileName)
		{
			return Util.LoadFile<ThdFreqViewModel>(page, fileName);
		}

		/// <summary>
		/// given a datatab, integrate it into the gui as the current datatab
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public async Task FinishLoad(MyDataTab page, bool doLoad, string fileName)
		{
			ClipName(page.Definition, fileName);
			// now recalculate everything
			await PostProcess(page, ct.Token);
			if (doLoad)
			{
				// we can't overwrite the viewmodel since it links to the display proper
				// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				page.ViewModel.OtherSetList = MyVModel.OtherSetList;
				page.ViewModel.CopyPropertiesTo<ThdFreqViewModel>(MyVModel);    // retract the gui
				PageData = page;    // set the current page to the loaded one

				// relink to the new definition
				MyVModel.LinkAbout(page.Definition);
				MyVModel.HasSave = true;
			}
			else
			{
				OtherTabs.Add(page); // add the new one
									 //var oss = new OtherSet(page.Definition.Name, page.Show, page.Id);
				MyVModel.OtherSetList.Add(page.Definition);
			}

			UpdateGraph(true);
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
			MyVModel.CopyPropertiesTo(PageData.ViewModel);  // update the view model with latest settings
			PageData.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var page = PageData;    // alias
			page.Sweep = new();
			var vm = page.ViewModel;
			if (vm == null)
				return false;
			var genType = ToDirection(vm.GenDirection);

			// Init mini plots
			var startFreq = vm.NearestBinFreq(vm.StartFreq);
			var endFreq = vm.NearestBinFreq(vm.EndFreq);

			QaLibrary.InitMiniFftPlot(fftPlot, Math.Max(10, startFreq), endFreq, -150, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -1, 1);

			// ********************************************************************
			// Determine input level / attenuation
			// ********************************************************************
			await CalculateGainCurve(MyVModel);
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
					ToD(vm.StartFreq, 10), ToD(vm.EndFreq, 10000), vm.StepsOctave);
				var maxBinFreq = vm.SampleRateVal / 4;  // nyquist over 2 since we're looking at distortion
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
				var noisy = await MeasureNoise(MyVModel, ct.Token);
				page.NoiseFloor = noisy.Item1;
				page.NoiseFloorA = noisy.Item2;
				page.NoiseFloorC = noisy.Item3;
				if (ct.IsCancellationRequested)
					return false;

				WaveContainer.SetMono(); // enable generator
												// ********************************************************************
												// Step through the list of frequencies
												// ********************************************************************
				MyVModel.GeneratorVoltage = MathUtil.FormatVoltage(genVolt);
				for (int f = 0; f < stepBinFrequencies.Length; f++)
				{
					var freqy = stepBinFrequencies[f];
					await showMessage($"Measuring {freqy:0.#} Hz at {genVolt:G3} V.");
					await showProgress(100 * (f + 1) / stepBinFrequencies.Length);

					WaveGenerator.SetGen1(true, freqy, genVolt, true);             // send a sine wave
					WaveGenerator.SetGen2(true, 0, 0, false);            // send a sine wave
					LeftRightSeries lrfs;

					FrequencyHistory.Clear();
					for (int ik = 0; ik < (vm.Averages - 1); ik++)
					{
						lrfs = await QaComm.DoAcquisitions(1, ct.Token, true);
						if (lrfs == null || lrfs.TimeRslt == null || lrfs.FreqRslt == null)
							break;
						FrequencyHistory.Add(lrfs.FreqRslt);
					}
					// now FrequencyHistory has n-1 samples
					{
						lrfs = await QaComm.DoAcquisitions(1, ct.Token, true);
						if (lrfs == null || lrfs.TimeRslt == null || lrfs.FreqRslt == null)
							break;
						lrfs.FreqRslt = CalculateAverages(lrfs.FreqRslt, vm.Averages);
					}
					if (ct.IsCancellationRequested)
						break;

					page.TimeRslt = lrfs.TimeRslt ?? new();
					page.FreqRslt = lrfs.FreqRslt;

					int fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
					if (page.TimeRslt.Left.Length == 0 || lrfs.FreqRslt == null || fundamentalBin >= lrfs.FreqRslt.Left.Length)               // Check in bin within range
						break;

					// Plot the mini graphs
					QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);
					QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, freqy, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);

					var work = CalculateColumn(page, freqy, ct.Token); // do the math for the columns
					if (work.Item1 != null && work.Item2 != null)
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
		private (ThdColumn?, ThdColumn?) CalculateColumn(MyDataTab msr, double dFreq, CancellationToken ct)
		{
			if (msr.FreqRslt == null)
			{
				return (null, null);
			}

			// left and right channels summary info to fill in
			var left = new ThdColumn();
			var right = new ThdColumn();
			ThdColumn[] steps = [left, right];
			ThdFreqViewModel vm = msr.ViewModel;

			var lrfs = msr.FreqRslt;    // frequency response

			var maxf = msr.FreqRslt.Df * msr.FreqRslt.Left.Length;
			//LeftRightPair snrdb = QaCompute.GetSnrDb(lrfs, dFreq, 20.0, maxf);
			LeftRightPair thds = QaCompute.GetThdDb(vm.WindowingMethod, lrfs, dFreq, 20.0, maxf);
			LeftRightPair thdN = QaCompute.GetThdnDb(vm.WindowingMethod, lrfs, dFreq, 20.0, Math.Min(ViewSettings.NoiseBandwidth, maxf), ViewSettings.NoiseWeight);

			var frq = msr.FreqRslt.Left;    // start with left
			foreach (var step in steps)
			{
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
			left.THDN = left.Mag * Math.Pow(10, thdN.Left / 20); // in volts from dB relative to mag
			right.THDN = right.Mag * Math.Pow(10, thdN.Right / 20);
			var floor = msr.NoiseFloor;
			switch (ViewSettings.NoiseWeight)
			{
				case "A":
					floor = msr.NoiseFloorA;
					break;
				case "C":
					floor = msr.NoiseFloorC;
					break;
				default:
					break;
			}
			left.Noise = Math.Max(1e-10, floor.Left);
			right.Noise = Math.Max(1e-10, floor.Right);

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

		/// <summary>
		/// Ititialize the THD % plot
		/// </summary>
		void InitializeThdPlot(string plotFormat = "%")
		{
			ScottPlot.Plot myPlot = thdPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			var thd = MyVModel;
			myPlot.Axes.SetLimits(Math.Log10(ToD(thd.GraphStartX, 20)), Math.Log10(ToD(thd.GraphEndX, 20000)),
				Math.Log10(ToD(thd.RangeBottom, -100)) - 0.00000001, Math.Log10(ToD(thd.RangeTop, -10)));  // - 0.000001 to force showing label
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

			myPlot.Axes.SetLimitsX(Math.Log10(ToD(thdFreq.GraphStartX, 20)),
				Math.Log10(ToD(thdFreq.GraphEndX, 20000)), myPlot.Axes.Bottom);

			myPlot.Axes.SetLimitsY(ToD(thdFreq.RangeBottomdB), ToD(thdFreq.RangeTopdB), myPlot.Axes.Left);

			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			thdPlot.Refresh();
		}

		// this always uses the 'global' format so others work too
		private double FormVal(double d1, double dMax)
		{
			var x = GraphUtil.ValueToPlot(MyVModel.PlotFormat, d1, dMax);
			return GraphUtil.IsPlotFormatLog(MyVModel.PlotFormat) ? x : Math.Log10(x);
		}

		/// <summary>
		/// Plot the  THD magnitude (dB) data
		/// </summary>
		/// <param name="data">The data to plot</param>
		private void PlotValues(MyDataTab page, int measurementNr, bool isMain)
		{
			var thdFreq = MyVModel;
			bool showLeft;
			bool showRight;
			if (isMain)
			{
				showLeft = thdFreq.ShowLeft; // dynamically update these
				showRight = thdFreq.ShowRight;
			}
			else
			{
				showLeft = page.Definition.IsOnL; // dynamically update these
				showRight = page.Definition.IsOnR;
			}

			if (!showLeft && !showRight)
				return;

			float lineWidth = thdFreq.ShowThickLines ? _Thickness : 1;
			float markerSize = thdFreq.ShowPoints ? lineWidth + 3 : 1;

			// here Y values are in dBV
			void AddPlot(double[] xValues, List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
			{
				var u = measurementNr;
				if (yValues.Count == 0) return;
				var plot = thdPlot.ThePlot.Add.SignalXY(xValues, yValues.ToArray());
				plot.LineWidth = lineWidth;
				plot.Color = GraphUtil.GetPaletteColor("Transparent", colorIndex);
				plot.MarkerSize = markerSize;
				plot.LegendText = legendText;
				plot.LinePattern = linePattern;
				MyVModel.LegendInfo.Add(new MarkerItem(linePattern, plot.Color, legendText, colorIndex, plot, thdPlot));
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
				suffix = ".L";

			// copy the vector of columns into vectors of values
			// scaling by X.mag since it's all relative to the fundamental
			foreach (var col in columns)
			{
				var freq = col.Select(x => Math.Log10(x.Freq)).ToArray();
				if (thdFreq.ShowMagnitude)
					AddPlot(freq, col.Select(x => FormVal(x.Mag, x.Mag)).ToList(), 1, "Mag" + suffix, LinePattern.DenselyDashed);
				if (thdFreq.ShowTHDN)
					AddPlot(freq, col.Select(x => FormVal(x.THDN, x.Mag)).ToList(), 2, "THDN" + suffix, lp);
				if (thdFreq.ShowTHD)
					AddPlot(freq, col.Select(x => FormVal(x.THD, x.Mag)).ToList(), 3, "THD" + suffix, lp);
				if (thdFreq.ShowD2)
					AddPlot(freq, col.Select(x => FormVal(x.D2, x.Mag)).ToList(), 4, "D2" + suffix, lp);
				if (thdFreq.ShowD3)
					AddPlot(freq, col.Select(x => FormVal(x.D3, x.Mag)).ToList(), 5, "D3" + suffix, lp);
				if (thdFreq.ShowD4)
					AddPlot(freq, col.Select(x => FormVal(x.D4, x.Mag)).ToList(), 6, "D4" + suffix, lp);
				if (thdFreq.ShowD5)
					AddPlot(freq, col.Select(x => FormVal(x.D5, x.Mag)).ToList(), 7, "D5" + suffix, lp);
				if (thdFreq.ShowD6)
					AddPlot(freq, col.Select(x => FormVal(x.D6P, x.Mag)).ToList(), 8, "D6+" + suffix, lp);
				if (thdFreq.ShowNoiseFloor)
					AddPlot(freq, col.Select(x => FormVal(x.Noise, x.Mag)).ToList(), 9, "Noise" + suffix, LinePattern.Dotted);
				suffix = ".R";          // second pass iff there are both channels
				lp = isMain ? LinePattern.DenselyDashed : LinePattern.Dotted;
			}

			thdPlot.Refresh();
		}

		public void UpdateGraph(bool settingsChanged)
		{
			thdPlot.ThePlot.Remove<SignalXY>();             // Remove all current lines
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
			thd.LegendInfo.Clear();
			PlotValues(PageData, resultNr++, true);
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null && other.Show != 0)
						PlotValues(other, resultNr, false);
					resultNr++;
				}
			}
		}


	}
}