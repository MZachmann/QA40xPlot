using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
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

	public class ActFreqSweep : ActBase
	{
		private readonly Views.PlotControl swpPlot;
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;

		public MyDataTab PageData { get; private set; } // Data used in this form instance
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document

		private float _Thickness = 2.0f;

		private static FreqSweepViewModel MyVModel { get => ViewSettings.Singleton.FreqVm; }
		CancellationTokenSource CanToken;                                 // Measurement cancelation token

		private List<FreqSweepLine>? FrequencyLines(MyDataTab page)
		{
			return (List<FreqSweepLine>?)page.GetProperty("Left");
		}

		private List<FreqSweepLine>? FrequencyLinesRight(MyDataTab page)
		{
			return (List<FreqSweepLine>?)page.GetProperty("Right");
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public ActFreqSweep(Views.PlotControl graphSwp, Views.PlotControl graphFft, Views.PlotControl graphTime)
		{
			fftPlot = graphFft;
			timePlot = graphTime;
			swpPlot = graphSwp;

			CanToken = new CancellationTokenSource();
			PageData = new(MyVModel, new LeftRightTimeSeries());

			// Show empty graphs
			FreqSweepViewModel vm = MyVModel;
			QaLibrary.InitMiniFftPlot(fftPlot, ToD(vm.StartFreq, 10),
				ToD(vm.EndFreq, 20000), -150, 20);
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
			CanToken.Cancel();
		}

		public void UpdatePlotTitle()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = swpPlot.ThePlot;
			var title = "Distortion vs Frequency";
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
			else
				myPlot.Title(title);
		}

		private double[] ColumnToArray(FreqSweepColumn col)
		{
			return new double[] { col.Freq, col.Mag, col.Phase, col.THD, col.THDN, col.Noise, col.GenVolts };
		}

		private FreqSweepColumn ArrayToColumn(double[] rawData, uint startIdx)
		{
			FreqSweepColumn col = new();
			col.Freq = rawData[startIdx];
			col.Mag = rawData[startIdx + 1];
			col.Phase = rawData[startIdx + 2];
			col.THD = rawData[startIdx + 3];
			col.THDN = rawData[startIdx + 4];
			col.Noise = rawData[startIdx + 5];
			col.GenVolts = rawData[startIdx + 6];
			return col;
		}

		private List<FreqSweepLine> RawToColumns(AcquireStep[] steps, double[] raw)
		{
			if (raw.Length == 0)
				return [];
			List<FreqSweepColumn> left = new();
			// make columns from the raw data
			int i;
			for (i = 0; i < raw.Length; i += FreqSweepColumn.FreqSweepColumnCount)
			{
				var col = ArrayToColumn(raw, (uint)i);
				left.Add(col);
			}
			// convert into freqsweeplines
			List<FreqSweepLine> lines = new();
			// line lengths
			var maxF = left.Max(x => x.Freq);   // maximum frequency
			int linelength = 0;
			for (i = 0; i < left.Count; i++)
			{
				if (left[i].Freq >= maxF)
				{
					linelength = i + 1;
					break;
				}
			}
			if (linelength == 0)
				return lines;
			// now create list of lines
			var numLines = (left.Count + linelength - 1) / linelength;  // round up
			bool showVolt = 1 < steps.Select(x => x.GenVolt).Distinct().Count();    // more than one voltage, show them
			for (i = 0; i < numLines; i++)
			{
				var line = new FreqSweepLine();
				line.Label = steps[i].ToSuffix(showVolt, MyVModel.HasQA430);
				line.Columns = left.Skip(i * linelength).Take(linelength).ToArray();
				lines.Add(line);
			}

			return lines;
		}

		// convert the raw data into columns of data
		private void RawToFreqSweepColumns(MyDataTab page)
		{
			var u = RawToColumns(page.SweepSteps.Steps, page.Sweep.RawLeft);
			page.SetProperty("Left", u);
			var v = RawToColumns(page.SweepSteps.Steps, page.Sweep.RawRight);
			page.SetProperty("Right", v);
		}

		/// <summary>
		/// add a column to the data table columns
		/// </summary>
		/// <param name="x"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		private void AddColumn(MyDataTab page, double x, FreqSweepColumn left, FreqSweepColumn right)
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
				var vm = PageData.ViewModel;    // measurement settings cloned to not shift...
				if (vm == null)
					return Rect.Empty;

				var specVm = MyVModel;     // current settings

				List<FreqSweepColumn> steps = new();
				if (specVm.ShowLeft)
				{
					var a1 = FrequencyLines(PageData);
					if (a1 != null)
						foreach (var x in a1)
						{
							steps.AddRange(x.Columns);
						}
				}
				if (specVm.ShowRight)
				{
					var a1 = FrequencyLinesRight(PageData);
					if (a1 != null)
						foreach (var x in a1)
						{
							steps.AddRange(x.Columns);
						}
				}
				var seen = DataUtil.FindShownInfo<FreqSweepViewModel, List<FreqSweepLine>>(OtherTabs);
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
				if (specVm.ShowMagnitude)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.Mag));
					minY = Math.Min(minY, arsteps.Min(x => x.Mag));
				}
				if (specVm.ShowTHD)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.THD));
					minY = Math.Min(minY, arsteps.Min(x => x.THD));
				}
				if (specVm.ShowTHDN)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.THDN));
					minY = Math.Min(minY, arsteps.Min(x => x.THDN));
				}
				if (specVm.ShowNoise)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.Noise));
					minY = Math.Min(minY, arsteps.Min(x => x.Noise));
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

		private List<FreqSweepDot> LookupColumn(MyDataTab page, double freq)
		{
			var vm = MyVModel;
			var allLines = FrequencyLines(page);
			List<FreqSweepDot> dots = new();
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
				var dot = new FreqSweepDot();
				dot.Label = line.Label;
				dot.Column = line.Columns[bin];
				dots.Add(dot);
			}
			return dots;
		}

		// for a given frequency, lookup the left and right columns
		// this is used by the cursor display
		public FreqSweepDot[] LookupX(double freq)
		{
			var vm = MyVModel;
			var allLines = LookupColumn(PageData, freq); // lookup the columns
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
						ail.Label = tabChr + ail.Label;
					}
					allLines.AddRange(all2);
					tabCnt++;
				}
			}
			return allLines.ToArray();
		}

		public void PinGraphRange(string who)
		{
			ScottPlot.Plot myPlot = swpPlot.ThePlot;
			var vm = MyVModel;
			PinGraphRanges(myPlot, vm, who);
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<FreqSweepViewModel>(PageData, fileName);
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
				page.ViewModel.OtherSetList = MyVModel.OtherSetList;
				page.ViewModel.CopyPropertiesTo<FreqSweepViewModel>(MyVModel);    // retract the gui
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

		// input noises is uniform frequency based on fft
		public static double GetNoiseSmooth(double[] noises, double binSize, double dFreq)
		{
			var bin = QaLibrary.GetBinOfFrequency(dFreq, binSize);  // which frequency bin
			var bincnt = dFreq / (20 * binSize);  // #bins == +-1/10 of an octave
			bincnt = Math.Min(10, bincnt);     // limit to 100 bins
			var minbin = Math.Max(0, (int)(bin - bincnt));
			var maxbin = Math.Min(noises.Length - 1, (int)(bin + bincnt));
			var avenoise = noises.Skip(minbin).Take(maxbin - minbin).Average(x => x); // average noise within these bins
			return avenoise / Math.Sqrt(binSize);   // ? i think
		}

		private (int,double) CalculateAttenuation(double voltage, BaseViewModel bvm, 
			int[] frqtest, LeftRightFrequencySeries LRGains)
		{
			// to figure out attenuation use the first gen voltage
			var voltx = voltage.ToString();
			var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
			var genVolt = bvm.ToGenVoltage(voltx, frqtest, GEN_INPUT, gains);   // input voltage for request
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
			return (attenuation, genVolt);
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

			await CalculateGainCurve(MyVModel);
			if (LRGains == null)
				return false;

			int[] frqtest = [ToBinNumber(startFreq, LRGains), ToBinNumber(endFreq, LRGains)];
			double[] stepBinFrequencies = [];
			double binSize = 0;
			// here they are in the format of the generator test...
			var voltValues = SelectItemList.ParseList(vm.VoltSummary, 0).Where(x => x.IsSelected).ToList();
			if(voltValues.Count == 0)
			{
				await showMessage("No generator voltages specified!", 200);
				return false;
			}
			try
			{
				// Check if cancel button pressed
				if (CanToken.IsCancellationRequested)
					return false;

				// ********************************************************************
				// Calculate frequency steps to do
				// ********************************************************************
				binSize = QaLibrary.CalcBinSize(vm.SampleRateVal, vm.FftSizeVal);
				// Generate a list of frequencies
				var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(
					ToD(vm.StartFreq, 10), ToD(vm.EndFreq, 10000), vm.StepsOctave);
				var maxBinFreq = vm.SampleRateVal / 4;  // nyquist over 2 since we're looking at distortion
														// Translate the generated list to bin center frequencies
				stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, vm.SampleRateVal, vm.FftSizeVal);
				stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x < maxBinFreq)                // Filter out values that are out of range 
					.GroupBy(x => x)                                                                    // Filter out duplicates
					.Select(y => y.First())
					.ToArray();

				// ********************************************************************  
				// Load a settings we want since we're done autoscaling
				// ********************************************************************  
				if (true != await QaComm.InitializeDevice(vm.SampleRateVal, vm.FftSizeVal, vm.WindowingMethod, 42))
					return false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}

			// now do the measurement stuff
			try
			{
				// enumerate the sweeps we are going to do
				var step = new AcquireStep() { Cfg = "Config6b", Load = QA430Model.LoadOptions.Open, Gain = 1, Distgain = 101, SupplyP = 15, SupplyN = 15 };    // unity 16b with 101 dist gain
				if (!vm.HasQA430)
				{
					step.Distgain = 1;
				}
				var variables = new List<AcquireStep>() { step };

				QA430Model? model430 = vm.HasQA430 ? Qa430Usb.Singleton?.QAModel : null;
				if (model430 != null)
				{
					if (vm.VaryLoad)
					{
						variables = model430.ExpandLoadOptions(variables, freqVm.LoadSummary) ?? variables;
					}

					if (vm.VaryGain)
					{
						variables = model430.ExpandGainOptions(variables, freqVm.GainSummary) ?? variables;
					}

					if (vm.VarySupply)
					{
						variables = model430.ExpandSupplyOptions(variables, freqVm.SupplySummary) ?? variables;
					}
				}

				// now expand the generator voltage options
				{
					var voltSet = SelectItemList.ParseList(freqVm.VoltSummary, 0).Where(x => x.IsSelected).ToList();
					if(voltSet.Count == 0)
					{
						await showMessage("No generator voltages specified!", 200);
						return false;
					}
					var vnew = new List<AcquireStep>();
					foreach (var v in voltValues)
					{
						var vval = MathUtil.ToDouble(v.Name, 0.1);
						var vnewlist = variables.Select(x => { 
							x.GenVolt = vval;
							x.GenVoltFmt = (vm.IsGenPower ? MathUtil.FormatPower(vval) : MathUtil.FormatVoltage(vval));
							return x; }).ToList();
						vnew.AddRange(vnewlist);
					}
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
					QA430Model? model = vm.HasQA430 ? Qa430Usb.Singleton?.QAModel : null;
					if (model != null)
					{
						if (myConfig.Cfg != lastCfg && myConfig.Cfg.Length > 0)
						{
							model.SetOpampConfig(myConfig.Cfg);
							lastCfg = myConfig.Cfg;
						}
						if (vm.VaryLoad)
							model.LoadOption = (short)myConfig.Load;
						else
							model.LoadOption = (short)QA430Model.LoadOptions.Open;
						if (vm.VarySupply && myConfig.SupplyP < 15)
						{
							model.NegRailVoltage = (-myConfig.SupplyN).ToString();
							model.PosRailVoltage = myConfig.SupplyP.ToString();
							model.UseFixedRails = false;
						}
						else
							model.UseFixedRails = true;
						// now that the QA430 relays are set, wait a bit...
						await model.WaitForQA430Relays();
					}
					// sweeping generator voltage as well
					var attenset = CalculateAttenuation(myConfig.GenVolt, vm, frqtest, LRGains);
					var attenuation = attenset.Item1;
					var genVolt = attenset.Item2;
					page.Definition.GeneratorVoltage = genVolt;

					// Set the new input range
					if(attenuation != QaComm.GetInputRange())
					{
						await QaComm.SetInputRange(attenuation);
					}
					MyVModel.Attenuation = attenuation;   // visible display
					vm.Attenuation = attenuation;         // my setting

					// ********************************************************************
					// Do noise floor measurement
					// ********************************************************************
					// do this with the specified opamp configuration
					var noisy = await MeasureNoise(MyVModel, CanToken.Token);
					noisy.Item1.Divby(myConfig.Distgain);
					noisy.Item2.Divby(myConfig.Distgain);
					noisy.Item3.Divby(myConfig.Distgain);
					page.NoiseFloor = noisy.Item1;
					page.NoiseFloorA = noisy.Item2;
					page.NoiseFloorC = noisy.Item3;

					if (CanToken.IsCancellationRequested)
						return false;
					// get the entire noise response (maybe scaled)
					var noiseRslt = await MeasureNoiseFreq(MyVModel, 4, CanToken.Token);    // get noise averaged 4 times

					// enable generator
					WaveGenerator.SetEnabled(true);
					// ********************************************************************
					// Step through the list of frequencies
					// ********************************************************************
					var genScaleVolt = genVolt / Math.Abs(myConfig.Gain);   // input voltage for request
					page.Definition.GeneratorVoltage = genScaleVolt;   // set the generator voltage in the definition
					MyVModel.GeneratorVoltage = MathUtil.FormatVoltage(genScaleVolt);
					for (int f = 0; f < stepBinFrequencies.Length; f++)
					{
						var freqy = stepBinFrequencies[f];
						await showMessage($"Measuring {freqy:0.#} Hz at {genVolt:G3} V.");
						await showProgress(100 * (f + 1) / stepBinFrequencies.Length);

						WaveGenerator.SetGen1(freqy, genScaleVolt, true);             // send a sine wave
						WaveGenerator.SetGen2(0, 0, false);            // send a sine wave
						LeftRightSeries lrfs;

						FrequencyHistory.Clear();
						for (int ik = 0; ik < (vm.Averages - 1); ik++)
						{
							lrfs = await QaComm.DoAcquisitions(1, CanToken.Token, true);
							if (lrfs == null || lrfs.TimeRslt == null || lrfs.FreqRslt == null)
								break;
							FrequencyHistory.Add(lrfs.FreqRslt);
						}
						// now FrequencyHistory has n-1 samples
						{
							lrfs = await QaComm.DoAcquisitions(1, CanToken.Token, true);
							if (lrfs == null || lrfs.TimeRslt == null || lrfs.FreqRslt == null)
								break;
							lrfs.FreqRslt = CalculateAverages(lrfs.FreqRslt, vm.Averages);
						}
						if (CanToken.IsCancellationRequested)
							break;

						page.TimeRslt = lrfs.TimeRslt ?? new();
						page.FreqRslt = lrfs.FreqRslt;

						int fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
						if (page.TimeRslt.Left.Length == 0 || lrfs.FreqRslt == null || fundamentalBin >= lrfs.FreqRslt.Left.Length)               // Check in bin within range
							break;

						// Plot the mini graphs
						QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);
						QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, freqy, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);

						// even with multiple configurations
						// this will keep stacking up stuff while frequency array shows min...max,min...max,...
						var work = CalculateColumn(page, freqy, CanToken.Token, myConfig, noiseRslt); // do the math for the columns
						if (work.Item1 != null && work.Item2 != null)
						{
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

		/// <summary>
		/// do post processing, just a bunch of easy math and moving stuff into viewmodels
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		private (FreqSweepColumn?, FreqSweepColumn?) CalculateColumn(MyDataTab msr, double dFreq,
			CancellationToken ct, AcquireStep acqConfig, LeftRightFrequencySeries? lfrsNoise)
		{
			if (msr.FreqRslt == null)
			{
				return (null, null);
			}

			// left and right channels summary info to fill in
			var left = new FreqSweepColumn();
			var right = new FreqSweepColumn();
			FreqSweepColumn[] steps = [left, right];
			FreqSweepViewModel vm = msr.ViewModel;

			var lrfs = msr.FreqRslt;    // frequency response
			var maxScan = msr.FreqRslt.Df * msr.FreqRslt.Left.Length;
			var maxf = Math.Min(ViewSettings.NoiseBandwidth, maxScan);  // opamps use 80KHz bandwidth, audio uses 20KHz

			LeftRightPair thds = QaCompute.GetThdDb(vm.WindowingMethod, lrfs, dFreq, 20.0, maxScan);
			LeftRightPair thdN = QaCompute.GetThdnDb(vm.WindowingMethod, lrfs, dFreq, 20.0, maxf, ViewSettings.NoiseWeight);

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

			double dmult = acqConfig.Distgain;
			// here steps just counts left then right
			foreach (var step in steps)
			{
				bool bl = step == left;     // stepping left?
				step.Freq = dFreq;
				step.Mag = QaMath.MagAtFreq((bl ? msr.FreqRslt.Left : msr.FreqRslt.Right), msr.FreqRslt.Df, dFreq);
				step.THD = step.Mag * Math.Pow(10, (bl ? thds.Left : thds.Right) / 20); // in volts from dB relative to mag
				step.THDN = step.Mag * Math.Pow(10, (bl ? thdN.Left : thdN.Right) / 20); // in volts from dB relative to mag
				step.Phase = 0;
				if (!bl)
				{
					if (lfrsNoise == null)
						step.Noise = floor.Right / dmult; // noise floor
					else
						step.Noise = GetNoiseSmooth(lfrsNoise.Right, lfrsNoise.Df, dFreq) / dmult; // noise density smoothed
				}
				else
				{
					if (lfrsNoise == null)
						step.Noise = floor.Left / dmult; // noise floor
					else
						step.Noise = GetNoiseSmooth(lfrsNoise.Left, lfrsNoise.Df, dFreq) / dmult; // noise density smoothed
				}

				// divide by the amount of distortion gain since that is a voltage gain
				step.THD /= dmult;
				step.THDN /= dmult;
			}

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
			swpPlot.ThePlot.Clear();
			swpPlot.Refresh();
		}

		/// <summary>
		/// Ititialize the THD % plot
		/// </summary>
		void InitializeThdPlot(string plotFormat = "%")
		{
			ScottPlot.Plot myPlot = swpPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			var thd = MyVModel;
			myPlot.Axes.SetLimits(Math.Log10(ToD(thd.GraphStartX, 20)), Math.Log10(ToD(thd.GraphEndX, 20000)),
				Math.Log10(ToD(thd.RangeBottom, -100)) - 0.00000001, Math.Log10(ToD(thd.RangeTop, -10)));  // - 0.000001 to force showing label
			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			swpPlot.Refresh();
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot(string plotFormat = "dBV")
		{
			var thdFreq = MyVModel;
			ScottPlot.Plot myPlot = swpPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			myPlot.Axes.SetLimitsX(Math.Log10(ToD(thdFreq.GraphStartX, 20)),
				Math.Log10(ToD(thdFreq.GraphEndX, 20000)), myPlot.Axes.Bottom);

			myPlot.Axes.SetLimitsY(ToD(thdFreq.RangeBottomdB), ToD(thdFreq.RangeTopdB), myPlot.Axes.Left);

			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));
			myPlot.HideLegend();
			swpPlot.Refresh();
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
				var plot = swpPlot.ThePlot.Add.SignalXY(xValues, yValues.ToArray());
				plot.LineWidth = lineWidth;
				plot.Color = GraphUtil.GetPaletteColor("Transparent", colorIndex);
				plot.MarkerSize = markerSize;
				plot.LegendText = legendText;
				plot.LinePattern = linePattern;
				MyVModel.LegendInfo.Add(new MarkerItem(linePattern, plot.Color, legendText, colorIndex, plot, swpPlot));
			}

			// which columns are we displaying? left, right or both
			List<FreqSweepLine>[] lineGroup;
			List<FreqSweepLine>? leftCol = FrequencyLines(page);
			List<FreqSweepLine>? rightCol = FrequencyLinesRight(page);
			if (showLeft && showRight && leftCol != null && rightCol != null)
			{
				lineGroup = [leftCol, rightCol];
			}
			else if (showLeft && leftCol != null)
			{
				lineGroup = [leftCol];
			}
			else
			{
				lineGroup = (rightCol != null) ? [rightCol] : [];
			}

			string suffix = ".";
			var lp = isMain ? LinePattern.Solid : LinePattern.Dashed;
			if (showRight && showLeft)
				suffix = ".L.";

			// for each list of lines (left and right)
			var prefix = (measurementNr == 0) ? string.Empty : (measurementNr + ".");
			foreach (var lineList in lineGroup)
			{
				var colorNum = (measurementNr > 1) ? (measurementNr - 1) * 5 : 0;
				foreach (var line in lineList)
				{
					var subsuffix = suffix + line.Label;
					var colArray = line.Columns;
					var freq = colArray.Select(x => Math.Log10(x.Freq)).ToArray();
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
				}
				suffix = ".R.";          // second pass iff there are both channels
				lp = isMain ? LinePattern.DenselyDashed : LinePattern.Dotted;
			}
			swpPlot.Refresh();
		}

		public void UpdateGraph(bool settingsChanged)
		{
			swpPlot.ThePlot.Remove<SignalXY>();             // Remove all current lines
			int resultNr = 0;
			FreqSweepViewModel freqVm = MyVModel;

			if (GraphUtil.IsPlotFormatLog(freqVm.PlotFormat))
			{
				if (settingsChanged)
				{
					InitializeMagnitudePlot(freqVm.PlotFormat);
				}
			}
			else
			{
				if (settingsChanged)
				{
					InitializeThdPlot(freqVm.PlotFormat);
				}
			}
			freqVm.LegendInfo.Clear();
			PlotValues(PageData, resultNr++, true);
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null && other.Show != 0)
						PlotValues(other, resultNr++, false);
				}
			}
		}
	}
}