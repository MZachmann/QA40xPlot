using FftSharp;
using Newtonsoft.Json.Linq;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Extensions;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.AxisRules;
using ScottPlot.PathStrategies;
using ScottPlot.Plottables;
using System.Data;
using System.Numerics;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

namespace QA40xPlot.Actions
{
	using MyDataTab = DataTab<FrQa430ViewModel>;

	public partial class ActFrQa430 : ActBase<FrQa430ViewModel>
	{
		private List<MyDataTab> OtherTabs { get; set; } = new(); // Other tabs in the document

		/// <summary>
		/// Constructor
		/// </summary>
		public ActFrQa430(FrQa430ViewModel vm)
		{
			// Show empty graphs
			QaLibrary.InitMiniFftPlot(vm.Mini2Plot, 10, 100000, -180, 20);
			QaLibrary.InitMiniTimePlot(vm.MiniPlot, 0, 4, -2, 2);

			MyVModel.ShowMiniPlots = false;
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
			ScottPlot.Plot myPlot = MyVModel.MainPlot.ThePlot;
			var title = GetTheTitle();
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
			else
				myPlot.Title(title);
		}

		public override void PinGraphRange(string who)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			if (who == "PH")
			{
				var u = myPlot.Axes.Right.Min;
				var w = myPlot.Axes.Right.Max;
				vm.PhaseBottom = u.ToString("G3");
				vm.PhaseTop = w.ToString("G3");
			}
			else
				PinGraphRanges(myPlot, vm, who);
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<FrQa430ViewModel>(PageData, MyVModel, fileName);
		}

		public override async Task LoadFromFile(string fileName, bool isMain)
		{
			var page = LoadFile(fileName);
			if (page != null)
				await FinishLoad(page, isMain, fileName);
		}

		/// <summary>
		/// Load a file into a DataTab
		/// </summary>
		/// <param name="fileName">full path name</param>
		/// <returns>a datatab with no frequency info</returns>
		public MyDataTab? LoadFile(string fileName)
		{
			return Util.LoadFile<FrQa430ViewModel>(PageData, fileName);
		}

		/// <summary>
		/// given a datatab, integrate it into the gui as the current datatab
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public async Task FinishLoad(MyDataTab page, bool isMain, string fileName)
		{
			ClipName(page.Definition, fileName);

			// now recalculate everything
			// BuildFrequencies(page);
			await PostProcess(page, CanToken.Token);
			if (isMain)
			{
				// we can't overwrite the viewmodel since it links to the display proper
				// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				var vm = MyVModel;
				vm.LoadViewFrom(page.ViewModel);
				PageData = page;    // set the current page to the loaded one

				// relink to the new definition
				vm.LinkAbout(page.Definition);
				vm.HasSave = true;
			}
			else
			{
				OtherTabs.Add(page); // add the new one
				MyVModel.OtherSetList.Add(page.Definition);
			}
			UpdateGraph(true);
		}

		private async Task<bool> PostProcess(MyDataTab msr, CancellationToken ct)
		{
			if (msr == null)
			{
				await showMessage("No frequency result");
			}
			else if (!msr.ViewModel.IsTracking)
			{
				msr.ViewModel.RaiseMouseTracked("track");
			}

			return false;
		}

		public async Task RunMeasurement(bool runContinuously)
		{
			CanToken = new();
			int index = 0;
			// set up the QA430 configuration
			var vm = MyVModel;
			var ttype = vm.GetTestingType(vm.TestType);
			var qaconfig = GetConfigForTest(ttype, 0);
			if (runContinuously)
			{
				await DoMeasurement(qaconfig, index++);
				while (!CanToken.IsCancellationRequested)
				{
					await DoMeasurement(qaconfig, index++);
				}
			}
			else
			{
				await DoMeasurement(qaconfig, 0);
			}
			DefaultQA430();
			await showMessage("Finished.");
			MyVModel.HasExport = (PageData.GainFrequencies.Length > 0);
		}

		public void NextToData(MyDataTab page)
		{
			// we can't do mic correction here because it gums up the data permanently
			// it should be applied at each data point or the chirp
			if (!CanToken.IsCancellationRequested)
			{
				if (!ReferenceEquals(PageData, page))
					PageData = page;        // finally update the pagedata for display and processing
				MyVModel.LinkAbout(PageData.Definition);  // ensure we're linked right during replays
				UpdateGraph(false);
			}
		}

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async Task DoMeasurement(AcquireStep cfg, int index)
		{
			if (index == 0)
			{
				await showProgress(0, 50);
			}
			await RunAcquisition(cfg, index);
		}

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async Task RunAcquisition(AcquireStep qaCfg, int index)
		{
			var vm = MyVModel;
			await showMessage($"Measuring step {index + 1}.", 20);
			if (!await StartAction(vm))
				return;
			if (!vm.IsChirp)
			{
				// Show empty graphs
				QaLibrary.InitMiniFftPlot(vm.Mini2Plot, 10, 40000, -180, 20);
				QaLibrary.InitMiniTimePlot(vm.MiniPlot, 0, 4, -2, 2);
			}
			else
			{
				vm.ShowMiniPlots = false;
			}

			vm.HasExport = false;
			if (index == 0)
				UpdateGraph(true);

			// sweep data
			LeftRightTimeSeries lrts = new();
			MyDataTab NextPage = new(vm, lrts); // copy the settings into our new page
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var msr = NextPage.ViewModel;
			if (msr == null)
				return;
			msr.QA430Cfg = qaCfg;

			// ********************************************************************
			// Setup the device
			// ********************************************************************
			if (msr.SampleRateVal == 0 || !FrQa430ViewModel.FftSizes.Contains(msr.FftSize))
			{
				MessageBox.Show("Invalid sample rate or fftsize settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			var fftsize = msr.FftSizeVal;
			var sampleRate = msr.SampleRateVal;

			var fmin = ToD(msr.StartFreq, 1000);
			var fmax = ToD(msr.EndFreq, 1000);
			// do we need to calculate the gain curve etc?
			bool needAttenuate = msr.DoAutoAttn || (ToDirection(msr.GenDirection) != E_GeneratorDirection.INPUT_VOLTAGE);

			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, msr.WindowingMethod, (int)msr.Attenuation))
			{
				return;
			}

			try
			{
				// ********************************************************************
				// Calculate frequency steps to do if discrete
				// ********************************************************************
				var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(ToD(msr.StartFreq), MathUtil.ToDouble(msr.EndFreq), msr.StepsOctave);
				// Translate the generated list to bin center frequencies
				var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize);
				stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x <= 95500)                // Filter out values that are out of range 
					.GroupBy(x => x)                                                                    // Filter out duplicates
					.Select(y => y.First())
					.ToArray();

				// get all qa430 variable combinations
				var variables = OpampViewModel.EnumerateVariables(msr, qaCfg);
				var ttype = msr.GetTestingType(msr.TestType);
				var lastCfg = string.Empty;

				// loop for each configuration
				foreach (var myConfig in variables)
				{
					var gvolt = GenVoltApplyUnit(msr.Gen1Voltage, msr.GenVoltageUnit, 1e-9);
					var genVolt = gvolt;
					// calculate required attenuation
					if (needAttenuate)
					{
						// find the highest output voltage
						var theGain = (ttype == TestingType.GBW) ? myConfig.Gain : 1;	// we know the gain
						var genv = msr.ToGenVoltage(gvolt, [], GEN_OUTPUT, [theGain]);  // output v
						var vdbv = QaLibrary.ConvertVoltage(genv, E_VoltageUnit.Volt, E_VoltageUnit.dBV);   // output dbv
						var attenuation = QaLibrary.DetermineAttenuation(vdbv);
						if (!msr.DoAutoAttn)
						{
							attenuation = (int)msr.Attenuation;
						}
						msr.Attenuation = attenuation;
						// get voltages for generator input based on GenDirection
						genVolt = msr.ToGenVoltage(gvolt, [], GEN_INPUT, [theGain]);
					}
					vm.Attenuation = msr.Attenuation; // display on-screen
					var voltagedBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);  // in dbv

					// If in continous mode we continue sweeping until cancellation requested.
					MyVModel.GeneratorVoltage = msr.GetGenVoltLine(genVolt); // save the actual generator voltage for display

					var u = lastCfg;
					lastCfg = await OpampViewModel.ExecuteModel(myConfig, lastCfg, vm.HasQA430); // update the qa430 if needed
					if(u != lastCfg)
					{
						await showMessage($"Settling new model: {myConfig.Cfg}.", 2000);
					}

					if (!CanToken.IsCancellationRequested)
					{
						NextPage.GainData = ([], []); // new list of complex data
						NextPage.GainFrequencies = [];
						NextPage.Definition.GeneratorVoltage = genVolt; // save the actual generator voltage
						if(ttype == TestingType.Noise)
						{
							await RunNoiseTest(NextPage, myConfig, index);
						}
						else if (msr.IsChirp)
						{
							await showProgress(98);
							await RunChirpTest(NextPage, voltagedBV, index);
						}
						else
						{
							// we have to clear since this does one step at atime
							vm.MainPlot.ThePlot.Clear();
							await RunFreqTest(NextPage, stepBinFrequencies, voltagedBV);
						}
						NextToData(NextPage);
						if(ttype == TestingType.InputZ)
						{
							await showProgress(50);
							var gains = NextPage.GainData;
							var gainf = NextPage.GainFrequencies;
							NextPage.GainData = ([], []); // new list of complex data
							NextPage.GainFrequencies = [];
							QA430Model? model = vm.HasQA430 ? Qa430Usb.Singleton?.QAModel : null;
							var acq2 = GetConfigForTest(ttype, 1);	// find out the 2nd cfg for inputz
							model?.SetOpampConfig(acq2.Cfg);        // just use the cfg not the user-settable stuff
							await Task.Delay(1000);	// settle
							if (msr.IsChirp)
							{
								await RunChirpTest(NextPage, voltagedBV, index);
							}
							else
							{
								// we have to clear since this does one step at atime
								vm.MainPlot.ThePlot.Clear();
								await RunFreqTest(NextPage, stepBinFrequencies, voltagedBV);
							}
							await showProgress(98);
							// now do the impedance math
							var gain2s = NextPage.GainData;	// complex value as real,imaginary
							var gain2f = NextPage.GainFrequencies;
							var cpl1 = gains.Item1.Zip(gains.Item2, (x, y) => new Complex(x, y));	// config8a
							var cpl2 = gain2s.Item1.Zip(gain2s.Item2, (x, y) => new Complex(x, y));	// config8b
							var gain3 = cpl1.Zip(cpl2, (x, y) => y / x).ToArray();  // ratio of gains
							var gaRl = gain3.Select(x => x.Real).ToArray();
							var gaIl = gain3.Select(x => x.Imaginary).ToArray();
							NextPage.GainData = (gaRl, gaIl);
							NextToData(NextPage);
						}
						var voltf = msr.GetGenVoltLine(genVolt);
						await showMessage($"Measuring step {index + 1} at {voltf} with attenuation {msr.Attenuation}.");
						await showProgress(100);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			UpdateGraph(false);
			PageData.TimeRslt = new();  // clear this before saving stuff
			await EndAction(vm);
		}

		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			if (PageData == null || PageData.GainFrequencies == null || PageData.GainData.Item1 == null)
				return null;
			DataBlob db = new();
			var frsqVm = MyVModel;
			var freqs = this.PageData.GainFrequencies;
			if (freqs.Length == 0)
				return null;

			db.FreqData = freqs.ToList();        // test frequencies
			var ttype = frsqVm.GetTestingType(frsqVm.TestType);
			switch (ttype)
			{
				case TestingType.Crosstalk:
				case TestingType.Noise:
				case TestingType.PSRR:
					if (frsqVm.ShowRight && !frsqVm.ShowLeft)
					{
						db.LeftData = PageData.GainData.Item2.ToList();
					}
					else
					{
						db.LeftData = PageData.GainData.Item1.ToList();
					}
					break;
				case TestingType.CMRR:
				case TestingType.GBW:
					db.LeftData = PageData.GainReal.Zip(PageData.GainImag, (x, y) => Math.Sqrt(x * x + y * y)).ToList();
					db.PhaseData = PageData.GainReal.Zip(PageData.GainImag, (x, y) => Math.Atan2(y, x)).ToList();
					break;
				case TestingType.InputZ:
					{
						double rref = ToD(MyVModel.ZReference, 8);
						db.LeftData = PageData.GainReal.Zip(PageData.GainImag, (x, y) => rref * MathUtil.ToImpedanceMag(x, y)).ToList();
						db.PhaseData = PageData.GainReal.Zip(PageData.GainImag, (x, y) => MathUtil.ToImpedancePhase(x, y)).ToList();
					}
					break;
			}
			return db;
		}

		// run a capture to get complex gain at a frequency
		async Task<Complex> GetGain(double showfreq, FrQa430ViewModel msr, TestingType ttype)
		{
			if (CanToken.Token.IsCancellationRequested)
				return new();

			LeftRightSeries lfrs = new();
			FrequencyHistory.Clear();
			var dset = WaveGenerator.GenerateBoth(msr.SampleRateVal, msr.FftSizeVal);
			var dataLeft = dset.Item1;
			var dataRight = dset.Item2;
			for (int i = 0; i < msr.Averages - 1; i++)
			{
				lfrs = await QaComm.DoAcquireUser(1, CanToken.Token, dataLeft, dataRight, true);
				msr.IORange = $"({QaComm.GetOutputRange()} - {QaComm.GetInputRange()})";
				if (lfrs == null || lfrs.TimeRslt == null || lfrs.FreqRslt == null)
					return new();
				FrequencyHistory.Add(lfrs.FreqRslt);
			}
			{
				lfrs = await QaComm.DoAcquireUser(1, CanToken.Token, dataLeft, dataRight, true);
				if (lfrs == null || lfrs.TimeRslt == null || lfrs.FreqRslt == null)
					return new();
				lfrs.FreqRslt = CalculateAverages(lfrs.FreqRslt, msr.Averages);
			}

			PageData.TimeRslt = lfrs.TimeRslt;
			PageData.FreqRslt = lfrs.FreqRslt;

			if (ttype == TestingType.Noise)
				return CalculateResponseAt(showfreq, lfrs);

			return QaMath.CalculateGainPhase(showfreq, lfrs);
		}

		public Rect GetPhaseBounds()
		{
			// here we want to show what's visible so use freqVm for visibility
			var vm = PageData.ViewModel;
			var freqVm = MyVModel;
			var vmr = PageData.GainFrequencies; // test data
			var ttype = freqVm.GetTestingType(freqVm.TestType);
			var gainReal = PageData.GainReal;
			var gainImag = PageData.GainImag;

			if (vmr == null || vmr.Length == 0 || gainReal == null || gainImag == null)
				return Rect.Empty;

			Rect rrc = new Rect(0, 0, 0, 0);

			rrc.X = vmr.Min();
			rrc.Width = vmr.Max() - rrc.X;
			if (ttype == TestingType.CMRR || ttype == TestingType.GBW || ttype == TestingType.PSRR)
			{
				var phaseValues = MathUtil.ToCplxPhase(gainReal, gainImag).Select(x => 180 * x / Math.PI).ToArray();
				var phases = UnWrap(phaseValues);
				rrc.Y = phases.Min();
				rrc.Height = phases.Max() - rrc.Y;
			}
			else if (ttype == TestingType.InputZ)
			{
				var phaseValues = MathUtil.ToImpedancePhase(gainReal, gainImag).Select(x => 180 * x / Math.PI).ToArray(); ;
				var phases = UnWrap(phaseValues);
				rrc.Y = phases.Min();
				rrc.Height = phases.Max() - rrc.Y;
			}
			return rrc;
		}

		public override Rect GetDataBounds()
		{
			// here we want to show what's visible so use freqVm for visibility
			var vm = MyVModel;
			var msdfrq = PageData.GainFrequencies; // test data
			var ttype = vm.GetTestingType(vm.TestType);
			var msdre = PageData.GainReal;
			var msdim = PageData.GainImag;
			bool is2Channel = ttype == TestingType.Noise;

			if (msdfrq == null || msdfrq.Length == 0 || msdre == null || msdim == null)
				return Rect.Empty;

			Rect rrc = new Rect(0, 0, 0, 0);

			List<LeftRightResult> tabs = new();
			if (vm.ShowLeft && msdre != null)
			{
				tabs.Add(new LeftRightResult
				{
					Left = msdre,
					Right = (vm.ShowRight || !is2Channel) ? msdim : [],
					XValues = msdfrq
				});
			}
			var u = DataUtil.FindShownGains(OtherTabs, is2Channel);
			if (u.Count > 0)
			{
				foreach (var other in u)
				{
					tabs.Add(other);
				}
			}

			if (tabs.Count != 0)
			{
				if (ttype == TestingType.Noise )
				{
					rrc.X = tabs.Min(x => x.XValues.Min());
					rrc.Width = tabs.Max(x => x.XValues.Max()) - rrc.X;
					rrc.Y = tabs.Min(y => (y.Right.Length > 0) ? 
						Math.Min(y.Left.Min(), y.Right.Min()) :
						y.Left.Min());
					rrc.Height = tabs.Max(y => (y.Right.Length > 0) ?
						Math.Max(y.Left.Max(), y.Right.Max()) :
						y.Left.Max()) - rrc.Y;
				}
				else if (ttype == TestingType.GBW || ttype == TestingType.CMRR || ttype == TestingType.PSRR)
				{
					rrc.X = tabs.Min(x => x.XValues.Min());
					rrc.Width = tabs.Max(x => x.XValues.Max()) - rrc.X;
					var x = double.MaxValue;
					var x2 = 0.0;
					foreach(var tab in tabs)
					{
						var u2 = MathUtil.ToCplxMag(tab.Left, tab.Right);
						 x = Math.Min(x, u2.Min());
						x2 = Math.Max(x, u2.Max());
					}
					rrc.Y = x;
					rrc.Height = x2 - x;
				}
				else if (PageData.GainLeft != null && PageData.GainLeft.Length > 0)
				{   // impedance
					double rref = ToD(vm.ZReference, 10);
					var gainZ = PageData.GainReal.Zip(PageData.GainImag, (x, y) => MathUtil.ToImpedanceMag(x, y));
					var minL = gainZ.Min();
					var maxL = gainZ.Max();
					var minZohms = rref * minL;
					var maxZohms = rref * maxL;
					rrc.Y = minZohms;
					rrc.Height = maxZohms - minZohms;
				}
			}
			return rrc;
		}

		/// <summary>
		/// return (freq, real, imag, 0) - was group delay in 4th
		/// </summary>
		/// <param name="freq"></param>
		/// <returns>(frequency, real value, imaginary value, 0)</returns>
		public ValueTuple<double, double, double, double> LookupX(double freq)
		{
			if (PageData.GainFrequencies == null || PageData.GainLeft == null || PageData.GainRight == null)
				return ValueTuple.Create(0.0, 0.0, 0.0, 0.0);

			var freqs = PageData.GainFrequencies;
			var valuesRe = PageData.GainReal;
			var valuesIm = PageData.GainImag;
			ValueTuple<double, double, double, double> tup = ValueTuple.Create(1.0, 1.0, 1.0, 0.0);
			if (freqs != null && freqs.Length > 0 && valuesRe.Length > 0 && valuesIm.Length > 0)
			{
				// find nearest frequency from list
				var bin = freqs.CountWhile(x => x < freq) - 1;    // find first freq less than me
				if (bin == -1)
					bin = 0;
				var fnearest = freqs[bin];
				if (bin < (freqs.Length - 1) && Math.Abs(freq - fnearest) > Math.Abs(freq - freqs[bin + 1]))
				{
					bin++;
				}

				var frsqVm = MyVModel;
				var ttype = frsqVm.GetTestingType(frsqVm.TestType);
				var myFreq = freqs[bin];
				switch (ttype)
				{
					case TestingType.Crosstalk:
						// send freq, gain, gain2
						tup = ValueTuple.Create(myFreq, valuesRe[bin], valuesIm[bin], 0.0);
						break;
					case TestingType.Noise:
						// send freq, gain, gain2
						{
							var fvi = GraphUtil.ValueToPlotFn(frsqVm, PageData.GainReal, PageData.GainFrequencies);
							var fvi2 = GraphUtil.ValueToPlotFn(frsqVm, PageData.GainImag, PageData.GainFrequencies);
							var fl = fvi(valuesRe[bin]);
							var fr = fvi2(valuesIm[bin]);
							tup = ValueTuple.Create(myFreq, fl, fr, 0.0);
						}
						break;
					case TestingType.InputZ:
						{   // send freq, ohms, phasedeg
							double rref = ToD(frsqVm.ZReference, 10);
							var impval = MathUtil.ToImpedanceMag(valuesRe[bin], valuesIm[bin]);
							var ohms = rref * impval;
							impval = MathUtil.ToImpedancePhase(valuesRe[bin], valuesIm[bin]);
							tup = ValueTuple.Create(myFreq, ohms, 180 * impval / Math.PI, 0.0);
						}
						break;
					case TestingType.Gain:
					case TestingType.GBW:
					case TestingType.CMRR:
					case TestingType.PSRR:
						{
							// send freq, gain, phasedeg
							var mag = MathUtil.ToCplxMag(valuesRe[bin], valuesIm[bin]);
							var phas = MathUtil.ToCplxPhase(valuesRe[bin], valuesIm[bin]);
							tup = ValueTuple.Create(myFreq, mag, 180 * phas / Math.PI, 0);
						}
						break;
				}
			}
			return tup;
		}

		/// <summary>
		/// fit to data
		/// </summary>
		/// <param name="bvm">the view model</param>
		/// <param name="parameter">which axis we're fitting</param>
		/// <param name="dRefs">list of data points from fft</param>
		public override void FitToData(BaseViewModel basevm, object? parameter, double[]? dRefs)
		{
			var bvm = basevm as FrQa430ViewModel;
			if (parameter == null || bvm == null)
			{
				return;
			}
			var axisType = parameter.ToString();
			Rect bounds = Rect.Empty;
			switch (axisType)
			{
				case "PH":
					bounds = GetPhaseBounds();
					break;
				default:
					bounds = GetDataBounds();
					break;
			}

			switch (axisType)
			{
				case "PH":  // X magnitude
							// calculate the bounds here. X is provided in input or output volts/power
					bvm.PhaseTop = bounds.Bottom.ToString("G3");
					bvm.PhaseBottom = bounds.Top.ToString("G3");
					break;
				case "XF":  // X frequency
					bvm.GraphStartX = bounds.Left.ToString("0");
					bvm.GraphEndX = bounds.Right.ToString("0");
					break;
				case "YP":  // Y percent
					var xp = bounds.Y + bounds.Height;  // max Y value
					var bot = ((100 * bounds.Y) / xp);  // bottom value in percent
					bot = Math.Pow(10, Math.Max(-7, Math.Floor(Math.Log10(bot))));  // nearest power of 10
					bvm.RangeTop = "100";  // always 100%
					bvm.RangeBottom = bot.ToString("0.##########");
					break;
				case "YM":  // Y magnitude
					var ttype = bvm.GetTestingType(bvm.TestType);
					if (ttype == TestingType.Impedance)
					{
						bvm.RangeBottomdB = bounds.Y.ToString("G3");
						bvm.RangeTopdB = (bounds.Height + bounds.Y).ToString("G3");
					}
					else if (ttype != TestingType.Noise)
					{
						bvm.RangeBottomdB = (20 * Math.Log10(Math.Max(1e-14, bounds.Y))).ToString("G3");
						bvm.RangeTopdB = Math.Ceiling((20 * Math.Log10(Math.Max(1e-14, bounds.Height + bounds.Y)))).ToString("G3");
					}
					else
					{
						var dref = GraphUtil.GetDbrReference(bvm, bvm.ShowLeft ? PageData.GainLeft : PageData.GainRight,
							PageData.GainFrequencies);
						if(GraphUtil.IsPlotFormatLog(bvm.PlotFormat))
						{
							var botx = GraphUtil.ValueToLogPlot(bvm, bounds.Y, dref);
							var topx = GraphUtil.ValueToLogPlot(bvm, bounds.Y + bounds.Height, dref);
							bvm.RangeBottomdB = Math.Floor(botx).ToString("G3");
							bvm.RangeTopdB = Math.Ceiling(topx).ToString("G3");
						}
						else
						{
							var botx = GraphUtil.ValueToPlot(bvm.PlotFormat, bounds.Y, dref);
							var topx = GraphUtil.ValueToPlot(bvm.PlotFormat, bounds.Y + bounds.Height, dref);
							bvm.RangeBottomdB = botx.ToString("G3");
							bvm.RangeTopdB = topx.ToString("G3");
						}
					}
					break;
				default:
					break;
			}
		}

		private async Task RunStep(MyDataTab page, TestingType ttype, double dfreq, double genVolt)
		{
			var vm = page.ViewModel;
			if (vm.Averages > 0)
			{
				List<Complex> readings = new();
				for (int j = 0; j < vm.Averages; j++)
				{
					var voltf = vm.GetGenVoltLine(genVolt);
					await showMessage(string.Format($"Checking + {dfreq:0} Hz at {voltf}"));   // need a delay to actually see it
					var ga = await GetGain(dfreq, vm, ttype);
					readings.Add(ga);
				}
				var total = Complex.Zero;
				foreach (var f in readings)
				{
					total += f;
				}
				total /= vm.Averages;
				if (ttype == TestingType.CMRR && vm.QA430Cfg.Gain != 0)
				{
					total /= Math.Abs(vm.QA430Cfg.Gain);
				}
				if(ttype == TestingType.PSRR || ttype == TestingType.CMRR)
				{
					total = 1 / total;	// use the inverse for them
				}
				page.GainData = (page.GainReal.Append(total.Real).ToArray(), page.GainImag.Append(total.Imaginary).ToArray());
			}
			else
			{
				await showMessage(string.Format("Checking + {0:0}", dfreq));   // need a delay to actually see it
				var ga = await GetGain(dfreq, vm, ttype);
				//page.GainData = page.GainData.Append(ga).ToArray();
				if (ttype == TestingType.CMRR && vm.QA430Cfg.Gain != 0)
				{
					ga /= Math.Abs(vm.QA430Cfg.Gain);
				}
				if (ttype == TestingType.PSRR || ttype == TestingType.CMRR)
				{
					ga = 1 / ga;  // use the inverse for them
				}
				page.GainData = (page.GainReal.Append(ga.Real).ToArray(), page.GainImag.Append(ga.Imaginary).ToArray());
			}
		}

		// wait for the circuit to settle down
		private async Task WaitForSynch(MyDataTab page, TestingType ttype, double dfreq, double genVolt)
		{
			double delta = 2;
			double lastu = 1;
			int icnt = 0;
			while(delta < .9 || delta > 1.1)
			{
				// waste three tests to get stuff in synch
				await RunStep(page, ttype, dfreq, genVolt);
				var x = page.GainData.Item1;
				if (x.Length == 0)
					break;
				var u = x[0];     // get the first gain data value
				delta = Math.Abs(u / Math.Max(lastu, 1e-10));
				lastu = u;
				page.GainData = ([], []); // new list of complex data
				page.GainFrequencies = []; // new list of frequencies
				if (++icnt > 5)
					break;
			}
			delta = 1;	// a place to wait for breakpoint
		}

		/// <summary>
		/// Determine the gain curve
		/// </summary>
		/// <param name="stepBinFrequencies">the frequencies to test at</param>
		/// <param name="voltagedBV">the sine generator voltage</param>
		/// <returns></returns>
		private async Task<bool> RunFreqTest(MyDataTab page, double[] stepBinFrequencies, double voltagedBV)
		{
			var vm = page.ViewModel;
			await showProgress(0, 50);
			// Check if cancel button pressed
			if (CanToken.IsCancellationRequested)
				return false;

			WaveContainer.SetMono();     // enable the generator
			WaveGenerator.SetGen2(true, 1000, 0, false); // disable the second wave left
			WaveGenerator.SetGen2(false, 1000, 0, false); // disable the second wave right
			var ttype = vm.GetTestingType(vm.TestType);
			var genVolt = QaLibrary.ConvertVoltage(voltagedBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt);

			try
			{
				page.GainData = ([], []); // new list of complex data
				page.GainFrequencies = []; // new list of frequencies
				if (CanToken.IsCancellationRequested)
					return false;
				for (int steps = 0; steps < stepBinFrequencies.Length; steps++)
				{
					if (CanToken.IsCancellationRequested)
						break;
					var dfreq = stepBinFrequencies[steps];

					// set up the waveform generators
					var dfq = (dfreq > 0 ? dfreq : 1000);
					WaveGenerator.SetGen1(true, dfq, genVolt, true);    // left
					WaveGenerator.SetGen1(false, dfq, genVolt, true);   // right, for crosstalk
					WaveContainer.SetMono();  // use one channel of generator by default

					// get the data and convert it to response or gain values
					if (steps == 0)
					{
						await WaitForSynch(page, ttype, dfreq, genVolt);
					}
					await RunStep(page, ttype, dfreq, genVolt);

					if (PageData.FreqRslt != null && !vm.IsChirp)
					{
						// display the current data run in the miniplot
						QaLibrary.PlotMiniFftGraph(vm.Mini2Plot, PageData.FreqRslt, true, false);
						QaLibrary.PlotMiniTimeGraph(vm.MiniPlot, PageData.TimeRslt, dfreq, true, false);
					}
					page.GainFrequencies = page.GainFrequencies.Append(dfreq).ToArray();
					await showProgress(100 * (steps + 1) / stepBinFrequencies.Length, 100);
					if (!vm.IsTracking)
					{
						vm.RaiseMouseTracked("track");
					}
					// update the page data so the display updates on each step
					NextToData(page);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			WaveContainer.SetMono(); // usual default
			return true;
		}

		/// <summary>
		/// Determine the gain curve
		/// </summary>
		/// <param name="stepBinFrequencies">the frequencies to test at</param>
		/// <param name="voltagedBV">the sine generator voltage</param>
		/// <returns></returns>
		private async Task<bool> RunNoiseTest(MyDataTab page, AcquireStep rsStep, int index)
		{
			var vm = page.ViewModel;
			await showProgress(0, 50);
			// Check if cancel button pressed
			if (CanToken.IsCancellationRequested)
				return false;

			WaveContainer.SetMono();     // enable the generator
			WaveGenerator.SetGen2(true, 1000, 0, false); // disable the second wave left
			WaveGenerator.SetGen2(false, 1000, 0, false); // disable the second wave right
			var ttype = vm.GetTestingType(vm.TestType);
			try
			{
				page.GainData = ([], []); // new list of complex data
				page.GainFrequencies = []; // new list of frequencies
				if (CanToken.IsCancellationRequested)
					return false;
				//if(index == 0)
				//	await WaitForSynch(page, ttype, 1000, 0);
				var noiseRslt = await MeasureNoiseFreq(vm, vm.Averages, CanToken.Token, false);
				if(noiseRslt != null)
				{
					var perOctave = (vm.SmoothingVal <= 0) ? 0 : vm.SmoothingVal;       // octave smoothing by default
					var fl = noiseRslt.Left.Select(x => x / rsStep.Distgain).Skip(1).ToArray();
					var fr = noiseRslt.Right.Select(x => x / rsStep.Distgain).Skip(1).ToArray();
					//fl = MathUtil.SmoothAverage(fl, 1);
					//fr = MathUtil.SmoothAverage(fr, 1);
					fl = MathUtil.SmoothAverage(fl, perOctave);
					fr = MathUtil.SmoothAverage(fr, perOctave);
					// this gives us the noise level per sector, not the noise density
					// so multiply by ... 1/df
					var divisor = Math.Sqrt(noiseRslt.Df);
					fl = fl.Select(x => x / divisor).ToArray();
					fr = fr.Select(x => x / divisor).ToArray();
					var frqy = Enumerable.Range(1, noiseRslt.Left.Length-1).Select(x => x * noiseRslt.Df).ToArray();
					page.GainData = (fl, fr);
					page.GainFrequencies = frqy;    // unlog it
				}
				NextToData(page);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return true;
		}

		private async Task<(LeftRightSeries?, Complex[], Complex[])> RunChirpAcquire(MyDataTab page, double voltagedBV, WaveChannels channels)
		{
			var vm = page.ViewModel;

			var startf = ToD(vm.StartFreq) / 3;
			var endf = ToD(vm.EndFreq) * 3;
			endf = Math.Min(endf, vm.SampleRateVal / 2);
			var genv = QaLibrary.ConvertVoltage(voltagedBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			var chirpy = Chirps.ChirpVp(vm.FftSizeVal, vm.SampleRateVal, genv, startf, endf, 0.8);
			var ucLeft = chirpy;
			var blank = new double[chirpy.Length];
			var ucRight = (channels == WaveChannels.Both || channels == WaveChannels.Right) ? chirpy : blank;
			if (vm.GetTestingType(vm.TestType) == TestingType.Noise)
			{
				ucRight = (channels == WaveChannels.Both || channels == WaveChannels.Right) ? ucLeft : blank;
			}
			ucLeft = (channels == WaveChannels.Both || channels == WaveChannels.Left) ? ucLeft : blank;
			LeftRightSeries lfrs = await QaComm.DoAcquireUser(1, CanToken.Token, ucLeft, ucRight, false);
			if (lfrs?.TimeRslt == null)
				return (null, [], []);
			page.TimeRslt = lfrs.TimeRslt;
			if (CanToken.IsCancellationRequested)
				return (null, [], []);

			Complex[] leftFft = [];
			Complex[] rightFft = [];
			var flength = lfrs.TimeRslt.Left.Length / 2;        // we want half this since freq is symmetric

			var ttype = vm.GetTestingType(vm.TestType);
			var chans = WaveContainer.Singleton.Channels;
			if (ttype == TestingType.Noise)
			{
				var lft = Chirps.NormalizeChirpCplx(vm.WindowingMethod, chirpy, genv, (lfrs.TimeRslt.Left, lfrs.TimeRslt.Right));
				leftFft = lft.Item1;
				rightFft = lft.Item2;
			}
			else
			{
				// best?
				var window = QaMath.GetWindowType(vm.WindowingMethod);
				double[] lftF = window.Apply(lfrs.TimeRslt.Left, true);
				leftFft = FFT.Forward(lftF);

				double[] rgtF = window.Apply(lfrs.TimeRslt.Right, true);
				rightFft = FFT.Forward(rgtF);
			}

			var lrfs = new LeftRightFrequencySeries();
			lrfs.Df = QaLibrary.CalcBinSize(vm.SampleRateVal, vm.FftSizeVal); // frequency step size
			leftFft = leftFft.Take(flength).ToArray();
			rightFft = rightFft.Take(flength).ToArray();
			// set the freq values because ?
			lrfs.Left = leftFft.Select(x => x.Magnitude).ToArray();
			lrfs.Right = rightFft.Select(x => x.Magnitude).ToArray();

			lfrs.FreqRslt = lrfs;

			return (lfrs, leftFft, rightFft);
		}

		// let the chirp settle
		private async Task WaitForChirpSync(MyDataTab page, double voltagedBV, WaveChannels channels)
		{
			double delta = 2;
			double lastu = 1;
			int icnt = 0;
			while(delta < .9 || delta > 1.1)
			{
				var rca = await RunChirpAcquire(page, voltagedBV, channels);
				var lfrs = rca.Item1;
				double u = QaMath.MagAtFreq(lfrs?.FreqRslt?.Left ?? [], lfrs?.FreqRslt?.Df ?? 1, 1000);
				delta = lastu / u;
				lastu = u;
				if (++icnt > 0)
					break;
			}
			delta = 3;
		}

		public async Task<(LeftRightFrequencySeries?, List<Complex[]>, List<Complex[]>)> DoChirpTest(MyDataTab page, double voltagedBV, TestingType ttype, WaveChannels channels, int index)
		{
			var vm = page.ViewModel;
			List<Complex[]> leftFfts = new List<Complex[]>();
			List<Complex[]> rightFfts = new List<Complex[]>();
			LeftRightFrequencySeries lfrs = new();
			(LeftRightFrequencySeries?, List<Complex[]>, List<Complex[]>) failOut = (null, new List<Complex[]>(), new List<Complex[]>());
			try
			{

				if (index == 0)
					await WaitForChirpSync(page, voltagedBV, channels);

				// manually average the complex data here
				double df = 0.0;
				for (int i = 0; i < vm.Averages; i++)
				{
					await showMessage($"Measuring step {index + 1}.{i + 1}");
					var rca = await RunChirpAcquire(page, voltagedBV, channels);
					if (rca.Item2 == null || rca.Item3 == null || CanToken.IsCancellationRequested)
						return failOut;
					leftFfts.Add(rca.Item2);
					rightFfts.Add(rca.Item3);
					df = rca.Item1?.FreqRslt?.Df ?? 0.0;                // ???
				}
				if (df == 0)
					return failOut;

				var leftFft = leftFfts.First();
				var m2 = Math.Sqrt(2);
				var nca2 = vm.SampleRateVal; // (int)(0.01 + 1 / lfrs.TimeRslt.dt);      // total time in tics = sample rate

				// trim the three vectors to the frequency range of interest
				var gfr = Enumerable.Range(0, leftFft.Length).Select(x => x * df).ToArray();
				// restrict the data to only the frequency spectrum
				var startf = ToD(vm.StartFreq) / 3;
				var endf = ToD(vm.EndFreq) * 3;
				// we have garbage near Nyquist f so trim it off
				if (endf > vm.SampleRateVal / 2)
					endf = 0.95 * vm.SampleRateVal / 2;
				var trimf = gfr.Count(x => x < startf);
				var trimEnd = gfr.Count(x => x <= endf) - trimf;
				// trim them all
				gfr = gfr.Skip(trimf).Take(trimEnd).ToArray();
				// trim all of the arrays
				for (int i = 0; i < leftFfts.Count; i++)
				{
					leftFfts[i] = leftFfts[i].Skip(trimf).Take(trimEnd).ToArray();
					rightFfts[i] = rightFfts[i].Skip(trimf).Take(trimEnd).ToArray();
				}
				// format the gain vectors as desired
				page.GainFrequencies = gfr;
				List<double[]> leftGains = new List<double[]>();
				List<double[]> rightGains = new List<double[]>();
				var leftg = new double[leftFfts[0].Length];
				var rightg = new double[leftFfts[0].Length];
				var cnt = vm.Averages;
				switch (ttype)
				{
					case TestingType.Noise:
					case TestingType.Crosstalk:
						// left, right are magnitude. left uses right as reference
						for (int i = 0; i < vm.Averages; i++)
						{
							// here complex value is the fft data left / right
							leftGains.Add(leftFfts[i].Select(x => { return x.Magnitude; }).ToArray());
							rightGains.Add(rightFfts[i].Select(x => { return x.Magnitude; }).ToArray());
						}
						if (cnt > 1)
						{
							for (int i = 0; i < cnt; i++)
							{
								leftg = leftg.Zip(leftGains[i], (x, y) => x + y * y).ToArray(); // sum of squares
								rightg = rightg.Zip(rightGains[i], (x, y) => x + y * y).ToArray(); // sum of squares
							}
							leftg = leftg.Select(x => Math.Sqrt(x / cnt)).ToArray();
							rightg = rightg.Select(x => Math.Sqrt(x / cnt)).ToArray();
						}
						else
						{
							leftg = leftGains[0];
							rightg = rightGains[0];
						}
						lfrs.Left = leftg;
						lfrs.Right = rightg;
						lfrs.Df = df;
						break;
					case TestingType.PSRR:
					case TestingType.CMRR:
					case TestingType.GBW:
					case TestingType.InputZ:
						// here complex value is the fft data left / right
						for (int i = 0; i < vm.Averages; i++)
						{
							// divide left/right complex
							Complex[] cplxdiv;
							if(ttype == TestingType.GBW)
								cplxdiv = leftFfts[i].Zip(rightFfts[i], (l, r) => { return l / r; }).ToArray();
							else // cmrr and psrr use 1/x for positive db
								cplxdiv = leftFfts[i].Zip(rightFfts[i], (l, r) => { return r / l; }).ToArray();
							leftGains.Add(cplxdiv.Select(x => x.Real).ToArray());
							rightGains.Add(cplxdiv.Select(x => x.Imaginary).ToArray());
						}
						if (cnt > 1)
						{
							for (int i = 0; i < cnt; i++)
							{
								leftg = leftg.Zip(leftGains[i], (x, y) => x + y).ToArray(); // sum of real
								rightg = rightg.Zip(rightGains[i], (x, y) => x + y).ToArray(); // sum of imaginary
							}
							leftg = leftg.Select(x => (x / cnt)).ToArray();
							rightg = rightg.Select(x => (x / cnt)).ToArray();
						}
						else
						{
							leftg = leftGains[0];
							rightg = rightGains[0];
						}
						if(ttype == TestingType.CMRR && vm.QA430Cfg.Gain != 0)
						{
							var gain = Math.Abs(vm.QA430Cfg.Gain);
							// this is a 60dB gain block correction
							leftg = leftg.Select(x => x / gain).ToArray();
							rightg = rightg.Select(x => x / gain).ToArray();
						}
						lfrs.Left = leftg;
						lfrs.Right = rightg;
						lfrs.Df = df;
						//// this is left and right channel real,imaginary gain data where data = Left/Right
						//page.GainData = (leftg, rightg);
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
				return failOut;
			}
			return (lfrs, leftFfts, rightFfts);
		}

		/// <summary>
		/// figure out the configuration to use for testing type and possibly option#
		/// </summary>
		/// <param name="ttype"></param>
		/// <param name="option"></param>
		/// <returns></returns>
		private AcquireStep GetConfigForTest(TestingType ttype, int option)
		{
			QA430Model? model = Qa430Usb.Singleton?.QAModel;
			var acq = new AcquireStep() { Cfg = string.Empty };
			switch (ttype)
			{
				case TestingType.CMRR:
					acq = new AcquireStep() { Cfg = "Config2", Load = QA430Model.LoadOptions.R2000, Gain = 1000, Distgain = 1, SupplyP = 15, SupplyN = 15 };    // unity 6b with 101 dist gain
					break;
				case TestingType.Noise:
					acq = new AcquireStep() { Cfg = "Config3a", Load = QA430Model.LoadOptions.R2000, Gain = 1, Distgain = 1000, SupplyP = 15, SupplyN = 15 };    // unity 6b with 101 dist gain
					break;
				case TestingType.PSRR:
					acq = new AcquireStep() { Cfg = "Config9a", Load = QA430Model.LoadOptions.R2000, Gain = 1, Distgain = 1, SupplyP = 8, SupplyN = 8 };    // unity 6a with gnd in and lower voltage
					acq.ConnectPssr = QA430Model.PsrrOptions.ToHighRail;
					break;
				case TestingType.GBW:
					acq = new AcquireStep() { Cfg = "Config1", Load = QA430Model.LoadOptions.R2000, Gain = 1000, Distgain = 1, SupplyP = 15, SupplyN = 15 };    // unity 6b with 101 dist gain
					break;
				case TestingType.InputZ:
					if(option != 1)
						acq = new AcquireStep() { Cfg = "Config8a", Load = QA430Model.LoadOptions.R2000, Gain = 1, Distgain = 1, SupplyP = 15, SupplyN = 15 };    // unity 6b with 101 dist gain
					else
						acq = new AcquireStep() { Cfg = "Config8b", Load = QA430Model.LoadOptions.R2000, Gain = 1, Distgain = 1, SupplyP = 15, SupplyN = 15 };    // unity 6b with 101 dist gain
					break;
				default:
					break;
			}
			return acq;
		}

		private void DefaultQA430()
		{
			QA430Model? model = Qa430Usb.Singleton?.QAModel;
			model?.SetDefaults();
		}

		/// <summary>
		/// Determine the gain curve based on measurement start,end frequency
		/// </summary>
		/// <param name="voltagedBV">the sine generator voltage</param>
		/// <returns></returns>
		private async Task<bool> RunChirpTest(MyDataTab page, double voltagedBV, int index)
		{
			var vm = page.ViewModel;
			var perOctave = (vm.SmoothingVal <= 0) ? 0 : vm.SmoothingVal;       // octave smoothing by default
			if (perOctave > 0.5)
				perOctave = 0.5;

			// Check if cancel button pressed
			if (CanToken.IsCancellationRequested)
				return false;

			var ttype = vm.GetTestingType(vm.TestType);
			// these are all the same calculation just different names
			// here complex value is the fft data left / right
			//
			//if (ttype != TestingType.InputZ)
			{
				var rca = await DoChirpTest(page, voltagedBV, ttype, WaveChannels.Both, index);
				if (rca.Item1 != null)
				{
					var gl = rca.Item1.Left;
					var gr = rca.Item1.Right;
					// smooth the curve?
					if (perOctave > 0)
					{
						gl = MathUtil.SmoothAverage(gl, perOctave);
						gr = MathUtil.SmoothAverage(gr, perOctave);
					}
					page.GainData = (gl, gr);
				}
			}

			UpdateGraph(false);
			if (!vm.IsTracking)
			{
				vm.RaiseMouseTracked("track");
			}
			return true;
		}

		// given a left-right time series it finds the voltage at freq for both channels
		public static System.Numerics.Complex CalculateResponseAt(double fundamentalFreq, LeftRightSeries measuredSeries)
		{
			System.Numerics.Complex u = new Complex(1, 1);
			var measuredFreqSeries = measuredSeries.FreqRslt;
			if (measuredFreqSeries == null)
				return u;

			try
			{
				double left = QaMath.MagAtFreq(measuredFreqSeries.Left, measuredFreqSeries.Df, fundamentalFreq);
				double right = QaMath.MagAtFreq(measuredFreqSeries.Right, measuredFreqSeries.Df, fundamentalFreq);
				u = new Complex(left, right);   // pack it in stupidly
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return u;
		}

		private string GetTheTitle()
		{
			var frqrsVm = MyVModel;
			var ttype = frqrsVm.GetTestingType(frqrsVm.TestType);
			string title = string.Empty;
			switch (ttype)
			{
				case TestingType.Noise:
					title = "Noise Density";
					break;
				case TestingType.CMRR:
					title = "CMRR";
					break;
				case TestingType.PSRR:
					title = "PSRR";
					break;
				case TestingType.GBW:
					title = "Gain Bandwidth";
					break;
				case TestingType.InputZ:
					title = "Input Impedance";
					break;
				case TestingType.Crosstalk:
					title = "Crosstalk";
					break;
			}
			return title;
		}

		public void AddPhase(FrQa430ViewModel frqrsVm, Plot myPlot)
		{
			if (frqrsVm.ShowPhase)
			{
				PlotUtil.AddPhasePlot(myPlot);
				myPlot.Axes.Right.Label.Text = "Phase (Deg)";
				frqrsVm.ToShowPhase = Visibility.Visible;
			}
		}

		void HandleChangedProperty(ScottPlot.Plot myPlot, FrQa430ViewModel vm, string changedProp)
		{
			if (changedProp == "GraphStartX" || changedProp == "GraphEndX" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsX(Math.Log10(ToD(vm.GraphStartX, 20.0)), Math.Log10(ToD(vm.GraphEndX, 20000)), myPlot.Axes.Bottom);
			if (changedProp == "RangeBottomdB" || changedProp == "RangeTopdB" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsY(ToD(vm.RangeBottomdB, -20), ToD(vm.RangeTopdB, 180), myPlot.Axes.Left);
			if (changedProp == "PhaseBottom" || changedProp == "PhaseTop" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsY(ToD(vm.PhaseBottom, 20.0), ToD(vm.PhaseTop, 20.0), myPlot.Axes.Right);
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializePlot()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;

			PlotUtil.InitializeMagFreqPlot(myPlot);
			PlotUtil.SetOhmFreqRule(myPlot);
			PlotUtil.SetHeadingColor(vm.MainPlot.MyLabel);

			var ttype = vm.GetTestingType(vm.TestType);
			// as if no phase
			vm.ToShowPhase = Visibility.Collapsed;
			myPlot.Axes.Right.Label.Text = string.Empty;

			// now set it up
			switch (ttype)
			{
				case TestingType.Noise:
					myPlot.YLabel(GraphUtil.GetFormatTitle(vm.PlotFormat));
					break;
				case TestingType.GBW:
				case TestingType.CMRR:
				case TestingType.PSRR:
					//AddPhase(vm, myPlot);
					myPlot.YLabel("dB");
					break;
				case TestingType.InputZ:
					AddPhase(vm, myPlot);
					myPlot.YLabel("|Z| Ohms");
					break;
				case TestingType.Crosstalk:
					myPlot.YLabel("dB");
					break;
			}
			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			vm.MainPlot.Refresh();
		}

		/// <summary>
		/// unwrap the phase data
		/// </summary>
		/// <param name="phaseData">in degrees</param>
		/// <returns></returns>
		private double[] UnWrap(double[] phaseData)
		{
			double[] outPhase = new double[phaseData.Length];
			outPhase[0] = phaseData[0];
			// make it as continuous as possible
			for (int i = 1; i < phaseData.Length; i++)
			{
				// place the next phase value near this one
				outPhase[i] = phaseData[i];
				var dot = outPhase[i] - outPhase[i - 1];
				while (Math.Abs(dot) > 180)
				{
					outPhase[i] += (dot < 0) ? 360 : -360;
					dot = outPhase[i] - outPhase[i - 1];
				}
			}
			// center it at +-180 if possible
			if (outPhase.Min() >= 0 && outPhase.Max() > 180)
			{
				outPhase = outPhase.Select(x => x - 360).ToArray();
			}
			else if (outPhase.Min() < -180 && outPhase.Max() <= 0)
			{
				outPhase = outPhase.Select(x => x + 360).ToArray();
			}
			return outPhase;
		}


		/// <summary>
		/// Plot the magnitude graph
		/// </summary>
		/// <param name="measurementResult">Data to plot</param>
		void PlotValues(MyDataTab page, int measurementNr, bool isMain)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			int skipped = 0;

			if (page.GainLeft == null || page.GainFrequencies == null)
				return;

			var freqX = page.GainFrequencies;
			if (page.GainLeft.Length == 0 || freqX.Length == 0)
				return;

			if (freqX[0] == 0)
				skipped = 1;

			double[] logFreqX = freqX.Select(x => (x > 0) ? Math.Log10(x) : 1e-6).ToArray();
			float lineWidth = vm.ShowThickLines ? ViewSettings.Thickness : 1;
			float markerSize = vm.ShowPoints ? lineWidth + 3 : 1;

			var ttype = vm.GetTestingType(vm.TestType);

			double[] YValues = [];
			double[] phaseValues = [];
			string legendname = string.Empty;
			var gainReal = page.GainReal;
			var gainImag = page.GainImag;
			switch (ttype)
			{
				case TestingType.Crosstalk:
					{
						YValues = gainReal.Skip(skipped).Select(x => 20 * Math.Log10(x)).ToArray(); // real is the left gain
						phaseValues = gainImag.Skip(skipped).Select(x => 20 * Math.Log10(x)).ToArray();
						legendname = "dB";
					}
					break;
				case TestingType.GBW:
				case TestingType.PSRR:
				case TestingType.CMRR:
					{
						YValues = MathUtil.ToCplxMag(gainReal, gainImag).Select(x => 20 * Math.Log10(x)).ToArray();
						phaseValues = MathUtil.ToCplxPhase(gainReal, gainImag).Select(x => 180 * x / Math.PI).ToArray();
						legendname = Enum.GetName(ttype) ?? "Gain";
					}
					break;
				case TestingType.Noise:
					{
						var fvi = GraphUtil.ValueToPlotFn(vm, gainReal, page.GainFrequencies);
						YValues = gainReal.Select(fvi).ToArray();
						fvi = GraphUtil.ValueToPlotFn(vm, gainImag, page.GainFrequencies);
						phaseValues = gainImag.Select(fvi).ToArray();
						legendname = isMain ? "Left" : "L";
					}
					break;
				case TestingType.InputZ:
					{
						double rref = 100000;
						YValues = MathUtil.ToImpedanceMag(gainReal, gainImag).Select(x => rref * x).ToArray();
						phaseValues = MathUtil.ToImpedancePhase(gainReal, gainImag).Select(x => 180 * x / Math.PI).ToArray();
						legendname = "|Z| Ohms";
						if (myPlot.Axes.Rules.Count > 0)
						{
							var rule = myPlot.Axes.Rules.First();
							if (rule is MaximumBoundary)
							{
								// change to an impedance set of limits
								var myrule = ((MaximumBoundary)rule);
								var oldlimit = myrule.Limits;
								AxisLimits axs = new AxisLimits(oldlimit.Left, oldlimit.Right, 0, 10000000000);
								myrule.Limits = axs;
							}
						}
						//if (myPlot.Axes.Rules.Count > 1)
						//{
						//	var rule = myPlot.Axes.Rules.ElementAt(1);
						//	if (rule is MaximumBoundary)
						//	{
						//		// change to an impedance set of limits
						//		var myrule = ((MaximumBoundary)rule);
						//		var oldlimit = myrule.Limits;
						//		AxisLimits axs = new AxisLimits(oldlimit.Left, oldlimit.Right, -3600, 3600);
						//		myrule.Limits = axs;
						//	}
						//}
					}
					break;
			}
			//SetMagFreqRule(myPlot);
			var showPlot = isMain || page.Definition.IsOnL;
			var prefix = (measurementNr == 0) ? string.Empty : (ClipName(page.Definition.Name) + ".");

			SignalXY? plot = null;
			if ((ttype == TestingType.GBW || ttype == TestingType.InputZ || showPlot) && YValues.Length > 0)
			{
				plot = myPlot.Add.SignalXY(logFreqX.Skip(skipped).ToArray(), YValues.Skip(skipped).ToArray());
				plot.LineWidth = lineWidth;
				plot.Color = GraphUtil.GetPaletteColor(page.Definition.LeftColor, measurementNr * 2);
				plot.MarkerSize = markerSize;
				plot.LegendText = prefix + legendname;
				plot.LinePattern = LinePattern.Solid;
				plot.IsVisible = !MyVModel.HiddenLines.Contains(plot.LegendText);
				MyVModel.LegendInfo.Add(new MarkerItem(plot.LinePattern, plot.Color, plot.LegendText, measurementNr * 2, plot, vm.MainPlot, plot.IsVisible));
			}

			showPlot = (isMain && vm.ShowRight) || (!isMain && page.Definition.IsOnR);
			if ((ttype == TestingType.GBW || ttype == TestingType.InputZ) || showPlot)
			{
				var phases = phaseValues;
				plot = null;
				if (ttype == TestingType.GBW || ttype == TestingType.InputZ)
				{
					if (vm.ShowPhase)
					{
						phases = UnWrap(phaseValues);
						plot = myPlot.Add.SignalXY(logFreqX.Skip(skipped).ToArray(), phases.Skip(skipped).ToArray());
						plot.Axes.YAxis = myPlot.Axes.Right;
						plot.LegendText = prefix + "Phase (Deg)";
					}
				}
				else if (ttype == TestingType.Noise)
				{
					plot = myPlot.Add.SignalXY(logFreqX.Skip(skipped).ToArray(), phases.Skip(skipped).ToArray());
					plot.LegendText = prefix + (isMain ? "Right" : "R");
				}
				else // it's crosstalk
				{
					plot = myPlot.Add.SignalXY(logFreqX.Skip(skipped).ToArray(), phases.Skip(skipped).ToArray());
					plot.LegendText = prefix + "Right dB";
				}
				if (plot != null)
				{
					plot.LineWidth = lineWidth;
					plot.Color = GraphUtil.GetPaletteColor(page.Definition.RightColor, measurementNr * 2 + 1);
					plot.MarkerSize = markerSize;
					plot.LinePattern = LinePattern.Solid;
					plot.IsVisible = !MyVModel.HiddenLines.Contains(plot.LegendText);
					MyVModel.LegendInfo.Add(new MarkerItem(plot.LinePattern, plot.Color, plot.LegendText, measurementNr * 2 + 1, plot, vm.MainPlot, plot.IsVisible));
				}
			}

			vm.MainPlot.Refresh();
		}

		public void UpdateGraph(bool settingsChanged, string theProperty = "")
		{
			var vm = MyVModel;

			int resultNr = 0;

			if (settingsChanged)
			{
				PlotUtil.SetupMenus(vm.MainPlot.ThePlot, this, vm);
				InitializePlot();
				// do all
				HandleChangedProperty(vm.MainPlot.ThePlot, vm, "");
				PlotUtil.SetHeadingColor(vm.MainPlot.MyLabel);
			}
			else if (theProperty.Length > 0)
			{
				// if we're told which graph property changed...
				HandleChangedProperty(vm.MainPlot.ThePlot, vm, theProperty);
			}

			vm.UpdateMouseCursor(vm.LookX, 0);
			DrawPlotLines(resultNr);
		}

		public int DrawPlotLines(int resultNr)
		{
			var vm = MyVModel;
			MyVModel.LegendInfo.Clear();
			vm.MainPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			vm.MainPlot.ThePlot.Remove<SignalXY>();             // Remove all current lines
			var mainFirst = PlotZMain;
			var rnr = resultNr++;
			if (!mainFirst)
			{
				PlotValues(PageData, rnr, true);  // frqsrVm.GraphType);
			}
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null && other.Show != 0)
						PlotValues(other, resultNr++, false);
				}
			}
			if (mainFirst)
			{
				PlotValues(PageData, rnr, true);
			}

			vm.MainPlot.Refresh();
			return resultNr;
		}
	}
}
