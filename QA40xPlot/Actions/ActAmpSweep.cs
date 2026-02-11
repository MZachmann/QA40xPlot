using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Extensions;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Net.Http;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

namespace QA40xPlot.Actions
{

	using MyDataTab = DataTab<AmpSweepViewModel>;

	public partial class ActAmpSweep : ActOpamp<AmpSweepViewModel>
	{
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document

		private float _Thickness = 2.0f;

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
		public ActAmpSweep(AmpSweepViewModel vm)
		{
			// Show empty graphs
			QaLibrary.InitMiniTimePlot(vm.MiniPlot, 0, 4, -1, 1);
			QaLibrary.InitMiniFftPlot(vm.Mini2Plot, 10, 100000, -150, 20);

			// TODO: depends on graph settings which graph is shown
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
			var title = SetPlotLabels();
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
			else
				myPlot.Title(title);
		}

		// convert the raw data into columns of data
		private void RawToAmpSweepColumns(MyDataTab page)
		{
			var u = RawToColumns(page.SweepSteps.Steps, page.Sweep.RawLeft, page.ViewModel.SweepColumnCount, false);
			page.SetProperty("Left", u);
			var v = RawToColumns(page.SweepSteps.Steps, page.Sweep.RawRight, page.ViewModel.SweepColumnCount, false);
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

		private double ConvertToInputVoltage(double outV, double[] gains)
		{
			return outV * gains[0];
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
				var seen = DataUtil.FindShownInfo<AmpSweepViewModel, List<SweepLine>>(OtherTabs);
				foreach (var s in seen)
				{
					if (s != null)
						foreach (var x in s)
							steps.AddRange(x.Columns);
				}

				if (steps.Count == 0)
					return Rect.Empty;

				var arsteps = steps.ToArray();
				rrc.X = arsteps.Min(x => x.GenVolts);              // min X/Freq value
				rrc.Width = arsteps.Max(x => x.GenVolts) - rrc.X;  // max frequency

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


		/// <summary>
		/// find a column by X coordinate where X is amplitude of generator input voltage 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="x">genV value</param>
		/// <returns></returns>
		private List<SweepDot> LookupColumn(MyDataTab page, double xValue)
		{
			var vm = MyVModel;
			var xvalues = page.Sweep.X;
			List<SweepDot> dots = new();
			if (xvalues == null || xvalues.Length == 0)
			{
				return dots;
			}
			// find nearest amplitude (both left and right will be identical here if scanned)
			var bin = xvalues.CountWhile(x => x <= xValue);    // find # of voltages <= me
			if (bin == xvalues.Length)
				bin = xvalues.Distinct().Count() - 1;
			var matchVolt = xvalues[bin];  // the actual voltage we want
			var allLines = FrequencyLines(page) ?? new();
			var allMatch = allLines.Where(x => x.Columns.Length > bin && Math.Abs(x.Columns[bin].GenVolts - matchVolt) < 0.0001).ToArray();
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

		/// <summary>
		/// find the column by X coordinate where X is formatted inputv
		/// </summary>
		/// <param name="freq"></param>
		/// <returns></returns>
		// for a given frequency, lookup the left and right columns
		// this is used by the cursor display
		public SweepDot[] LookupX(double volts)
		{
			if (LRGains == null || LRGains.Left.Length == 0 || LRGains.Right.Length == 0)
				return [];
			var vm = MyVModel;
			var myVolts = volts;
			var genType = ToDirection(vm.GenDirection);
			if (genType != E_GeneratorDirection.INPUT_VOLTAGE && LRGains != null)
			{
				// found this as output
				var myFreq = PageData.SweepSteps.Steps[0].GenFrequency; // first frequency?
				var gains = (ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right);
				var bin = LRGains.ToBinNumber(myFreq);
				var x = vm.ToGenVoltage(volts, [bin], GEN_INPUT, gains);
				myVolts = x;
			}
			var aline = LookupColumn(PageData, myVolts); // lookup the columns
			if (OtherTabs.Count > 0)
			{
				int tabCnt = 1;
				foreach (var o in OtherTabs)
				{
					if (!o.Definition.IsOnL)
						continue;
					var all2 = LookupColumn(o, myVolts); // lookup the columns
					var tabChr = tabCnt + ".";
					foreach (var ail in all2)
					{
						ail.Label = tabChr + ail.Label;
					}
					aline.AddRange(all2);
					tabCnt++;
				}
			}
			return aline.ToArray();
		}

		public override void PinGraphRange(string who)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			if (who == "XM")
			{
				// setting start seems to reset max... this is strange...
				var minx = Math.Pow(10, myPlot.Axes.Bottom.Min).ToString("0.##");
				vm.GraphEndX = Math.Pow(10, myPlot.Axes.Bottom.Max).ToString("0.##");
				vm.GraphStartX = minx;
			}
			else
			{
				PinGraphRanges(myPlot, vm, who);
			}
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<AmpSweepViewModel>(PageData, MyVModel, fileName);
		}

		public override async Task LoadFromFile(string fileName, bool doLoad)
		{
			var page = Util.LoadFile<AmpSweepViewModel>(PageData, fileName);
			if (page != null)
			{
				RawToAmpSweepColumns(page);
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
			return Util.LoadFile<AmpSweepViewModel>(page, fileName);
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
				MyVModel.OtherSetList.Add(page.Definition);
			}

			UpdateGraph(true);
		}

		// build a sinewave for the sweep
		private static double[] BuildWave(MyDataTab page, double dFreq)
		{
			var vm = page.ViewModel;
			var v1 = page.Definition.GeneratorVoltage;
			WaveContainer.SetMono();
			WaveGenerator.SetGen1(true, dFreq, v1, true);          // send a sine wave
			return WaveGenerator.Generate(true, (uint)vm.SampleRateVal, (uint)vm.FftSizeVal); // generate the waveform
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
				CanToken = new();
				await RunAcquisition();
			}
			catch (Exception ex)
			{
				await showMessage(ex.Message.ToString());
			}
			await EndAction(thdAmp);
		}

		public (double[], double[], double[]) DetermineVoltageSequences(AmpSweepViewModel myVm, double dFreq)
		{
			// specified voltages boundaries
			if (LRGains == null || LRGains.Left.Length == 0 || LRGains.Right.Length == 0)
				return ([], [], []);
			var startV = GenVoltApplyUnit(myVm.StartVoltage, myVm.GenVoltageUnit, 1e-9);
			var endV = GenVoltApplyUnit(myVm.EndVoltage, myVm.GenVoltageUnit, 1e-9);
			var stepVoltages = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(startV, endV, myVm.StepsOctave);
			// now convert all of the step voltages to input voltages
			var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
			var binno = LRGains.ToBinNumber(dFreq);
			// do this last so Power correctly per-octaves
			var stepInVoltages = stepVoltages.Select(x => myVm.ToGenVoltage(x, [binno], GEN_INPUT, gains)).ToArray();
			// get output values for left and right so we can attenuate
			var stepOutLVoltages = stepInVoltages.Select(x => ToGenOutVolts(x, [binno], LRGains.Left)).ToArray();
			var stepOutRVoltages = stepInVoltages.Select(x => ToGenOutVolts(x, [binno], LRGains.Right)).ToArray();
			return (stepInVoltages, stepOutLVoltages, stepOutRVoltages);
		}

		public async Task<bool> RunAcquisition()
		{
			var thdAmp = MyVModel;

			LeftRightTimeSeries lrts = new();
			PageData.ViewModel.LoadViewFrom(MyVModel); // ensure view model is up to date
			PageData.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var page = PageData;    // alias
			page.Sweep = new();
			var vm = page.ViewModel;
			if (vm == null)
				return false;
			var genType = ToDirection(vm.GenDirection);

			// Init mini plots
			QaLibrary.InitMiniFftPlot(vm.Mini2Plot, 10, 100000, -150, 20);
			QaLibrary.InitMiniTimePlot(vm.MiniPlot, 0, 4, -1, 1);

			if (vm.HasQA430)
			{
				var model = Qa430Usb.Singleton?.QAModel;
				if (model != null)
				{
					await model.PrepareDefault();
				}
			}

			var freqValues = SelectItemList.ParseList(vm.FreqSummary).Where(x => x.IsSelected).ToList();
			if (freqValues.Count == 0)
			{
				await showMessage("No frequencies were specified!", 200);
				return false;
			}

			var fmin = freqValues.Min(x => ToD(x.Name, 1000));
			var fmax = freqValues.Max(x => ToD(x.Name, 1000));

			await CalculateGainCurve(vm, fmin, fmax);
			if (LRGains == null)
				return false;

			if (CanToken.IsCancellationRequested)
				return false;

			// ********************************************************************
			// Init device
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

			// get the variable list to sweep over
			try
			{
				// get all qa430 variable combinations
				var variables = OpampViewModel.EnumerateVariables(vm, null);

				// now expand the generator frequency options
				{
					var vnew = new List<AcquireStep>();
					foreach (var f in freqValues)
					{
						var vval = ToD(f.Name, 1000);
						var tFreq = vm.NearestBinFreq(vval);
						var vnewlist = variables.Select(x =>
						{
							var ux = new AcquireStep(x);
							ux.GenFrequency = tFreq;
							ux.GenXFmt = MathUtil.FormatFrequency(Math.Round(vval));
							return ux;
						}).ToList();
						vnew.AddRange(vnewlist);
					}

					variables = vnew;
				}

				// save the step list
				page.SweepSteps.Steps = variables.ToArray();
				string lastCfg = string.Empty;
				(LeftRightPair, LeftRightPair, LeftRightPair) noisy = new(new(), new(), new());
				LeftRightFrequencySeries? noiseRslt = null;
				bool longRest = true;

				// ********************************************************************
				// loop through the variables being changed
				// they are config name, load, gain, supply voltage
				// ********************************************************************
				foreach (var myConfig in variables) // sweep the different configurations
				{
					// update the qa430 if needed
					lastCfg = await vm.ExecuteModel(myConfig, lastCfg);
					// ********************************************************************
					// Determine voltage sequences potentially based on configuration
					// ********************************************************************
					var dvsOut = DetermineVoltageSequences(vm, myConfig.GenFrequency);
					var stepInVoltages = dvsOut.Item1;
					var stepOutLVoltages = dvsOut.Item2;
					var stepOutRVoltages = dvsOut.Item3;

					//QaLibrary.PlotMiniFftGraph(vm.Mini2Plot, noisy.FreqRslt, vm.ShowLeft, vm.ShowRight);
					//QaLibrary.PlotMiniTimeGraph(vm.MiniPlot, noisy.TimeRslt, testFrequency, vm.ShowLeft, vm.ShowRight);
					// ********************************************************************
					// Step through the list of voltages
					// ********************************************************************
					for (int i = 0; i < stepInVoltages.Length; i++)
					{
						// attenuate for both channels
						var attenuate = (int)vm.Attenuation;
						if (vm.DoAutoAttn)
						{
							var voutLdbv = QaLibrary.ConvertVoltage(stepOutLVoltages[i], E_VoltageUnit.Volt, E_VoltageUnit.dBV);
							var voutRdbv = QaLibrary.ConvertVoltage(stepOutRVoltages[i], E_VoltageUnit.Volt, E_VoltageUnit.dBV);
							attenuate = QaLibrary.DetermineAttenuation(Math.Max(voutLdbv, voutRdbv));
						}
						var voltf = vm.GetGenVoltLine(stepInVoltages[i]);
						await showMessage($"Measuring step {i + 1} at {voltf} with attenuation {attenuate}.");
						await showProgress(100 * (i + 1) / (stepInVoltages.Length));
						// Set the new input range
						MyVModel.Attenuation = attenuate;   // visible display
						vm.Attenuation = attenuate;         // my setting
															// ********************************************************************
															// Do noise floor measurement
															// ********************************************************************
															// do this with the specified opamp configuration
															// wait for the noise to stabilize
						if (longRest)
						{
							// so noise stabilizes which takes quite a while at first
							var nstart = DateTime.Now;
							while ((DateTime.Now - nstart).TotalMilliseconds < 3200)
							{
								await MeasureNoise(MyVModel, CanToken.Token);
							}
						}
						if (longRest || attenuate != QaComm.GetInputRange())
						{
							await QaComm.SetInputRange(attenuate);
							noisy = await MeasureNoise(vm, CanToken.Token, false);
							// get the entire noise response (maybe scaled)
							noiseRslt = await MeasureNoiseFreq(vm, 2, CanToken.Token);    // get noise averaged 4 times
						}
						longRest = false;

						// ********************************************************************
						// Do noise floor measurement
						// ********************************************************************
						page.NoiseFloor = noisy.Item1;
						page.NoiseFloorA = noisy.Item2;
						page.NoiseFloorC = noisy.Item3;
						if (CanToken.IsCancellationRequested)
							return false;

						// Convert generator voltage from V to dBV
						var generatorVoltageV = stepInVoltages[i];
						page.Definition.GeneratorVoltage = generatorVoltageV;
						MyVModel.GeneratorVoltage = vm.GetGenVoltLine(generatorVoltageV);

						LeftRightSeries? lrfs = null;
						try
						{
							var wave = BuildWave(page, myConfig.GenFrequency);   // also update the waveform variables

							FrequencyHistory.Clear();
							for (int ik = 0; ik < (thdAmp.Averages - 1); ik++)
							{
								lrfs = await QaComm.DoAcquireUser(1, CanToken.Token, wave, wave, true);
								vm.IORange = $"({QaComm.GetOutputRange()} - {QaComm.GetInputRange()})";
								if (lrfs == null || lrfs.TimeRslt == null || lrfs.FreqRslt == null)
									break;
								FrequencyHistory.Add(lrfs.FreqRslt);
							}
							{
								lrfs = await QaComm.DoAcquireUser(1, CanToken.Token, wave, wave, true);
								if (lrfs == null || lrfs.TimeRslt == null || lrfs.FreqRslt == null)
									break;
								lrfs.FreqRslt = CalculateAverages(lrfs.FreqRslt, thdAmp.Averages);
							}
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
						if (CanToken.IsCancellationRequested)
							break;
						page.TimeRslt = lrfs.TimeRslt ?? new();
						page.FreqRslt = lrfs.FreqRslt;

						if (page.TimeRslt.Left.Length == 0 || lrfs.FreqRslt == null)               // Check in bin within range
							break;

						// Plot the mini graphs
						QaLibrary.PlotMiniFftGraph(vm.Mini2Plot, lrfs.FreqRslt, vm.ShowLeft, vm.ShowRight);
						QaLibrary.PlotMiniTimeGraph(vm.MiniPlot, lrfs.TimeRslt, myConfig.GenFrequency, vm.ShowLeft, vm.ShowRight);

						// Check if cancel button pressed
						if (CanToken.IsCancellationRequested)
						{
							break;
						}

						var flr = GetNoiseFloor(page);
						var work = CalculateColumn(page.FreqRslt, vm, flr, myConfig.GenFrequency,
									CanToken.Token, myConfig, noiseRslt); // do the math for the columns
						if (work.Item1 != null && work.Item2 != null)
						{
							work.Item1.GenVolts = stepInVoltages[i];
							work.Item2.GenVolts = stepInVoltages[i];
							if (vm.DeembedDistortion)
							{
								work.Item1 = DeembedColumns(work.Item1, work.Item2, myConfig.Distgain);
							}
							AddColumn(page, stepInVoltages[i], work.Item1, work.Item2);
						}

						MyVModel.LinkAbout(PageData.Definition);
						RawToAmpSweepColumns(page);
						UpdateGraph(false);

						// Check if cancel button pressed
						if (CanToken.IsCancellationRequested)
						{
							break;
						}
					} // next voltage step
				} // next configuration
			}
			catch (Exception)
			{

			}

			// Get maximum signal for attenuation prediction of next step
			//prevInputAmplitudedBV = 20 * Math.Log10(lrfs.FreqRslt.Left.Max());
			//prevInputAmplitudedBV = Math.Max(prevInputAmplitudedBV, 20 * Math.Log10(lrfs.FreqRslt.Left.Max()));
			if (!vm.IsTracking)
			{
				vm.RaiseMouseTracked("track");
			}

			PageData.TimeRslt = new();
			PageData.FreqRslt = null;
			PageData.DelayRslt = null;

			// Show message
			await showMessage(CanToken.IsCancellationRequested ? $"Measurement cancelled!" : $"Measurement finished!");
			return true;
		}

		private string SetPlotLabels()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			var tt = ToDirection(vm.GenDirection);
			string title = string.Empty;
			if (tt == E_GeneratorDirection.INPUT_VOLTAGE)
			{
				myPlot.XLabel("Input voltage (Vrms)");
				title = "Distortion vs Voltage";
			}
			else if (tt == E_GeneratorDirection.OUTPUT_VOLTAGE)
			{
				myPlot.XLabel("Output voltage (Vrms)");
				title = "Distortion vs Voltage";
			}
			else if (tt == E_GeneratorDirection.OUTPUT_POWER)
			{
				myPlot.XLabel("Output power (W)");
				title = "Distortion vs Power";
			}
			return title;
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
		/// Initialize the THD % plot
		/// </summary>
		void InitializeThdPlot(string plotFormat = "%")
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			PlotUtil.InitializeMagAmpPlot(myPlot, plotFormat);

			UpdatePlotTitle();
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			vm.MainPlot.Refresh();
		}

		/// <summary>
		/// Initialize the THD magnitude (dB) plot
		/// </summary>
		void InitializeMagnitudePlot(string plotFormat = "dBV")
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			PlotUtil.InitializeMagAmpPlot(myPlot, plotFormat);

			UpdatePlotTitle();
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
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
			var freqVm = MyVModel;
			bool showLeft;
			bool showRight;
			if (isMain)
			{
				showLeft = freqVm.ShowLeft; // dynamically update these
				showRight = freqVm.ShowRight;
			}
			else
			{
				showLeft = page.Definition.IsOnL; // dynamically update these
				showRight = page.Definition.IsOnR;
			}

			if (!showLeft && !showRight)
				return;

			float lineWidth = freqVm.ShowThickLines ? _Thickness : 1;
			float markerSize = freqVm.ShowPoints ? lineWidth + 3 : 1;

			// here Y values are in dBV
			void AddPlot(double[] xValues, List<double> yValues, int colorIndex, string legendText, LinePattern linePattern)
			{
				var u = measurementNr;
				if (yValues.Count == 0) return;
				var plot = freqVm.MainPlot.ThePlot.Add.SignalXY(xValues, yValues.ToArray());
				plot.LineWidth = lineWidth;
				plot.Color = GraphUtil.GetPaletteColor("Transparent", colorIndex);
				plot.MarkerSize = markerSize;
				plot.LegendText = legendText;
				plot.LinePattern = linePattern;
				plot.IsVisible = !MyVModel.HiddenLines.Contains(legendText);
				MyVModel.LegendInfo.Add(new MarkerItem(linePattern, plot.Color, legendText, colorIndex, plot, freqVm.MainPlot, plot.IsVisible));
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
			var lp = patternList[(isMain && solidFirst) ? 0 : (isMain ? 1 : (solidFirst ? 2 : 3))];
			if (showRight && showLeft)
				suffix = (leftTop ? ".R" : ".L") + tosuffix;
			else
				suffix = tosuffix;

			// for each list of lines (left and right)
			var ttype = ToDirection(freqVm.GenDirection);
			var prefix = isMain ? string.Empty : (ClipName(page.Definition.Name) + ".");
			foreach (var lineList in lineGroup)
			{
				var colorNum = (measurementNr > 1) ? (measurementNr - 1) * 5 : 0;
				foreach (var line in lineList)
				{
					var subsuffix = suffix + line.Label;
					var colArray = line.Columns;
					double[] freq = [];
					switch (ttype)
					{
						case E_GeneratorDirection.INPUT_VOLTAGE:
							freq = colArray.Select(x => x.GenVolts).ToArray();
							break;
						case E_GeneratorDirection.OUTPUT_VOLTAGE:
							freq = colArray.Select(x => x.Mag).ToArray();
							break;
						case E_GeneratorDirection.OUTPUT_POWER:
							freq = colArray.Select(x => (x.Mag * x.Mag / ViewSettings.AmplifierLoad)).ToArray();
							break;
					}
					freq = freq.Select(x => Math.Log10(x)).ToArray();
					if (freqVm.ShowMagnitude)
						AddPlot(freq, colArray.Select(x => FormVal(x.Mag, x.Mag)).ToList(), colorNum, prefix + "Mag" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowTHDN)
						AddPlot(freq, colArray.Select(x => FormVal(x.THDN, x.Mag)).ToList(), colorNum, prefix + "THDN" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowTHD)
						AddPlot(freq, colArray.Select(x => FormVal(x.THD, x.Mag)).ToList(), colorNum, prefix + "THD" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowNoise)
						AddPlot(freq, colArray.Select(x => FormVal(x.Noise, x.Mag)).ToList(), colorNum, prefix + "Noise" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowNoiseFloor)
						AddPlot(freq, colArray.Select(x => FormVal(x.NoiseFloor, x.Mag)).ToList(), colorNum, prefix + "Floor" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowD2)
						AddPlot(freq, colArray.Select(x => FormVal(x.D2, x.Mag)).ToList(), colorNum, prefix + "D2" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowD3)
						AddPlot(freq, colArray.Select(x => FormVal(x.D3, x.Mag)).ToList(), colorNum, prefix + "D3" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowD4)
						AddPlot(freq, colArray.Select(x => FormVal(x.D4, x.Mag)).ToList(), colorNum, prefix + "D4" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowD5)
						AddPlot(freq, colArray.Select(x => FormVal(x.D5, x.Mag)).ToList(), colorNum, prefix + "D5" + subsuffix, lp);
					colorNum++;
					if (freqVm.ShowD6)
						AddPlot(freq, colArray.Select(x => FormVal(x.D6P, x.Mag)).ToList(), colorNum, prefix + "D6+" + subsuffix, lp);
				}
				suffix = (leftTop ? ".L" : ".R") + tosuffix;          // second pass iff there are both channels
				lp = patternList[(isMain && solidFirst) ? 1 : (isMain ? 2 : (solidFirst ? 3 : 2))]; ;
			}
			freqVm.MainPlot.Refresh();
		}

		void HandleChangedProperty(ScottPlot.Plot myPlot, AmpSweepViewModel vm, string changedProp)
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
			AmpSweepViewModel vm = MyVModel;
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

			MyVModel.LegendInfo.Clear();
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
					resultNr++;
				}
			}
			if (mainTop)
			{
				PlotValues(PageData, rnr, true);
			}
		}

	}
}
