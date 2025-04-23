using FftSharp;
using QA40x_BareMetal;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
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

    public partial class ActFrequencyResponse : ActBase
    {
        public FrequencyResponseData Data { get; set; }       // Data used in this form instance
		private readonly Views.PlotControl frqrsPlot;
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;

		private FrequencyResponseMeasurementResult MeasurementResult;
		private static FreqRespViewModel MyVModel { get => ViewSettings.Singleton.FreqRespVm; }

		CancellationTokenSource ct;                                 // Measurement cancelation token

        /// <summary>
        /// Constructor
        /// </summary>
        public ActFrequencyResponse(ref FrequencyResponseData data, Views.PlotControl graphFreq, Views.PlotControl graphFft, Views.PlotControl graphTime)
		{
            Data = data;
            frqrsPlot = graphFreq;
			fftPlot = graphFft;
			timePlot = graphTime;

			// Show empty graphs
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 100000, -180, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -2, 2);

			MeasurementResult = new(MyVModel); // TODO. Add to list
            ct = new CancellationTokenSource();

            UpdateGraph(true);
        }

        public void DoCancel()
        {
            ct.Cancel();
		}

		private async Task<(LeftRightSeries?, double[])> CallChirp(CancellationToken ct, double f0, double f1)
		{
			var msr = MeasurementResult.MeasurementSettings;
			var genv = msr.ToGenVoltage(msr.Gen1Voltage, [], GEN_INPUT, LRGains?.Left);
			// output v
			//var chirp = QAMath.CalculateChirp(f0, f1, genv, msr.FftSizeVal, msr.SampleRateVal);
            var chirptwo = Chirps.ChirpVp((int)msr.FftSizeVal, msr.SampleRateVal, genv, f0, f1);
            var chirpy = chirptwo.Item1;
			LeftRightSeries lrfs = await QaUsb.DoAcquireUser(1, ct, chirpy, chirpy, false);
			return (lrfs, chirptwo.Item2);
		}

		/// <summary>
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void StartMeasurement()
		{
			var vm = MyVModel;
			if (!StartAction(vm))
				return;

			vm.HasExport = false;
			ct = new();
												  // Show empty graphs
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 40000, -180, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -2, 2); 
            
            UpdateGraph(true);

			await PerformMeasurement(ct.Token, false);
			await showMessage("Finished");
			vm.IsRunning = false;
            vm.HasExport = MeasurementResult.GainFrequencies.Count > 0;
		}


		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			DataBlob db = new();
            var frsqVm = MyVModel;
			var freqs = this.MeasurementResult.GainFrequencies;
            if (freqs == null || freqs.Count == 0)
                return null;

            db.FreqData = freqs;        // test frequencies
            var ttype = frsqVm.GetTestingType(MeasurementResult.MeasurementSettings.TestType);
            switch( ttype)
            {
                case TestingType.Response:
					if (frsqVm.ShowRight && !frsqVm.ShowLeft)
					{
						db.LeftData = MeasurementResult.GainData.Select(x => x.Imaginary).ToList();
					}
					else
					{
						db.LeftData = MeasurementResult.GainData.Select(x => x.Real).ToList();
					}
					break;
                case TestingType.Gain:
                    var gld = MeasurementResult.GainData.ToArray();
					db.LeftData = FFT.Magnitude(gld).ToList();
					db.PhaseData = FFT.Phase(gld).ToList();
					break;
				case TestingType.Impedance:
                    {
						double rref = ViewSettings.AmplifierLoad;
						db.LeftData = MeasurementResult.GainData.Select(x => rref * ToImpedance(x).Magnitude).ToList();
						// YValues = gainY.Select(x => rref * x.Magnitude/(1-x.Magnitude)).ToArray();
						db.PhaseData = MeasurementResult.GainData.Select(x => ToImpedance(x).Phase).ToList();
					}
					break;
            }
			return db;
		}

        // run a capture to get complex gain at a frequency
        async Task<Complex> GetGain(double showfreq, FreqRespViewModel msr, TestingType ttype)
        {
			if (ct.Token.IsCancellationRequested)
				return new();
			var lfrs = await QaUsb.DoAcquisitions(1, ct.Token);
            if (lfrs == null)
                return new();
			MeasurementResult.FrequencyResponseData = lfrs;
			var ga = CalculateGain(showfreq, lfrs, ttype == TestingType.Response); // gain,phase or gain1,gain2
            return ga;
		}

		public Rect GetDataBounds()
		{
			var msr = MeasurementResult.MeasurementSettings;    // measurement settings
			var vmr = MeasurementResult.GainFrequencies; // test data
			if (vmr == null )
				return Rect.Empty;

			var freqrVm = MyVModel;     // current settings

			Rect rrc = new Rect(0, 0, 0, 0);
			if (vmr.Count == 0)
				return rrc;

            rrc.X = vmr.Min();
            rrc.Width = vmr.Max() - rrc.X;
			var ttype = msr.GetTestingType(msr.TestType);
            var msd = MeasurementResult.GainData;
			if (ttype == TestingType.Response)
			{
                if (freqrVm.ShowLeft)
                {
                    rrc.Y = msd.Min(x => x.Real);
                    rrc.Height = msd.Max(x => x.Real) - rrc.Y;
                    if (freqrVm.ShowRight)
                    {
                        rrc.Y = Math.Min(rrc.Y, msd.Min(x => x.Imaginary));
                        rrc.Height = Math.Max(rrc.Height, msd.Max(x => x.Imaginary) - rrc.Y);
                    }
                }
				else if (freqrVm.ShowRight)
				{
					rrc.Y = msd.Min(x => x.Imaginary);
					rrc.Height = msd.Max(x => x.Imaginary) - rrc.Y;
				}
			}
			else if (ttype == TestingType.Gain)
			{
				rrc.Y = msd.Min(x => x.Magnitude);
				rrc.Height = msd.Max(x => x.Magnitude) - rrc.Y;
			}
            else
            {   // impedance
				double rref = ViewSettings.AmplifierLoad;
				var minL = MeasurementResult.GainData.Min(x => ToImpedance(x).Magnitude);
				var maxL = MeasurementResult.GainData.Max(x => ToImpedance(x).Magnitude);
				var minZohms = rref * minL;
				var maxZohms = rref * maxL;
                rrc.Y = minZohms;
                rrc.Height = maxZohms - minZohms;
			}
			return rrc;
		}

		public ValueTuple<double, double, double> LookupX(double freq)
        {
			var freqs = MeasurementResult.GainFrequencies;
			ValueTuple<double, double, double> tup = ValueTuple.Create(1.0,1.0,1.0);
			if (freqs != null && freqs.Count > 0)
            {
                var values = MeasurementResult.GainData;
                // find nearest frequency from list
                var bin = freqs.Count(x => x < freq)-1;    // find first freq less than me
                if (bin == -1)
                    bin = 0;
                var fnearest = freqs[bin];
                if (bin < (freqs.Count-1) && Math.Abs(freq - fnearest) > Math.Abs(freq - freqs[bin + 1]))
                {
                    bin++;
                }

                var frsqVm = MyVModel;
                var ttype = frsqVm.GetTestingType(frsqVm.TestType);
                switch(ttype)
                {
                    case TestingType.Response:
						// send freq, gain, gain2
						tup = ValueTuple.Create(freqs[bin], values[bin].Real, values[bin].Imaginary);
                        break;
                    case TestingType.Impedance:
                        {   // send freq, ohms, phasedeg
							double rref = ViewSettings.AmplifierLoad;
							var ohms = rref * ToImpedance(MeasurementResult.GainData[bin]).Magnitude;
							tup = ValueTuple.Create(freqs[bin], ohms, 180 * values[bin].Phase / Math.PI);
						}
						break;
                    case TestingType.Gain:
						    // send freq, gain, phasedeg
							tup = ValueTuple.Create(freqs[bin], values[bin].Magnitude, 180 * values[bin].Phase / Math.PI);
						break;
                }
			}
			return tup;
		}

		/// <summary>
		/// Determine the gain curve
		/// </summary>
		/// <param name="stepBinFrequencies">the frequencies to test at</param>
		/// <param name="voltagedBV">the sine generator voltage</param>
		/// <returns></returns>
		private async Task<bool> RunFreqTest(double[] stepBinFrequencies, double voltagedBV)
        {
            var frqrsVm = MyVModel;
            var msr = MeasurementResult.MeasurementSettings;
			// Check if cancel button pressed
			if (ct.IsCancellationRequested)
                return false;

            QaUsb.SetOutputSource(OutputSources.Sine);

            var ttype = frqrsVm.GetTestingType(msr.TestType);

            try
            {

                if (ct.IsCancellationRequested)
                    return false;
                for (int steps = 0; steps < stepBinFrequencies.Count(); steps++)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    var dfreq = stepBinFrequencies[steps];
                    if (dfreq > 0)
                        QaUsb.SetGen1(dfreq, voltagedBV, true);
                    else
                        QaUsb.SetGen1(1, voltagedBV, false);

                    if (msr.Averages > 0)
                    {
                        List<Complex> readings = new();
                        for (int j = 0; j < msr.Averages; j++)
                        {
                            var genVolt = Math.Pow(10, voltagedBV/20);
							await showMessage(string.Format($"Checking + {dfreq:0} Hz at {genVolt:0.###}V"));   // need a delay to actually see it
                            var ga = await GetGain(dfreq, msr, ttype);
                            readings.Add(ga);
                        }
                        var total = Complex.Zero;
                        foreach (var f in readings)
                        {
                            total += f;
                        }
                        MeasurementResult.GainData.Add(total / msr.Averages);
                    }
                    else
                    {
                        await showMessage(string.Format("Checking + {0:0}", dfreq));   // need a delay to actually see it
                        var ga = await GetGain(dfreq, msr, ttype);
                        MeasurementResult.GainData.Add(ga);
                    }
                    if (MeasurementResult.FrequencyResponseData.FreqRslt != null)
                    {
                        QaLibrary.PlotMiniFftGraph(fftPlot, MeasurementResult.FrequencyResponseData.FreqRslt, true, false);
                        QaLibrary.PlotMiniTimeGraph(timePlot, MeasurementResult.FrequencyResponseData.TimeRslt, dfreq, true, false);
                    }
                    MeasurementResult.GainFrequencies.Add(dfreq);
                    UpdateGraph(false);
                    if (!frqrsVm.IsTracking)
                    {
                        frqrsVm.RaiseMouseTracked("track");
                    }
                }
			}
            catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
            return true;
		}

		/// <summary>
		/// Determine the gain curve
		/// </summary>
		/// <param name="stepBinFrequencies">the frequencies to test at</param>
		/// <param name="voltagedBV">the sine generator voltage</param>
		/// <returns></returns>
		private async Task<bool> RunChirpTest(double voltagedBV)
		{
			var frqrsVm = MyVModel;
			var msr = MeasurementResult.MeasurementSettings;
			// Check if cancel button pressed
			if (ct.IsCancellationRequested)
				return false;

			//await Qa40x.SetOutputSource(OutputSources.ExpoChirp);

			var ttype = frqrsVm.GetTestingType(msr.TestType);

			try
			{
                //await Qa40x.SetExpoChirpGen(voltagedBV, 0, 0, false);               // use right as reference on input
                //if (ct.IsCancellationRequested)
                //	return false;
                var startf = MathUtil.ToDouble(msr.StartFreq);
                var endf = MathUtil.ToDouble(msr.EndFreq);
                var endfrq = Math.Min(endf, msr.SampleRateVal / 4);

				//var lfrs = await QaLibrary.DoAcquisitions(1, ct.Token);
				var lfrs2 = await CallChirp(ct.Token, startf/2, endfrq * 2);
                var lfrs = lfrs2.Item1;
                var filter = lfrs2.Item2;
				if (lfrs?.TimeRslt == null)
                    return false;
				if (ct.IsCancellationRequested)
					return false;

                Complex[] spectrum_measured = [];
                Complex[] spectrum_ref = [];
				var flength = lfrs.TimeRslt.Left.Length / 2;        // we want half this since freq is symmetric

				if (ttype == TestingType.Response)
                {
					var myLeft = Chirps.NormalizeAndComputeFft(lfrs.TimeRslt.Left, filter, 1 / lfrs.TimeRslt.dt, true,
						0.01, 0.5, 0.0005, 0.02);
                    spectrum_measured = myLeft.Item2;
					var myRight = Chirps.NormalizeAndComputeFft(lfrs.TimeRslt.Left, filter, 1 / lfrs.TimeRslt.dt, true,
						0.01, 0.5, 0.0005, 0.02);
                    spectrum_ref = myRight.Item2;
				}
                else
                {
					// Left channel
					var window = new FftSharp.Windows.Rectangular();    // best?
					double[] windowed_measured = window.Apply(lfrs.TimeRslt.Left, true);
					spectrum_measured = FFT.Forward(windowed_measured);

					double[] windowed_ref = window.Apply(lfrs.TimeRslt.Right, true);
					spectrum_ref = FFT.Forward(windowed_ref);
				}

				spectrum_measured = spectrum_measured.Take(flength).ToArray();
				spectrum_ref = spectrum_ref.Take(flength).ToArray();
				var m2 = Math.Sqrt(2);
				var nca2 = (int)(0.01 + 1 / lfrs.TimeRslt.dt);      // total time in tics = sample rate
				var df = nca2 / (double)flength / 2;                // ???

                // trim the three vectors to the frequency range of interest
				MeasurementResult.GainFrequencies = Enumerable.Range(0, spectrum_measured.Length).Select(x => x * df).ToList();
                var gfr = MeasurementResult.GainFrequencies;
                // restrict the data to only the frequency spectrum
                var trimf = gfr.Count(x => x < startf);
                var trimEnd = gfr.Count(x => x <= endf) - trimf;
                gfr = gfr.Skip(trimf).Take(trimEnd).ToList();
                var mx = spectrum_measured.Skip(trimf).Take(trimEnd).ToArray();
                var mref = spectrum_ref.Skip(trimf).Take(trimEnd).ToArray();
                // format the gain vectors as desired
                MeasurementResult.GainFrequencies = gfr;
				switch (ttype)
                {
					case TestingType.Response:
                        // left, right are magnitude. left uses right as reference
                        var ts = lfrs.TimeRslt.Right;
						var totalV = Math.Sqrt(ts.Select(x => x
                        
                        
                        * x).Sum() / ts.Length); // rms voltage
                        if( totalV > .001)
						{   // if right channel is active use it as reference
							MeasurementResult.GainData = mx.Zip(mref,
								(l, r) => { return new Complex((l / r).Magnitude, r.Magnitude * m2); }).ToList();
						}
                        else
                        {
							MeasurementResult.GainData = mx.Zip(mref,
								(l, r) => { return new Complex(l.Magnitude * m2, r.Magnitude * m2); }).ToList();
						}
						break;
					case TestingType.Gain:
					case TestingType.Impedance:
                        // here complex value is the fft data left / right
						MeasurementResult.GainData = mx.Zip(mref, (l, r) => { return l / r; }).ToList();
						break;
				}

				UpdateGraph(false);
				if (!frqrsVm.IsTracking)
				{
					frqrsVm.RaiseMouseTracked("track");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return true;
		}

		/// <summary>
		/// Perform the measurement
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		async Task<bool> PerformMeasurement(CancellationToken ct, bool continuous)
        {
            frqrsPlot.ThePlot.Clear();

			// Clear measurement result
			MeasurementResult = new(MyVModel)
            {
                CreateDate = DateTime.Now,
                Show = true,                                      // Show in graph
			};
            var frqrsVm = MyVModel;
            var msr = MeasurementResult.MeasurementSettings;

			// ********************************************************************
			// Setup the device
			if (MathUtil.ToDouble(msr.SampleRate,0) == 0 || !FreqRespViewModel.FftSizes.Contains(msr.FftSize))
			{
				MessageBox.Show("Invalid sample rate or fftsize settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			var fftsize = msr.FftSizeVal;
			var sampleRate = msr.SampleRateVal;

			// ********************************************************************
			// Calculate frequency steps to do
			// ********************************************************************
			var binSize = QaLibrary.CalcBinSize(sampleRate, fftsize);
			// Generate a list of frequencies
			var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(MathUtil.ToDouble(msr.StartFreq), MathUtil.ToDouble(msr.EndFreq), msr.StepsOctave);
			// Translate the generated list to bin center frequencies
			var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize);
			stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x <= 95500)                // Filter out values that are out of range 
				.GroupBy(x => x)                                                                    // Filter out duplicates
				.Select(y => y.First())
				.ToArray();

			// calculate gain to autoattenuate
			frqrsVm.Attenuation = 42; // display on-screen
			await showMessage("Calculating gain");
            LRGains = await DetermineGainCurve(true, 1);
            if(LRGains == null)
                { return false; }

			int[] frqtest = [ToBinNumber(stepBinFrequencies[0], LRGains),
								 ToBinNumber(stepBinFrequencies[stepBinFrequencies.Length-1], LRGains)];
			{
				// to get attenuation, use a frequency of zero (all)
				// find the highest output voltage

				var genv = frqrsVm.ToGenVoltage(msr.Gen1Voltage, frqtest, GEN_OUTPUT, LRGains.Left);                  // output v
                genv = Math.Max(genv, frqrsVm.ToGenVoltage(msr.Gen1Voltage, frqtest, GEN_OUTPUT, LRGains.Right));    // output v
				var vdbv = QaLibrary.ConvertVoltage(genv, E_VoltageUnit.Volt, E_VoltageUnit.dBV);   // out dbv
				var attenuation = QaLibrary.DetermineAttenuation(vdbv);
				msr.Attenuation = attenuation;
				frqrsVm.Attenuation = msr.Attenuation; // display on-screen
			}
            // get voltages for generator
			var genVolt = frqrsVm.ToGenVoltage(msr.Gen1Voltage, frqtest, GEN_INPUT, LRGains?.Left);
			var voltagedBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);  // in dbv

			if (true != QaUsb.InitializeDevice(sampleRate, fftsize, "Hann", (int)msr.Attenuation, true))
			{
				return false;
			}

			try
			{
                // Check if cancel button pressed
                if (ct.IsCancellationRequested)
                    return false;

				// If in continous mode we continue sweeping until cancellation requested.
				MeasurementResult.GainData = new List<Complex>();
				MeasurementResult.GainFrequencies = new List<double>();
				// just one result to show
				Data.Measurements.Clear();
				Data.Measurements.Add(MeasurementResult);

                var ttype = frqrsVm.GetTestingType(msr.TestType);

				do
				{
                    if (ct.IsCancellationRequested)
                        break;
                    if( msr.IsChirp)
						await RunChirpTest(voltagedBV);
					else
						await RunFreqTest(stepBinFrequencies, voltagedBV);
					UpdateGraph(false);
                } while (continuous && !ct.IsCancellationRequested);
			}
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Turn the generator off
            QaUsb.SetOutputSource(OutputSources.Off);

            // Show message
            await showMessage($"Measurement finished!");

            UpdateGraph(false);
			MyVModel.HasExport = true;

			return ct.IsCancellationRequested;
        }


        private Complex CalculateGain(double dFreq, LeftRightSeries data, bool showBoth)
        {
            Complex gain = new();
            if(showBoth)
				gain = QAMath.CalculateDualGain(dFreq, data);
            else
				gain = QAMath.CalculateGainPhase(dFreq, data);
			return gain;
        }

        /// <summary>
        /// Initialize the magnitude plot
        /// </summary>
        void InitializePlot()
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var frqrsVm = MyVModel;

			PlotUtil.InitializeMagFreqPlot(myPlot);
			PlotUtil.SetOhmFreqRule(myPlot);
			PlotUtil.AddPhaseFreqRule(myPlot);

            myPlot.Axes.SetLimitsX(Math.Log10(MathUtil.ToDouble(frqrsVm.GraphStartFreq, 20.0)), Math.Log10(MathUtil.ToDouble(frqrsVm.GraphEndFreq, 20000)), myPlot.Axes.Bottom);
			myPlot.Axes.SetLimitsY(MathUtil.ToDouble(frqrsVm.RangeBottomdB, -20), MathUtil.ToDouble(frqrsVm.RangeTopdB, 180), myPlot.Axes.Left);
            myPlot.Axes.SetLimitsY(-360, 360, myPlot.Axes.Right);

            var ttype = frqrsVm.GetTestingType(frqrsVm.TestType);
            switch( ttype)
            {
                case TestingType.Response:
					myPlot.Title("Frequency Response (dBV)");
					myPlot.YLabel("dBV");
					myPlot.Axes.Right.Label.Text = string.Empty;
					break;
				case TestingType.Gain:
					PlotUtil.AddPhasePlot(myPlot);
					myPlot.Title("Gain");
					myPlot.YLabel("dB");
					myPlot.Axes.Right.Label.Text = "Phase (Deg)";
					break;
				case TestingType.Impedance:
					PlotUtil.AddPhasePlot(myPlot);
					myPlot.Title("Impedance");
					myPlot.YLabel("|Z| Ohms");
					myPlot.Axes.Right.Label.Text = "Phase (Deg)";
					break;
			}
			myPlot.XLabel("Frequency (Hz)");
			frqrsPlot.Refresh();
        }

        private Complex ToImpedance(Complex z)
        {
			var xtest = z / ((new Complex(1, 0)) - z);  // do the math
            return xtest;
		}

        /// <summary>
        /// unwrap the phase data
        /// </summary>
        /// <param name="phaseData"></param>
        /// <returns></returns>
        private double[] Regularize(double[] phaseData)
		{
            var allPos = phaseData.Select(x => (x >= 0) ? x : x + 360);
            var deltain = phaseData.Select((x,index) => Math.Abs(x - phaseData[((index==0) ? 1 : index) - 1])).Sum();
			var deltaPos = allPos.Select((x, index) => Math.Abs(x - phaseData[((index == 0) ? 1 : index) - 1])).Sum();
			if (deltain > deltaPos)
			{
                return allPos.ToArray();
			}
            return phaseData;
		}


		/// <summary>
		/// Plot the magnitude graph
		/// </summary>
		/// <param name="measurementResult">Data to plot</param>
		void PlotValues(FrequencyResponseMeasurementResult measurementResult, int measurementNr, bool showLeftChannel, bool showRightChannel, E_FrequencyResponseGraphType graphType)
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var frqrsVm = MyVModel;

			if (measurementResult == null || measurementResult.GainData == null || measurementResult.GainFrequencies == null)
                return;

            var freqX = measurementResult.GainFrequencies;
            var gainY = measurementResult.GainData;
			if (gainY.Count == 0 || freqX.Count == 0)
				return;

			if (freqX[0] == 0)
                freqX[0] = 1e-6;    // so can log10

            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;
            float markerSize = frqrsVm.ShowPoints ? lineWidth + 3 : 1;

            var colors = new GraphColors();
            int color = measurementNr * 2;
            var ttype = frqrsVm.GetTestingType(frqrsVm.TestType);

            double[] YValues = [];
            double[] phaseValues = [];
            double rref = ViewSettings.AmplifierLoad;
			string legendname = string.Empty;
            switch(ttype)
            {
                case TestingType.Gain:
					YValues = gainY.Select(x => 20 * Math.Log10(x.Magnitude)).ToArray();
					phaseValues = gainY.Select(x => 180 * x.Phase / Math.PI).ToArray();
                    legendname = "Gain";
					break;
				case TestingType.Response:
					YValues = gainY.Select(x => 20 * Math.Log10(x.Real)).ToArray(); // real is the left gain
					phaseValues = gainY.Select(x => 20 * Math.Log10(x.Imaginary)).ToArray();
					legendname = "Left dBV";
					break;
				case TestingType.Impedance:
					YValues = gainY.Select(x => rref * ToImpedance(x).Magnitude).ToArray();
					// YValues = gainY.Select(x => rref * x.Magnitude/(1-x.Magnitude)).ToArray();
					phaseValues = gainY.Select(x => 180 * ToImpedance(x).Phase / Math.PI).ToArray();
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
						var rule = myPlot.Axes.Rules.Last();
						if (rule is MaximumBoundary)
						{
							// change to an impedance set of limits
							var myrule = ((MaximumBoundary)rule);
							var oldlimit = myrule.Limits;
							AxisLimits axs = new AxisLimits(oldlimit.Left, oldlimit.Right, -360, 360);
							myrule.Limits = axs;
						}
					}

					break;
			}
			//SetMagFreqRule(myPlot);
			var plot = myPlot.Add.Scatter(logFreqX, YValues);
			plot.LineWidth = lineWidth;
			plot.Color = colors.GetColor(0, color);
			plot.MarkerSize = markerSize;
            plot.LegendText = legendname;
			plot.LinePattern = LinePattern.Solid;
            if( ttype != TestingType.Response || frqrsVm.ShowRight)
            {
                var phases = phaseValues;
                if(ttype == TestingType.Gain || ttype == TestingType.Impedance)
                {
					phases = Regularize(phaseValues);
					plot = myPlot.Add.Scatter(logFreqX, phases);
					plot.Axes.YAxis = myPlot.Axes.Right;
					plot.LegendText = "Phase (Deg)";
				}
                else
                {
					plot = myPlot.Add.Scatter(logFreqX, phases);
					plot.LegendText = "Right dBV";
				}
				plot.LineWidth = lineWidth;
				plot.Color = colors.GetColor(3, color);
				plot.MarkerSize = markerSize;
				plot.LinePattern = LinePattern.Solid;
			}

			frqrsPlot.Refresh();
        }

		public void UpdateGraph(bool settingsChanged)
        {
            frqrsPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			var frqsrVm = MyVModel;

			int resultNr = 0;

            switch(frqsrVm.GetTestingType(frqsrVm.TestType))
            {
                case TestingType.Response:
					frqsrVm.GraphUnit = "dBV";
                    break;
				case TestingType.Impedance:
					frqsrVm.GraphUnit = "Ohms";
					break;
				case TestingType.Gain:
					frqsrVm.GraphUnit = "dB";
					break;
			}

            if (settingsChanged)
            {
                InitializePlot();
            }

            foreach (var result in Data.Measurements.Where(m => m.Show))
            {
                PlotValues(result, resultNr++, frqsrVm.ShowLeft, frqsrVm.ShowRight, E_FrequencyResponseGraphType.DBV);  // frqsrVm.GraphType);
            }

            PlotBandwidthLines();
            frqrsPlot.Refresh();
        }

        private void PlotBandwidthLines()
        {
            // Plot
            //if (GraphSettings.GraphType == E_FrequencyResponseGraphType.DBV)   
                PlotDbVBandwidthLines();
            //else 
            //    PlotGainBandwidthLines();
        }

        void PlotDbVBandwidthLines() 
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			FreqRespViewModel frqrsVm = MyVModel;

			// Remove old lines
			myPlot.Remove<VerticalLine>();
			myPlot.Remove<Arrow>();
			myPlot.Remove<Text>();

            if (MeasurementResult != null && MeasurementResult.FrequencyResponseData != null && MeasurementResult.FrequencyResponseData.FreqRslt != null)
            {
                BandwidthData bandwidthData3dB = new BandwidthData();
                BandwidthData bandwidthData1dB = new BandwidthData();
                if (MeasurementResult.MeasurementSettings.LeftChannel)
                {
                    bandwidthData3dB.Left = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqRslt.Left, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                    bandwidthData1dB.Left = CalculateBandwidth(-1, MeasurementResult.FrequencyResponseData.FreqRslt.Left, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                }

                if (MeasurementResult.MeasurementSettings.RightChannel)
                {
                    bandwidthData3dB.Right = CalculateBandwidth(-3, MeasurementResult.FrequencyResponseData.FreqRslt.Right, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                    bandwidthData1dB.Right = CalculateBandwidth(-1, MeasurementResult.FrequencyResponseData.FreqRslt.Right, MeasurementResult.FrequencyResponseData.FreqRslt.Df);
                }

                // Draw bandwidth lines
                var colors = new GraphColors();
                float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;

                if (frqrsVm.ShowLeft && frqrsVm.LeftChannel)
                {
                    if (frqrsVm.Show3dBBandwidth_L)
                    {
                        DrawBandwithLines(3, bandwidthData3dB.Left, 0);
                    }

                    if (frqrsVm.Show1dBBandwidth_L)
                    {
                        DrawBandwithLines(1, bandwidthData1dB.Left, 1);
                    }
                }

                if (frqrsVm.ShowRight && frqrsVm.RightChannel)
                {
                    if (frqrsVm.Show3dBBandwidth_R)
                    {
                        DrawBandwithLines(3, bandwidthData3dB.Right, 2);
                    }

                    if (frqrsVm.Show1dBBandwidth_R)
                    {
                        DrawBandwithLines(1, bandwidthData3dB.Right, 3);
                    }
                }
            }
        }

        void PlotGainBandwidthLines()
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			FreqRespViewModel frqrsVm = MyVModel;

			// Remove old lines
			myPlot.Remove<VerticalLine>();
            myPlot.Remove<Arrow>();
            myPlot.Remove<Text>();

            // GAIN
            if (MeasurementResult != null && MeasurementResult.GainData != null)
            {
                // Gain BW
                if (MeasurementResult.MeasurementSettings.LeftChannel)
                {
                    var gainBW3dB = CalculateBandwidth(-3, MeasurementResult.GainData.Select(x => x.Magnitude).ToArray(), MeasurementResult.FrequencyResponseData.FreqRslt?.Df ?? 1);        // Volts is gain

                    var gainBW1dB = CalculateBandwidth(-1, MeasurementResult.GainData.Select(x => x.Magnitude).ToArray(), MeasurementResult.FrequencyResponseData.FreqRslt?.Df ?? 1);

                    // Draw bandwidth lines
                    var colors = new GraphColors();
                    float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;

                    if (frqrsVm.ShowLeft && frqrsVm.LeftChannel)
                    {
                        if (frqrsVm.Show3dBBandwidth_L)
                        {
                            DrawBandwithLines(3, gainBW3dB, 0);
                        }

                        if (frqrsVm.Show1dBBandwidth_L)
                        {
                            DrawBandwithLines(1, gainBW1dB, 1);
                        }
                    }
                }
            }
        }


        class BandwidthChannelData
        {
            public double LowestAmplitudeVolt = 0;
            public double LowestAmplitudeFreq = 0;

            public double HighestAmplitudeVolt = 0;
            public double HighestAmplitudeFreq = 0;

            public double LowerFreq = 0;
            public double LowerFreqAmplitudeVolt = 0;

            public double UpperFreq = 0;
            public double UpperFreqAmplitudeVolt = 0;

            public double Bandwidth = 0;
        }

        class BandwidthData
        {
            public BandwidthChannelData Left;
            public BandwidthChannelData Right;

            public BandwidthData()
            {
                Left = new BandwidthChannelData();
                Right = new BandwidthChannelData();
            }
        }

        /// <summary>
        /// Calculate bandwidth from equally spaced data.
        /// </summary>
        /// <param name="dB"></param>
        /// <param name="data"></param>
        /// <param name="frequencyResolution"></param>
        /// <returns></returns>
        BandwidthChannelData CalculateBandwidth(double dB, double[] data, double frequencyResolution)
        {
            BandwidthChannelData bandwidthData = new BandwidthChannelData();

            if (data == null)
                return bandwidthData;

            var gainValue = Math.Pow(10, dB / 20);

            bandwidthData.LowestAmplitudeVolt = data.Skip(1).Min(); // Skip dc
            var lowestAmplitude_left_index = data.ToList().IndexOf(bandwidthData.LowestAmplitudeVolt);
            bandwidthData.LowestAmplitudeFreq = frequencyResolution * (lowestAmplitude_left_index + 1);

            // Get highest amplitude
            //bandwidthData.HighestAmplitudeVolt = data.Skip((int)(5 / frequencyResolution)).Max();      // Skip first 5 Hz for now.
            bandwidthData.HighestAmplitudeVolt = data.Skip(1).Max();      // Skip dc.
            var highestAmplitude_left_index = data.ToList().IndexOf(bandwidthData.HighestAmplitudeVolt);
            bandwidthData.HighestAmplitudeFreq = frequencyResolution * highestAmplitude_left_index;

            // Get lower frequency
            //var lowerFreq_left = data.Select((Value, Index) => new { Value, Index }).Where(f => f.Value <= (bandwidthData.HighestAmplitudeVolt * gainValue) && f.Index < highestAmplitude_left_index).LastOrDefault();
            var lowerFreq_left = data.Select((Value, Index) => new { Value, Index })
                .Where(f => f.Index < highestAmplitude_left_index)
                .Select(n => new { n.Value, n.Index, delta = Math.Abs(n.Value - (bandwidthData.HighestAmplitudeVolt * gainValue)) })
                .OrderBy(p => p.delta)
                .FirstOrDefault();

            if (lowerFreq_left != default)
            {
                double lowerFreq_left_index = lowerFreq_left.Index;
                bandwidthData.LowerFreqAmplitudeVolt = lowerFreq_left.Value;
                double lowerFreq_left_amplitude_dBV = 20 * Math.Log10(lowerFreq_left.Value);
                bandwidthData.LowerFreq = (lowerFreq_left_index + 1) * frequencyResolution;
            }
            else
                bandwidthData.LowerFreq = 1;

            // Get upper frequency
            //var upperFreq_left = data.Select((Value, Index) => new { Value, Index }).Where(f => f.Value <= bandwidthData.HighestAmplitudeVolt * gainValue && f.Index > highestAmplitude_left_index).FirstOrDefault();
            var upperFreq_left = data.Select((Value, Index) => new { Value, Index })
                .Where(f => f.Index > highestAmplitude_left_index)
                .Select(n => new { n.Value, n.Index, delta = Math.Abs(n.Value - (bandwidthData.HighestAmplitudeVolt * gainValue)) })
                .OrderBy(p => p.delta)
                .FirstOrDefault();

            if (upperFreq_left != default)
            {
                double upperFreq_left_index = upperFreq_left.Index;
                bandwidthData.UpperFreqAmplitudeVolt = upperFreq_left.Value;
                double upperFreq_left_amplitude_dBV = 20 * Math.Log10(upperFreq_left.Value);
                bandwidthData.UpperFreq = upperFreq_left_index * frequencyResolution;
            }
            else
                bandwidthData.UpperFreq = 100000;

            bandwidthData.Bandwidth = bandwidthData.UpperFreq - bandwidthData.LowerFreq;

            return bandwidthData;
        }

        private string AutoUnitText(double value, string unit, int decimals, int milliDecimals = 0)
        {
            bool isNegative = value < 0;
            string newString = string.Empty;

            value = Math.Abs(value);

            if (value < 1)
                newString = ((int)(value * 1000)).ToString("0." + new string('0', milliDecimals)) + " m" + unit;
            else if (value < 1000)
                newString = value.ToString("0." + new string('0', decimals)) + " " + unit;
            else
                newString = (value / 1000).ToString("0." + new string('0', decimals)) + " k" + unit;

            return (isNegative ? "-" : "") + newString;
        }

        void DrawBandwithLines(int gain, BandwidthChannelData channelData, int colorRange)
        {
			var frqrsVm = MyVModel;

			var colors = new GraphColors();
            float lineWidth = frqrsVm.ShowThickLines ? 1.6f : 1;

            // Low frequency vertical line
            var lowerFreq_dBV_left = Math.Log10(channelData.LowerFreq);
            AddVerticalLine(lowerFreq_dBV_left, 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), colors.GetColor(colorRange, 2), lineWidth);

            // High frequency vertical line
            var upperFreq_dBV_left = Math.Log10(channelData.UpperFreq);
            AddVerticalLine(upperFreq_dBV_left, 20 * Math.Log10(channelData.UpperFreqAmplitudeVolt), colors.GetColor(colorRange, 2), lineWidth);

            // Bandwidht arrow
            AddArrow(lowerFreq_dBV_left, 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), upperFreq_dBV_left, 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), colors.GetColor(colorRange, 4), lineWidth);

            // Bandwitdh text
            var lowerFreq = Math.Log10(channelData.LowerFreq);
            var upperFreq = Math.Log10(channelData.UpperFreq);
            
            var bwText = $"B{gain:0}: {channelData.Bandwidth:0 Hz}";
            if (channelData.Bandwidth > 1000)
                bwText = $"B{gain:0}: {(channelData.Bandwidth / 1000):0.00# kHz}";
            if (channelData.UpperFreq > 96000)
                bwText = $"B{gain:0}: > 96 kHz";
            AddText(bwText, (lowerFreq + ((upperFreq - lowerFreq) / 2)), 20 * Math.Log10(channelData.LowerFreqAmplitudeVolt), colors.GetColor(colorRange, 8), -35, -10);

            // Low frequency text
            var bwLowF = $"{channelData.LowerFreq:0 Hz}";
            if (channelData.LowerFreq > 1000)
                bwLowF = $"{(channelData.LowerFreq / 1000):0.00# kHz}";
            AddText(bwLowF, lowerFreq_dBV_left, frqrsPlot.ThePlot.Axes.GetLimits().Bottom, colors.GetColor(colorRange, 8), -20, -30);

            // High frequency text         
            var bwHighF = $"{channelData.UpperFreq:0 Hz}";
            if (channelData.UpperFreq > 1000)
                bwHighF = $"{(channelData.UpperFreq / 1000):0.00# kHz}";
            AddText(bwHighF, upperFreq_dBV_left, frqrsPlot.ThePlot.Axes.GetLimits().Bottom, colors.GetColor(colorRange, 8), -20, -30);
        }


        void AddVerticalLine(double x, double maximum, ScottPlot.Color color, float lineWidth)
        {
            var line = frqrsPlot.ThePlot.Add.VerticalLine(x);
            line.Maximum = maximum;
            line.Color = color;
            line.LineWidth = lineWidth;
            line.LinePattern = LinePattern.DenselyDashed;
        }

        void AddArrow(double x1, double y1, double x2, double y2, ScottPlot.Color color, float lineWidth)
        {
            Coordinates arrowTip = new Coordinates(x1, y1);
            Coordinates arrowBase = new Coordinates(x2, y2);
            var arrow = frqrsPlot.ThePlot.Add.Arrow(arrowTip, arrowBase);
            arrow.ArrowStyle.LineWidth = lineWidth;
            arrow.ArrowStyle.ArrowheadLength = 12;
            arrow.ArrowStyle.ArrowheadWidth = 8;
            arrow.ArrowShape = new ScottPlot.ArrowShapes.DoubleLine();
            arrow.ArrowLineColor = color;
        }

        void AddText(string text, double x, double y, ScottPlot.Color backgroundColor, int offsetX, int offsetY)
        {
            var txt = frqrsPlot.ThePlot.Add.Text(text, x, y);
            txt.LabelFontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			txt.LabelBorderColor = Colors.Black;
            txt.LabelBorderWidth = 1;
            txt.LabelPadding = 2;
            txt.LabelBold = false;
            txt.LabelBackgroundColor = backgroundColor;
            txt.OffsetX = offsetX;
            txt.OffsetY = offsetY;
        }
    }
}
