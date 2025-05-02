using Newtonsoft.Json;
using QA40x_BareMetal;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Data;
using System.IO;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

	public class ActScope : ActBase
    {
		public DataTab<ScopeViewModel> PageData { get; private set; } // Data used in this form instance
		private List<DataTab<ScopeViewModel>> OtherTabs { get; set; } = new List<DataTab<ScopeViewModel>>(); // Other tabs in the document
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
			db.FreqData = frequencies;	// time actually but w/e
			return db;
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<ScopeViewModel>(PageData, fileName);
		}

		public async Task LoadFromFile(string fileName, bool isMain)
		{
			var page = LoadFile(fileName);
			await FinishLoad(page, isMain);
		}

		/// <summary>
		/// Load a file into a DataTab
		/// </summary>
		/// <param name="fileName">full path name</param>
		/// <returns>a datatab with no frequency info</returns>
		public DataTab<ScopeViewModel> LoadFile(string fileName)
		{
			return Util.LoadFile<ScopeViewModel>(PageData, fileName);
		}

		/// <summary>
		/// given a datatab, integrate it into the gui as the current datatab
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public async Task FinishLoad(DataTab<ScopeViewModel> page, bool isMain)
		{
			// now recalculate everything
			BuildFrequencies(page);
			await PostProcess(page, ct.Token);
			if(isMain)
			{
				PageData = page;    // set the current page to the loaded one
									// we can't overwrite the viewmodel since it links to the display proper
									// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				page.ViewModel.CopyPropertiesTo<ScopeViewModel>(MyVModel);    // retract the gui

				// relink to the new definition
				MyVModel.LinkAbout(PageData.Definition);
			}
			else
			{
				// add to the other tabs
				OtherTabs.Clear();
				OtherTabs.Add(page);
			}
			UpdateGraph(true);
		}

		private static double[] BuildWave(DataTab<ScopeViewModel> page)
		{
			var vm = page.ViewModel;

			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 0);
			var freq2 = MathUtil.ToDouble(vm.Gen2Frequency, 0);
			// for the first go around, turn on the generator
			// Set the generators via a usermode
			var waveForm1 = new GenWaveform()
			{
				Frequency = freq,
				Voltage = page.Definition.GeneratorVoltage,
				Name = vm.Gen1Waveform
			};
			// scale generatorvoltage by the ratio of v2 and v1
			var v2 = MathUtil.ToDouble(vm.Gen2Voltage, 1e-10);
			var v1 = MathUtil.ToDouble(vm.Gen1Voltage, 1e-10);
			var waveForm2 = new GenWaveform()
			{
				Frequency = freq2,
				Voltage = page.Definition.GeneratorVoltage * v2 / v1,
				Name = vm.Gen2Waveform
			};
			var waveSample = new GenWaveSample()
			{
				SampleRate = (int)vm.SampleRateVal,
				SampleSize = (int)vm.FftSizeVal
			};

			double[] wave;

			if (vm.UseGenerator1 || vm.UseGenerator2)
			{
				GenWaveform[] waves = [];
				if (vm.UseGenerator1 && vm.UseGenerator2)
					waves = [waveForm1, waveForm2];
				else if (vm.UseGenerator1)
					waves = [waveForm1];
				else if (vm.UseGenerator2)
					waves = [waveForm2];

				wave = QaMath.CalculateWaveform(waves, waveSample).ToArray();
			}
			else
			{
				wave = new double[waveSample.SampleSize];
			}
			return wave;
		}

		static void BuildFrequencies(DataTab<ScopeViewModel> page)
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

		public Rect GetDataBounds()
		{
			var msr = PageData.ViewModel;    // measurement settings
			if (msr == null || PageData.TimeRslt.Left.Length == 0)
				return Rect.Empty;
			var scopeVm = MyVModel;     // current settings
			var timeData = PageData.TimeRslt;

			Rect rrc = new Rect(0, 0, 0, 0);
			rrc.X = 0;
			double maxY = 0;
			if (scopeVm.ShowLeft)
			{
				rrc.Y = timeData.Left.Min();
				maxY = timeData.Left.Max();
				if (scopeVm.ShowRight)
				{
					rrc.Y = Math.Min(rrc.Y, timeData.Right.Min());
					maxY = Math.Max(maxY, timeData.Right.Max());
				}
			}
			else if (scopeVm.ShowRight)
			{
				rrc.Y = timeData.Right.Min();
				maxY = timeData.Right.Max();
			}

			rrc.Width = 1000 * timeData.Left.Length * timeData.dt - rrc.X;       // max ms
			rrc.Height = maxY - rrc.Y;      // max voltage absolute

			return rrc;
		}

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> RunAcquisition(DataTab<ScopeViewModel> msr, CancellationToken ct)
        {
			// Setup
			ScopeViewModel thd = msr.ViewModel;

			var freq = MathUtil.ToDouble(thd.Gen1Frequency, 0);
			var freq2 = MathUtil.ToDouble(thd.Gen2Frequency, 0);
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
				//if (msr.NoiseFloor == null)
    //            {
				//	var noisy = await MeasureNoise(ct);
				//	if (ct.IsCancellationRequested)
				//		return false;
				//	msr.NoiseFloor = new LeftRightPair();
				//	msr.NoiseFloor.Right = QaCompute.CalculateNoise(noisy.FreqRslt, true);
				//	msr.NoiseFloor.Left = QaCompute.CalculateNoise(noisy.FreqRslt, false);
				//}

				var gains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				var genVolt = thd.ToGenVoltage(thd.Gen1Voltage, [], GEN_INPUT, gains) ;
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
				await showProgress(0);


				var wave = BuildWave(msr);   // also update the waveform variables
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
		private async Task<bool> PostProcess(DataTab<ScopeViewModel> msr, CancellationToken ct)
		{
			var thd = msr.ViewModel;
			try
			{ 
				UpdateGraph(false);
				if(! thd.IsTracking)
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
			await showMessage("Measurement finished");
			await Task.Delay(100);	// let it be seen

            return !ct.IsCancellationRequested;
        }

        private void AddAMarker(ScopeMeasurementResult fmr, double frequency, bool isred = false)
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = timePlot.ThePlot;
			var sampleRate = fmr.MeasurementSettings.SampleRateVal;
			var fftsize = fmr.MeasurementSettings.FftSizeVal;
			int bin = (int)QaLibrary.GetBinOfFrequency(frequency, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
			var leftData = fmr.FrequencySteps[0].fftData?.Left;
			var rightData = fmr.FrequencySteps[0].fftData?.Right;

			double markVal = 0;
			if (rightData != null && !vm.ShowLeft)
			{
				markVal = 20 * Math.Log10(rightData[bin]);
			}
			else if(leftData != null )
			{
				markVal = 20 * Math.Log10(leftData[bin]);
			}
			ScottPlot.Color markerCol = new ScottPlot.Color();
            if( ! vm.ShowLeft)
            {
                markerCol = isred ? Colors.Green : Colors.DarkGreen;
			}
            else
            {
				markerCol = isred ? Colors.Red : Colors.DarkOrange;
			}
			var mymark = myPlot.Add.Marker(Math.Log10(frequency), markVal,
				MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
			mymark.LegendText = string.Format("{1}: {0:F1}", markVal, (int)frequency);
		}

		/// <summary>
		/// Perform the calculations of a single channel (left or right)
		/// </summary>
		/// <param name="binSize"></param>
		/// <param name="fundamentalFrequency"></param>
		/// <param name="generatorAmplitudeDbv"></param>
		/// <param name="fftData"></param>
		/// <param name="noiseFloorFftData"></param>
		/// <returns></returns>
		private ThdFrequencyStepChannel ChannelCalculations(double binSize, double generatorV, ThdFrequencyStep step, ScopeMeasurementResult msr, bool isRight)
		{
			uint fundamentalBin = QaLibrary.GetBinOfFrequency(step.FundamentalFrequency, binSize);
			var ffts = isRight ? step.fftData?.Right : step.fftData?.Left;
			var ltdata = step.timeData?.Left;

			// this should never happen
			if (ffts == null || ltdata == null)
				return new();

			double allvolts = Math.Sqrt(ltdata.Select(x => x * x ).Sum() / ltdata.Count()); // use the time data for best accuracy gain math

			ThdFrequencyStepChannel channelData = new()
			{
				Fundamental_V = ffts[fundamentalBin],
				Total_V = allvolts,
				Total_W = allvolts * allvolts / ViewSettings.AmplifierLoad,
				Fundamental_dBV = 20 * Math.Log10(ffts[fundamentalBin]),
				Gain_dB = 20 * Math.Log10(ffts[fundamentalBin] / generatorV)

			};
			// Calculate average noise floor
			var noiseFlr = (msr.NoiseFloor == null) ? null : (isRight ? msr.NoiseFloor.FreqRslt?.Left : msr.NoiseFloor.FreqRslt?.Right);
			channelData.TotalNoiseFloor_V = QaCompute.CalculateNoise(msr.NoiseFloor?.FreqRslt, !isRight);


			// Reset harmonic distortion variables
			double distortionSqrtTotal = 0;
			double distortionSqrtTotalN = 0;
			double distortionD6plus = 0;

			// Loop through harmonics up tot the 10th
			for (int harmonicNumber = 2; harmonicNumber <= 10; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
			{
				double harmonicFrequency = step.FundamentalFrequency * harmonicNumber;
				uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

				if (bin >= ffts.Length)
					bin = (uint)Math.Max(0, ffts.Length - 1);             // Invalid bin, skip harmonic

				double amplitude_V = ffts[bin];
				double noise_V = channelData.TotalNoiseFloor_V;

				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
				double thd_Percent = (amplitude_V / channelData.Fundamental_V) * 100;
				double thdN_Percent = (noiseFlr == null) ? 0 : ((amplitude_V - noiseFlr[bin]) / channelData.Fundamental_V) * 100;

				HarmonicData harmonic = new()
				{
					HarmonicNr = harmonicNumber,
					Frequency = harmonicFrequency,
					Amplitude_V = amplitude_V,
					Amplitude_dBV = amplitude_dBV,
					Thd_Percent = thd_Percent,
					Thd_dB = 20 * Math.Log10(thd_Percent / 100.0),
					Thd_dBN = 20 * Math.Log10(thdN_Percent / 100.0),
					NoiseAmplitude_V = (noiseFlr == null) ? 1e-3 : noiseFlr[bin]
				};

				if (harmonicNumber >= 6)
					distortionD6plus += Math.Pow(amplitude_V, 2);

				distortionSqrtTotal += Math.Pow(amplitude_V, 2);
				distortionSqrtTotalN += Math.Pow(amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
			}

			// Calculate D6+ (D6 - D12)
			if (distortionD6plus != 0)
            {
                channelData.D6Plus_dBV = 20 * Math.Log10(Math.Sqrt(distortionD6plus));
                channelData.ThdPercent_D6plus = Math.Sqrt(distortionD6plus / Math.Pow(channelData.Fundamental_V, 2)) * 100;
                channelData.ThdDbD6plus = 20 * Math.Log10(channelData.ThdPercent_D6plus / 100.0);
            }

            // If load not zero then calculate load power
            if (ViewSettings.AmplifierLoad != 0)
                channelData.Power_Watt = Math.Pow(channelData.Fundamental_V, 2) / ViewSettings.AmplifierLoad;

            return channelData;
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
        void PlotValues(DataTab<ScopeViewModel> page, int measurementNr, bool isMain)
        {
			ScottPlot.Plot myPlot = timePlot.ThePlot;

			var scopeVm = MyVModel;
			bool leftChannelEnabled = isMain ? scopeVm.ShowLeft : scopeVm.ShowOtherLeft;	// dynamically update these
			bool rightChannelEnabled = isMain ? scopeVm.ShowRight : scopeVm.ShowOtherRight;

			var timeData = page.TimeRslt;
			if (timeData == null || timeData.Left.Length == 0)
				return;

			double maxleft = timeData.Left.Max();
			double maxright = timeData.Right.Max();

			var timeX = Enumerable.Range(0, timeData.Left.Length).Select(x => x * 1000 * timeData.dt).ToArray(); // in ms
			var showThick = MyVModel.ShowThickLines;	// so it dynamically updates
			var markerSize = scopeVm.ShowPoints ? (showThick ? _Thickness : 1) + 3 : 1;
			if (leftChannelEnabled)
			{
				Scatter pLeft = myPlot.Add.Scatter(timeX, timeData.Left);
				pLeft.LineWidth = showThick ? _Thickness : 1;
				pLeft.Color = isMain ? QaLibrary.BlueColor : QaLibrary.GreenXColor;  // Blue
				pLeft.MarkerSize = markerSize;
			}

			if (rightChannelEnabled)
			{
				Scatter pRight = myPlot.Add.Scatter(timeX, timeData.Right);
				pRight.LineWidth = showThick ? _Thickness : 1;
				if(!isMain)
					pRight.Color = QaLibrary.OrangeXColor; // Red transparant
				else if (leftChannelEnabled)
					pRight.Color = QaLibrary.RedXColor; // Red transparant
				else
					pRight.Color = QaLibrary.RedColor; // Red
				pRight.MarkerSize = markerSize;
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

			myPlot.Axes.SetLimits(MathUtil.ToDouble(thdFreq.GraphStartTime), MathUtil.ToDouble(thdFreq.GraphEndTime), 
				MathUtil.ToDouble(thdFreq.RangeBottom), MathUtil.ToDouble(thdFreq.RangeTop));

            myPlot.Title("Scope");
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
			DataTab<ScopeViewModel> NextPage = new(scopeVm, lrts);
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var vm = NextPage.ViewModel;
			if (vm == null)
				return;

			var genType = ToDirection(scopeVm.GenDirection);
			var freq = MathUtil.ToDouble(scopeVm.Gen1Frequency, 1000);
			// if we're doing adjusting here
			if (vm.DoAutoAttn || genType != E_GeneratorDirection.INPUT_VOLTAGE)
			{
				// show that we're autoing...
				if (vm.DoAutoAttn)
					scopeVm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;
				await showMessage("Calculating DUT gain"); 
				LRGains = await DetermineGainAtFreq(freq, true, 1);
			}

			if (scopeVm.DoAutoAttn && LRGains != null)
			{
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
				var vinL = scopeVm.ToGenVoltage(scopeVm.Gen1Voltage, [], GEN_INPUT, gains); // get primary input voltage
				var vinL2 = scopeVm.ToGenVoltage(scopeVm.Gen2Voltage, [], GEN_INPUT, gains); // get primary input voltage
				vinL = Math.Max(vinL, vinL2);
				double voutL = ToGenOutVolts(vinL, [], LRGains.Left);	// what is that as output voltage?
				double voutR = ToGenOutVolts(vinL, [], LRGains.Right);	// for both channels
				var vdbv = QaLibrary.ConvertVoltage( Math.Max(voutL, voutR), E_VoltageUnit.Volt, E_VoltageUnit.dBV );
				scopeVm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);             // find attenuation for both
				vm.Attenuation = scopeVm.Attenuation;	// update the scopeVm to update the gui, then this for the steps
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

			scopeVm.IsRunning = false;
			await showMessage("");
			MyVModel.HasExport = (PageData.TimeRslt.Left.Length > 0);
			await EndAction();
		}
        
        // show the latest step values in the table
        public void DrawChannelInfoTable()
        {
			var thd = MyVModel;
			var vm = ViewSettings.Singleton.ScopeChanLeft;
            vm.FundamentalFrequency = 0;
            vm.CalculateChannelValues(MathUtil.ToDouble( thd.Gen1Frequency), false);
		}

		public void UpdateGraph(bool settingsChanged)
        {
            timePlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			timePlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			var thd = MyVModel;

            if (settingsChanged)
            {
                InitializeMagnitudePlot();
            }

			if (thd.ShowOtherLeft || thd.ShowOtherRight)
			{
				if (OtherTabs.Count > 0)
				{
					foreach (var other in OtherTabs)
					{
						if (other != null)
							PlotValues(other, resultNr++, false);
					}
				}
			}
			PlotValues(PageData, resultNr++, true);

   //         if( MeasurementResult.FrequencySteps.Count > 0)
   //         {
			//	DrawChannelInfoTable();
			//}

			timePlot.Refresh();
		}
	}
}