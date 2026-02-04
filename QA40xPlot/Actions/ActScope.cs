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
	using MyDataTab = DataTab<ScopeViewModel>;

	public class ActScope : ActBase<ScopeViewModel>
	{
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document

		private float _Thickness = 2.0f;

		/// <summary>
		/// Constructor
		/// </summary>
		public ActScope(ScopeViewModel vm)
		{
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

		public LeftRightTimeSeries? GetResidual(MyDataTab page)
		{
			return page.GetProperty<LeftRightTimeSeries?>(ScopeViewModel.ResidualName);
		}

		public void SetResidual(MyDataTab page, LeftRightTimeSeries? lrts)
		{
			page.SetProperty(ScopeViewModel.ResidualName, lrts);
		}

		/// <summary>
		/// Create a blob for data export
		/// </summary>
		/// <returns></returns>
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();
			var ffs = PageData.TimeRslt;
			if (ffs == null || ffs.Left.Length == 0)
				return null;

			var vm = MyVModel;
			var sampleRate = MathUtil.ToUint(vm.SampleRate);
			var fftsize = ffs.Left.Length;
			if (vm.ShowRight && !vm.ShowLeft)
			{
				db.LeftData = ffs.Right.ToList();
			}
			else
			{
				db.LeftData = ffs.Left.ToList();
			}
			var frqs = Enumerable.Range(0, fftsize).ToList();
			var frequencies = frqs.Select(x => x * ffs.dt).ToList(); // .Select(x => x * binSize);
			db.FreqData = frequencies;  // time actually but w/e
			return db;
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
			else if (who == "XT")
			{
				myPlot = vm.MainPlot.ThePlot;
				var myAxis = myPlot.Axes.Bottom;

				// setting start seems to reset max...
				var minx = myAxis.Min;
				var maxx = myAxis.Max;
				vm.GraphEndX = maxx.ToString("0.###");
				vm.GraphStartX = minx.ToString("0.###");
			}
			else if (who == "YP")
			{
				myPlot = vm.MainPlot.ThePlot;
				var myAxis = myPlot.Axes.Left;
				var minx = myAxis.Min;
				var maxx = myAxis.Max;
				// setting start seems to reset max...
				if (maxx > 0.01)
				{
					vm.RangeTop = maxx.ToString("0.###");
					vm.RangeBottom = minx.ToString("0.###");
				}
				else
				{
					// if we set rangebottom first it adjust axis max so...
					vm.RangeTop = maxx.ToString("0.#####");
					vm.RangeBottom = minx.ToString("0.#####");
				}
			}
			else
			{
				PinGraphRanges(myPlot, vm, who);
			}
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<ScopeViewModel>(PageData, MyVModel, fileName);
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
			return Util.LoadFile<ScopeViewModel>(PageData, fileName);
		}

		public void UpdatePlotTitle()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			var title = "Scope";
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
			else
				myPlot.Title(title);
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
			BuildFrequencies(page);
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
									 //var oss = new OtherSet(page.Definition.Name, page.Show, page.Id);
				MyVModel.OtherSetList.Add(page.Definition);
			}
			UpdateGraph(true);
		}

		public void DoShowResiduals(bool isShown)
		{
			if (isShown)
			{
				if (!HasResidualPlot())
				{
					AddResidualPlot();
				}
				if (HasResidualPlot())
				{
					UpdateResidualPlot(PageData.TimeRslt);
				}
			}
			else
			{
				if (HasResidualPlot())
				{
					RemoveResidualPlot();
				}
			}
		}

		/// <summary>
		/// return the id of the residual plot datapage
		/// </summary>
		/// <returns></returns>
		public bool HasResidualPlot()
		{
			return (null != GetResidual(PageData));
		}

		public void RemoveResidualPlot()
		{
			SetResidual(PageData, null);
			UpdateGraph(true);
		}

		public void UpdateResidualPlot(LeftRightTimeSeries source)
		{
			var vm = MyVModel;
			var freq = vm.UseGenerator1 ? vm.NearestBinFreq(ToD(vm.Gen1Frequency, 0)) : 0.0;
			var lrts = QaMath.CalculateResidual(source, freq, 1000, vm.ResidualHarm);
			SetResidual(PageData, lrts);
		}

		public void AddResidualPlot()
		{
			var vm = MyVModel;
			var freq = vm.UseGenerator1 ? vm.NearestBinFreq(ToD(vm.Gen1Frequency, 0)) : 0.0;
			var lrts = QaMath.CalculateResidual(PageData.TimeRslt, freq, 1000, vm.ResidualHarm);
			SetResidual(PageData, lrts);
			AddResidualAxis(vm, vm.MainPlot.ThePlot);
		}

		/// <summary>
		/// create a wave
		/// </summary>
		/// <param name="page"></param>
		/// <param name="volts">generator voltage</param>
		/// <param name="force"></param>
		/// <returns></returns>
		private static double[] BuildWave(MyDataTab page, double volts, bool force = false)
		{
			var vm = page.ViewModel;
			var freq = vm.NearestBinFreq(ToD(vm.Gen1Frequency, 0));
			var freq2 = vm.NearestBinFreq(ToD(vm.Gen2Frequency, 0));
			// gen1 and gen2 are essentially scale factors for volts
			var v1 = GenVoltApplyUnit(vm.Gen1Voltage, vm.GenVoltageUnit, 1e-9);
			var v2 = GenVoltApplyUnit(vm.Gen2Voltage, vm.GenVoltageUnit, 1e-9);
			WaveContainer.SetMono(); // turn on the generator
			WaveGenerator.SetGen1(true, freq, volts, force ? true : vm.UseGenerator1, vm.Gen1Waveform);          // send a sine wave
			WaveGenerator.SetGen2(true, freq2, volts * v2 / v1, vm.UseGenerator2, vm.Gen2Waveform);          // send a sine wave
			WaveGenerator.SetWaveFile(true, vm.GenWavFile); // in case we're using a wave file
			var vsee1 = MathUtil.FormatVoltage(volts);
			var vsee2 = MathUtil.FormatVoltage(volts * v2 / v1);
			string vout = "";
			if (vm.UseGenerator1 && vm.UseGenerator2)
			{
				var uv1 = vm.GetGenVoltLine(volts);
				var uv2 = vm.GetGenVoltLine(volts * v2 / v1);
				vout = uv1 + "," + uv2;
			}
			else if (vm.UseGenerator1)
			{
				vout = vm.GetGenVoltLine(volts);
			}
			else if (vm.UseGenerator2)
			{
				vout = vm.GetGenVoltLine(volts * v2 / v1);
			}
			else
			{
				vout = "off"; // no output
			}
			MyVModel.GeneratorVoltage = vout; // set the generator voltage in the viewmodel
			return WaveGenerator.Generate(true, (uint)vm.SampleRateVal, (uint)vm.FftSizeVal); // generate the waveform
		}

		static void BuildFrequencies(MyDataTab page)
		{
			var vm = page.ViewModel;
			if (vm == null)
				return;

			LeftRightFrequencySeries? fseries;
			fseries = QaMath.CalculateSpectrum(page.TimeRslt, vm.WindowingMethod);  // do the fft and calculate the frequency response
			if (fseries != null)
			{
				page.SetProperty("FFT", fseries); // set the frequency response
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="atime">time on chart</param>
		/// <param name="posnV">Y of mouse in plot</param>
		/// <param name="useRight">which channel</param>
		/// <returns>a tuple of value, value in pct</returns>
		public ValueTuple<double, double> LookupXY(double atime, double posnV, bool useRight)
		{
			var timedata = PageData.TimeRslt;
			try
			{
				// get the data to look through
				var ffs = useRight ? timedata?.Right : timedata?.Left;
				if (timedata != null && ffs != null && ffs.Length > 0 && atime > 0)
				{
					int abin = (int)(atime / (1000 * timedata.dt));       // approximate bin in mS

					var vm = MyVModel;
					if (abin < ffs.Length)
					{
						return ValueTuple.Create(1000 * abin * timedata.dt, ffs[abin]);
					}
				}
			}
			catch (Exception)
			{
			}
			return ValueTuple.Create(0.0, 0.0);
		}

		public override Rect GetDataBounds()
		{
			if (PageData.TimeRslt.Left.Length == 0 && OtherTabs.Count() == 0)
				return new Rect(0, 0, 0, 0);

			var vm = MyVModel;     // current settings
			var ffs = PageData.TimeRslt;
			var hasdata = ffs.Left.Length > 0;

			Rect rrc = new Rect(0, 0, 0, 0);
			List<double[]> tabs = new List<double[]>();
			if (vm.ShowLeft && hasdata)
			{
				tabs.Add(ffs.Left);
			}
			if (vm.ShowRight && hasdata)
			{
				tabs.Add(ffs.Right);
			}
			var u = DataUtil.FindShownTimes(OtherTabs);
			if (u.Count > 0)
			{
				foreach (var item in u)
				{
					tabs.Add(item);
				}
			}

			if (tabs.Count == 0)
				return new Rect(0, 0, 0, 0);

			rrc.X = 0;
			rrc.Y = tabs.Min(x => x.Min());
			rrc.Width = 1000 * ffs.dt * tabs.First().Length - rrc.X;
			rrc.Height = tabs.Max(x => x.Max()) - rrc.Y;

			return rrc;
		}

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> RunAcquisition(MyDataTab msr, int iteration, CancellationToken ct)
		{
			// Setup
			ScopeViewModel thd = msr.ViewModel;

			var freq = ToD(thd.Gen1Frequency, 0);
			var sampleRate = thd.SampleRateVal;
			if (freq == 0 || sampleRate == 0 || !BaseViewModel.FftSizes.Contains(thd.FftSize))
			{
				MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = thd.FftSizeVal;

			// ********************************************************************  
			// Load a settings we want
			// ********************************************************************  
			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, thd.WindowingMethod, (int)thd.Attenuation))
				return false;

			LeftRightSeries lrfs = new();

			try
			{
				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
					return false;

				// do the noise floor acquisition and math
				// note measurenoise uses the existing init setup
				// except InputRange (attenuation) which is push/pop-ed
				// if (msr.NoiseFloor.Left == 0)
				//            {
				//	var noisy = await MeasureNoise(MyVModel, ct);
				//	if (ct.IsCancellationRequested)
				//		return false;
				//	msr.NoiseFloor = new LeftRightPair();
				//	msr.NoiseFloor.Left = QaCompute.CalculateNoise(noisy.FreqRslt, true);
				//	msr.NoiseFloor.Right = QaCompute.CalculateNoise(noisy.FreqRslt, false);
				//}

				var gains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				var gv = GenVoltApplyUnit(thd.Gen1Voltage, thd.GenVoltageUnit, 1e-5);
				var genVolt = thd.ToGenVoltage(gv, [], GEN_INPUT, gains);
				gv = GenVoltApplyUnit(thd.Gen2Voltage, thd.GenVoltageUnit, 1e-5);
				var genVolt2 = thd.ToGenVoltage(gv, [], GEN_INPUT, gains);
				if (genVolt > 5)
				{
					await showMessage($"Requesting input voltage of {genVolt} volts, check connection and settings");
					genVolt = 0.01;
				}
				double amplitudeSetpointdBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				msr.Definition.GeneratorVoltage = genVolt;  // used by the buildwave

				// ********************************************************************
				// Measure once
				// ********************************************************************
				// now do the step measurement
				var voltf = thd.GetGenVoltLine(genVolt);
				await showMessage($"{iteration:0} Measuring waveform with input of {voltf}.");

				var wave = BuildWave(msr, genVolt);   // also update the waveform variables
				lrfs = await QaComm.DoAcquireUser(msr.ViewModel.Averages, ct, wave, wave, false);

				if (lrfs.TimeRslt == null)
					return false;
				msr.TimeRslt = lrfs.TimeRslt;
				//BuildFrequencies(msr);      // do the relevant fft work
				await showProgress(90);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return true;
		}



		/// <summary>
		/// do post processing, just a bunch of easy math and moving stuff into viewmodels
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		private async Task<bool> PostProcess(MyDataTab msr, CancellationToken ct)
		{
			var thd = msr.ViewModel;
			try
			{
				UpdateGraph(false);
				if (!thd.IsTracking)
				{
					thd.RaiseMouseTracked("track");
				}
				MyVModel.HasExport = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			// Show message
			var leftInfo = ViewSettings.Singleton.ScopeInfoLeft;
			SetInfoChannels(msr);
			await showMessage("Measurement finished");
			await Task.Delay(1);    // let it be seen

			return !ct.IsCancellationRequested;
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

		public void AddResidualAxis(ScopeViewModel frqrsVm, Plot myPlot)
		{
			var axis = PlotUtil.AddSecondYR(myPlot, frqrsVm);
			var y2axis = frqrsVm.SecondYAxis;
			if (y2axis != null)
			{
				myPlot.Axes.SetLimitsY(ToD(frqrsVm.Range2Bottom, -10), ToD(frqrsVm.Range2Top, 10), y2axis);
			}
			frqrsVm.Y2AxisUnit = "mV";
			axis.Label.Text = "Residual (mV)";
			PlotUtil.SetStockAxis(myPlot, axis);
		}

		/// <summary>
		/// Plot the THD % graph
		/// </summary>
		/// <param name="data"></param>
		int PlotValues(MyDataTab page, int measurementNr, bool isMain)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			bool useLeft;   // dynamically update these
			bool useRight;
			if (isMain)
			{
				useLeft = vm.ShowLeft; // dynamically update these
				useRight = vm.ShowRight;
			}
			else
			{
				useLeft = page.Definition.IsOnL; // dynamically update these
				useRight = page.Definition.IsOnR;
			}

			var timeData = page.TimeRslt;
			if (timeData == null || timeData.Left.Length == 0)
				return measurementNr;

			double maxleft = timeData.Left.Max();
			double maxright = timeData.Right.Max();

			var timeX = Enumerable.Range(0, timeData.Left.Length).Select(x => x * 1000 * timeData.dt).ToArray(); // in ms
			var showThick = MyVModel.ShowThickLines;    // so it dynamically updates
			var markerSize = vm.ShowPoints ? (showThick ? _Thickness : 1) + 3 : 1;
			if (useLeft)
			{
				var pLeft = myPlot.Add.SignalXY(timeX, timeData.Left);
				pLeft.LineWidth = showThick ? _Thickness : 1;
				pLeft.Color = GraphUtil.GetPaletteColor(page.Definition.LeftColor, measurementNr);
				pLeft.MarkerSize = markerSize;
				pLeft.LegendText = isMain ? "Left" : ClipName(page.Definition.Name) + ".L";
				pLeft.IsVisible = !MyVModel.HiddenLines.Contains(pLeft.LegendText);
				MyVModel.LegendInfo.Add(new MarkerItem(LinePattern.Solid, pLeft.Color, pLeft.LegendText,
					measurementNr, pLeft, vm.MainPlot, pLeft.IsVisible));
			}
			measurementNr++;

			if (useRight)
			{
				var pRight = myPlot.Add.SignalXY(timeX, timeData.Right);
				pRight.LineWidth = showThick ? _Thickness : 1;
				pRight.Color = GraphUtil.GetPaletteColor(page.Definition.RightColor, measurementNr);
				pRight.MarkerSize = markerSize;
				pRight.LegendText = isMain ? "Right" : ClipName(page.Definition.Name) + ".R";
				pRight.IsVisible = !MyVModel.HiddenLines.Contains(pRight.LegendText);
				MyVModel.LegendInfo.Add(new MarkerItem(LinePattern.Solid, pRight.Color, pRight.LegendText,
					measurementNr, pRight, vm.MainPlot, pRight.IsVisible));
			}
			measurementNr++;

			if (isMain && HasResidualPlot())
			{
				var lrts = GetResidual(page);
				if (lrts != null)
				{
					if (useLeft)
					{
						var pLeft = myPlot.Add.SignalXY(timeX, lrts.Left);
						pLeft.LineWidth = showThick ? _Thickness : 1;
						pLeft.Color = GraphUtil.GetPaletteColor(null, measurementNr);
						pLeft.MarkerSize = markerSize;
						pLeft.LegendText = "Residual.L";
						pLeft.IsVisible = !MyVModel.HiddenLines.Contains(pLeft.LegendText);
						pLeft.Axes.YAxis = vm.SecondYAxis ?? myPlot.Axes.Left;
						MyVModel.LegendInfo.Add(new MarkerItem(LinePattern.Solid, pLeft.Color, pLeft.LegendText,
							measurementNr, pLeft, vm.MainPlot, pLeft.IsVisible));
					}
					measurementNr++;

					if (useRight)
					{
						var pRight = myPlot.Add.SignalXY(timeX, lrts.Right);
						pRight.LineWidth = showThick ? _Thickness : 1;
						pRight.Color = GraphUtil.GetPaletteColor(null, measurementNr * 2 + 1);
						pRight.MarkerSize = markerSize;
						pRight.LegendText = "Residual.R";
						pRight.Axes.YAxis = vm.SecondYAxis ?? myPlot.Axes.Left;
						pRight.IsVisible = !MyVModel.HiddenLines.Contains(pRight.LegendText);
						MyVModel.LegendInfo.Add(new MarkerItem(LinePattern.Solid, pRight.Color, pRight.LegendText,
							measurementNr, pRight, vm.MainPlot, pRight.IsVisible));
					}
					measurementNr++;
				}
			}
			else if (isMain)
			{
				// leave room for the two residuals
				measurementNr += 2;
			}

			vm.MainPlot.Refresh();
			return measurementNr;
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = vm.MainPlot.ThePlot;
			PlotUtil.InitializeMagTimePlot(myPlot);

			UpdatePlotTitle();
			myPlot.XLabel("Time (ms)");
			myPlot.YLabel("Voltage");

			vm.MainPlot.Refresh();
		}

		/// <summary>
		///  Start measurement button click
		/// </summary>
		public async Task DoMeasurement(bool repeat)
		{
			var scopeVm = MyVModel;
			if (!await StartAction(scopeVm))
				return;
			CanToken = new();

			// sweep data
			LeftRightTimeSeries lrts = new();
			MyDataTab NextPage = new(scopeVm, lrts);
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var vm = NextPage.ViewModel;
			if (vm == null)
				return;

			var genType = ToDirection(scopeVm.GenDirection);
			var freq = ToD(scopeVm.Gen1Frequency, 1000);
			// if we're doing adjusting here
			if (vm.DoAutoAttn || genType != E_GeneratorDirection.INPUT_VOLTAGE)
			{
				// show that we're autoing...
				await CalculateGainAtFreq(MyVModel, freq);
			}

			if (scopeVm.DoAutoAttn && LRGains != null)
			{
				var maxv = GenVoltApplyUnit(scopeVm.Gen1Voltage, scopeVm.GenVoltageUnit, 1e-3);
				var wave = BuildWave(NextPage, maxv, true);   // build a wave to evaluate the peak values
															  // get the peak voltages then fake an rms math div by 2*sqrt(2) = 2.828
															  // since I assume that's the hardware math
				var waveVOut = (wave.Max() - wave.Min()) / 2.828;
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
				var vinL = scopeVm.ToGenVoltage(waveVOut, [], GEN_INPUT, gains); // get gen1 input voltage
				double voutL = ToGenOutVolts(vinL, [], LRGains.Left);   // what is that as output voltage?
				double voutR = ToGenOutVolts(vinL, [], LRGains.Right);  // for both channels
				var vdbv = QaLibrary.ConvertVoltage(Math.Max(voutL, voutR), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				scopeVm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);             // find attenuation for both
				vm.Attenuation = scopeVm.Attenuation;   // update the scopeVm to update the gui, then this for the steps
			}

			int iteration = 1;
			// do the actual measurements
			var rslt = true;
			await showProgress(0);
			rslt = await RunAcquisition(NextPage, iteration++, CanToken.Token);
			if (rslt)
				rslt = await PostProcess(NextPage, CanToken.Token);

			if (rslt)
			{
				if (!ReferenceEquals(PageData, NextPage))
					PageData = NextPage;        // finally update the pagedata for display and processing
				UpdateGraph(true);
			}
			MyVModel.LinkAbout(PageData.Definition);  // ensure we're linked right during replays

			while (repeat && rslt && !CanToken.IsCancellationRequested)
			{
				// make sure the page data viewmodel is up to date
				if (PageData.ViewModel != null)
					PageData.ViewModel.LoadViewFrom(MyVModel);
				rslt = await RunAcquisition(PageData, iteration++, CanToken.Token);
				if (rslt)
				{
					rslt = await PostProcess(PageData, CanToken.Token);
					UpdateGraph(false);
				}
			}

			await showMessage("");
			scopeVm.HasExport = (PageData.TimeRslt.Left.Length > 0);
			await EndAction(scopeVm);
		}

		private void FillChannelInfo(ScopeInfoViewModel vm, double[] timeData)
		{
			vm.TotalVolts = QaCompute.ComputeRmsTime(timeData); // set the total volts
			vm.MaxVolts = timeData.Max(); // set the max Volts
			vm.MinVolts = timeData.Min(); // set the min Volts
			vm.PtPVolts = vm.MaxVolts - vm.MinVolts; // set the peak to peak Volts
			vm.TsDelay = QaMath.CalculateTimeDelay(PageData.TimeRslt); // set the time delay
			vm.PlotFormat = MyVModel.PlotFormat;
		}

		// show the latest step values in the table
		public void SetInfoChannels(MyDataTab tab)
		{
			var timeData = tab.TimeRslt;
			if (timeData == null || timeData.Left.Length == 0)
				return;
			var vm = ViewSettings.Singleton.ScopeInfoLeft;
			FillChannelInfo(vm, timeData.Left);
			vm.TsDelay = -vm.TsDelay; // left is negative delay
			vm = ViewSettings.Singleton.ScopeInfoRight;
			FillChannelInfo(vm, timeData.Right);
		}

		void HandleChangedProperty(ScottPlot.Plot myPlot, ScopeViewModel vm, string changedProp)
		{
			if (changedProp == "GraphStartX" || changedProp == "GraphEndX" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsX(ToD(vm.GraphStartX, 0), ToD(vm.GraphEndX, 10), myPlot.Axes.Bottom);
			if (changedProp == "RangeBottom" || changedProp == "RangeTop" || changedProp.Length == 0)
				myPlot.Axes.SetLimitsY(ToD(vm.RangeBottom, 1e-6), ToD(vm.RangeTop, 1), myPlot.Axes.Left);  // - 0.000001 to force showing label
			var y2axis = vm.SecondYAxis;
			if ((y2axis != null) && (changedProp == "Range2Bottom" || changedProp == "Range2Top" || changedProp.Length == 0))
				myPlot.Axes.SetLimitsY(ToD(vm.Range2Bottom, -20.0), ToD(vm.Range2Top, 20.0), y2axis);
		}

		public void UpdateGraph(bool settingsChanged, string theProperty = "")
		{
			var vm = MyVModel;
			vm.MainPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;

			if (settingsChanged)
			{
				var myPlot = vm.MainPlot.ThePlot;
				var y2axis = vm.SecondYAxis;
				if (y2axis != null)
				{
					//myPlot.Axes.Remove(y2axis);
					y2axis.Label.Text = string.Empty;
					y2axis = null;
					vm.SecondYAxis = null;
				}
				PlotUtil.SetupMenus(myPlot, this, vm);
				InitializeMagnitudePlot();
				PlotUtil.SetHeadingColor(vm.MainPlot.MyLabel);
				HandleChangedProperty(myPlot, vm, "");

			}
			else if (theProperty.Length > 0)
			{
				HandleChangedProperty(vm.MainPlot.ThePlot, vm, theProperty);
			}
			DoShowResiduals(vm.ShowResiduals);
			vm.UpdateMouseCursor(vm.LookX, vm.LookY);
			DrawPlotLines(resultNr); // draw the lines 
		}

		public int DrawPlotLines(int resultNr)
		{
			var vm = MyVModel;
			vm.MainPlot.ThePlot.Remove<SignalXY>();             // Remove all current lines
			vm.LegendInfo.Clear();
			resultNr = PlotValues(PageData, resultNr, true);
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null && other.Show != 0)
						resultNr = PlotValues(other, resultNr, false);
				}
			}
			vm.MainPlot.Refresh();
			return resultNr;
		}
	}
}

