using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Windows;

// various things for the thd vs frequency activity

namespace QA40xPlot.Actions
{

	public class ActImd : ActBase
    {
        public ImdData Data { get; set; }                  // Data used in this form instance

        private readonly Views.PlotControl fftPlot;

        private ImdMeasurementResult MeasurementResult;

        private float _Thickness = 2.0f;

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActImd(ref ImdData data, Views.PlotControl graphFft)
        {
            Data = data;
            
			fftPlot = graphFft;

			ct = new CancellationTokenSource();

            // TODO: depends on graph settings which graph is shown
            MeasurementResult = new(ViewSettings.Singleton.ImdVm);
			UpdateGraph(true);
        }

        public void DoCancel()
        {
            ct.Cancel();
		}

		/// <summary>
		/// Update the generator voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateGeneratorVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.ImdVm;
			vm.Gen1Voltage = QaLibrary.ConvertVoltage(vm.GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)vm.GeneratorUnits).ToString();
        }

		/// <summary>
		/// Update the amplifier output voltage in the textbox based on the selected unit.
		/// If the unit changes then the voltage will be converted
		/// </summary>
		public void UpdateAmpOutputVoltageDisplay()
        {
			var vm = ViewSettings.Singleton.ImdVm;
			switch (vm.OutputUnits)
            {
                case 0: // mV
                    vm.OutVoltage = ((int)QaLibrary.ConvertVoltage(vm.AmpOutputAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.MilliVolt));
                    break;
                case 1: // V
					vm.OutVoltage = QaLibrary.ConvertVoltage(vm.AmpOutputAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
                    break;
                case 2: // dB
					vm.OutVoltage = QaLibrary.ConvertVoltage(vm.AmpOutputAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.dBV);
                    break;
            }
        }

		/// <summary>
		/// Generator type changed
		/// </summary>
		public void UpdateGeneratorParameters()
        {
            var vm = ViewSettings.Singleton.ImdVm;
			switch (vm.MeasureType)
            {
                case 0: // Input voltage
					vm.ReadVoltage = false;
					vm.ReadOutPower = true;
					vm.ReadOutVoltage = true;
					UpdateGeneratorVoltageDisplay();
					break;
				case 1: // Output voltage
					vm.ReadVoltage = true;
					vm.ReadOutPower = true;
					vm.ReadOutVoltage = false;
					UpdateGeneratorVoltageDisplay();
					break;
				case 2: // Output power
					vm.ReadVoltage = true;
					vm.ReadOutPower = false;
					vm.ReadOutVoltage = true;
					//vm.OutPower = MathUtil.ParseTextToDouble(txtAmplifierOutputPower.Text, MeasurementSettings.AmpOutputPower);
					//vm.AmpLoad = MathUtil.ParseTextToDouble(txtOutputLoad.Text, MeasurementSettings.Load);
					//vm.Voltage = Math.Sqrt(MeasurementSettings.AmpOutputPower * MeasurementSettings.Load);      // Expected output DUT amplitude in Volts
					vm.GeneratorUnits = (int)E_VoltageUnit.Volt;                                           // Expected output DUT amplitude in dBV
                    UpdateAmpOutputVoltageDisplay();
                    break;
            }
        }

        private async Task showMessage(String msg, int delay = 0)
        {
            var vm = ViewModels.ViewSettings.Singleton.Main;
            await vm.SetProgressMessage(msg, delay);
		}

		private async Task showProgress(int progress, int delay = 0)
		{
			var vm = ViewModels.ViewSettings.Singleton.Main;
			await vm.SetProgressBar(progress, delay);
		}

		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();
			var vf = this.MeasurementResult?.FrequencySteps;
			if( vf == null )
			{
				return null;
			}
			var vm = ViewSettings.Singleton.SpectrumVm;
			var sampleRate = MathUtil.ParseTextToUint(vm.SampleRate, 0);
			var fftsize = vf[0].fftData.Left.Length;
			var binSize = QaLibrary.CalcBinSize(sampleRate, (uint)fftsize);
			if (vf != null && vf.Count > 0)
			{
				db.LeftData = vf[0].fftData.Left.ToList();
				db.RightData = vf[0].fftData.Right.ToList();
				var frqs = Enumerable.Range(0, fftsize).ToList();
				var frequencies = frqs.Select(x => x * binSize).ToList(); // .Select(x => x * binSize);
				db.FreqData = frequencies;
			}
			return db;
		}


		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurementSteps(ImdMeasurementResult msr, CancellationToken ct)
        {
			// Setup
			ImdViewModel thd = msr.MeasurementSettings;

			var freq = MathUtil.ParseTextToDouble(thd.Gen1Frequency, 0);
			var freq2 = MathUtil.ParseTextToDouble(thd.Gen2Frequency, 0);
			var sampleRate = MathUtil.ParseTextToUint(thd.SampleRate, 0);
			if (freq == 0 || freq2 == 0 || sampleRate == 0 || !FreqRespViewModel.FftSizes.Contains(thd.FftSize))
            {
                MessageBox.Show("Invalid settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = FreqRespViewModel.FftActualSizes.ElementAt(FreqRespViewModel.FftSizes.IndexOf(thd.FftSize));

            // Check if REST interface is available and device connected
            if (await QaLibrary.CheckDeviceConnected() == false)
                return false;

            // ********************************************************************  
            // Load a settings we want
            // ********************************************************************  
            if (msr.FrequencySteps.Count == 0)
            {
				await Qa40x.SetDefaults();
			}

			await Qa40x.SetSampleRate(sampleRate);
            await Qa40x.SetBufferSize(fftsize);
			await Qa40x.SetWindowing(thd.WindowingMethod);
            await Qa40x.SetRoundFrequencies(true);
			await Qa40x.SetInputRange((int)thd.Attenuation);

			try
            {
                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;

                // ********************************************************************
                // Calculate frequency steps to do
                // ********************************************************************
                var binSize = QaLibrary.CalcBinSize(sampleRate, fftsize);
				// Generate a list of frequencies
				double[] stepFrequencies = [freq, freq2];
                // Translate the generated list to bin center frequencies
                var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize).ToArray();

                if(msr.NoiseFloor == null)
                {
					// ********************************************************************
					// Do noise floor measurement with source off
					// ********************************************************************
					await showMessage($"Determining noise floor.");
					await Qa40x.SetOutputSource(OutputSources.Off);
					await Qa40x.DoAcquisition();    // do a single acquisition for settling
					msr.NoiseFloor = await QaLibrary.DoAcquisitions(thd.Averages, ct);
					if (ct.IsCancellationRequested)

						return false;
				}

				var genVolt = MathUtil.ParseTextToDouble(thd.Gen1Voltage, 0.001);
				double amplitudeSetpoint1dBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
				genVolt = MathUtil.ParseTextToDouble(thd.Gen2Voltage, 0.001);
				double amplitudeSetpoint2dBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

				// ********************************************************************
				// Do a spectral sweep once (ignore the true)
				// ********************************************************************
				while(true)
				{
					// now do the step measurement
					await showMessage($"Measuring spectrum.");
					await showProgress(0);

                    // Set the generators
                    await Qa40x.SetGen1(stepBinFrequencies[0], amplitudeSetpoint1dBV, thd.UseGenerator);
					await Qa40x.SetGen2(stepBinFrequencies[1], amplitudeSetpoint2dBV, thd.UseGenerator2);
					// for the first go around, turn on the generator
					if ( thd.UseGenerator || thd.UseGenerator2)
                    {
						await Qa40x.SetOutputSource(OutputSources.Sine);            // We need to call this to make the averages reset
					}
					else
					{
						await Qa40x.SetOutputSource(OutputSources.Off);            // We need to call this to make the averages reset
					}

					LeftRightSeries lrfs = await QaLibrary.DoAcquisitions(thd.Averages, ct);
                    if (ct.IsCancellationRequested)
                        break;

					uint fundamentalBin = QaLibrary.GetBinOfFrequency(stepBinFrequencies[0], binSize);
                    if (fundamentalBin >= lrfs.FreqRslt.Left.Length)               // Check in bin within range
                        break;

                    ImdStep step = new()
                    {
						Gen1Freq = stepBinFrequencies[0],
						Gen2Freq = stepBinFrequencies[1],
						Gen1Volts = QaLibrary.ConvertVoltage(amplitudeSetpoint1dBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
						Gen2Volts = QaLibrary.ConvertVoltage(amplitudeSetpoint2dBV, E_VoltageUnit.dBV, E_VoltageUnit.Volt),
						fftData = lrfs.FreqRslt,
                        timeData = lrfs.TimeRslt
                    };

					step.Left = ChannelCalculations(binSize, amplitudeSetpoint1dBV, step, msr, false);
					step.Right = ChannelCalculations(binSize, amplitudeSetpoint1dBV, step, msr, true);

					// Add step data to list
					msr.FrequencySteps.Clear();
					msr.FrequencySteps.Add(step);

					// For now clear measurements to allow only one until we have a UI to manage them.
					ViewSettings.Singleton.ImdVm.HasExport = true;

					// the first one gets added. after that it's the same object
					if (Data.Measurements.Count == 0)
						Data.Measurements.Add(msr);

					ClearPlot();
					UpdateGraph(false);

					// we always run this exactly once
                    break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Show message
			await showMessage($"Measurement finished");

            return !ct.IsCancellationRequested;
        }

        private void AddAMarker(ImdMeasurementResult fmr, double frequency, bool isred = false)
		{
			var vm = ViewSettings.Singleton.ImdVm;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			var sampleRate = Convert.ToUInt32(vm.SampleRate);
			var fftsize = ImdViewModel.FftActualSizes.ElementAt(ImdViewModel.FftSizes.IndexOf(vm.FftSize));
			int bin = (int)QaLibrary.GetBinOfFrequency(frequency, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
            double markVal = 0;
			if( vm.ShowPercent)
			{
				if (!vm.ShowLeft)
				{
					double maxright = fmr.FrequencySteps[0].fftData.Left.Max();
					markVal = 100 * fmr.FrequencySteps[0].fftData.Right[bin] / maxright;
				}
				else
				{
					double maxleft = fmr.FrequencySteps[0].fftData.Right.Max();
					markVal = 100 * fmr.FrequencySteps[0].fftData.Left[bin] / maxleft;
				}
			}
			else
			{
				if (!vm.ShowLeft)
				{
					markVal = 20 * Math.Log10(fmr.FrequencySteps[0].fftData.Right[bin]);
				}
				else
				{
					markVal = 20 * Math.Log10(fmr.FrequencySteps[0].fftData.Left[bin]);
				}
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
			if (vm.ShowPercent)
			{
				var mymark = myPlot.Add.Marker(Math.Log10(frequency), Math.Log10(markVal),
					MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
				mymark.LegendText = string.Format("{1}: {0:F6}", markVal, (int)frequency);
			}
			else
			{
				var mymark = myPlot.Add.Marker(Math.Log10(frequency), markVal,
					MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), markerCol);
				mymark.LegendText = string.Format("{1}: {0:F1}", markVal, (int)frequency);
			}
		}

		private void ShowHarmonicMarkers(ImdMeasurementResult fmr)
        {
            var vm = ViewSettings.Singleton.ImdVm;
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if ( vm.ShowMarkers)
            {
                AddAMarker(fmr, fmr.FrequencySteps[0].Gen1Freq);
				AddAMarker(fmr, fmr.FrequencySteps[0].Gen2Freq);
				var flist = fmr.FrequencySteps[0].Left.Harmonics.OrderBy(x => x.Frequency).ToArray();
				var cn = flist.Length;
				for (int i=0; i<cn; i++)
                {
                    var frq = flist[i].Frequency;
					AddAMarker(fmr, frq);
				}
			}
		}

		private void ShowPowerMarkers(ImdMeasurementResult fmr)
		{
			var vm = ViewSettings.Singleton.ImdVm;
            List<double> freqchecks = new List<double> { 50, 60 };
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			if (vm.ShowPowerMarkers)
			{
				var sampleRate = Convert.ToUInt32(vm.SampleRate);
				var fftsize = ImdViewModel.FftActualSizes.ElementAt(ImdViewModel.FftSizes.IndexOf(vm.FftSize));
                var nfloor = MeasurementResult.FrequencySteps[0].Left.Average_NoiseFloor_dBV;   // Average noise floor in dBVolts after the fundamental
                double fsel = 0;
                double maxdata = -10;
                // find if 50 or 60hz is higher, indicating power line frequency
				foreach(double freq in freqchecks)
				{
					var actfreq = QaLibrary.GetNearestBinFrequency(freq, sampleRate, fftsize);
					int bin = (int)QaLibrary.GetBinOfFrequency(actfreq, sampleRate, fftsize);        // Calculate bin of the harmonic frequency
					var data = vm.ShowLeft ? fmr.FrequencySteps[0].fftData.Left[bin] : fmr.FrequencySteps[0].fftData.Right[bin];
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
                    var data = vm.ShowLeft ? fmr.FrequencySteps[0].fftData.Left[bin] : fmr.FrequencySteps[0].fftData.Right[bin];
                    double udif = 20 * Math.Log10(data);
                    AddAMarker(fmr, actfreq, true);
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
			Addif(ref harmFreqs, f2 - f1);
			Addif(ref harmFreqs, f2 + f1);
			Addif(ref harmFreqs, 2*f1 - f2);
			Addif(ref harmFreqs, 2*f2 - f1);
			Addif(ref harmFreqs, 3*f1 - 2*f2);
			Addif(ref harmFreqs, 3*f2 - 2*f1);
			Addif(ref harmFreqs, 4 * f1 - 3 * f2);
			Addif(ref harmFreqs, 4 * f2 - 3 * f1);
			Addif(ref harmFreqs, 3 * f2 - f1);
			Addif(ref harmFreqs, 3 * f1 - f2);
			return harmFreqs.ToArray();
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
		private ImdStepChannel ChannelCalculations(double binSize, double generatorAmplitudeDbv, ImdStep step, ImdMeasurementResult msr, bool isRight)
		{
            uint fundamental1Bin = QaLibrary.GetBinOfFrequency(step.Gen1Freq, binSize);
			uint fundamental2Bin = QaLibrary.GetBinOfFrequency(step.Gen2Freq, binSize);
			var ffts = isRight ? step.fftData.Right : step.fftData.Left;
			var lfdata = step.timeData.Left;
			double allvolts = Math.Sqrt(lfdata.Select(x => x * x).Sum() / lfdata.Count());  // use the time data for best accuracy gain math

			ImdStepChannel channelData = new()
			{
				Fundamental1_V = ffts[fundamental1Bin],
				Fundamental1_dBV = 20 * Math.Log10(ffts[fundamental1Bin]),
				Fundamental2_V = ffts[fundamental2Bin],
				Fundamental2_dBV = 20 * Math.Log10(ffts[fundamental2Bin]),
				Total_V = allvolts,
				Gain_dB = 20 * Math.Log10(ffts[fundamental1Bin] / Math.Pow(10, generatorAmplitudeDbv / 20))
			};
			// Calculate average noise floor
			var noiseFlr = (msr.NoiseFloor == null) ? null : (isRight ? msr.NoiseFloor.FreqRslt.Left : msr.NoiseFloor.FreqRslt.Right);
			if (noiseFlr != null)
			{
				channelData.Average_NoiseFloor_V = noiseFlr.Average();   // Average noise floor in Volts after the fundamental
				var v2 = noiseFlr.Select(x => x * x).Sum() / (1.5 * noiseFlr.Length);    // 1.5 for hann window Squared noise floor in Volts after the fundamental
				channelData.TotalNoiseFloor_V = Math.Sqrt(v2);   // Average noise floor in Volts after the fundamental
				channelData.Average_NoiseFloor_dBV = 20 * Math.Log10(channelData.TotalNoiseFloor_V);         // Average noise floor in dBV
			}

			// Reset harmonic distortion variables
			double distortionSqrtTotal = 0;
			double distortionSqrtTotalN = 0;
			double distortionD6plus = 0;
			double voltagetotal = 0;
			var binlist = new List<uint> { fundamental1Bin - 1, fundamental1Bin, fundamental1Bin + 1, fundamental2Bin - 1, fundamental2Bin, fundamental2Bin + 1 };
			for (uint i=0; i<ffts.Length; i++)
			{
				if( ! binlist.Contains(i))
				{
					voltagetotal += ffts[i] * ffts[i];		// squared sum
				}
			}
			voltagetotal = Math.Sqrt(voltagetotal);
            // Loop through harmonics up tot the 12th
			var vsum = Math.Sqrt(channelData.Fundamental1_V * channelData.Fundamental1_V + channelData.Fundamental2_V * channelData.Fundamental2_V);
			channelData.Fundamental_AllV = vsum;

			channelData.Snr_dB = 20 * Math.Log10(voltagetotal / vsum);
			var harmonicFreq = MakeHarmonics(step.Gen1Freq, step.Gen2Freq);
			var maxfreq = binSize * ffts.Count();

			for (int harmonicNumber = 0; harmonicNumber < harmonicFreq.Length; harmonicNumber++)                                                  // For now up to 12 harmonics, start at 2nd
            {
                double harmonicFrequency = harmonicFreq[harmonicNumber];
				if (harmonicFrequency >= maxfreq)
					continue;

                uint bin = QaLibrary.GetBinOfFrequency(harmonicFrequency, binSize);        // Calculate bin of the harmonic frequency

                double amplitude_V = (bin >= ffts.Length) ? 0 : ffts[bin];
                double noise_V = channelData.TotalNoiseFloor_V;

				double amplitude_dBV = 20 * Math.Log10(amplitude_V);
                double thd_Percent = (amplitude_V / vsum) * 100;
				double thdN_Percent = (noiseFlr == null || bin >= noiseFlr.Length) ? 0 : ((amplitude_V - noiseFlr[bin]) / vsum) * 100;

				HarmonicData harmonic = new()
                {
                    HarmonicNr = harmonicNumber,
                    Frequency = harmonicFrequency,
                    Amplitude_V = amplitude_V,
                    Amplitude_dBV = amplitude_dBV,
                    Thd_Percent = thd_Percent,
					Thd_dB = 20 * Math.Log10(thd_Percent / 100.0),
					Thd_dBN = 20 * Math.Log10(thdN_Percent / 100.0),
					NoiseAmplitude_V = (bin >= ffts.Length) ? 0 : noiseFlr[bin]
                };

                if (harmonicNumber >= 6)
                    distortionD6plus += Math.Pow(amplitude_V, 2);

				distortionSqrtTotal += Math.Pow(amplitude_V, 2);
				distortionSqrtTotalN += Math.Pow(amplitude_V, 2);
				channelData.Harmonics.Add(harmonic);
            }

			// Calculate THD
			if (distortionSqrtTotal != 0)
			{
				channelData.Thd_Percent = (Math.Sqrt(distortionSqrtTotal) / vsum) * 100;
				channelData.Thd_PercentN = (Math.Sqrt(distortionSqrtTotal) / vsum) * 100;
				channelData.Thd_dB = 20 * Math.Log10(channelData.Thd_Percent / 100.0);
				var Thdn_Percent = (Math.Sqrt(distortionSqrtTotalN) / vsum) * 100;
				channelData.Thd_dBN = 20 * Math.Log10(Thdn_Percent / 100.0);
			}

			// Calculate D6+ (D6 - D12)
			if (distortionD6plus != 0)
            {
                channelData.D6Plus_dBV = 20 * Math.Log10(Math.Sqrt(distortionD6plus));
                channelData.ThdPercent_D6plus = 100 * Math.Sqrt(distortionD6plus) / vsum;
                channelData.ThdDbD6plus = 20 * Math.Log10(channelData.ThdPercent_D6plus / 100.0);
            }

            // If load not zero then calculate load power
            if (msr.MeasurementSettings.AmpLoad != 0)
                channelData.Power_Watt = Math.Pow(vsum, 2) / msr.MeasurementSettings.AmpLoad;

            return channelData;
        }


        /// <summary>
        /// Clear the plot
        /// </summary>
        void ClearPlot()
        {
            fftPlot.ThePlot.Clear();
            fftPlot.Refresh();
        }

		void SetTheTitle(ScottPlot.Plot myPlot)
		{
			var imdVm = ViewSettings.Singleton.ImdVm;
			if( imdVm.IntermodType == "Custom")
				myPlot.Title("Intermodulation Distortion");
			else
			{
				var vsa = imdVm.IntermodType.Split('(').First();
				myPlot.Title(String.Format("{0} Intermodulation Distortion", vsa ));
			}
		}

		/// <summary>
		/// Ititialize the THD % plot
		/// </summary>
		void InitializefftPlot()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			InitializePctFreqPlot(myPlot);
			var imdVm = ViewSettings.Singleton.ImdVm;

            ImdViewModel thd = ViewSettings.Singleton.ImdVm;
            myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(thd.GraphStartFreq)), Math.Log10(Convert.ToInt32(thd.GraphEndFreq)), 
				Math.Log10(Convert.ToDouble(thd.RangeBottom)) - 0.00000001, Math.Log10(Convert.ToDouble(thd.RangeTop)));  // - 0.000001 to force showing label
			SetTheTitle(myPlot);
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("%");

			fftPlot.Refresh();
        }

        /// <summary>
        /// Plot the THD % graph
        /// </summary>
        /// <param name="data"></param>
        void PlotValues(ImdMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel)
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
			myPlot.Clear();

			var imdVm = ViewSettings.Singleton.ImdVm;
			bool leftChannelEnabled = imdVm.ShowLeft && showLeftChannel;	// dynamically update these
			bool rightChannelEnabled = imdVm.ShowRight && showRightChannel;

			var fftData = MeasurementResult.FrequencySteps[0].fftData;

			List<double> freqX = [];
			List<double> dBV_Left_Y = [];
			List<double> dBV_Right_Y = [];
			double frequency = 0;
			double maxleft = fftData.Left.Max();
			double maxright = fftData.Right.Max();

			for (int f = 1; f < fftData.Left.Length; f++)   // Skip dc bin
			{
				frequency += fftData.Df;
				freqX.Add(frequency);
				if(imdVm.ShowPercent)
				{
					if (leftChannelEnabled)
					{
						var lv = fftData.Left[f];       // V of input data
						var lvp = 100 * lv / maxleft;
						dBV_Left_Y.Add(Math.Log10(lvp));
					}
					if (rightChannelEnabled)
					{
						var lv = fftData.Right[f];       // V of input data
						var lvp = 100 * lv / maxright;
						dBV_Right_Y.Add(Math.Log10(lvp));
					}
				}
				else
				{
					if (leftChannelEnabled)
					{
						var lv = fftData.Left[f];       // V of input data
						dBV_Left_Y.Add(20*Math.Log10(lv));
					}
					if (rightChannelEnabled)
					{
						var lv = fftData.Right[f];       // V of input data
						dBV_Right_Y.Add(20*Math.Log10(lv));
					}
				}
			}

			// add a scatter plot to the plot
			double[] logFreqX = freqX.Select(Math.Log10).ToArray();
			double[] logHTot_Left_Y = dBV_Left_Y.ToArray();
			double[] logHTot_Right_Y = dBV_Right_Y.ToArray();

			var showThick = ViewSettings.Singleton.ImdVm.ShowThickLines;	// so it dynamically updates

			if (leftChannelEnabled)
			{
				Scatter plotTot_Left = myPlot.Add.Scatter(logFreqX, logHTot_Left_Y);
				plotTot_Left.LineWidth = showThick ? _Thickness : 1;
				plotTot_Left.Color = new ScottPlot.Color(1, 97, 170, 255);  // Blue
				plotTot_Left.MarkerSize = 1;
			}

			if (rightChannelEnabled)
			{
				Scatter plotTot_Right = myPlot.Add.Scatter(logFreqX, logHTot_Right_Y);
				plotTot_Right.LineWidth = showThick ? _Thickness : 1;
				if (leftChannelEnabled)
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 120); // Red transparant
				else
					plotTot_Right.Color = new ScottPlot.Color(220, 5, 46, 255); // Red
				plotTot_Right.MarkerSize = 1;
			}

			fftPlot.Refresh();
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot()
        {
			ScottPlot.Plot myPlot = fftPlot.ThePlot;
            InitializeMagFreqPlot(myPlot);

			var imdVm = ViewSettings.Singleton.ImdVm;

			myPlot.Axes.SetLimits(Math.Log10(Convert.ToInt32(imdVm.GraphStartFreq)), Math.Log10(Convert.ToInt32(imdVm.GraphEndFreq)), imdVm.RangeBottomdB, imdVm.RangeTopdB);
			SetTheTitle(myPlot);
			myPlot.XLabel("Frequency (Hz)");
			myPlot.YLabel("dBV");

            fftPlot.Refresh();
        }

		// this calculates gain using all input voltage because we use it to set the attenuator
		private async Task<double> CalculateInVolts()
		{
			var domore = await PerformMeasurementSteps(MeasurementResult, ct.Token);
			if (domore && MeasurementResult.FrequencySteps?.Count > 0)
			{
				return MeasurementResult.FrequencySteps[0].Left.Total_V;
			}
			return 1.0;
		}

		/// <summary>
		///  Start measurement button click
		/// </summary>
		public async void StartMeasurement()
        {
			var imdVm = ViewSettings.Singleton.ImdVm;
			if (imdVm.IsRunning)
			{
                MessageBox.Show("Device is already running");
				return;
			}
			imdVm.IsRunning = true;
            ct = new();

			// Clear measurement result
			MeasurementResult = new(imdVm)
			{
				CreateDate = DateTime.Now,
				Show = true,                                      // Show in graph
			};
			Data.Measurements.Clear();

			if (imdVm.DoAutoAttn)
			{
				double inpVolts = 0;
				var msr = MeasurementResult.MeasurementSettings;
				msr.Attenuation = 42;        // set to max attenuation
				imdVm.Attenuation = 42;    // to update the gui while testing
				inpVolts = await CalculateInVolts();
				Debug.WriteLine("inpvolts={0}", inpVolts);
				msr.Attenuation = QaLibrary.DetermineAttenuation(20 * Math.Log10(inpVolts));
				imdVm.Attenuation = msr.Attenuation;    // to update the gui while testing
			}

			var rslt = true;
			rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
            var fftsize = imdVm.FftSize;
            var sampleRate = imdVm.SampleRate;
            var atten = imdVm.Attenuation;
            if (rslt)
            {
                await showMessage("Running");
                while (!ct.IsCancellationRequested)
                {
                    if (imdVm.FftSize != fftsize || imdVm.SampleRate != sampleRate || imdVm.Attenuation != atten)
                    {
						fftsize = imdVm.FftSize;
						sampleRate = imdVm.SampleRate;
						atten = imdVm.Attenuation;
						MeasurementResult = new(imdVm)
                        {
                            CreateDate = DateTime.Now,
                            Show = true,                                      // Show in graph
                        };
                    }
                    else
                    {
                        ViewSettings.Singleton.ImdVm.CopyPropertiesTo(MeasurementResult.MeasurementSettings);
                    }
					rslt = await PerformMeasurementSteps(MeasurementResult, ct.Token);
					if (ct.IsCancellationRequested || !rslt)
						break;
				}
			}
			// Turn the generator off since we leave it on during the loop for settling
			await Qa40x.SetOutputSource(OutputSources.Off);

			imdVm.IsRunning = false;
			await showMessage("");
			ViewSettings.Singleton.ImdVm.HasExport = this.MeasurementResult.FrequencySteps.Count > 0;
			ViewSettings.Singleton.Main.CurrentView = ViewSettings.Singleton.ImdVm;
		}


		/// <summary>
		/// Validate the generator voltage and show red text if invalid
		/// </summary>
		/// <param name="sender"></param>
		//private void ValidateGeneratorAmplitude(object sender)
		//{
		//    if (cmbGeneratorVoltageUnit.SelectedIndex == (int)E_VoltageUnit.MilliVolt)
		//        QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_MV, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_MV);        // mV
		//    else if (cmbGeneratorVoltageUnit.SelectedIndex == (int)E_VoltageUnit.Volt)
		//        QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_V, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_V);     // V
		//    else
		//        QaLibrary.ValidateRangeAdorner(sender, QaLibrary.MINIMUM_GENERATOR_VOLTAGE_DBV, QaLibrary.MAXIMUM_GENERATOR_VOLTAGE_DBV);       // dBV
		//}

		// user entered a new voltage, update the generator amplitude
		public void UpdateGenAmplitude(string value)
        {
			ImdViewModel thd = ViewSettings.Singleton.ImdVm;
            var val = MathUtil.ParseTextToDouble(value, Convert.ToDouble(thd.Gen1Voltage));
			thd.GeneratorAmplitude = QaLibrary.ConvertVoltage(val, (E_VoltageUnit)thd.GeneratorUnits, E_VoltageUnit.dBV);
		}

        // show the latest step values in the table
        public void DrawChannelInfoTable()
        {
			ImdViewModel thd = ViewSettings.Singleton.ImdVm;
			var vm = ViewSettings.Singleton.ImdChannelLeft;
            vm.CalculateChannelValues(MeasurementResult.FrequencySteps[0].Left, Convert.ToDouble( thd.Gen1Frequency), Convert.ToDouble(thd.Gen2Frequency), thd.ShowDataPercent);
		}

		public void UpdateGraph(bool settingsChanged)
        {
            fftPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			fftPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			int resultNr = 0;
			ImdViewModel thd = ViewSettings.Singleton.ImdVm;

			if (!thd.ShowPercent)
            {
                if (settingsChanged)
                {
                    InitializeMagnitudePlot();
                }

                foreach (var result in Data.Measurements.Where(m => m.Show))
                {
					ImdViewModel mvs = result.MeasurementSettings;
					PlotValues(result, resultNr++, mvs.ShowLeft, mvs.ShowRight);
                }
            }
            else
            {
                if (settingsChanged)
                {
                    InitializefftPlot();
                }
          
                foreach (var result in Data.Measurements.Where(m => m.Show))
                {
					ImdViewModel mvs = result.MeasurementSettings;
					PlotValues(result, resultNr++, mvs.ShowLeft, mvs.ShowRight);
				}
			}

            if( MeasurementResult.FrequencySteps.Count > 0)
            {
				ShowHarmonicMarkers(MeasurementResult);
				ShowPowerMarkers(MeasurementResult);
				DrawChannelInfoTable();
			}
		}

	}
}