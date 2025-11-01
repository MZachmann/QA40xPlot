using Microsoft.VisualBasic.FileIO;
using OpenTK.Compute.OpenCL;
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
            QaLibrary.InitMiniFftPlot(fftPlot, MathUtil.ToDouble(vm.StartFreq, 10),
                MathUtil.ToDouble(vm.EndFreq, 20000), -150, 20);
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

		private FreqSweepColumn  ArrayToColumn(double[] rawData, uint startIdx)
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
			for (i = 0; i < left.Count; i ++)
			{
				if (left[i].Freq >= maxF)
				{
					linelength = i+1;
					break;
				}
			}
			if (linelength == 0)
				return lines;
			// now create list of lines
			var numLines = (left.Count + linelength - 1) / linelength;	// round up
			for (i = 0; i < numLines; i++)
			{
				var line = new FreqSweepLine();
				line.Label = steps[i].ToSuffix();
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

		public Rect GetDataBounds()
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
						foreach(var x in s)
							steps.AddRange(x.Columns);
				}

				if (steps.Count == 0)
					return Rect.Empty;

				var arsteps = steps.ToArray();

				rrc.X = arsteps.Min(x => x.Freq);              // min X/Freq value
				rrc.Width = arsteps.Max(x => x.Freq) - rrc.X;  // max frequency

				double maxY = -1e10;
				double minY = 1e10;
				if(specVm.ShowMagnitude)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.Mag));
					minY = Math.Min(minY, arsteps.Min(x => x.Mag));
				}
				if(specVm.ShowTHD)
				{
					maxY = Math.Max(maxY, arsteps.Max(x => x.THD));
					minY = Math.Min(minY, arsteps.Min(x => x.THD));
				}
				if(specVm.ShowTHDN)
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
			catch( Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return rrc;
		}

		private (FreqSweepColumn, FreqSweepColumn) LookupColumn(MyDataTab page, double freq)
		{
			var vm = MyVModel;
			var allLines = FrequencyLines(page);
			if (allLines == null || allLines.Count == 0)
			{
				return (new FreqSweepColumn(), new FreqSweepColumn());
			}
			// find nearest amplitude (both left and right will be identical here if scanned)
			var bin = allLines[0].Columns.Count(x => x.Freq <= freq) - 1;    // find first freq less than me
			if (bin == -1)
				bin = 0;
			var anearest = allLines[0].Columns[bin].Freq;
			//if (bin < (vf.Length - 1) && Math.Abs(freq - anearest) > Math.Abs(freq - vf[bin + 1]))
			//{
			//	bin++;
			//}
			FreqSweepColumn? mf1 = new FreqSweepColumn();
			FreqSweepColumn? mf2 = new FreqSweepColumn();
			mf1 = (allLines[0].Columns)[bin];    // get the left channel
			List<FreqSweepLine>? u = FrequencyLinesRight(page);
			if(u != null)
				mf2 = (u[0].Columns)[bin];    // get the left channel
			return (mf1, mf2);
		}

		public FreqSweepColumn[] LookupX(double freq)
		{
			var vm = MyVModel;
			List<FreqSweepColumn> myset = new();
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
			ScottPlot.Plot myPlot = swpPlot.ThePlot;
			var vm = MyVModel;
			PinGraphRanges(myPlot, vm, who);
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<FreqSweepViewModel>(PageData, fileName);
		}

		public async Task LoadFromFile(string fileName, bool doLoad)
		{
			var page = Util.LoadFile<FreqSweepViewModel>(PageData, fileName);
			if(page != null)
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
				page.Show = 1; // show the left channel new
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

			QaLibrary.InitMiniFftPlot(fftPlot, Math.Max(10,startFreq), endFreq, -150, 20);
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
			double[] stepBinFrequencies = [];
			double binSize = 0;

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
				if (true != await QaComm.InitializeDevice(vm.SampleRateVal, vm.FftSizeVal, vm.WindowingMethod, attenuation))
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
				var variables = new List<AcquireStep>()
				{
					new AcquireStep() { Cfg = "Config6b", Load = QA430Model.LoadOptions.Open, Gain = 1, Distgain=101, Supply = 15 }    // unity 16b with 101 dist gain
				};

				QA430Model? model430 = Qa430Usb.Singleton?.QAModel;

				if (vm.VaryLoad)
				{
					bool[] useOption = freqVm.Loadsets.Select(x => x.IsSelected).ToArray();
					variables = model430?.ExpandLoadOptions(variables, useOption) ?? variables;
				}

				if (vm.VaryGain)
				{

					bool[] useOption = freqVm.Gainsets.Select(x => x.IsSelected).ToArray();
					variables = model430?.ExpandGainOptions(variables, useOption) ?? variables;
				}

				if (vm.VarySupply)
				{
					double[] supplies = vm.SupplyList.Split([';', ' '], StringSplitOptions.RemoveEmptyEntries).
						Select(x => MathUtil.ToDouble(x, 15)).ToArray();
					variables = model430?.ExpandSupplyOptions(variables, supplies) ?? variables;
				}

				string lastCfg = string.Empty;
				page.SweepSteps.Steps = variables.ToArray();
				// ********************************************************************
				// loop through the variables being changed
				// they are config name, load, gain, supply voltage
				// ********************************************************************
				foreach (var myvar in variables) // sweep the different configurations
				{
					var model = Qa430Usb.Singleton?.QAModel;
					if(model != null)
					{
						if(myvar.Cfg != lastCfg && myvar.Cfg.Length > 0)
						{
							model.SetOpampConfig(myvar.Cfg);
							lastCfg = myvar.Cfg;
						}
						if (vm.VaryLoad)
							model.LoadOption = (short)myvar.Load;
						if(vm.VarySupply)
						{
							if(myvar.Supply < 15)
							{
								model.NegRailVoltage = (-myvar.Supply).ToString();
								model.PosRailVoltage = myvar.Supply.ToString();
								model.UseFixedRails = false;
							}
							else
							{
								model.UseFixedRails = true;
							}
						}
					}
					// ********************************************************************
					// Do noise floor measurement
					// ********************************************************************
					// do this with the config6b configuration, 101x dist gain
					var noisy = await MeasureNoise(MyVModel, CanToken.Token);
					{
						noisy.Item1.Left = noisy.Item1.Left / myvar.Distgain;
						noisy.Item1.Right = noisy.Item1.Right / myvar.Distgain;
						noisy.Item2.Left = noisy.Item2.Left / myvar.Distgain;
						noisy.Item2.Right = noisy.Item2.Right / myvar.Distgain;
						noisy.Item3.Left = noisy.Item3.Left / myvar.Distgain;
						noisy.Item3.Right = noisy.Item3.Right / myvar.Distgain;
					}
					page.NoiseFloor = noisy.Item1;
					page.NoiseFloorA = noisy.Item2;
					page.NoiseFloorC = noisy.Item3;
					if (CanToken.IsCancellationRequested)
						return false;
					var noiseRslt = await MeasureNoiseFreq(MyVModel, 4, CanToken.Token);	// get noise 
					if(noiseRslt != null)
					{
						noiseRslt.Left = noiseRslt.Left.Select(x => x / myvar.Distgain).ToArray();
					}

					// enable generator
					WaveGenerator.SetEnabled(true);
					// ********************************************************************
					// Step through the list of frequencies
					// ********************************************************************
					genVolt = vm.ToGenVoltage(vm.GenVoltage, frqtest, GEN_INPUT, gains) / Math.Abs(myvar.Gain);   // input voltage for request
					page.Definition.GeneratorVoltage = genVolt;   // set the generator voltage in the definition
					MyVModel.GeneratorVoltage = MathUtil.FormatVoltage(genVolt);
					for (int f = 0; f < stepBinFrequencies.Length; f++)
					{
						var freqy = stepBinFrequencies[f];
						await showMessage($"Measuring {freqy:0.#} Hz at {genVolt:G3} V.");
						await showProgress(100 * (f + 1) / stepBinFrequencies.Length);

						WaveGenerator.SetGen1(freqy, genVolt, true);             // send a sine wave
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

						uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[f], binSize);
						if (page.TimeRslt.Left.Length == 0 || lrfs.FreqRslt == null || fundamentalBin >= lrfs.FreqRslt.Left.Length)               // Check in bin within range
							break;

						// Plot the mini graphs
						QaLibrary.PlotMiniFftGraph(fftPlot, lrfs.FreqRslt, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);
						QaLibrary.PlotMiniTimeGraph(timePlot, lrfs.TimeRslt, freqy, vm.LeftChannel && vm.ShowLeft, vm.RightChannel && vm.ShowRight);

						// even with multiple configurations
						// this will keep stacking up stuff while frequency array shows min...max,min...max,...
						var work = CalculateColumn(page, freqy, CanToken.Token, myvar); // do the math for the columns
						if(work.Item1 != null && work.Item2 != null)
							AddColumn(page, freqy, work.Item1, work.Item2);

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
		private (FreqSweepColumn?,FreqSweepColumn?) CalculateColumn(MyDataTab msr, double dFreq, CancellationToken ct, AcquireStep acqConfig)
		{
			if (msr.FreqRslt == null)
			{
				return (null,null);
			}

			// left and right channels summary info to fill in
			var left = new FreqSweepColumn();
			var right = new FreqSweepColumn();
			FreqSweepColumn[] steps = [left, right];
			FreqSweepViewModel vm = msr.ViewModel;

			var lrfs = msr.FreqRslt;    // frequency response
			var maxf = msr.FreqRslt.Df * msr.FreqRslt.Left.Length;

			LeftRightPair thds = QaCompute.GetThdDb(vm.WindowingMethod, lrfs, dFreq, 20.0, Math.Min(20000, maxf));
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

			foreach (var step in steps)
			{
				bool bl = step == left;
				step.Freq = dFreq;
				step.Mag = QaMath.MagAtFreq((bl ? msr.FreqRslt.Left : msr.FreqRslt.Right), msr.FreqRslt.Df, dFreq);
				step.THD = step.Mag * Math.Pow(10, (bl ? thds.Left : thds.Right) / 20); // in volts from dB relative to mag
				step.THDN = step.Mag * Math.Pow(10, (bl ? thdN.Left : thdN.Right) / 20); // in volts from dB relative to mag
				step.Phase = 0;
				step.Noise = (bl ? floor.Left : floor.Right); // noise floor adjusted for bin size
				
				// divide by the amount of distortion gain since that is a voltage gain
				step.THD /= dmult;
				step.THDN /= dmult;
				step.Noise /= dmult;
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
			swpPlot.Refresh();
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
                Scatter? plot = null;
				plot = swpPlot.ThePlot.Add.Scatter(xValues, yValues.ToArray());
				plot.LineWidth = lineWidth;
				plot.Color = GraphUtil.GetPaletteColor("Transparent", colorIndex);
                plot.MarkerSize = markerSize;
                plot.LegendText = legendText;
                plot.LinePattern = linePattern;
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
                lineGroup = (rightCol!=null) ? [rightCol] : [];
            }

            string suffix = ".";
            var lp = isMain ? LinePattern.Solid : LinePattern.Dashed;
            if (showRight && showLeft)
                suffix = ".L.";

			// for each list of lines (left and right)
			foreach (var lineList in lineGroup)
			{
				var colorNum = 0;
				foreach (var line in lineList)
				{
					var subsuffix = suffix + line.Label;
					var colArray = line.Columns;
					var freq = colArray.Select(x => Math.Log10(x.Freq)).ToArray();
					if (freqVm.ShowMagnitude)
						AddPlot(freq, colArray.Select(x => FormVal(x.Mag, x.Mag)).ToList(), colorNum++, "Mag" + subsuffix, lp);
					if (freqVm.ShowTHDN)
						AddPlot(freq, colArray.Select(x => FormVal(x.THDN, x.Mag)).ToList(), colorNum++, "THDN" + subsuffix, lp);
					if (freqVm.ShowTHD)
						AddPlot(freq, colArray.Select(x => FormVal(x.THD, x.Mag)).ToList(), colorNum++, "THD" + subsuffix, lp);
					if (freqVm.ShowNoise)
						AddPlot(freq, colArray.Select(x => FormVal(x.Noise, x.Mag)).ToList(), colorNum++, "Noise" + subsuffix, lp);
				}
				suffix = ".R.";          // second pass iff there are both channels
				//lp = isMain ? LinePattern.DenselyDashed : LinePattern.Dotted;
			}
			swpPlot.Refresh();
        }

        public void UpdateGraph(bool settingsChanged)
        {
			swpPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
            int resultNr = 0;
            FreqSweepViewModel thd = MyVModel;

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