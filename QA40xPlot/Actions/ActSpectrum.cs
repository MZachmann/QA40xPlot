using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.BareMetal;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.Data;
using System.Windows;
using static QA40xPlot.ViewModels.BaseViewModel;
using QA40x_BareMetal;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Interop;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

	public class ActSpectrum : ActBase
    {
		private DataTab<SpectrumViewModel>? PageData { get; set; } // Data used in this form instance
		private List<DataTab<SpectrumViewModel>> OtherTabs { get; set; } = new List<DataTab<SpectrumViewModel>>(); // Other tabs in the document
		private readonly Views.PlotControl fftPlot;

        private float _Thickness = 2.0f;
		private static SpectrumViewModel MyVModel { get => ViewSettings.Singleton.SpectrumVm; }

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActSpectrum(ref SpectrumData data, Views.PlotControl graphFft)
        {
			fftPlot = graphFft;
			ct = new CancellationTokenSource();
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
			var specVm = MyVModel;
			var vm = PageData?.ViewModel as SpectrumViewModel;
			if ( vm == null || PageData?.FreqRslt == null)
				return null;

			DataBlob db = new();
			var ffs = PageData.FreqRslt;

			var sampleRate = MathUtil.ToUint(vm.SampleRate);
			var fftsize = ffs.Left.Length;
			var binSize = ffs.Df;
			if (specVm.ShowRight && !specVm.ShowLeft)
			{
				db.LeftData = ffs.Right.ToList();
			}
			else
			{
				db.LeftData = ffs.Left.ToList();
			}
			var frqs = Enumerable.Range(0, fftsize).ToList();
			var frequencies = frqs.Select(x => x * binSize).ToList(); // .Select(x => x * binSize);
			db.FreqData = frequencies;
			return db;
		}

		public void SaveToFile(string fileName)
		{
			var tofile = PageData;
			var container = new Dictionary<string, object>();
			container["PageData"] = tofile;
			// Serialize the object to a JSON string
			string jsonString = JsonConvert.SerializeObject(tofile, Formatting.Indented);

			// Write the JSON string to a file
			File.WriteAllText(fileName, jsonString);
		}

		public void LoadFromFile(string fileName)
		{
			try
			{
				// Read the JSON file into a string
				string jsonContent = File.ReadAllText(fileName);
				// Deserialize the JSON string into an object
				var jsonObject = JsonConvert.DeserializeObject<DataTab<SpectrumViewModel>>(jsonContent);
				if (jsonObject != null)
				{
					try
					{
						if(PageData == null)
						{
							PageData = new DataTab<SpectrumViewModel>(MyVModel, new LeftRightTimeSeries());
							PageData.NoiseFloor = new LeftRightPair();
						}
						// file pagedata with new stuff
						PageData.NoiseFloor = jsonObject.NoiseFloor;
						PageData.Definition = jsonObject.Definition;
						PageData.TimeRslt = jsonObject.TimeRslt;
						jsonObject.ViewModel.CopyPropertiesTo<SpectrumViewModel>(PageData.ViewModel);
						jsonObject.ViewModel.CopyPropertiesTo<SpectrumViewModel>(ViewSettings.Singleton.SpectrumVm);    // retract the gui
						// now recalculate everything
						BuildFrequencies(PageData);
						PostProcess(PageData, ct.Token).Wait();
						UpdateGraph(true);
						var x = 12;
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
					}

				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}

		}

		private static double[] BuildWave(DataTab<SpectrumViewModel> page)
		{
			var vm = page.ViewModel as SpectrumViewModel;
			if(vm == null)
				return Array.Empty<double>();

			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 0);
			// for the first go around, turn on the generator
			// Set the generators via a usermode
			var waveForm = new GenWaveform()
			{
				Frequency = freq,
				Voltage = page.Definition.GeneratorVoltage,
				Name = vm.Gen1Waveform
			};
			var waveSample = new GenWaveSample()
			{
				SampleRate = (int)vm.SampleRateVal,
				SampleSize = (int)vm.FftSizeVal
			};

			double[] wave;
			if (vm.UseGenerator)
			{
				wave = QAMath.CalculateWaveform([waveForm], waveSample).ToArray();
			}
			else
			{
				wave = new double[waveSample.SampleSize];
			}
			return wave;
		}

		/// <summary>
		///  Start measurement button click
		/// </summary>
		public async void DoMeasurement()
		{
			var specVm = MyVModel;			// the active viewmodel
			if (!StartAction(specVm))
				return;

			ct = new();
			LeftRightTimeSeries lrts = new();
			DataTab<SpectrumViewModel> NextPage = new(specVm, lrts);
			var vm = NextPage.ViewModel as SpectrumViewModel;
			if (vm == null)
				return;

			var genType = ToDirection(vm.GenDirection);
			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 1000);
			var binSize = QaLibrary.CalcBinSize(vm.SampleRateVal, vm.FftSizeVal);

			// if we're doing adjusting here we need gain information
			if (vm.DoAutoAttn || genType != E_GeneratorDirection.INPUT_VOLTAGE)
			{
				// show that we're autoing...
				if (vm.DoAutoAttn)
					vm.Attenuation = QaLibrary.DEVICE_MAX_ATTENUATION;
				await showMessage("Calculating DUT gain");
				LRGains = await DetermineGainAtFreq(freq, true, 1);
			}

			// auto attenuation?
			if (vm.DoAutoAttn && LRGains != null)
			{
				var gains = ViewSettings.IsTestLeft ? LRGains.Left : LRGains.Right;
				var vinL = vm.ToGenVoltage(vm.Gen1Voltage, [], GEN_INPUT, gains);   // get primary input voltage
				double voutL = ToGenOutVolts(vinL, [], LRGains.Left);   // what is that as output voltage?
				double voutR = ToGenOutVolts(vinL, [], LRGains.Right);  // for both channels
				var vdbv = QaLibrary.ConvertVoltage(Math.Max(voutL, voutR), E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				vm.Attenuation = QaLibrary.DetermineAttenuation(vdbv);              // find attenuation for both
				specVm.Attenuation = vm.Attenuation;	// set both to show on indicator
			}

			// run a measurement and get time data
			// and frequency data

			var rslt = await RunAcquisition(NextPage, ct.Token);
			if (!rslt)
				return;

			rslt = await PostProcess(NextPage, ct.Token);

			if (rslt)
			{
				if (!ReferenceEquals(PageData, NextPage))
					PageData = NextPage;        // finally update the pagedata for display and processing
			}
			UpdateGraph(true);

			while ( ! ct.IsCancellationRequested)
			{
				if(PageData?.ViewModel != null)
					MyVModel.CopyPropertiesTo(PageData.ViewModel);  // update the view model with latest settings
				rslt = await RunAcquisition(PageData, ct.Token);
				if (!rslt)
					return;
				rslt = await PostProcess(PageData, ct.Token);
				UpdateGraph(false);
			}

			specVm.IsRunning = false;
			await showMessage("");
			MyVModel.HasExport = NextPage.FreqRslt != null;
			EndAction();
		}

		static void BuildFrequencies(DataTab<SpectrumViewModel> page)
		{
			var vm = page.ViewModel as SpectrumViewModel;
			LeftRightFrequencySeries? fseries;
			if (vm?.Gen1Waveform != "Chirp")
			{
				fseries = QAMath.CalculateSpectrum(page.TimeRslt, vm.WindowingMethod);  // do the fft and calculate the frequency response
			}
			else
			{
				var wave = BuildWave(page);
				fseries = QaUsb.CalculateChirpFreq(page.TimeRslt, wave.ToArray(), page.Definition.GeneratorVoltage, vm.SampleRateVal, vm.FftSizeVal);   // normalize the result for flat response
			}
			if (fseries != null)
			{
				page.SetProperty("FFT", fseries); // set the frequency response
			}
		}

		/// <summary>
		/// run an acquisition and get the frequency and time results
		/// </summary>
		/// <param name="msr">the datatab we're using</param>
		/// <param name="ct"></param>
		/// <returns></returns>
		async Task<bool> RunAcquisition(DataTab<SpectrumViewModel> msr, CancellationToken ct)
		{
			SpectrumViewModel? vm = msr.ViewModel as SpectrumViewModel; // cached model
			if (vm == null)
				return false;

			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 0);
			var sampleRate = vm.SampleRateVal;
			if (freq == 0 || sampleRate == 0 || !SpectrumViewModel.FftSizes.Contains(vm.FftSize))
			{
				MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = vm.FftSizeVal;


			// ********************************************************************  
			// Load a settings we want for the noise floor run
			// ********************************************************************  
			if (true != QaUsb.InitializeDevice(sampleRate, fftsize, vm.WindowingMethod, (int)vm.Attenuation))
				return false;

			LeftRightSeries lrfs = new();

			try
			{
				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
					return false;

				await showProgress(0);
				// do the noise floor acquisition and math
				// note measurenoise uses the existing init setup
				// except InputRange (attenuation) which is push/pop-ed
				if (msr.NoiseFloor == null)
				{
					var noisy = await MeasureNoise(ct);
					msr.NoiseFloor = new LeftRightPair();
					msr.NoiseFloor.Right = QaCompute.CalculateNoise(noisy.FreqRslt, true);
					msr.NoiseFloor.Left = QaCompute.CalculateNoise(noisy.FreqRslt, false);
				}
				var gains = ViewSettings.IsTestLeft ? LRGains?.Left : LRGains?.Right;
				var genVolt = vm.ToGenVoltage(vm.Gen1Voltage, [], GEN_INPUT, gains);
				if (genVolt > 5)
				{
					await showMessage($"Requesting input voltage of {genVolt} volts, check connection and settings");
					genVolt = 0.01;
				}
				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
					return false;

				msr.Definition.GeneratorVoltage = genVolt; // save the generator voltage for serialization

				// ********************************************************************
				// measure once
				// ********************************************************************
				// now do the step measurement
				await showMessage($"Measuring spectrum with input of {genVolt:G3}V.");
				await showProgress(25);

				var wave = BuildWave(msr);   // also update the waveform variables
				lrfs = await QaUsb.DoAcquireUser(vm.Averages, ct, wave, wave, false);

				if (lrfs.TimeRslt != null)
					msr.TimeRslt = lrfs.TimeRslt;

				await showProgress(50);
				BuildFrequencies(msr);		// do the relevant fft work
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
		private async Task<bool> PostProcess(DataTab<SpectrumViewModel> msr, CancellationToken ct)
        {
			if(msr.FreqRslt == null)
			{
				await showMessage("No frequency result");
				return false;
			}

			// left and right channels summary info to fill in
			var left = new ThdChannelViewModel();
			var right = new ThdChannelViewModel();
			SpectrumViewModel? vm = msr.ViewModel as SpectrumViewModel;
			if (vm == null)
				return false;

			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 0);
			left.FundamentalFrequency = freq;
			left.GeneratorVolts = msr.Definition.GeneratorVoltage;
			right.FundamentalFrequency = freq;
			right.GeneratorVolts = msr.Definition.GeneratorVoltage;

			var lrfs = msr.FreqRslt;    // frequency response

			uint fundamental1Bin = QaLibrary.GetBinOfFrequency(freq, (uint)vm.SampleRateVal, (uint)vm.FftSizeVal);
			var x = QAMath.MagAtFreq(msr.FreqRslt.Left, msr.FreqRslt.Df, freq);
			left.FundamentalVolts = x;
			x = QAMath.MagAtFreq(msr.FreqRslt.Right, msr.FreqRslt.Df, freq);
			right.FundamentalVolts = x;

			var maxf = 20000; // the app seems to use 20,000 so not sampleRate/ 2.0;
			LeftRightPair snrdb = QaCompute.GetSnrDb(lrfs, freq, 20.0, maxf);
			LeftRightPair thds = QaCompute.GetThdDb(lrfs, freq, 20.0, maxf);
			LeftRightPair thdN = QaCompute.GetThdnDb(lrfs, freq, 20.0, maxf);

			left.SNRatio = snrdb.Left;
			right.SNRatio = snrdb.Right;
			left.ENOB = (snrdb.Left - 1.76) / 6.02;
			right.ENOB = (snrdb.Right - 1.76) / 6.02;
			left.ThdNInV = left.FundamentalVolts * QaLibrary.ConvertVoltage(thdN.Left, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			right.ThdNInV = left.FundamentalVolts * QaLibrary.ConvertVoltage(thdN.Right, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			left.ThdInV = left.FundamentalVolts * QaLibrary.ConvertVoltage(thds.Left, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			right.ThdInV = left.FundamentalVolts * QaLibrary.ConvertVoltage(thds.Right, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			left.NoiseFloorV = msr.NoiseFloor?.Left ?? 1e-10;
			right.NoiseFloorV = msr.NoiseFloor?.Right ?? 1e-10;
			left.NoiseFloorPct = 100 * left.NoiseFloorV / left.FundamentalVolts;
			right.NoiseFloorPct = 100 * right.NoiseFloorV / right.FundamentalVolts;
			left.ThdInPercent = 100 * Math.Pow(10, thdN.Left / 20);
			right.ThdInPercent = 100 * Math.Pow(10, thdN.Right / 20);
			left.ThdNInPercent = 100 * left.ThdNInV / left.FundamentalVolts;
			right.ThdNInPercent = 100 * right.ThdNInV / right.FundamentalVolts;
			left.ThdNIndB = thdN.Left;
			right.ThdNIndB = thdN.Right;
			left.ThdIndB = thds.Left;
			right.ThdIndB = thds.Right;

			left.NoiseFloorPct = 100 * (left.NoiseFloorV / left.FundamentalVolts);
			right.NoiseFloorPct = 100 * (right.NoiseFloorV / right.FundamentalVolts);
			left.GaindB = 20 * Math.Log10(left.FundamentalVolts / Math.Max(1e-10, left.GeneratorVolts));
			right.GaindB = 20 * Math.Log10(right.FundamentalVolts / Math.Max(1e-10, right.GeneratorVolts));

			left.NoiseFloorView = GraphUtil.DoValueFormat(vm.PlotFormat, left.NoiseFloorV, left.FundamentalVolts);
			left.AmplitudeView = GraphUtil.DoValueFormat(vm.PlotFormat, left.FundamentalVolts, left.FundamentalVolts);
			right.NoiseFloorView = GraphUtil.DoValueFormat(vm.PlotFormat, right.NoiseFloorV, right.FundamentalVolts);
			right.AmplitudeView = GraphUtil.DoValueFormat(vm.PlotFormat, right.FundamentalVolts, right.FundamentalVolts);

			left.TotalW = left.FundamentalVolts * left.FundamentalVolts / ViewSettings.AmplifierLoad;
			right.TotalW = right.FundamentalVolts * right.FundamentalVolts / ViewSettings.AmplifierLoad;

			left.ShowDataPercents = vm.ShowDataPercent;
			right.ShowDataPercents = vm.ShowDataPercent;

			var ltdata = msr.TimeRslt.Left;
			// this should never happen
			if (ltdata != null)
			{
				double allvolts = Math.Sqrt(ltdata.Select(x => x * x).Sum() / ltdata.Count()); // use the time data for best accuracy gain math
				left.FundamentalVolts = allvolts;
			}
			ltdata = msr.TimeRslt.Right;
			// this should never happen
			if (ltdata != null)
			{
				double allvolts = Math.Sqrt(ltdata.Select(x => x * x).Sum() / ltdata.Count()); // use the time data for best accuracy gain math
				right.FundamentalVolts = allvolts;
			}

			CalculateHarmonics(msr, left, right);
			ViewSettings.Singleton.ChannelLeft.Harmonics = new List<HarmonicData>();
			ViewSettings.Singleton.ChannelRight.Harmonics = new List<HarmonicData>();

			// we're nearly done
			msr.SetProperty("Left", left);
			msr.SetProperty("Right", right);

			// CalculateDistortion(msr, left, right);
			left.CopyPropertiesTo(ViewSettings.Singleton.ChannelLeft);	// clone to our statics
			right.CopyPropertiesTo(ViewSettings.Singleton.ChannelRight);

			// Show message
			await showMessage($"Measurement finished");

            return !ct.IsCancellationRequested;
        }

		private void CalculateHarmonics(DataTab<SpectrumViewModel> page, ThdChannelViewModel left, ThdChannelViewModel right)
		{
			List<HarmonicData> harmonics = new List<HarmonicData>();
			var vm = page.ViewModel as SpectrumViewModel;
			if(vm == null || page.FreqRslt == null)
				return;

			// Loop through harmonics up tot the 10th
			var freq = MathUtil.ToDouble(vm.Gen1Frequency, 1000);
			freq = QaLibrary.GetNearestBinFrequency(freq, vm.SampleRateVal, vm.FftSizeVal);
			var maxfreq = vm.SampleRateVal / 2.0;
			var binSize = QaLibrary.CalcBinSize(vm.SampleRateVal, vm.FftSizeVal);

			for (int harmonicNumber = 2; harmonicNumber <= 10; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
			{
				double harmonicFrequency = freq * harmonicNumber;
				if(harmonicFrequency > maxfreq)
					break;  // no more harmonics

				double amplitude_V = QAMath.MagAtFreq(page.FreqRslt.Left, binSize, harmonicFrequency);
				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
				double thdPercent = (amplitude_V / left.FundamentalVolts) * 100;

				HarmonicData harmonic = new()
				{
					HarmonicNr = harmonicNumber,
					Frequency = harmonicFrequency,
					Amplitude_V = amplitude_V,
					Amplitude_dBV = amplitude_dBV,
					Thd_Percent = thdPercent,
					Thd_dB = 20 * Math.Log10(thdPercent / 100.0),
				};
				harmonics.Add(harmonic);
			}
			left.Harmonics = harmonics;

			harmonics = new List<HarmonicData>();
			for (int harmonicNumber = 2; harmonicNumber <= 10; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
			{
				double harmonicFrequency = freq * harmonicNumber;
				if (harmonicFrequency > maxfreq)
					break;  // no more harmonics

				double amplitude_V = QAMath.MagAtFreq(page.FreqRslt.Right, binSize, harmonicFrequency);
				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
				double thdPercent = (amplitude_V / right.FundamentalVolts) * 100;

				HarmonicData harmonic = new()
				{
					HarmonicNr = harmonicNumber,
					Frequency = harmonicFrequency,
					Amplitude_V = amplitude_V,
					Amplitude_dBV = amplitude_dBV,
					Thd_Percent = thdPercent,
					Thd_dB = 20 * Math.Log10(thdPercent / 100.0),
				};
				harmonics.Add(harmonic);
			}
			right.Harmonics = harmonics;

			return;
		}

		private void AddAMarker(DataTab<SpectrumViewModel> page, double frequency, bool isred = false)
		{
			var vm = page.ViewModel as SpectrumViewModel;
			if (vm == null)
				return;

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
			else if(leftData != null)
			{
				double maxleft = leftData.Max();
				markVal = GraphUtil.ReformatValue(vm.PlotFormat, leftData[bin], maxleft);
			}
			var markView = GraphUtil.IsPlotFormatLog(vm.PlotFormat) ? markVal : Math.Log10(markVal);

			ScottPlot.Color markerCol = new ScottPlot.Color();
            if( ! vm.ShowLeft)
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

		private void ShowHarmonicMarkers(DataTab<SpectrumViewModel> page)
		{
			var vm = page.ViewModel as SpectrumViewModel;
			if (vm == null)
				return;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowMarkers)
			{
				ThdChannelViewModel? thdView = null;
				if (vm.ShowLeft)
					thdView = page.GetProperty("Left") as ThdChannelViewModel;
				else if (vm.ShowRight)
					thdView = page.GetProperty("Right") as ThdChannelViewModel;
				if (thdView != null)
				{
					AddAMarker(page, thdView.FundamentalFrequency);
					var flist = thdView.Harmonics.OrderBy(x => x.Frequency).ToArray();
					var cn = flist.Length;
					for (int i = 0; i < cn; i++)
					{
						var frq = flist[i].Frequency;
						AddAMarker(page, frq);
					}
				}
			}
		}

		private void ShowPowerMarkers(DataTab<SpectrumViewModel> page)
		{
			var vm = page.ViewModel as SpectrumViewModel;
			if (vm == null)
				return;
			if (!vm.ShowLeft && !vm.ShowRight)
				return;

            List<double> freqchecks = new List<double> { 50, 60 };
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowPowerMarkers)
			{
				var sampleRate = vm.SampleRateVal;
				var fftsize = vm.FftSizeVal;
                double fsel = 0;
                double maxdata = -10;
				var fftdata = vm.ShowLeft ? page.FreqRslt?.Left : page.FreqRslt?.Right;
				if (fftdata == null)
					return;
				// find if 50 or 60hz is higher, indicating power line frequency
				foreach (double freq in freqchecks)
				{
					var actfreq = QaLibrary.GetNearestBinFrequency(freq, sampleRate, fftsize);
					int bin = (int)QaLibrary.GetBinOfFrequency(actfreq, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
					var data = fftdata[bin];
                    if(data > maxdata)
                    {
                        fsel = freq;
                    }
				}
                // check 4 harmonics of power frequency
                for(int i=1; i<4; i++)
                {
                    var actfreq = QaLibrary.GetNearestBinFrequency(fsel * i, sampleRate, fftsize);
					int bin = (int)QaLibrary.GetBinOfFrequency(actfreq, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
                    var data = fftdata[bin];
                    double udif = 20 * Math.Log10(data);
                    AddAMarker(page, actfreq, true);
				}
			}
		}


		public Rect GetDataBounds()
		{
			var vm = PageData?.ViewModel;	// measurement settings
			if(vm == null || PageData == null || PageData.FreqRslt == null)
				return Rect.Empty;

			var specVm = MyVModel;     // current settings
			var ffs = PageData.FreqRslt;

			Rect rrc = new Rect(0, 0, 0, 0);
			double maxY = 0;
			if(specVm.ShowLeft)
			{
				rrc.Y = ffs.Left.Min();
				maxY = ffs.Left.Max();
				if (specVm.ShowRight)
				{
					rrc.Y = Math.Min(rrc.Y, ffs.Right.Min());
					maxY = Math.Max(maxY, ffs.Right.Max());
				}
			}
			else if (specVm.ShowRight)
			{
				rrc.Y = ffs.Right.Min();
				maxY = ffs.Right.Max();
			}

			rrc.X = 20;
			rrc.Width = ffs.Left.Length * ffs.Df - rrc.X;       // max frequency
			rrc.Height = maxY - rrc.Y;      // max voltage absolute

			return rrc;
		}

		/// <summary>
		/// find the nearest data point to the mouse
		/// </summary>
		/// <param name="freq">frequency on chart</param>
		/// <param name="posndBV">Y of mouse in plot</param>
		/// <param name="useRight">which channel</param>
		/// <returns>a tuple of df, value, value in pct</returns>
		public ValueTuple<double,double,double> LookupXY(double freq, double posndBV, bool useRight)
		{
			var fftdata = PageData?.FreqRslt;
			if (freq <= 0 || fftdata == null || PageData == null)
				return ValueTuple.Create(0.0,0.0,0.0);
			try
			{
				// get the data to look through
				var ffs = useRight ? fftdata?.Right : fftdata?.Left;
				if (fftdata != null && ffs != null && ffs.Length > 0 && freq < fftdata.Df * ffs.Length)
				{
					int bin = 0;
					ScottPlot.Plot myPlot = fftPlot.ThePlot;
					var pixel = myPlot.GetPixel(new Coordinates(Math.Log10(freq), posndBV));
					var left = ViewSettings.Singleton.ChannelLeft;
					var right = ViewSettings.Singleton.ChannelRight;

					// get screen coords for some of the data
					int abin = (int)(freq / fftdata.Df);       // apporoximate bin
					var binmin = Math.Max(1, abin - 5);            // random....
					var binmax = Math.Min(ffs.Length - 1, abin + 5);           // random....
					var msr = PageData.ViewModel;
					var vfi = GraphUtil.GetLogFormatter(msr.PlotFormat, useRight ? right.FundamentalVolts : left.FundamentalVolts);
					var distsx = ffs.Skip(binmin).Take(binmax - binmin);
					IEnumerable<Pixel> distasx = distsx.Select((fftd, index) => 
							myPlot.GetPixel(new Coordinates(Math.Log10((index + binmin) * fftdata.Df),
									vfi(ffs[binmin + index]))));
					var distx = distasx.Select(x => Math.Pow(x.X - pixel.X, 2) + Math.Pow(x.Y - pixel.Y, 2));
					var dlist = distx.ToList(); // no dc
					bin = binmin + dlist.IndexOf(dlist.Min());

					var vm = MyVModel;
					if ( bin < ffs.Length)
					{
						var vfun = useRight ? right.FundamentalVolts : left.FundamentalVolts;
						return ValueTuple.Create(bin * fftdata.Df, ffs[bin], vfun);
					}
				}
			}
			catch (Exception )
			{
			}
			return ValueTuple.Create(0.0,0.0,0.0);
		}

		private double ToD(string stri)
		{
			return MathUtil.ToDouble(stri);
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot(string plotFormat = "dBV")
		{
			var thdFreq = MyVModel;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			PlotUtil.InitializeLogFreqPlot(myPlot, plotFormat);

			myPlot.Axes.SetLimitsX(Math.Log10(ToD(thdFreq.GraphStartFreq)), 
				Math.Log10(ToD(thdFreq.GraphEndFreq)), myPlot.Axes.Bottom);

			myPlot.Axes.SetLimitsY( ToD(thdFreq.RangeBottomdB), ToD(thdFreq.RangeTopdB), myPlot.Axes.Left);

			myPlot.Title("Spectrum");
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

            SpectrumViewModel thd = MyVModel;
            myPlot.Axes.SetLimits(Math.Log10(ToD(thd.GraphStartFreq)), Math.Log10(ToD(thd.GraphEndFreq)), 
				Math.Log10(ToD(thd.RangeBottom)) - 0.00000001, Math.Log10(ToD(thd.RangeTop)));  // - 0.000001 to force showing label
            myPlot.Title("Spectrum");
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel(GraphUtil.GetFormatTitle(plotFormat));

			fftPlot.Refresh();
        }

        /// <summary>
        /// Plot all of the spectral data values
        /// </summary>
        /// <param name="data"></param>
        void PlotValues(DataTab<SpectrumViewModel>? page, int measurementNr)
        {
			if (page == null)
				return;

			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			myPlot.Clear();

			var specVm = MyVModel;
			bool useLeft = specVm.ShowLeft;	// dynamically update these
			bool useRight = specVm.ShowRight;
			var fftData = page.FreqRslt;
			if (fftData == null)
				return;

			double[] freqLogX = Enumerable.Range(1, fftData.Left.Length-1).
								Select(x => Math.Log10(x * fftData.Df)).ToArray();
			//
			double[] leftdBV = [];
			double[] rightdBV = [];
			string plotForm = MyVModel.PlotFormat;

			// add a scatter plot to the plot
			var lineWidth = MyVModel.ShowThickLines ? _Thickness : 1;	// so it dynamically updates

			if (useLeft)
			{
				double maxleft = Math.Max(1e-20, fftData.Left.Max());
				// the usual dbv display
				var fvi = GraphUtil.GetLogFormatter(plotForm, maxleft);
				leftdBV = fftData.Left.Skip(1).Select(fvi).ToArray();

				Scatter plotLeft = myPlot.Add.Scatter(freqLogX, leftdBV);
				plotLeft.LineWidth = lineWidth;
				plotLeft.Color = QaLibrary.BlueColor;  // Blue
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
				if (useLeft)
					plotRight.Color = QaLibrary.RedXColor; // Red transparant
				else
					plotRight.Color = QaLibrary.RedColor; // Red
				plotRight.MarkerSize = 1;
			}

			fftPlot.Refresh();
		}

		public void UpdateGraph(bool settingsChanged)
        {
            fftPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			fftPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			SpectrumViewModel thd = MyVModel;

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

			PlotValues(PageData, resultNr++);


            if( PageData?.FreqRslt != null)
            {
				ShowHarmonicMarkers(PageData);
				ShowPowerMarkers(PageData);
			}

			fftPlot.Refresh();
		}
	}
}