using FftSharp;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Extensions;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.AxisRules;
using ScottPlot.Plottables;
using System.Data;
using System.Numerics;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

namespace QA40xPlot.Actions
{
	using MyDataTab = DataTab<FreqRespViewModel>;

	public partial class ActFrequencyResponse : ActBase<FreqRespViewModel>
	{
		private List<MyDataTab> OtherTabs { get; set; } = new(); // Other tabs in the document

		private float _Thickness = 2.0f;

		/// <summary>
		/// Constructor
		/// </summary>
		public ActFrequencyResponse(FreqRespViewModel vm)
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
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
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
			if (who == "Y2")
			{
				var y2axis = vm.SecondYAxis;
				if (y2axis != null)
				{
					var u = y2axis.Min;
					var w = y2axis.Max;
					vm.Range2Top = w.ToString("G3");
					vm.Range2Bottom = u.ToString("G3");
				}
			}
			else if (who == "PH")
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
			return Util.SaveToFile<FreqRespViewModel>(PageData, MyVModel, fileName);
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
			return Util.LoadFile<FreqRespViewModel>(PageData, fileName);
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
				MyVModel.LoadViewFrom(page.ViewModel);
				PageData = page;    // set the current page to the loaded one

				// relink to the new definition
				var vm = MyVModel;
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
			return false;
		}

		private static void AddMicCorrection(MyDataTab page)
		{
			if (page == null || page.ViewModel == null)
				return;
			var vm = page.ViewModel;
			if (vm.UseMicCorrection)
			{
				var ttype = vm.GetTestingType(vm.TestType);
				if (ttype != TestingType.Response)
				{
					// only add mic correction for response tests
					return;
				}
				LeftRightFrequencySeries miccomp = Util.LoadMicCompensation();
				if (miccomp.Left != null && miccomp.Left.Length > 0)
				{
					var freqs = miccomp.Left;
					// we want inverse gain since the mic compensation file is frequency response data for the mic
					var gains = miccomp.Right.Select(x => QaLibrary.ConvertVoltage(-x, E_VoltageUnit.dBV, E_VoltageUnit.Volt)).ToArray();
					var crctdata = QaMath.LinearApproximate(freqs, gains, page.GainFrequencies); // interpolate the mic correction data
																								 // apply mic correction to the left channel
																								 //page.GainData = page.GainData.Zip(crctdata, (x, y) => x * y).ToArray();
					page.GainData = (page.GainLeft.Zip(crctdata, (x, y) => x * y).ToArray(),
						page.GainRight.Zip(crctdata, (x, y) => x * y).ToArray());

				}
			}
		}

		public async Task RunMeasurement(bool runContinuously)
		{
			CanToken = new();
			int index = 0;
			if (runContinuously)
			{
				await DoMeasurement(index++);
				while (!CanToken.IsCancellationRequested)
				{
					await DoMeasurement(index++);
				}
			}
			else
			{
				await DoMeasurement(0);
			}
			await showMessage("Finished.");
			MyVModel.HasExport = (PageData.GainFrequencies.Length > 0);
		}

		public void PrepGraph(MyDataTab page)
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
		public async Task DoMeasurement(int index)
		{
			if (index == 0)
			{
				await showProgress(0, 50);
			}
			await RunAcquisition(index);
		}

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async Task RunAcquisition(int index)
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
			MyDataTab NextPage = new(vm, lrts);
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var msr = NextPage.ViewModel;
			if (msr == null)
				return;

			// ********************************************************************
			// Setup the device
			if (msr.SampleRateVal == 0 || !FreqRespViewModel.FftSizes.Contains(msr.FftSize))
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

			// calculate gain to autoattenuate
			if ((index == 0 || LRGains == null) && needAttenuate)
			{
				await CalculateGainCurve(MyVModel, fmin, fmax);
				if (LRGains == null)
				{
					// cancelled?
					return;
				}
			}

			var gvolt = GenVoltApplyUnit(msr.Gen1Voltage, msr.GenVoltageUnit, 1e-9);
			var genVolt = gvolt;
			// calculate required attenuation
			if (needAttenuate && LRGains != null)
			{
				int[] frqtest = [LRGains.ToBinNumber(fmin), LRGains.ToBinNumber(fmax)];
				// to get attenuation, use a frequency of zero (all)
				// find the highest output voltage
				var genv = msr.ToGenVoltage(gvolt, frqtest, GEN_OUTPUT, LRGains.Left);                  // output v
				genv = Math.Max(genv, msr.ToGenVoltage(gvolt, frqtest, GEN_OUTPUT, LRGains.Right));    // output v
				var vdbv = QaLibrary.ConvertVoltage(genv, E_VoltageUnit.Volt, E_VoltageUnit.dBV);   // out dbv
				var attenuation = QaLibrary.DetermineAttenuation(vdbv);
				if (!msr.DoAutoAttn)
				{
					attenuation = (int)msr.Attenuation;
				}
				msr.Attenuation = attenuation;
				// get voltages for generator input based on GenDirection
				genVolt = msr.ToGenVoltage(gvolt, frqtest, GEN_INPUT, LRGains?.Left);
			}
			vm.Attenuation = msr.Attenuation; // display on-screen
			var voltagedBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);  // in dbv

			NextPage.Definition.GeneratorVoltage = genVolt; // save the actual generator voltage
			MyVModel.GeneratorVoltage = msr.GetGenVoltLine(genVolt); // save the actual generator voltage for display

			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, msr.WindowingMethod, (int)msr.Attenuation))
			{
				return;
			}

			try
			{
				// If in continous mode we continue sweeping until cancellation requested.
				NextPage.GainData = ([], []); // new list of complex data
				NextPage.GainFrequencies = [];

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

				if (!CanToken.IsCancellationRequested)
				{
					if (msr.IsChirp)
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
					AddMicCorrection(NextPage); // add mic correction if any
					PrepGraph(NextPage);
					var voltf = msr.GetGenVoltLine(genVolt);
					await showMessage($"Measuring step {index + 1} at {voltf} with attenuation {msr.Attenuation}.");
					await showProgress(100);
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
				case TestingType.Response:
					if (frsqVm.ShowRight && !frsqVm.ShowLeft)
					{
						db.LeftData = PageData.GainData.Item2.ToList();
					}
					else
					{
						db.LeftData = PageData.GainData.Item1.ToList();
					}
					break;
				case TestingType.Gain:
					db.LeftData = PageData.GainReal.Zip(PageData.GainImag, (x, y) => Math.Sqrt(x * x + y * y)).ToList();
					db.PhaseData = PageData.GainReal.Zip(PageData.GainImag, (x, y) => Math.Atan2(y, x)).ToList();
					break;
				case TestingType.Impedance:
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
		async Task<Complex> GetGain(double showfreq, FreqRespViewModel msr, TestingType ttype)
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
			var ga = CalculateGain(showfreq, lfrs, ttype == TestingType.Response); // gain,phase or gain1,gain2
			return ga;
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
			if (ttype == TestingType.Response || ttype == TestingType.Crosstalk)
			{
			}
			else if (ttype == TestingType.Gain)
			{
				var phaseValues = MathUtil.ToCplxPhase(gainReal, gainImag).Select(x => 180 * x / Math.PI).ToArray();
				var phases = UnWrap(phaseValues);
				rrc.Y = phases.Min();
				rrc.Height = phases.Max() - rrc.Y;
			}
			else if (ttype == TestingType.Impedance)
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
			var vmr = PageData.GainFrequencies; // test data
			var ttype = vm.GetTestingType(vm.TestType);
			var msdre = PageData.GainReal;
			var msdim = PageData.GainImag;

			if (vmr == null || vmr.Length == 0 || msdre == null || msdim == null)
				return Rect.Empty;

			Rect rrc = new Rect(0, 0, 0, 0);

			rrc.X = vmr.Min();
			rrc.Width = vmr.Max() - rrc.X;
			if (ttype == TestingType.Response || ttype == TestingType.Crosstalk)
			{
				if (vm.ShowLeft)
				{
					rrc.Y = msdre.Min();
					rrc.Height = msdre.Max() - rrc.Y;
					if (vm.ShowRight)
					{
						rrc.Y = Math.Min(msdre.Min(), msdim.Min());
						rrc.Height = Math.Max(msdre.Max(), msdim.Max()) - rrc.Y;
					}
				}
				else if (vm.ShowRight)
				{
					rrc.Y = msdim.Min();
					rrc.Height = msdim.Max() - rrc.Y;
				}
			}
			else if (ttype == TestingType.Gain)
			{
				var mags = MathUtil.ToCplxMag(msdre, msdim);
				rrc.Y = mags.Min();
				rrc.Height = mags.Max() - rrc.Y;
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
			return rrc;
		}

		/// <summary>
		/// return (freq, real, imag, group_delay)
		/// </summary>
		/// <param name="freq"></param>
		/// <returns></returns>
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
					case TestingType.Response:
						// send freq, gain, gain2
						{
							var fvi = GraphUtil.ValueToPlotFn(frsqVm, PageData.GainReal, PageData.GainFrequencies);
							var fvi2 = GraphUtil.ValueToPlotFn(frsqVm, PageData.GainImag, PageData.GainFrequencies);
							var fl = fvi(valuesRe[bin]);
							var fr = fvi2(valuesIm[bin]);
							tup = ValueTuple.Create(myFreq, fl, fr, 0.0);
						}
						break;
					case TestingType.Impedance:
						{   // send freq, ohms, phasedeg
							double rref = ToD(frsqVm.ZReference, 10);
							var impval = MathUtil.ToImpedanceMag(valuesRe[bin], valuesIm[bin]);
							var ohms = rref * impval;
							impval = MathUtil.ToImpedancePhase(valuesRe[bin], valuesIm[bin]);
							var gd = PageData.DelayRslt;
							tup = ValueTuple.Create(myFreq, ohms, 180 * impval / Math.PI, (gd == null) ? 0.0 : gd.ElementAt(bin));
						}
						break;
					case TestingType.Gain:
						{
							// send freq, gain, phasedeg
							var mag = MathUtil.ToCplxMag(valuesRe[bin], valuesIm[bin]);
							var phas = MathUtil.ToCplxPhase(valuesRe[bin], valuesIm[bin]);
							var gd = PageData.DelayRslt;
							tup = ValueTuple.Create(myFreq, mag, 180 * phas / Math.PI, (gd == null) ? 0.0 : gd.ElementAt(bin));
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
			var bvm = basevm as FreqRespViewModel;
			if (parameter == null || bvm == null)
			{
				return;
			}
			var axisType = parameter.ToString();
			Rect bounds = Rect.Empty;
			switch (axisType)
			{
				case "Y2":

					break;
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
				case "Y2":  // group delay
					var rsl = PageData.DelayRslt;
					if (rsl != null && rsl.Length > 0)
					{
						bvm.Range2Bottom = rsl.Min().ToString("G3");
						bvm.Range2Top = rsl.Max().ToString("G3");
					}
					break;
				case "YM":  // Y magnitude
					var ttype = bvm.GetTestingType(bvm.TestType);
					if (ttype == TestingType.Impedance)
					{
						bvm.RangeBottomdB = bounds.Y.ToString("G3");
						bvm.RangeTopdB = (bounds.Height + bounds.Y).ToString("G3");
					}
					else if (ttype != TestingType.Response)
					{
						bvm.RangeBottomdB = (20 * Math.Log10(Math.Max(1e-14, bounds.Y))).ToString("G3");
						bvm.RangeTopdB = Math.Ceiling((20 * Math.Log10(Math.Max(1e-14, bounds.Height + bounds.Y)))).ToString("G3");
					}
					else
					{
						var dref = GraphUtil.GetDbrReference(bvm, bvm.ShowLeft ? PageData.GainLeft : PageData.GainRight,
							PageData.GainFrequencies);
						var botx = GraphUtil.ValueToLogPlot(bvm, bounds.Y, dref);
						bvm.RangeBottomdB = Math.Floor(botx).ToString("G3");
						var topx = GraphUtil.ValueToLogPlot(bvm, bounds.Y + bounds.Height, dref);
						bvm.RangeTopdB = Math.Ceiling(topx).ToString("G3");
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
				page.GainData = (page.GainReal.Append(total.Real).ToArray(), page.GainImag.Append(total.Imaginary).ToArray());
				//page.GainData = page.GainData.Append(total / vm.Averages).ToArray();
			}
			else
			{
				await showMessage(string.Format("Checking + {0:0}", dfreq));   // need a delay to actually see it
				var ga = await GetGain(dfreq, vm, ttype);
				//page.GainData = page.GainData.Append(ga).ToArray();
				page.GainData = (page.GainReal.Append(ga.Real).ToArray(), page.GainImag.Append(ga.Imaginary).ToArray());

			}
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
					// crosstalk doesn't support riaa preemph
					if (vm.IsRiaa && (ttype != TestingType.Crosstalk))
					{
						var gv = genVolt;
						// scale to max unity gain at highest freq
						var gvmax = RiaaTransform.Fvalue3(stepBinFrequencies.Last());
						gv = gv * RiaaTransform.Fvalue3(dfreq) / gvmax;
						// enable both generators here with one riaa preemph
						WaveGenerator.SetGen1(true, dfreq, gv, true);
						WaveGenerator.SetGen1(false, dfreq, (ttype != TestingType.Gain) ? gv : genVolt, true);
						WaveContainer.SetStereo();  // use both channels of generator
					}
					else
					{   // not riaa
						var dfq = (dfreq > 0 ? dfreq : 1000);
						WaveGenerator.SetGen1(true, dfq, genVolt, true);    // left
						WaveGenerator.SetGen1(false, dfq, genVolt, true);   // right, for crosstalk
						WaveContainer.SetMono();  // use one channel of generator by default
					}
					if (ttype == TestingType.Crosstalk)
					{
						// each one adds a step so carefully....
						// run it once, read the gain data, then run it over again the other direction
						// here GainRight = the response right channel, GainLeft = left channel response
						var exData = page.GainData; // save the data
						var exFreq = page.GainFrequencies; // save the frequencies
						WaveContainer.SetStereo();      // use both channels
						WaveContainer.SetEnabled(false, true);
						await RunStep(page, TestingType.Response, dfreq, genVolt);      // get gain of both channels
						var gainLeft = page.GainLeft.Last() / Math.Max(1e-10, page.GainRight.Last());   // signal was sent on left channel

						page.GainData = exData; // new list of complex data
						page.GainFrequencies = exFreq; // new list of frequencies
						WaveContainer.SetEnabled(true, false);
						await RunStep(page, TestingType.Response, dfreq, genVolt);      // get gain of both channels
						var gainRight = page.GainRight.Last() / Math.Max(1e-10, page.GainLeft.Last());  // signal was sent on right channel
																										// merge the data and update the GainData Property
						var idxread = page.GainLeft.Length - 1;
						page.GainData.Item1[idxread] = gainLeft; // set the gain data to the new crosstalk gain
						page.GainData.Item2[idxread] = gainRight; // set the gain data to the new crosstalk gain
					}
					else
					{
						await RunStep(page, ttype, dfreq, genVolt);
					}
					if (PageData.FreqRslt != null && !vm.IsChirp)
					{
						QaLibrary.PlotMiniFftGraph(vm.Mini2Plot, PageData.FreqRslt, true, false);
						QaLibrary.PlotMiniTimeGraph(vm.MiniPlot, PageData.TimeRslt, dfreq, true, false);
					}
					page.GainFrequencies = page.GainFrequencies.Append(dfreq).ToArray();
					await showProgress(100 * (steps + 1) / stepBinFrequencies.Length, 100);
					if (!vm.IsTracking)
					{
						vm.RaiseMouseTracked("track");
					}
					PrepGraph(page);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			WaveContainer.SetMono(); // usual default
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
			if (vm.IsRiaa)
			{
				// time domain filtering to riaa preemphasis
				var bq = BiquadBuilder.BuildRiaaBiquad(vm.SampleRateVal, false);
				ucLeft = chirpy.Select(x => bq.Process(x)).ToArray();
			}
			var blank = new double[chirpy.Length];
			var ucRight = (channels == WaveChannels.Both || channels == WaveChannels.Right) ? chirpy : blank;
			if (vm.GetTestingType(vm.TestType) == TestingType.Response)
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
			if (ttype == TestingType.Response)
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

		public async Task<bool> DoChirpTest(MyDataTab page, double voltagedBV, TestingType ttype, WaveChannels channels, int index)
		{
			var vm = page.ViewModel;
			try
			{
				List<Complex[]> leftFfts = new List<Complex[]>();
				List<Complex[]> rightFfts = new List<Complex[]>();
				// manually average the complex data here
				double df = 0.0;
				for (int i = 0; i < vm.Averages; i++)
				{
					await showMessage($"Measuring step {index + 1}.{i + 1}");
					var rca = await RunChirpAcquire(page, voltagedBV, channels);
					if (rca.Item2 == null || rca.Item3 == null || CanToken.IsCancellationRequested)
						return false;
					leftFfts.Add(rca.Item2);
					rightFfts.Add(rca.Item3);
					df = rca.Item1?.FreqRslt?.Df ?? 0.0;                // ???
				}
				if (df == 0)
					return false;

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
					case TestingType.Crosstalk:
					case TestingType.Response:
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
						page.GainData = (leftg, rightg);
						break;
					case TestingType.Gain:
					case TestingType.Impedance:
						// here complex value is the fft data left / right
						for (int i = 0; i < vm.Averages; i++)
						{
							var cplxdiv = leftFfts[i].Zip(rightFfts[i], (l, r) => { return l / r; });
							leftGains.Add(cplxdiv.Select(x => x.Real).ToArray());
							rightGains.Add(cplxdiv.Select(x => x.Imaginary).ToArray());
						}
						if (cnt > 1)
						{
							for (int i = 0; i < cnt; i++)
							{
								leftg = leftg.Zip(leftGains[i], (x, y) => x + y).ToArray(); // sum of squares
								rightg = rightg.Zip(rightGains[i], (x, y) => x + y).ToArray(); // sum of squares
							}
							leftg = leftg.Select(x => (x / cnt)).ToArray();
							rightg = rightg.Select(x => (x / cnt)).ToArray();
						}
						else
						{
							leftg = leftGains[0];
							rightg = rightGains[0];
						}
						page.GainData = (leftg, rightg);
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}
			return true;
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

			var didRun = false;
			double[] gainLeft;
			double[] gainRight;

			var ttype = vm.GetTestingType(vm.TestType);
			if (ttype != TestingType.Crosstalk)
			{
				didRun = await DoChirpTest(page, voltagedBV, ttype, WaveChannels.Both, index);
				if (perOctave > 0)
				{
					// smooth the curve?
					gainLeft = MathUtil.SmoothAverage(page.GainLeft, perOctave);
					gainRight = MathUtil.SmoothAverage(page.GainRight, perOctave);
					page.GainData = (gainLeft, gainRight);
				}
			}
			else
			{
				page.GainData = ([], []); // new list of complex data
				page.GainFrequencies = []; // new list of frequencies
										   // left->right crosstalk
				didRun = await DoChirpTest(page, voltagedBV, TestingType.Response, WaveChannels.Right, index);
				if (perOctave > 0)
				{
					// smooth the curve?
					var gl = MathUtil.SmoothAverage(page.GainLeft, perOctave);
					var gr = MathUtil.SmoothAverage(page.GainRight, perOctave);
					page.GainData = (gl, gr);
				}
				gainLeft = page.GainLeft.Zip(page.GainRight, (x, y) => x / Math.Max(1e-10, y)).ToArray();
				if (!CanToken.IsCancellationRequested)
				{
					// right->left crosstalk
					page.GainData = ([], []); // new list of complex data
					page.GainFrequencies = []; // new list of frequencies
					didRun = await DoChirpTest(page, voltagedBV, TestingType.Response, WaveChannels.Left, index);
					if (!CanToken.IsCancellationRequested)
					{
						if (perOctave > 0)
						{
							// smooth the curve?
							var gl = MathUtil.SmoothAverage(page.GainLeft, perOctave);
							var gr = MathUtil.SmoothAverage(page.GainRight, perOctave);
							page.GainData = (gl, gr);
						}
						gainRight = page.GainRight.Zip(page.GainLeft, (x, y) => x / Math.Max(1e-10, y)).ToArray();
						page.GainData = (gainLeft, gainRight);
					}
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


		private Complex CalculateGain(double dFreq, LeftRightSeries data, bool showBoth)
		{
			Complex gain = new();
			if (showBoth)
				gain = CalculateResponseAt(dFreq, data);
			else
				gain = QaMath.CalculateGainPhase(dFreq, data);
			return gain;
		}

		private string GetTheTitle()
		{
			var frqrsVm = MyVModel;
			var ttype = frqrsVm.GetTestingType(frqrsVm.TestType);
			string title = string.Empty;
			switch (ttype)
			{
				case TestingType.Response:
					title = "Frequency Response";
					break;
				case TestingType.Gain:
					title = "Gain";
					break;
				case TestingType.Impedance:
					title = "Impedance";
					break;
				case TestingType.Crosstalk:
					title = "Crosstalk";
					break;
			}
			return title;
		}

		public void AddPhase(FreqRespViewModel frqrsVm, Plot myPlot)
		{
			if (frqrsVm.ShowPhase)
			{
				PlotUtil.AddPhasePlot(myPlot);
				myPlot.Axes.Right.Label.Text = "Phase (Deg)";
				frqrsVm.ToShowPhase = Visibility.Visible;
			}
			if (frqrsVm.ShowGroupDelay)
			{
				PlotUtil.AddGroupDelay(frqrsVm, myPlot);
			}
		}

		private static double ToBrightness(Color clr)
		{
			// BT.601 Y = 0.299 R + 0.587 G + 0.114 B
			return clr.R * 0.299 + clr.G * 0.587 + clr.B * 0.114;
		}

		private void AddCustomMarker()
		{

		}

		void HandleChangedProperty(ScottPlot.Plot myPlot, FreqRespViewModel vm, string changedProp)
		{
			if (changedProp == "GraphStartX" || changedProp == "GraphEndX" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsX(Math.Log10(ToD(vm.GraphStartX, 20.0)), Math.Log10(ToD(vm.GraphEndX, 20000)), myPlot.Axes.Bottom);
			if (changedProp == "RangeBottomdB" || changedProp == "RangeTopdB" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsY(ToD(vm.RangeBottomdB, -20), ToD(vm.RangeTopdB, 180), myPlot.Axes.Left);
			if (changedProp == "PhaseBottom" || changedProp == "PhaseTop" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsY(ToD(vm.PhaseBottom, 20.0), ToD(vm.PhaseTop, 20.0), myPlot.Axes.Right);
			var y2axis = vm.SecondYAxis;
			if ((y2axis != null) && (changedProp == "Range2Bottom" || changedProp == "Range2Top" || changedProp.Length == 0))
				myPlot.Axes.SetLimitsY(ToD(vm.Range2Bottom, -20.0), ToD(vm.Range2Top, 20.0), y2axis);
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
			var y2axis = vm.SecondYAxis;
			if (y2axis != null)
			{
				myPlot.Axes.Remove(y2axis);
				vm.SecondYAxis = null;
				y2axis = null;
			}
			PageData.DelayRslt = null;

			// now set it up
			switch (ttype)
			{
				case TestingType.Response:
					myPlot.YLabel(GraphUtil.GetFormatTitle(vm.PlotFormat));
					break;
				case TestingType.Gain:
					AddPhase(vm, myPlot);
					myPlot.YLabel("dB");
					break;
				case TestingType.Impedance:
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
			float lineWidth = vm.ShowThickLines ? _Thickness : 1;
			float markerSize = vm.ShowPoints ? lineWidth + 3 : 1;

			var ttype = vm.GetTestingType(vm.TestType);

			double[] YValues = [];
			double[] phaseValues = [];
			double rref = ToD(vm.ZReference, 10);
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
				case TestingType.Gain:
					{
						YValues = MathUtil.ToCplxMag(gainReal, gainImag).Select(x => 20 * Math.Log10(x)).ToArray();
						phaseValues = MathUtil.ToCplxPhase(gainReal, gainImag).Select(x => 180 * x / Math.PI).ToArray();
						legendname = "Gain";
					}
					break;
				case TestingType.Response:
					{
						var fvi = GraphUtil.ValueToPlotFn(vm, gainReal, page.GainFrequencies);
						YValues = gainReal.Select(fvi).ToArray();
						fvi = GraphUtil.ValueToPlotFn(vm, gainImag, page.GainFrequencies);
						phaseValues = gainImag.Select(fvi).ToArray();
						legendname = isMain ? "Left" : "L";
					}
					break;
				case TestingType.Impedance:
					{
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
								AxisLimits axs = new AxisLimits(oldlimit.Left, oldlimit.Right, 0, 2000);
								myrule.Limits = axs;
							}
						}
						if (myPlot.Axes.Rules.Count > 1)
						{
							var rule = myPlot.Axes.Rules.ElementAt(1);
							if (rule is MaximumBoundary)
							{
								// change to an impedance set of limits
								var myrule = ((MaximumBoundary)rule);
								var oldlimit = myrule.Limits;
								AxisLimits axs = new AxisLimits(oldlimit.Left, oldlimit.Right, -3600, 3600);
								myrule.Limits = axs;
							}
						}
					}
					break;
			}
			//SetMagFreqRule(myPlot);
			var showPlot = isMain || page.Definition.IsOnL;
			var prefix = (measurementNr == 0) ? string.Empty : (ClipName(page.Definition.Name) + ".");

			SignalXY? plot = null;
			if ((ttype == TestingType.Gain || ttype == TestingType.Impedance) || showPlot)
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
			if ((ttype == TestingType.Gain || ttype == TestingType.Impedance) || showPlot)
			{
				var phases = phaseValues;
				plot = null;
				if (ttype == TestingType.Gain || ttype == TestingType.Impedance)
				{
					if (vm.ShowPhase)
					{
						phases = UnWrap(phaseValues);
						plot = myPlot.Add.SignalXY(logFreqX.Skip(skipped).ToArray(), phases.Skip(skipped).ToArray());
						plot.Axes.YAxis = myPlot.Axes.Right;
						plot.LegendText = prefix + "Phase (Deg)";
					}
				}
				else if (ttype == TestingType.Response)
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

				if (isMain && vm.ShowGroupDelay && (phases.Length > 1) && (ttype == TestingType.Impedance || ttype == TestingType.Gain))
				{
					// note phases is unwrapped
					double[] gdelay = new double[phases.Length];
					for (int i = 1; i < phases.Length; i++)
					{
						// in degrees / (degrees / second) == seconds
						// so convert to ms
						gdelay[i] = 1000 * (phases[i - 1] - phases[i]) / (360 * (freqX[i] - freqX[i - 1]));
					}
					gdelay[0] = gdelay[1];  // may as well
											// save it in the pagedata for other uses
					page.DelayRslt = gdelay;
					// now plot it and add it to the legend
					plot = myPlot.Add.SignalXY(logFreqX.Skip(skipped).ToArray(), gdelay.Skip(skipped).ToArray());
					plot.Axes.YAxis = vm.SecondYAxis ?? myPlot.Axes.Left;
					plot.LegendText = prefix + "Group Delay (ms)";
					plot.LineWidth = lineWidth;
					plot.MarkerSize = markerSize;
					plot.IsVisible = !MyVModel.HiddenLines.Contains(plot.LegendText);
					MyVModel.LegendInfo.Add(new MarkerItem(plot.LinePattern, plot.Color, plot.LegendText, measurementNr * 2 + 2, plot, vm.MainPlot, plot.IsVisible));
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
			PlotValues(PageData, resultNr++, true);  // frqsrVm.GraphType);
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null && other.Show != 0)
						PlotValues(other, resultNr++, false);
				}
			}

			vm.MainPlot.Refresh();
			return resultNr;
		}
	}
}
