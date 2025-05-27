using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

// this is the top level class for the Intermodulation test
// the code that runs the test and analyzes the results

namespace QA40xPlot.Actions
{
	using MyDataTab = DataTab<ImdViewModel>;

	public class ActImd : ActBase
    {
		public MyDataTab PageData { get; private set; } // Data used in this form instance
		private List<MyDataTab> OtherTabs { get; set; } = new List<MyDataTab>(); // Other tabs in the document
		private readonly Views.PlotControl fftPlot;

		private float _Thickness = 2.0f;
		private static ImdViewModel MyVModel { get => ViewSettings.Singleton.ImdVm; }

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

		/// <summary>
		/// Constructor
		/// </summary>
		public ActImd(Views.PlotControl graphFft)
        {
			fftPlot = graphFft;
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

		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();

			var vfs = PageData.FreqRslt;
			if (vfs == null)
				return null;

			var imdVm = MyVModel;
			var sampleRate = MathUtil.ToUint(imdVm.SampleRate);
			var fftsize = vfs.Left.Length;
			var binSize = QaLibrary.CalcBinSize(sampleRate, (uint)fftsize);
			if(imdVm.ShowRight && ! imdVm.ShowLeft)
			{
				db.LeftData = vfs.Right.ToList();
			}
			else
			{
				db.LeftData = vfs.Left.ToList();
			}
			// db.RightData = vf[0].fftData.Right.ToList();
			var frqs = Enumerable.Range(0, fftsize).ToList();
			var frequencies = frqs.Select(x => x * binSize).ToList(); // .Select(x => x * binSize);
			db.FreqData = frequencies;
			return db;
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<ImdViewModel>(PageData, fileName);
		}

		public async Task LoadFromFile(string fileName, bool isMain)
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
			return Util.LoadFile<ImdViewModel>(PageData, fileName);
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

			if( isMain)
			{
				// we can't overwrite the viewmodel since it links to the display proper
				// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				PageData.ViewModel.OtherSetList = MyVModel.OtherSetList;
				PageData.ViewModel.CopyPropertiesTo<ImdViewModel>(MyVModel);    // retract the gui
				PageData = page;    // set the current page to the loaded one
									// relink to the new definition
				MyVModel.LinkAbout(PageData.Definition);
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

		private static double[] BuildWave(MyDataTab page, double volts)
		{ 
			var vm = page.ViewModel;
			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 0);
			var freq2 = MathUtil.ToDouble(vm.Gen2Frequency, 0);
			var v2 = volts / vm.GenDivisor;
			var v1 = volts;
			WaveGenerator.SetGen1(freq, v1, vm.UseGenerator);          // send a sine wave
			WaveGenerator.SetGen2(freq2, v2, vm.UseGenerator2);          // send a sine wave
			WaveGenerator.SetEnabled(true); // turn on the generator
			return WaveGenerator.Generate((uint)vm.SampleRateVal, (uint)vm.FftSizeVal); // generate the waveform
		}

		void BuildFrequencies(MyDataTab page)
		{
			var vm = page.ViewModel;
			if (vm == null)
				return;

			LeftRightFrequencySeries? fseries;
			fseries = QaMath.CalculateSpectrum(page.TimeRslt, vm.WindowingMethod);  // do the fft and calculate the frequency response
			if (fseries != null)
			{
				fseries = CalculateAverages(fseries, vm.Averages);
				page.FreqRslt = fseries; // set the frequency response
			}
		}

		/// <summary>
		/// find the nearest data point to the mouse
		/// </summary>
		/// <param name="freq">frequency on chart</param>
		/// <param name="posndBV">Y of mouse in plot</param>
		/// <param name="useRight">which channel</param>
		/// <returns>a tuple of df, value, value in pct</returns>
		public ValueTuple<double, double, double> LookupXY(double freq, double posndBV, bool useRight)
		{
			var fftdata = PageData.FreqRslt;
			if (freq <= 0 || fftdata == null || PageData == null)
				return ValueTuple.Create(0.0, 0.0, 0.0);
			try
			{
				// get the data to look through
				var ffs = useRight ? fftdata?.Right : fftdata?.Left;
				if (fftdata != null && ffs != null && ffs.Length > 0 && freq < fftdata.Df * ffs.Length)
				{
					int bin = 0;
					ScottPlot.Plot myPlot = fftPlot.ThePlot;
					var pixel = myPlot.GetPixel(new Coordinates(Math.Log10(freq), posndBV));
					var left = ViewSettings.Singleton.ImdChannelLeft;
					var right = ViewSettings.Singleton.ImdChannelRight;

					// get screen coords for some of the data
					int abin = (int)(freq / fftdata.Df);       // apporoximate bin
					var binmin = Math.Max(1, abin - 5);            // random....
					var binmax = Math.Min(ffs.Length - 1, abin + 5);           // random....
					var msr = PageData.ViewModel;
					var vfi = GraphUtil.GetLogFormatter(msr.PlotFormat, useRight ? right.Fundamental1Volts : left.Fundamental1Volts);
					var distsx = ffs.Skip(binmin).Take(binmax - binmin);
					IEnumerable<Pixel> distasx = distsx.Select((fftd, index) =>
							myPlot.GetPixel(new Coordinates(Math.Log10((index + binmin) * fftdata.Df),
									vfi(ffs[binmin + index]))));
					var distx = distasx.Select(x => Math.Pow(x.X - pixel.X, 2) + Math.Pow(x.Y - pixel.Y, 2));
					var dlist = distx.ToList(); // no dc
					bin = binmin + dlist.IndexOf(dlist.Min());

					var vm = MyVModel;
					if (bin < ffs.Length)
					{
						var vfun = useRight ? right.Fundamental1Volts : left.Fundamental1Volts;
						return ValueTuple.Create(bin * fftdata.Df, ffs[bin], vfun);
					}
				}
			}
			catch (Exception)
			{
			}
			return ValueTuple.Create(0.0, 0.0, 0.0);
		}


		public Rect GetDataBounds()
		{
			var vm = PageData.ViewModel;    // measurement settings
			if (PageData.FreqRslt == null && OtherTabs.Count() == 0)
				return new Rect(0, 0, 0, 0);

			var specVm = MyVModel;     // current settings
			var ffs = PageData.FreqRslt;

			Rect rrc = new Rect(0, 0, 0, 0);
			List<double[]> tabs = new List<double[]>();
			if (specVm.ShowLeft && ffs != null)
			{
				tabs.Add(ffs.Left);
			}
			if (specVm.ShowRight && ffs != null)
			{
				tabs.Add(ffs.Right);
			}
			var u = DataUtil.FindShownFreqs(OtherTabs);
			if (u.Count > 0)
			{
				foreach (var item in u)
				{
					tabs.Add(item);
				}
			}

			if (tabs.Count == 0)
				return new Rect(0, 0, 0, 0);

			rrc.X = ffs?.Df ?? 1.0; // ignore 0
			rrc.Y = tabs.Min(x => x.Min());
			rrc.Width = (ffs?.Df ?? 1) * tabs.First().Length - rrc.X;
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
			ImdViewModel msrImd = msr.ViewModel;

			var freq = MathUtil.ToDouble(msrImd.Gen1Frequency, 0);
			var freq2 = MathUtil.ToDouble(msrImd.Gen2Frequency, 0);
			var sampleRate = msrImd.SampleRateVal;
			if (freq == 0 || freq2 == 0 || sampleRate == 0 || !BaseViewModel.FftSizes.Contains(msrImd.FftSize))
			{
				MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = msrImd.FftSizeVal;
			freq = QaLibrary.GetNearestBinFrequency(freq, sampleRate, fftsize); // make sure it's a bin center frequency
			freq2 = QaLibrary.GetNearestBinFrequency(freq2, sampleRate, fftsize); // make sure it's a bin center frequency
			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, msrImd.WindowingMethod, (int)msrImd.Attenuation))
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
				if (msr.NoiseFloor.Left == 0)
				{
					var noisy = await MeasureNoise(ct);
					if (ct.IsCancellationRequested)
						return false;
					msr.NoiseFloor = new LeftRightPair();
					msr.NoiseFloor.Right = QaCompute.CalculateNoise(noisy.FreqRslt, true);
					msr.NoiseFloor.Left = QaCompute.CalculateNoise(noisy.FreqRslt, false);
				}

				var gains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				int[] frqtest = [ToBinNumber(freq, LRGains)];
				var genVolt = msrImd.ToGenVoltage(msrImd.Gen1Voltage, frqtest, GEN_INPUT, gains);   // input voltage 1

				msr.Definition.GeneratorVoltage = genVolt;	// used by the buildwave

				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
					return false;

				// ********************************************************************
				// Measure once
				// ********************************************************************
				// now do the step measurement
				await showMessage($"Measuring spectrum.");
				await showProgress(0);

				var wave = BuildWave(msr, genVolt);   // also update the waveform variables
				lrfs = await QaComm.DoAcquireUser(1, ct, wave, wave, false);

				if (lrfs.TimeRslt == null)
					return false;

				msr.TimeRslt = lrfs.TimeRslt;
				await showProgress(50);
				BuildFrequencies(msr);      // do the relevant fft work
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
			if (msr.FreqRslt == null)
			{
				await showMessage("No frequency result");
				return false;
			}

			// left and right channels summary info to fill in
			var left = new ImdChannelViewModel();
			left.IsLeft = true;
			var right = new ImdChannelViewModel();
			right.IsLeft = false;
			ImdViewModel vm = msr.ViewModel;

			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 0);
			var freq2 = MathUtil.ToDouble(vm.Gen2Frequency, 0);
			var lrfs = msr.FreqRslt;    // frequency response

			var maxf = 20000; // the app seems to use 20,000 so not sampleRate/ 2.0;
			LeftRightPair snrdb = QaCompute.GetSnrDb(lrfs, freq, 20.0, maxf);
			LeftRightPair thds = QaCompute.GetThdDb(lrfs, freq, 20.0, maxf);
			LeftRightPair thdN = QaCompute.GetThdnDb(lrfs, freq, 20.0, maxf);
			LeftRightPair snrimdb = QaCompute.GetSnrImdDb(lrfs, freq, freq2);

			ImdChannelViewModel[] steps = [left, right];
			foreach (var step in steps)
			{
				bool isleft = step.IsLeft;
				var frq = isleft ? msr.FreqRslt.Left : msr.FreqRslt.Right;

				step.BorderColor = isleft ? System.Windows.Media.Brushes.Blue : System.Windows.Media.Brushes.Red;
				step.Fundamental1Frequency = freq;
				step.Fundamental2Frequency = freq2;
				step.Generator1Volts = msr.Definition.GeneratorVoltage;
				var x = QaMath.MagAtFreq(frq, msr.FreqRslt.Df, freq);
				step.Fundamental1Volts = x;
				x = QaMath.MagAtFreq(frq, msr.FreqRslt.Df, freq2);
				step.Fundamental2Volts = x;
				step.SNRatio = isleft ? snrimdb.Left : snrimdb.Right;
				step.ENOB = (step.SNRatio - 1.76) / 6.02;
				step.ThdNInV = step.Fundamental1Volts * QaLibrary.ConvertVoltage(isleft ? thdN.Left : thdN.Right, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
				step.NoiseFloorV = (isleft ? msr.NoiseFloor.Left : msr.NoiseFloor.Right);
				step.NoiseFloorPct = 100 * step.NoiseFloorV / step.Fundamental1Volts;
				step.ThdInV = step.Fundamental1Volts * QaLibrary.ConvertVoltage(isleft ? thds.Left : thds.Right, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
				step.ThdInPercent = 100 * step.ThdInV / step.Fundamental1Volts;
				step.ThdNInPercent = 100 * step.ThdNInV / step.Fundamental1Volts;
				step.ThdNIndB = isleft ? thdN.Left : thdN.Right;
				step.ThdIndB = isleft ? thds.Left : thds.Right;
				step.Gain1dB = 20 * Math.Log10(step.Fundamental1Volts / Math.Max(1e-10, step.Generator1Volts));
				step.Gain2dB = 20 * Math.Log10(step.Fundamental2Volts / Math.Max(1e-10, step.Generator2Volts));
				var rmsV = Math.Sqrt(step.Fundamental1Volts * step.Fundamental1Volts + step.Fundamental2Volts * step.Fundamental2Volts);
				step.TotalV = rmsV;
				step.TotalW = rmsV * rmsV / ViewSettings.AmplifierLoad;
				step.ShowDataPercents = vm.ShowDataPercent;
				step.NoiseFloorView = GraphUtil.DoValueFormat(vm.PlotFormat, step.NoiseFloorV, step.Fundamental1Volts);
				step.Amplitude1View = GraphUtil.DoValueFormat(vm.PlotFormat, step.Fundamental1Volts, step.Fundamental1Volts);
				step.Amplitude2View = GraphUtil.DoValueFormat(vm.PlotFormat, step.Fundamental2Volts, step.Fundamental1Volts);
				step.AmplitudesView = GraphUtil.DoValueFormat(vm.PlotFormat, rmsV, rmsV);
			}

			CalculateHarmonics(msr, left, right);

			// we're nearly done
			msr.SetProperty("Left", left);
			msr.SetProperty("Right", right);

			// CalculateDistortion(msr, left, right);

			// Show message
			await showMessage($"Measurement finished");

			return !ct.IsCancellationRequested;
		}

		private void AddAMarker(MyDataTab page, double frequency, bool isred = false)
		{
			var vm = page.ViewModel;

			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			var sampleRate = vm.SampleRateVal;
			var fftsize = vm.FftSizeVal;
			int bin = (int)QaLibrary.GetBinOfFrequency(frequency, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
			var leftData = page.FreqRslt?.Left;
			var rightData = page.FreqRslt?.Right;

			double markVal = 0;
			if (rightData != null && !vm.ShowLeft)
			{
				double maxright = rightData.Max();
				markVal = GraphUtil.ReformatValue(vm.PlotFormat, rightData[bin], maxright);
			}
			else if (leftData != null)
			{
				double maxleft = leftData.Max();
				markVal = GraphUtil.ReformatValue(vm.PlotFormat, leftData[bin], maxleft);
			}
			var markView = GraphUtil.IsPlotFormatLog(vm.PlotFormat) ? markVal : Math.Log10(markVal);

			ScottPlot.Color markerCol = new ScottPlot.Color();
			if (!vm.ShowLeft)
			{
				markerCol = isred ? Colors.Green : Colors.DarkGreen;
			}
			else
			{
				markerCol = isred ? Colors.Red : Colors.DarkOrange;
			}
			var mymark = myPlot.Add.Marker(Math.Log10(frequency), markView,
				MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
			mymark.LegendText = string.Format("{1}: {0:F1}", GraphUtil.PrettyPrint(markVal, vm.PlotFormat), (int)frequency);
		}

		private void ShowHarmonicMarkers(MyDataTab page)
		{
			var vm = page.ViewModel;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowMarkers)
			{
				ImdChannelViewModel? thdView = null;
				if (vm.ShowLeft)
					thdView = page.GetProperty("Left") as ImdChannelViewModel;
				else if (vm.ShowRight)
					thdView = page.GetProperty("Right") as ImdChannelViewModel;
				var maxfreq = vm.SampleRateVal / 2.0;
				if (thdView != null)
				{
					AddAMarker(page, thdView.Fundamental1Frequency);
					AddAMarker(page, thdView.Fundamental2Frequency);
					var flist = thdView.Harmonics.OrderBy(x => x.Frequency).ToArray();
					var cn = flist.Length;
					for (int i = 0; i < cn; i++)
					{
						var frq = flist[i].Frequency;
						if ((frq > 0 && frq < maxfreq))
						{
							AddAMarker(page, frq);
						}
					}
				}
			}
		}

		private void ShowPowerMarkers(MyDataTab page)
		{
			var vm = page.ViewModel;
			if (!vm.ShowLeft && !vm.ShowRight)
				return;

			List<double> freqchecks = new List<double> { 50, 60, 100, 120, 180, 150 };
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowPowerMarkers)
			{
				var sampleRate = vm.SampleRateVal;
				var fftsize = vm.FftSizeVal;
				double fsel = 0;
				var fftdata = vm.ShowLeft ? page.FreqRslt?.Left : page.FreqRslt?.Right;
				if (fftdata == null)
					return;
				// find if 50 or 60hz is higher, indicating power line frequency
				//double maxdata = -10;
				//foreach (double freq in freqchecks)
				//{
				//	var data = QaMath.MagAtFreq(fftdata, vm.FftSizeVal, freq);
				//	if (data > maxdata)
				//	{
				//		fsel = (freq == 50 || freq == 100 || freq==100) ? 50 : 60;
				//	}
				//}
				fsel = ToD(ViewSettings.Singleton.SettingsVm.PowerFrequency); // 50 or 60hz
				if (fsel < 10)
					fsel = 60;
				// check 4 harmonics of power frequency
				for (int i = 1; i < 5; i++)
				{
					var data = QaMath.MagAtFreq(fftdata, vm.FftSizeVal, fsel*i);
					double udif = 20 * Math.Log10(data);
					AddAMarker(page, fsel*i, true);
				}
			}
		}

		private void Addif(ref List<double> frqs, double dval )
		{
			if( dval > 0)
				frqs.Add(dval);
		}

		private double[] MakeHarmonics(double f1, double f2)
		{
			List<double> harmFreqs = new List<double>();
			if( f1 > f2)
			{
				var a = f2;
				f2 = f1;
				f1 = f2;
			}
			var hf = harmFreqs.Append( f2 - f1);
			hf = hf.Append( f2 + f1);
			hf = hf.Append( 2*f1 - f2);
			hf = hf.Append( 2*f2 - f1);
			hf = hf.Append( 3*f1 - 2*f2);
			hf = hf.Append( 3*f2 - 2*f1);
			hf = hf.Append( 4 * f1 - 3 * f2);
			hf = hf.Append( 4 * f2 - 3 * f1);
			hf = hf.Append( 3 * f2 - f1);
			hf = hf.Append( 3 * f1 - f2);
			return hf.ToArray();
		}

        /// <summary>
        /// Clear the plot
        /// </summary>
        void ClearPlot()
        {
            fftPlot.ThePlot.Clear();
            fftPlot.Refresh();
        }

		string GetTheTitle(ScottPlot.Plot myPlot)
		{
			var imdVm = MyVModel;
			if( imdVm.IntermodType == "Custom")
				return "Intermodulation Distortion";
			else
			{
				var vsa = imdVm.IntermodType.Split('(').First();
				return String.Format("{0} Intermodulation Distortion", vsa);
			}
		}

		/// <summary>
		/// Plot all of the spectral data values
		/// </summary>
		/// <param name="data"></param>
		void PlotValues(MyDataTab? page, int measurementNr, bool isMain)
		{
			if (page == null)
				return;

			ScottPlot.Plot myPlot = fftPlot.ThePlot;

			var specVm = MyVModel;
			bool useLeft;
			bool useRight;
			if (isMain)
			{
				useLeft = specVm.ShowLeft; // dynamically update these
				useRight = specVm.ShowRight;
			}
			else
			{
				useLeft = page.Definition.IsOnL; // dynamically update these
				useRight = page.Definition.IsOnR;
			}

			var fftData = page.FreqRslt;
			if (fftData == null)
				return;

			double[] freqLogX = Enumerable.Range(1, fftData.Left.Length - 1).
								Select(x => Math.Log10(x * fftData.Df)).ToArray();
			//
			double[] leftdBV = [];
			double[] rightdBV = [];
			string plotForm = MyVModel.PlotFormat;

			// add a scatter plot to the plot
			var lineWidth = MyVModel.ShowThickLines ? _Thickness : 1;   // so it dynamically updates

			if (useLeft)
			{
				double maxleft = Math.Max(1e-20, fftData.Left.Max());
				// the usual dbv display
				var fvi = GraphUtil.GetLogFormatter(plotForm, maxleft);
				leftdBV = fftData.Left.Skip(1).Select(fvi).ToArray();

				Scatter plotLeft = myPlot.Add.Scatter(freqLogX, leftdBV);
				plotLeft.LineWidth = lineWidth;
				plotLeft.Color = GraphUtil.GetPaletteColor(page.Definition.LeftColor, measurementNr * 2);
				plotLeft.MarkerSize = 1;
			}

			if (useRight)
			{
				// find the max value of the left and right channels
				double maxright = Math.Max(1e-20, fftData.Right.Max());
				// now use that to calculate percents. Since Y axis is logarithmic use log of percent
				var fvi = GraphUtil.GetLogFormatter(plotForm, maxright);
				rightdBV = fftData.Right.Skip(1).Select(fvi).ToArray();

				Scatter plotRight = myPlot.Add.Scatter(freqLogX, rightdBV);
				plotRight.LineWidth = lineWidth;
				plotRight.Color = GraphUtil.GetPaletteColor(page.Definition.RightColor, measurementNr * 2 + 1);
				plotRight.MarkerSize = 1;
			}

			fftPlot.Refresh();
		}

		public void UpdateGraph(bool settingsChanged)
		{
			fftPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			fftPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			ImdViewModel thd = MyVModel;

			if (settingsChanged)
			{
				if (GraphUtil.IsPlotFormatLog(thd.PlotFormat))
				{
					InitializeMagnitudePlot(thd.PlotFormat);
				}
				else
				{
					InitializefftPlot(thd.PlotFormat);
				}
			}

			ShowPageInfo(PageData);

			PlotValues(PageData, resultNr++, true);
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null)
						PlotValues(other, resultNr, false);
					resultNr++;	// consistency
				}
			}

			if (PageData.FreqRslt != null)
			{
				ShowHarmonicMarkers(PageData);
				ShowPowerMarkers(PageData);
			}

			fftPlot.Refresh();
		}

		private void ShowPageInfo(MyDataTab page)
		{
			List<ImdChannelViewModel?> channels = new();
			var specVm = MyVModel;  // the active viewmodel
			if (specVm.ShowLeft)
			{
				var mdl = page.GetProperty("Left") as ImdChannelViewModel;
				if (mdl != null)
				{
					channels.Add(mdl);
					mdl.BorderColor = System.Windows.Media.Brushes.Blue;
				}
			}
			if (specVm.ShowRight)
			{
				var mdl = page.GetProperty("Right") as ImdChannelViewModel;
				if (mdl != null)
				{
					channels.Add(mdl);
					mdl.BorderColor = System.Windows.Media.Brushes.Red;
				}
			}
			if (channels.Count < 2 && OtherTabs.Count > 0)
			{
				var seen = DataUtil.FindShownInfo<ImdViewModel, ImdChannelViewModel>(OtherTabs);
				if (seen.Count > 0)
				{
					var mdl = seen[0];
					if (mdl != null)
					{
						channels.Add(mdl);
						mdl.BorderColor = System.Windows.Media.Brushes.DarkGreen;
					}
				}
				if (channels.Count < 2 && seen.Count > 1)
				{
					var mdl = seen[1];
					if (mdl != null)
					{
						channels.Add(mdl);
						mdl.BorderColor = System.Windows.Media.Brushes.DarkOrange;
					}
				}
			}


			if (channels.Count > 0)
			{
				channels[0].CopyPropertiesTo(ViewSettings.Singleton.ImdChannelLeft);  // clone to our statics
			}
			if (channels.Count > 1)
			{
				channels[1].CopyPropertiesTo(ViewSettings.Singleton.ImdChannelRight);
			}
		}

		/// <summary>
		///  Start measurement button click
		/// </summary>
		public async Task DoMeasurement()
        {
			var imdVm = MyVModel;
			if (! await StartAction(imdVm))
				return; 
            ct = new();

			// sweep data
			LeftRightTimeSeries lrts = new();
			MyDataTab NextPage = new(imdVm, lrts);
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var vm = NextPage.ViewModel;
			if (vm == null)
				return;

			var genType = ToDirection(vm.GenDirection);
			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 1000);
			var freq2 = MathUtil.ToDouble(vm.Gen2Frequency, 1000);
			// calculate the gain curve if we need it
			if (vm.DoAutoAttn || genType != E_GeneratorDirection.INPUT_VOLTAGE)
			{
				await CalculateGainCurve(MyVModel);
			}

			// calculate the required attenuation
			if (vm.DoAutoAttn && LRGains != null)
			{
				int[] frqtest = [ToBinNumber(freq, LRGains)];
				int[] frq2test = [ToBinNumber(freq2, LRGains)];
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;

				// find the two input voltages for our testing
				var v1in = vm.ToGenVoltage(vm.Gen1Voltage, frqtest, GEN_INPUT, gains);  // get generator voltage
				var v2in = v1in / vm.GenDivisor;  // get second input voltage
																						 // now find the output voltages for this input
				var v1lout = ToGenOutVolts(v1in, frqtest, LRGains.Left);	// left channel output V
				var v2lout = ToGenOutVolts(v2in, frq2test, LRGains.Left);
				var v1rout = ToGenOutVolts(v1in, frqtest, LRGains.Right);	// right channel output V
				var v2rout = ToGenOutVolts(v2in, frq2test, LRGains.Right);
				var vtotal = Math.Max(v1lout*v1lout + v2lout*v2lout, v1rout*v1rout + v2rout*v2rout);	// max sum of squares
				vtotal = Math.Sqrt(vtotal);

				var vdbv = QaLibrary.ConvertVoltage(vtotal, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

				imdVm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);
				vm.Attenuation = imdVm.Attenuation; // update the specVm to update the gui, then this for the steps
			}

			// do the actual measurements
			var rslt = true;
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
			MyVModel.HasExport = PageData.FreqRslt != null && PageData.FreqRslt.Left?.Length > 0;
			await EndAction(imdVm);
		}
		private double ToD(string stri)
		{
			return MathUtil.ToDouble(stri);
		}

		public void UpdatePlotTitle()
		{
			var thdFreq = MyVModel;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			var title = GetTheTitle(myPlot);
			myPlot.Title(title);
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot(string plotFormat = "dBV")
		{
			var imdVm = MyVModel;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			myPlot.Axes.SetLimitsX(Math.Log10(ToD(imdVm.GraphStartFreq)),
				Math.Log10(ToD(imdVm.GraphEndFreq)), myPlot.Axes.Bottom);

			myPlot.Axes.SetLimitsY(ToD(imdVm.RangeBottomdB), ToD(imdVm.RangeTopdB), myPlot.Axes.Left);

			UpdatePlotTitle();
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));

			fftPlot.Refresh();
		}

		/// <summary>
		/// Ititialize the THD % plot
		/// </summary>
		void InitializefftPlot(string plotFormat = "%")
		{
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			ImdViewModel imdVm = MyVModel;
			myPlot.Axes.SetLimits(Math.Log10(ToD(imdVm.GraphStartFreq)), Math.Log10(ToD(imdVm.GraphEndFreq)),
				Math.Log10(ToD(imdVm.RangeBottom)) - 0.00000001, Math.Log10(ToD(imdVm.RangeTop)));  // - 0.000001 to force showing label
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));

			UpdatePlotTitle();
			fftPlot.Refresh();
		}

		private void CalculateHarmonics(MyDataTab page, ImdChannelViewModel left, ImdChannelViewModel right)
		{
			var vm = page.ViewModel;
			if (page.FreqRslt == null)
				return;

			// Loop through harmonics up tot the 10th
			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 1000);
			var freq2 = MathUtil.ToDouble(vm.Gen2Frequency, 1000);
			var maxfreq = vm.SampleRateVal / 2.0;

			var freqList = MakeHarmonics(freq, freq2);
			ImdChannelViewModel[] steps = [left, right];

			foreach(var step in steps)
			{
				List<HarmonicData> harmonics = new List<HarmonicData>();
				for (int harmonicNumber = 0; harmonicNumber < freqList.Length; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
				{
					double harmonicFrequency = freqList[harmonicNumber];
					bool good = harmonicFrequency > 0 && harmonicFrequency < maxfreq;
					var hfreq = good ? harmonicFrequency : maxfreq / 2;
					var ffts = step.IsLeft ? page.FreqRslt.Left : page.FreqRslt.Right;
					double amplitudeV = QaMath.MagAtFreq(ffts, page.FreqRslt.Df, hfreq);
					double amplitudedBV = 20 * Math.Log10(amplitudeV);
					double thdPercent = (amplitudeV / left.Fundamental1Volts) * 100;

					HarmonicData harmonic = new()
					{
						HarmonicNr = harmonicNumber,
						Frequency = harmonicFrequency,
						Amplitude_V = good ? amplitudeV : 0,
						Amplitude_dBV = amplitudedBV,
						Thd_Percent = good ? thdPercent : 0,
						Thd_dB = 20 * Math.Log10(thdPercent / 100.0),
					};
					harmonics.Add(harmonic);
				}
				step.Harmonics = harmonics;
			}

			return;
		}

	}
}