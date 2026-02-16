using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Diagnostics;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;


// various things for frequency sweep
// this is designed for the qa430 for now
// it does thd vs frequency but can
// additionally sweep QA430 parameters like load and supply voltage
// to mimic opamp datasheets

namespace QA40xPlot.Actions
{
	using MyDataTab = DataTab<FreqSweepViewModel>;

	public class ActFreqSweep : ActOpamp<FreqSweepViewModel>
	{
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document

		private List<SweepLine>? FrequencyLines(MyDataTab page)
		{
			return (List<SweepLine>?)page.GetProperty("Left");
		}

		private List<SweepLine>? FrequencyLinesRight(MyDataTab page)
		{
			return (List<SweepLine>?)page.GetProperty("Right");
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public ActFreqSweep(FreqSweepViewModel vm)
		{
			// Show empty graphs
			QaLibrary.InitMiniTimePlot(vm.MiniPlot, 0, 4, -1, 1);
			QaLibrary.InitMiniFftPlot(vm.Mini2Plot, ToD(vm.StartFreq, 10),
				ToD(vm.EndFreq, 20000), -150, 20);
			vm.ShowMiniPlots = false;
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
			CanToken.Cancel();
		}

		public void UpdatePlotTitle()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			var title = "Distortion vs Frequency";
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
			else
				myPlot.Title(title);
		}

		// convert the raw data into columns of data
		private void RawToFreqSweepColumns(MyDataTab page)
		{
			var u = RawToColumns(page.SweepSteps.Steps, page.Sweep.RawLeft, page.ViewModel.SweepColumnCount, true);
			page.SetProperty("Left", u);
			var v = RawToColumns(page.SweepSteps.Steps, page.Sweep.RawRight, page.ViewModel.SweepColumnCount, true);
			page.SetProperty("Right", v);
		}

		/// <summary>
		/// add a column to the data table columns
		/// </summary>
		/// <param name="x"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		private void AddColumn(MyDataTab page, double x, SweepColumn left, SweepColumn right)
		{
			page.Sweep.X = page.Sweep.X.Append(x).ToArray();
			page.Sweep.RawLeft = page.Sweep.RawLeft.Concat(ColumnToArray(left)).ToArray();
			page.Sweep.RawRight = page.Sweep.RawRight.Concat(ColumnToArray(right)).ToArray();
		}

		public override Rect GetDataBounds()
		{
			Rect rrc = new Rect(0, 0, 0, 0);
			try
			{
				var vm = MyVModel;     // current settings

				List<SweepColumn> steps = new();
				if (vm.ShowLeft)
				{
					var a1 = FrequencyLines(PageData);
					if (a1 != null)
						foreach (var x in a1)
						{
							steps.AddRange(x.Columns);
						}
				}
				if (vm.ShowRight)
				{
					var a1 = FrequencyLinesRight(PageData);
					if (a1 != null)
						foreach (var x in a1)
						{
							steps.AddRange(x.Columns);
						}
				}
				var seen = DataUtil.FindShownInfo<FreqSweepViewModel, List<SweepLine>>(OtherTabs);
				foreach (var s in seen)
				{
					if (s != null)
						foreach (var x in s)
							steps.AddRange(x.Columns);
				}

				if (steps.Count == 0)
					return Rect.Empty;

				var arsteps = steps.ToArray();

				rrc.X = arsteps.Min(x => x.Freq);              // min X/Freq value
				rrc.Width = arsteps.Max(x => x.Freq) - rrc.X;  // max frequency

				double maxY = -1e10;
				double minY = 1e10;
				if (vm.ShowMagnitude)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.Mag));
					minY = Math.Min(minY, arsteps.Min(x => x.Mag));
				}
				if (vm.ShowTHD)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.THD));
					minY = Math.Min(minY, arsteps.Min(x => x.THD));
				}
				if (vm.ShowTHDN)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.THDN));
					minY = Math.Min(minY, arsteps.Min(x => x.THDN));
				}
				if (vm.ShowNoise)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.Noise));
					minY = Math.Min(minY, arsteps.Min(x => x.Noise));
				}
				if (vm.ShowNoiseFloor)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.NoiseFloor));
					minY = Math.Min(minY, arsteps.Min(x => x.NoiseFloor));
				}
				if (vm.ShowD2)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.D2));
					minY = Math.Min(minY, arsteps.Min(x => x.D2));
				}
				if (vm.ShowD3)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.D3));
					minY = Math.Min(minY, arsteps.Min(x => x.D3));
				}
				if (vm.ShowD4)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.D4));
					minY = Math.Min(minY, arsteps.Min(x => x.D4));
				}
				if (vm.ShowD5)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.D5));
					minY = Math.Min(minY, arsteps.Min(x => x.D5));
				}
				if (vm.ShowD6)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.D6P));
					minY = Math.Min(minY, arsteps.Min(x => x.D6P));
				}
				rrc.Y = minY;      // min magnitude will be min value shown
				rrc.Height = maxY - minY;      // max voltage absolute
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return rrc;
		}

		private List<SweepDot> LookupColumn(MyDataTab page, double freq)
		{
			var allLines = FrequencyLines(page);
			List<SweepDot> dots = new();
			if (allLines == null || allLines.Count == 0)
			{
				return dots;
			}
			// find nearest amplitude (both left and right will be identical here if scanned)
			var bin = allLines[0].Columns.Count(x => x.Freq <= freq) - 1;    // find first freq less than me
			if (bin == -1)
				bin = 0;
			var matchFreq = allLines[0].Columns[bin].Freq;  // the actual frequency we want
			var allMatch = allLines.Where(x => x.Columns.Length > bin && Math.Abs(x.Columns[bin].Freq - matchFreq) < 0.001).ToArray();
			foreach (var line in allMatch)
			{
				// last line isn't long enough yet
				if (line.Columns.Length < bin)
					break;
				var dot = new SweepDot();
				dot.Label = line.Label;
				dot.Column = line.Columns[bin];
				dots.Add(dot);
			}
			return dots;
		}

		// for a given frequency, lookup the left and right columns
		// this is used by the cursor display
		public SweepDot[] LookupX(double freq)
		{
			var vm = MyVModel;
			var allLines = LookupColumn(PageData, freq); // lookup the columns
			for(int i=0; i<allLines.Count; i++)
			{
				var u = allLines[i];
				u.Label = ClipName(PageData.Definition.Name);
			}
			if (OtherTabs.Count > 0)
			{
				int tabCnt = 1;
				foreach (var o in OtherTabs)
				{
					if (!o.Definition.IsOnL)
						continue;
					var all2 = LookupColumn(o, freq); // lookup the columns
					var tabChr = tabCnt + ".";
					foreach (var ail in all2)
					{
						ail.Label = o.Definition.Name + ail.Label;
					}
					allLines.AddRange(all2);
					tabCnt++;
				}
			}
			return allLines.ToArray();
		}

		public override void PinGraphRange(string who)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			PinGraphRanges(myPlot, vm, who);
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<FreqSweepViewModel>(PageData, MyVModel, fileName);
		}

		public override async Task LoadFromFile(string fileName, bool doLoad)
		{
			var page = Util.LoadFile<FreqSweepViewModel>(PageData, fileName);
			if (page != null)
			{
				RawToFreqSweepColumns(page);
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
			return Util.LoadFile<FreqSweepViewModel>(page, fileName);
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
			await PostProcess(page, CanToken.Token);
			if (doLoad)
			{
				// we can't overwrite the viewmodel since it links to the display proper
				// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				MyVModel.LoadViewFrom(page.ViewModel);
				PageData = page;    // set the current page to the loaded one

				// relink to the new definition
				var vm = MyVModel;
				vm.LinkAbout(page.Definition);
				vm.HasSave = true;
				vm.ShowMiniPlots = false; // hide mini plots on load
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

			// rotate through any variables that change for each test
			try
			{
				CanToken = new();
				await RunAcquisition();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			PageData.TimeRslt = new();
			PageData.FreqRslt = null;

			// Show message
			await showMessage(CanToken.IsCancellationRequested ? "Measurement cancelled!" : "Measurement finished!");

			await EndAction(freqVm);

			await showMessage("Finished");
		}

		private (int, double) CalculateAttenuation(double voltage, BaseViewModel bvm,
			int[] frqtest, LeftRightFrequencySeries LRGains)
		{
			// to figure out attenuation use the first gen voltage
			var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
			var genVolt = bvm.ToGenVoltage(voltage, frqtest, GEN_INPUT, gains);   // input voltage for request
																				  // ********************************************************************
																				  // Determine input level for attenuation
																				  // we know that all settings of the QA430 have defined gain so that
																				  // we can calculate the required attenuation up front and then vary the driving voltage
																				  // to keep constant output voltage no matter configuration
																				  // ********************************************************************
			var genOutL = ToGenOutVolts(genVolt, frqtest, LRGains.Left);   // output voltage for request left channel
			var genOutR = ToGenOutVolts(genVolt, frqtest, LRGains.Right);   // output voltage for request right channel
			var genOut = Math.Max(genOutL, genOutR);    // use both channels max value to get attenuation value
			double amplifierOutputVoltagedBV = QaLibrary.ConvertVoltage(genOut, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			// Get input voltage based on desired output voltage
			var attenuation = QaLibrary.DetermineAttenuation(amplifierOutputVoltagedBV);
			if (!bvm.DoAutoAttn)
			{
				attenuation = (int)bvm.Attenuation;
			}
			return (attenuation, genVolt);
		}

		public async Task<bool> RunAcquisition()
		{
			var freqVm = MyVModel;          // the active viewmodel
			LeftRightTimeSeries lrts = new();
			PageData.ViewModel.LoadViewFrom(MyVModel); // make sure the page data viewmodel is up to date
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

			QaLibrary.InitMiniFftPlot(vm.Mini2Plot, Math.Max(10, startFreq), endFreq, -150, 20);
			QaLibrary.InitMiniTimePlot(vm.MiniPlot, 0, 4, -1, 1);

			// ********************************************************************
			// Determine input level / attenuation
			// ********************************************************************
			// before getting the gain curve set to gain of 1
			// and then chirp
			if (vm.HasQA430)
			{
				var model = Qa430Usb.Singleton?.QAModel;
				if (model != null)
				{
					await model.PrepareDefault();
				}
			}

			await CalculateGainCurve(vm, startFreq, endFreq);
			if (LRGains == null)
				return false;

			int[] frqtest = [LRGains.ToBinNumber(startFreq), LRGains.ToBinNumber(endFreq)];
			double[] stepBinFrequencies = [];
			double binSize = 0;
			// here they are in the format of the generator test...
			var voltValues = SelectItemList.ParseList(vm.VoltSummary).Where(x => x.IsSelected).ToList();
			if (voltValues.Count == 0)
			{
				await showMessage("No generator voltages specified!", 200);
				return false;
			}
			// ********************************************************************
			// Calculate frequency steps and init device
			// ********************************************************************
			try
			{
				if (true != await QaComm.InitializeDevice(vm.SampleRateVal, vm.FftSizeVal, vm.WindowingMethod, (int)vm.Attenuation))
					return false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}

			// Check if cancel button pressed
			if (CanToken.IsCancellationRequested)
				return false;

			// Calculate frequency steps to do
			binSize = QaLibrary.CalcBinSize(vm.SampleRateVal, vm.FftSizeVal);
			var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(
				ToD(vm.StartFreq, 10), ToD(vm.EndFreq, 10000), vm.StepsOctave);
			var maxBinFreq = vm.SampleRateVal / 4;  // nyquist over 2 since we're looking at distortion
													// Translate the generated list to bin center frequencies
			stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, vm.SampleRateVal, vm.FftSizeVal);
			stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x < maxBinFreq)                // Filter out values that are out of range 
				.GroupBy(x => x)                                                                    // Filter out duplicates
				.Select(y => y.First())
				.ToArray();

			bool longRest = true;

			// now do the measurement stuff
			try
			{
				// get all qa430 variable combinations
				var variables = OpampViewModel.EnumerateVariables(freqVm, null);

				// now expand the generator voltage options
				{
					var vnew = new List<AcquireStep>();
					foreach (var v in voltValues)
					{
						var vnewlist = variables.Select(x =>
						{
							var ux = new AcquireStep(x);
							var ampD = GenVoltApplyUnit(v.Name, vm.GenVoltageUnit, 1e-9);
							ux.GenVolt = ampD;
							ux.GenXFmt = (vm.IsGenPower ? MathUtil.FormatPower(ampD) : MathUtil.FormatVoltage(ampD));
							return ux;
						}).ToList();
						vnew.AddRange(vnewlist);
					}
					// now vnew is the full list of steps with voltages swept
					variables = vnew;
				}

				// now variables is the list of sweep steps we will execute

				string lastCfg = string.Empty;
				page.SweepSteps.Steps = variables.ToArray();

				// ********************************************************************
				// loop through the variables being changed
				// they are config name, load, gain, supply voltage
				// ********************************************************************
				foreach (var myConfig in variables) // sweep the different configurations
				{
					lastCfg = await OpampViewModel.ExecuteModel(myConfig, lastCfg, vm.HasQA430); // update the qa430 if needed
																		// sweeping generator voltage as well
					var attenset = CalculateAttenuation(myConfig.GenVolt, vm, frqtest, LRGains);
					var attenuation = attenset.Item1;
					var genVolt = attenset.Item2;

					bool newAtten = (attenuation != MyVModel.Attenuation);
					page.Definition.GeneratorVoltage = genVolt;

					// Set the new input range
					if (attenuation != QaComm.GetInputRange())
					{
						await QaComm.SetInputRange(attenuation);
					}
					MyVModel.Attenuation = attenuation;   // visible display
					vm.Attenuation = attenuation;         // my setting

					// ********************************************************************
					// Do noise floor measurement
					// ********************************************************************
					// do this with the specified opamp configuration
					// wait for the noise to stabilize
					//if (newAtten || myConfig.Equals(variables[0]))
					if (longRest)
					{
						// so noise stabilizes which takes quite a while at first
						var nstart = DateTime.Now;
						while ((DateTime.Now - nstart).TotalMilliseconds < 3200)
						{
							await MeasureNoise(MyVModel, CanToken.Token);
						}
						longRest = false;
					}
					{
						//wait up to another 10 cycles for noise to stabilize
						double lastndbv = 1000;
						int iii = 0;
						int iij = 0;
						while (iii < (longRest ? 10 : 2))
						{
							var noisy = await MeasureNoise(MyVModel, CanToken.Token);
							page.NoiseFloor = noisy.Item1;
							var ndbv = 20 * Math.Log10(page.NoiseFloor.Left);
							Debug.WriteLine($"at {iii} left Noise is {page.NoiseFloor.Left}V or {ndbv}dBV");
							iij = (Math.Abs((lastndbv - ndbv) / ndbv) < .005) ? iij + 1 : 0;
							if (iij > (newAtten ? 2 : 1))
								break;
							iii++;
							lastndbv = ndbv;
						}
					}

					if (CanToken.IsCancellationRequested)
						return false;
					// get the entire noise response (maybe scaled)
					var noiseRslt = await MeasureNoiseFreq(MyVModel, 4, CanToken.Token);    // get noise averaged 4 times

					// enable generator
					WaveContainer.SetMono();
					// ********************************************************************
					// Step through the list of frequencies
					// ********************************************************************
					var genScaleVolt = genVolt / Math.Abs(myConfig.Gain);   // input voltage for request
					page.Definition.GeneratorVoltage = genScaleVolt;   // set the generator voltage in the definition
					MyVModel.GeneratorVoltage = vm.GetGenVoltLine(genScaleVolt);
					for (int f = 0; f < stepBinFrequencies.Length; f++)
					{
						var freqy = stepBinFrequencies[f];
						var voltf = vm.GetGenVoltLine(genScaleVolt);
						await showMessage($"Measuring {freqy:0.#} Hz at {voltf}.");
						await showProgress(100 * (f + 1) / stepBinFrequencies.Length);

						WaveGenerator.SetGen1(true, freqy, genScaleVolt, true);             // send a sine wave
						WaveGenerator.SetGen2(true, 0, 0, false);            // send a sine wave
						LeftRightSeries lrs;

						FrequencyHistory.Clear();
						for (int ik = 0; ik < (vm.Averages - 1); ik++)
						{
							lrs = await QaComm.DoAcquisitions(1, CanToken.Token, true);
							if (lrs == null || lrs.TimeRslt == null || lrs.FreqRslt == null)
								break;
							FrequencyHistory.Add(lrs.FreqRslt);
						}
						// now FrequencyHistory has n-1 samples
						{
							lrs = await QaComm.DoAcquisitions(1, CanToken.Token, true);
							if (lrs == null || lrs.TimeRslt == null || lrs.FreqRslt == null)
								break;
							lrs.FreqRslt = CalculateAverages(lrs.FreqRslt, vm.Averages);
						}
						if (CanToken.IsCancellationRequested)
							break;

						page.TimeRslt = lrs.TimeRslt ?? new();
						page.FreqRslt = lrs.FreqRslt;

						int fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
						if (page.TimeRslt.Left.Length == 0 || lrs.FreqRslt == null || fundamentalBin >= lrs.FreqRslt.Left.Length)               // Check in bin within range
							break;

						// Plot the mini graphs
						QaLibrary.PlotMiniFftGraph(vm.Mini2Plot, lrs.FreqRslt, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);
						QaLibrary.PlotMiniTimeGraph(vm.MiniPlot, lrs.TimeRslt, freqy, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);

						// even with multiple configurations
						// this will keep stacking up stuff while frequency array shows min...max,min...max,...
						var flr = GetNoiseFloor(page);
						var work = CalculateColumn(page.FreqRslt, freqVm, flr, freqy, CanToken.Token, myConfig, noiseRslt); // do the math for the columns
						if (work.Item1 != null && work.Item2 != null)
						{
							work.Item1.GenVolts = genVolt;
							work.Item2.GenVolts = genVolt;
							if (vm.DeembedDistortion)
							{
								work.Item1 = DeembedColumns(work.Item1, work.Item2, myConfig.Distgain);
							}
							AddColumn(page, freqy, work.Item1, work.Item2);
						}

						MyVModel.LinkAbout(PageData.Definition);
						// this just needs to extract all the pieces
						RawToFreqSweepColumns(page);
						UpdateGraph(false);

						// Check if cancel button pressed
						if (CanToken.IsCancellationRequested)
						{
							break;
						}
					} // end for loop through frequencies
				} // end for loop through variable sweeps
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return !CanToken.IsCancellationRequested;
		}

		public static LeftRightPair GetNoiseFloor(MyDataTab msr)
		{
			LeftRightPair floor = msr.NoiseFloor;
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
			return floor;
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
			var vm = MyVModel;
			vm.MainPlot.ThePlot.Clear();
			vm.MainPlot.Refresh();
		}

		/// <summary>
		/// Ititialize the THD % plot
		/// </summary>
		void InitializeThdPlot(string plotFormat = "%")
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			myPlot.Axes.SetLimits(Math.Log10(ToD(vm.GraphStartX, 20)), Math.Log10(ToD(vm.GraphEndX, 20000)),
				Math.Log10(ToD(vm.RangeBottom, -100)) - 0.00000001, Math.Log10(ToD(vm.RangeTop, -10)));  // - 0.000001 to force showing label
			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			vm.MainPlot.Refresh();
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot(string plotFormat = "dBV")
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			myPlot.Axes.SetLimitsX(Math.Log10(ToD(vm.GraphStartX, 20)),
				Math.Log10(ToD(vm.GraphEndX, 20000)), myPlot.Axes.Bottom);

			myPlot.Axes.SetLimitsY(ToD(vm.RangeBottomdB), ToD(vm.RangeTopdB), myPlot.Axes.Left);

			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			myPlot.HideLegend();
			vm.MainPlot.Refresh();
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
			var vm = MyVModel;
			bool showLeft;
			bool showRight;
			if (isMain)
			{
				showLeft = vm.ShowLeft; // dynamically update these
				showRight = vm.ShowRight;
			}
			else
			{
				showLeft = page.Definition.IsOnL; // dynamically update these
				showRight = page.Definition.IsOnR;
			}

			if (!showLeft && !showRight)
				return;

			float lineWidth = vm.ShowThickLines ? ViewSettings.Thickness : 1;
			float markerSize = vm.ShowPoints ? lineWidth + 3 : 1;

			// here Y values are in dBV
			void AddPlot(double[] xValues, List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
			{
				var u = measurementNr;
				if (yValues.Count == 0) return;
				var plot = vm.MainPlot.ThePlot.Add.SignalXY(xValues, yValues.ToArray());
				plot.LineWidth = lineWidth;
				plot.Color = GraphUtil.GetPaletteColor("Transparent", colorIndex);
				plot.MarkerSize = markerSize;
				plot.LegendText = legendText;
				plot.LinePattern = linePattern;
				plot.IsVisible = !MyVModel.HiddenLines.Contains(legendText);
				MyVModel.LegendInfo.Add(new MarkerItem(linePattern, plot.Color, legendText, colorIndex, plot, vm.MainPlot, plot.IsVisible));
			}

			// which columns are we displaying? left, right or both
			List<SweepLine>[] lineGroup;
			List<SweepLine>? leftCol = FrequencyLines(page);
			List<SweepLine>? rightCol = FrequencyLinesRight(page);
			var leftTop = PlotZLeft;
			if (showLeft && showRight && leftCol != null && rightCol != null)
			{
				lineGroup = leftTop ? [rightCol, leftCol] : [leftCol, rightCol];
			}
			else if (showLeft && leftCol != null)
			{
				lineGroup = [leftCol];
			}
			else
			{
				lineGroup = (rightCol != null) ? [rightCol] : [];
			}

			string tosuffix = MyVModel.HasQA430 ? "." : "";
			string suffix = string.Empty;
			LinePattern[] patternList = [LinePattern.Solid, LinePattern.Dashed, LinePattern.DenselyDashed, LinePattern.Dotted];
			var solidFirst = lineGroup.Length > 0 && ReferenceEquals(lineGroup[0], leftCol);
			var lp = patternList[ (isMain && solidFirst) ? 0 : (isMain ? 1 : (solidFirst ? 2 : 3))];
			if (showRight && showLeft)
				suffix = (leftTop ? ".R" : ".L") + tosuffix;
			else
				suffix = tosuffix;

			// for each list of lines (left and right)
			var prefix = isMain ? string.Empty : (ClipName(page.Definition.Name) + "."); 
			foreach (var lineList in lineGroup)
			{
				var colorNum = (measurementNr > 1) ? (measurementNr - 1) * 5 : 0;
				foreach (var line in lineList)
				{
					var subsuffix = suffix + line.Label;
					var colArray = line.Columns;
					var freq = colArray.Select(x => Math.Log10(x.Freq)).ToArray();
					if (vm.ShowMagnitude)
						AddPlot(freq, colArray.Select(x => FormVal(x.Mag, x.Mag)).ToList(), colorNum, prefix + "Mag" + subsuffix, lp);
					colorNum++;
					if (vm.ShowTHDN)
						AddPlot(freq, colArray.Select(x => FormVal(x.THDN, x.Mag)).ToList(), colorNum, prefix + "THDN" + subsuffix, lp);
					colorNum++;
					if (vm.ShowTHD)
						AddPlot(freq, colArray.Select(x => FormVal(x.THD, x.Mag)).ToList(), colorNum, prefix + "THD" + subsuffix, lp);
					colorNum++;
					if (vm.ShowNoise)
						AddPlot(freq, colArray.Select(x => FormVal(x.Noise, x.Mag)).ToList(), colorNum, prefix + "Noise" + subsuffix, lp);
					colorNum++;
					if (vm.ShowNoiseFloor)
						AddPlot(freq, colArray.Select(x => FormVal(x.NoiseFloor, x.Mag)).ToList(), colorNum, prefix + "Floor" + subsuffix, lp);
					colorNum++;
					if (vm.ShowD2)
						AddPlot(freq, colArray.Select(x => FormVal(x.D2, x.Mag)).ToList(), colorNum, prefix + "D2" + subsuffix, lp);
					colorNum++;
					if (vm.ShowD3)
						AddPlot(freq, colArray.Select(x => FormVal(x.D3, x.Mag)).ToList(), colorNum, prefix + "D3" + subsuffix, lp);
					colorNum++;
					if (vm.ShowD4)
						AddPlot(freq, colArray.Select(x => FormVal(x.D4, x.Mag)).ToList(), colorNum, prefix + "D4" + subsuffix, lp);
					colorNum++;
					if (vm.ShowD5)
						AddPlot(freq, colArray.Select(x => FormVal(x.D5, x.Mag)).ToList(), colorNum, prefix + "D5" + subsuffix, lp);
					colorNum++;
					if (vm.ShowD6)
						AddPlot(freq, colArray.Select(x => FormVal(x.D6P, x.Mag)).ToList(), colorNum, prefix + "D6+" + subsuffix, lp);
				}
				suffix = (leftTop ? ".L" : ".R") + tosuffix;          // second pass iff there are both channels
				lp = patternList[(isMain && solidFirst) ? 1 : (isMain ? 0 : (solidFirst ? 3 : 2))]; ;
			}
			vm.MainPlot.Refresh();
		}

		void HandleChangedProperty(ScottPlot.Plot myPlot, FreqSweepViewModel vm, string changedProp)
		{
			var ismag = GraphUtil.IsPlotFormatLog(vm.PlotFormat);
			if (changedProp == "GraphStartX" || changedProp == "GraphEndX" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsX(Math.Log10(ToD(vm.GraphStartX, 20)), Math.Log10(ToD(vm.GraphEndX, 20000)), myPlot.Axes.Bottom);
			if (ismag)
			{
				if (changedProp == "RangeBottomdB" || changedProp == "RangeTopdB" || changedProp.Length == 0)
					myPlot.Axes.SetLimitsY(ToD(vm.RangeBottomdB, -100), ToD(vm.RangeTopdB, 0), myPlot.Axes.Left);
			}
			else
			{
				if (changedProp == "RangeBottom" || changedProp == "RangeTop" || changedProp.Length == 0)
					myPlot.Axes.SetLimitsY(Math.Log10(ToD(vm.RangeBottom, 1e-6)) - 0.00000001, Math.Log10(ToD(vm.RangeTop, 1)), myPlot.Axes.Left);  // - 0.000001 to force showing label
			}
		}


		public void UpdateGraph(bool settingsChanged, string theProperty = "")
		{
			FreqSweepViewModel vm = MyVModel;
			vm.MainPlot.ThePlot.Remove<SignalXY>();             // Remove all current lines
			int resultNr = 0;

			if (settingsChanged)
			{
				PlotUtil.SetupMenus(vm.MainPlot.ThePlot, this, vm);
				if (GraphUtil.IsPlotFormatLog(vm.PlotFormat))
				{
					InitializeMagnitudePlot(vm.PlotFormat);
				}
				else
				{
					InitializeThdPlot(vm.PlotFormat);
				}
				HandleChangedProperty(vm.MainPlot.ThePlot, vm, "");
				PlotUtil.SetHeadingColor(vm.MainPlot.MyLabel);
			}
			else if (theProperty.Length > 0)
			{
				HandleChangedProperty(vm.MainPlot.ThePlot, vm, theProperty);
			}
			vm.LegendInfo.Clear();
			var mainTop = PlotZMain;
			var rnr = resultNr++;
			if (!mainTop)
			{
				PlotValues(PageData, rnr, true);
			}
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null && other.Show != 0)
						PlotValues(other, resultNr++, false);
				}
			}
			if (mainTop)
			{
				PlotValues(PageData, rnr, true);
			}
		}
	}
}