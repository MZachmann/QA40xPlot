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

	public class ActScope : ActBase
	{
		public MyDataTab PageData { get; private set; } // Data used in this form instance
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document
		private readonly Views.PlotControl timePlot;

		private float _Thickness = 2.0f;
		private static ScopeViewModel MyVModel { get => ViewSettings.Singleton.ScopeVm; }

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

		/// <summary>
		/// Constructor
		/// </summary>
		public ActScope(Views.PlotControl graphFft)
		{
			timePlot = graphFft;
			ct = new CancellationTokenSource();
			PageData = new(MyVModel, new LeftRightTimeSeries());
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

		public void PinGraphRange(string who)
		{
			ScottPlot.Plot myPlot = timePlot.ThePlot;
			var vm = MyVModel;
			if (who == "XT")
			{
				myPlot = timePlot.ThePlot;
				// setting start seems to reset max...
				var minx = myPlot.Axes.Bottom.Min.ToString("0.##");
				vm.GraphEndX = myPlot.Axes.Bottom.Max.ToString("0.##");
				vm.GraphStartX = minx;
			}
			else if (who == "YM")
			{
				myPlot = timePlot.ThePlot;
				// setting start seems to reset max...
				var minx = myPlot.Axes.Left.Min.ToString("0.##");
				vm.RangeTop = myPlot.Axes.Left.Max.ToString("0.##");
				vm.RangeBottom = minx;
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
			ScottPlot.Plot myPlot = timePlot.ThePlot;
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
			await PostProcess(page, ct.Token);
			if (isMain)
			{
				// we can't overwrite the viewmodel since it links to the display proper
				// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				page.ViewModel.OtherSetList = MyVModel.OtherSetList;
				page.ViewModel.CopyPropertiesTo<ScopeViewModel>(MyVModel);    // retract the gui
				PageData = page;    // set the current page to the loaded one

				// relink to the new definition
				MyVModel.LinkAbout(PageData.Definition);
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

		private static double[] BuildWave(MyDataTab page, double volts, bool force = false)
		{
			var vm = page.ViewModel;
			var freq = ToD(vm.Gen1Frequency, 0);
			var freq2 = ToD(vm.Gen2Frequency, 0);
			var v2 = ToD(vm.Gen2Voltage, 1e-5);
			var v1 = ToD(vm.Gen1Voltage, 1e-5);
			WaveContainer.SetMono(); // turn on the generator
			WaveGenerator.SetGen1(true, freq, volts, force ? true : vm.UseGenerator1, vm.Gen1Waveform);          // send a sine wave
			WaveGenerator.SetGen2(true, freq2, volts * v2 / v1, vm.UseGenerator2, vm.Gen2Waveform);          // send a sine wave
			WaveGenerator.SetWaveFile(true, vm.GenWavFile);
			var vsee1 = MathUtil.FormatVoltage(volts);
			var vsee2 = MathUtil.FormatVoltage(volts * v2 / v1);
			string vout = "";
			if (vm.UseGenerator1 && vm.UseGenerator2)
			{
				vout = $"{vsee1}, {vsee2}";
			}
			else if (vm.UseGenerator1)
			{
				vout = vsee1;
			}
			else if (vm.UseGenerator2)
			{
				vout = vsee2;
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
			var vm = PageData.ViewModel;    // measurement settings
			if (PageData.TimeRslt.Left.Length == 0 && OtherTabs.Count() == 0)
				return new Rect(0, 0, 0, 0);

			var specVm = MyVModel;     // current settings
			var ffs = PageData.TimeRslt;
			var hasdata = ffs.Left.Length > 0;

			Rect rrc = new Rect(0, 0, 0, 0);
			List<double[]> tabs = new List<double[]>();
			if (specVm.ShowLeft && hasdata)
			{
				tabs.Add(ffs.Left);
			}
			if (specVm.ShowRight && hasdata)
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
		async Task<bool> RunAcquisition(MyDataTab msr, CancellationToken ct)
		{
			// Setup
			ScopeViewModel thd = msr.ViewModel;

			var freq = ToD(thd.Gen1Frequency, 1000);
			var freq2 = ToD(thd.Gen2Frequency, 1000);
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
				var genVolt = thd.ToGenVoltage(thd.Gen1Voltage, [], GEN_INPUT, gains);
				var genVolt2 = thd.ToGenVoltage(thd.Gen2Voltage, [], GEN_INPUT, gains);
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
				await showMessage($"Measuring spectrum with input of {genVolt:G3}V.");

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
			timePlot.ThePlot.Clear();
			timePlot.Refresh();
		}

		/// <summary>
		/// Plot the THD % graph
		/// </summary>
		/// <param name="data"></param>
		void PlotValues(MyDataTab page, int measurementNr, bool isMain)
		{
			ScottPlot.Plot myPlot = timePlot.ThePlot;
			var scopeVm = MyVModel;
			bool useLeft;   // dynamically update these
			bool useRight;
			if (isMain)
			{
				useLeft = scopeVm.ShowLeft; // dynamically update these
				useRight = scopeVm.ShowRight;
			}
			else
			{
				useLeft = page.Definition.IsOnL; // dynamically update these
				useRight = page.Definition.IsOnR;
			}

			var timeData = page.TimeRslt;
			if (timeData == null || timeData.Left.Length == 0)
				return;

			double maxleft = timeData.Left.Max();
			double maxright = timeData.Right.Max();

			var timeX = Enumerable.Range(0, timeData.Left.Length).Select(x => x * 1000 * timeData.dt).ToArray(); // in ms
			var showThick = MyVModel.ShowThickLines;    // so it dynamically updates
			var markerSize = scopeVm.ShowPoints ? (showThick ? _Thickness : 1) + 3 : 1;
			if (useLeft)
			{
				var pLeft = myPlot.Add.SignalXY(timeX, timeData.Left);
				pLeft.LineWidth = showThick ? _Thickness : 1;
				pLeft.Color = GraphUtil.GetPaletteColor(page.Definition.LeftColor, measurementNr * 2);
				pLeft.MarkerSize = markerSize;
				pLeft.LegendText = isMain ? "Left" : ClipName(page.Definition.Name) + ".L";
			}

			if (useRight)
			{
				var pRight = myPlot.Add.SignalXY(timeX, timeData.Right);
				pRight.LineWidth = showThick ? _Thickness : 1;
				pRight.Color = GraphUtil.GetPaletteColor(page.Definition.RightColor, measurementNr * 2 + 1);
				pRight.MarkerSize = markerSize;
				pRight.LegendText = isMain ? "Right" : ClipName(page.Definition.Name) + ".R";
			}

			timePlot.Refresh();
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot()
		{
			ScottPlot.Plot myPlot = timePlot.ThePlot;
			PlotUtil.InitializeMagTimePlot(myPlot);

			var thdFreq = MyVModel;

			myPlot.Axes.SetLimits(ToD(thdFreq.GraphStartX), ToD(thdFreq.GraphEndX),
				ToD(thdFreq.RangeBottom), ToD(thdFreq.RangeTop));

			UpdatePlotTitle();
			myPlot.XLabel("Time (mS)");
			myPlot.YLabel("Voltage");

			timePlot.Refresh();
		}

		/// <summary>
		///  Start measurement button click
		/// </summary>
		public async Task DoMeasurement()
		{
			var scopeVm = MyVModel;
			if (!await StartAction(scopeVm))
				return;
			ct = new();

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
				var maxv = ToD(scopeVm.Gen1Voltage, .001);
				var wave = BuildWave(NextPage, maxv, true);   // build a wave to evaluate the peak values
															  // get the peak voltages then fake an rms math div by 2*sqrt(2) = 2.828
															  // since I assume that's the hardware math
				var waveVOut = (wave.Max() - wave.Min()) / 2.828;
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
				var vinL = scopeVm.ToGenVoltage(waveVOut.ToString(), [], GEN_INPUT, gains); // get gen1 input voltage
				double voutL = ToGenOutVolts(vinL, [], LRGains.Left);   // what is that as output voltage?
				double voutR = ToGenOutVolts(vinL, [], LRGains.Right);  // for both channels
				var vdbv = QaLibrary.ConvertVoltage(Math.Max(voutL, voutR), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				scopeVm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);             // find attenuation for both
				vm.Attenuation = scopeVm.Attenuation;   // update the scopeVm to update the gui, then this for the steps
			}

			// do the actual measurements
			var rslt = true;
			await showProgress(0);
			rslt = await RunAcquisition(NextPage, ct.Token);
			if (rslt)
				rslt = await PostProcess(NextPage, ct.Token);

			if (rslt)
			{
				if (!ReferenceEquals(PageData, NextPage))
					PageData = NextPage;        // finally update the pagedata for display and processing
				UpdateGraph(true);
			}
			MyVModel.LinkAbout(PageData.Definition);  // ensure we're linked right during replays

			while (rslt && !ct.IsCancellationRequested)
			{
				if (PageData.ViewModel != null)
					MyVModel.CopyPropertiesTo(PageData.ViewModel);  // update the view model with latest settings
				rslt = await RunAcquisition(PageData, ct.Token);
				if (rslt)
				{
					rslt = await PostProcess(PageData, ct.Token);
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

		public void UpdateGraph(bool settingsChanged)
		{
			timePlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			var thd = MyVModel;

			if (settingsChanged)
			{
				InitializeMagnitudePlot();
			}
			DrawPlotLines(resultNr); // draw the lines 
		}

		public int DrawPlotLines(int resultNr)
		{

			timePlot.ThePlot.Remove<SignalXY>();             // Remove all current lines
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
			timePlot.Refresh();
			return resultNr;
		}
	}
}

