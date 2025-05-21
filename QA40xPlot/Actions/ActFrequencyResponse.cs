using FftSharp;
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
using System.Windows.Controls;
using static QA40xPlot.ViewModels.BaseViewModel;


namespace QA40xPlot.Actions
{
	using MyDataTab = DataTab<FreqRespViewModel>;

	public partial class ActFrequencyResponse : ActBase
    {
		public MyDataTab PageData { get; private set; } // Data used in this form instance
		private List<MyDataTab> OtherTabs { get; set; } = new(); // Other tabs in the document
		private readonly Views.PlotControl fftPlot;
		private readonly Views.PlotControl timePlot;
		private readonly Views.PlotControl frqrsPlot;

		private float _Thickness = 2.0f;
		private static FreqRespViewModel MyVModel { get => ViewSettings.Singleton.FreqRespVm; }

		CancellationTokenSource ct { set; get; }                                 // Measurement cancelation token

		/// <summary>
		/// Constructor
		/// </summary>
		public ActFrequencyResponse(Views.PlotControl graphFreq, Views.PlotControl graphFft, Views.PlotControl graphTime)
		{
            frqrsPlot = graphFreq;
			fftPlot = graphFft;
			timePlot = graphTime;

			// Show empty graphs
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 100000, -180, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -2, 2);

			ct = new CancellationTokenSource();
			PageData = new(MyVModel, new LeftRightTimeSeries());
			UpdateGraph(true);
		}

        public void DoCancel()
        {
            ct.Cancel();
		}

		public void UpdatePlotTitle()
		{
			var vm = MyVModel;
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var title = GetTheTitle();
			if (PageData.Definition.Name.Length > 0)
				myPlot.Title(title + " : " + PageData.Definition.Name);
			else
				myPlot.Title(title);
		}

		public bool SaveToFile(string fileName)
		{
			return Util.SaveToFile<FreqRespViewModel>(PageData, fileName);
		}

		public async Task LoadFromFile(string fileName, bool isMain)
		{
			var page = LoadFile(fileName);
			if(page != null)
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
			await PostProcess(page, ct.Token);
			if(isMain)
			{
				// we can't overwrite the viewmodel since it links to the display proper
				// update both the one we're using to sweep (PageData) and the dynamic one that links to the gui
				PageData.ViewModel.OtherSetList = MyVModel.OtherSetList;
				PageData.ViewModel.CopyPropertiesTo<FreqRespViewModel>(MyVModel);    // retract the gui
				PageData = page;    // set the current page to the loaded one

				// relink to the new definition
				MyVModel.LinkAbout(PageData.Definition);
			}
			else
			{
				page.Show = 1; // show the left channel new
				OtherTabs.Add(page); // add the new one
				var oss = new OtherSet(page.Definition.Name, page.Show, page.Id, string.Empty);
				MyVModel.OtherSetList.Add(oss);
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
		/// Start measurement button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async Task DoMeasurement()
		{
			var vmFreq = MyVModel;
			if (! await StartAction(vmFreq))
				return;

			vmFreq.HasExport = false;
			ct = new();
												  // Show empty graphs
			QaLibrary.InitMiniFftPlot(fftPlot, 10, 40000, -180, 20);
			QaLibrary.InitMiniTimePlot(timePlot, 0, 4, -2, 2); 
            
            UpdateGraph(true);

			frqrsPlot.ThePlot.Clear();

			// sweep data
			LeftRightTimeSeries lrts = new();
			MyDataTab NextPage = new(vmFreq, lrts);
			PageData.Definition.CopyPropertiesTo(NextPage.Definition);
			NextPage.Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var msr = NextPage.ViewModel;
			if (msr == null)
				return;

			// ********************************************************************
			// Setup the device
			if (MathUtil.ToDouble(msr.SampleRate, 0) == 0 || !FreqRespViewModel.FftSizes.Contains(msr.FftSize))
			{
				MessageBox.Show("Invalid sample rate or fftsize settings", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			var fftsize = msr.FftSizeVal;
			var sampleRate = msr.SampleRateVal;

			// calculate gain to autoattenuate
			await CalculateGainCurve(MyVModel);
			if (LRGains == null)
			{
				// cancelled?
				return;
			}

			var fmin = MathUtil.ToDouble(msr.StartFreq);
			var fmax = MathUtil.ToDouble(msr.EndFreq);

			int[] frqtest = [ToBinNumber(fmin, LRGains), ToBinNumber(fmax, LRGains)];
			{
				// to get attenuation, use a frequency of zero (all)
				// find the highest output voltage

				var genv = msr.ToGenVoltage(msr.Gen1Voltage, frqtest, GEN_OUTPUT, LRGains.Left);                  // output v
				genv = Math.Max(genv, msr.ToGenVoltage(msr.Gen1Voltage, frqtest, GEN_OUTPUT, LRGains.Right));    // output v
				var vdbv = QaLibrary.ConvertVoltage(genv, E_VoltageUnit.Volt, E_VoltageUnit.dBV);   // out dbv
				var attenuation = QaLibrary.DetermineAttenuation(vdbv);
				msr.Attenuation = attenuation;
				vmFreq.Attenuation = msr.Attenuation; // display on-screen
			}
			// get voltages for generator
			var genVolt = vmFreq.ToGenVoltage(msr.Gen1Voltage, frqtest, GEN_INPUT, LRGains?.Left);
			var voltagedBV = QaLibrary.ConvertVoltage(genVolt, E_VoltageUnit.Volt, E_VoltageUnit.dBV);  // in dbv

			NextPage.Definition.GeneratorVoltage = genVolt; // save the actual generator voltage

			if (true != await QaComm.InitializeDevice(sampleRate, fftsize, "Hann", (int)msr.Attenuation))
			{
				return;
			}

			try
			{
				// Check if cancel button pressed
				if (ct.IsCancellationRequested)
					return;

				// If in continous mode we continue sweeping until cancellation requested.
				NextPage.GainData = []; // new list of complex data
				NextPage.GainFrequencies = [];

				// ********************************************************************
				// Calculate frequency steps to do if discrete
				// ********************************************************************
				var stepFrequencies = QaLibrary.GetLinearSpacedLogarithmicValuesPerOctave(MathUtil.ToDouble(msr.StartFreq), MathUtil.ToDouble(msr.EndFreq), msr.StepsOctave);
				// Translate the generated list to bin center frequencies
				var stepBinFrequencies = QaLibrary.TranslateToBinFrequencies(stepFrequencies, sampleRate, fftsize);
				stepBinFrequencies = stepBinFrequencies.Where(x => x >= 1 && x <= 95500)                // Filter out values that are out of range 
					.GroupBy(x => x)                                                                    // Filter out duplicates
					.Select(y => y.First())
					.ToArray();

				var ttype = vmFreq.GetTestingType(msr.TestType);

				if (ct.IsCancellationRequested)
					return;
				if (msr.IsChirp)
					await RunChirpTest(NextPage, voltagedBV);
				else
				{
					await RunFreqTest(NextPage, stepBinFrequencies, voltagedBV);
				}
				UpdateGraph(false);
				if (!ReferenceEquals(PageData, NextPage))
					PageData = NextPage;        // finally update the pagedata for display and processing
				MyVModel.LinkAbout(PageData.Definition);  // ensure we're linked right during replays

				bool continuous = false;
				while (continuous && !ct.IsCancellationRequested)
				{
					if (ct.IsCancellationRequested)
						break;
					if (msr.IsChirp)
						await RunChirpTest(PageData, voltagedBV);
					else
					{
						await RunFreqTest(PageData, stepBinFrequencies, voltagedBV);
					}
					UpdateGraph(false);
				} 
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			// Show message
			await showMessage($"Measurement finished!");

			UpdateGraph(false);
			PageData.TimeRslt = new();  // clear this before saving stuff
			await EndAction(vmFreq);
			await showMessage("Finished");
			vmFreq.HasExport = (PageData.GainFrequencies.Length > 0);
		}

		// create a blob with F,Left,Right data for export
		public DataBlob? CreateExportData()
		{
			if (PageData == null || PageData.GainFrequencies == null || PageData.GainData == null)
				return null;
			DataBlob db = new();
            var frsqVm = MyVModel;
			var freqs = this.PageData.GainFrequencies;
            if (freqs.Length == 0)
                return null;

            db.FreqData = freqs.ToList();        // test frequencies
            var ttype = frsqVm.GetTestingType(frsqVm.TestType);
            switch( ttype)
            {
                case TestingType.Response:
					if (frsqVm.ShowRight && !frsqVm.ShowLeft)
					{
						db.LeftData = PageData.GainData.Select(x => x.Imaginary).ToList();
					}
					else
					{
						db.LeftData = PageData.GainData.Select(x => x.Real).ToList();
					}
					break;
                case TestingType.Gain:
                    var gld = PageData.GainData.ToArray();
					db.LeftData = FFT.Magnitude(gld).ToList();
					db.PhaseData = FFT.Phase(gld).ToList();
					break;
				case TestingType.Impedance:
                    {
						double rref = MathUtil.ToDouble(MyVModel.ZReference, 8);
						db.LeftData = PageData.GainData.Select(x => rref * ToImpedance(x).Magnitude).ToList();
						// YValues = gainY.Select(x => rref * x.Magnitude/(1-x.Magnitude)).ToArray();
						db.PhaseData = PageData.GainData.Select(x => ToImpedance(x).Phase).ToList();
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

			LeftRightSeries lfrs = new();
			FrequencyHistory.Clear();
			for (int i = 0; i < msr.Averages-1; i++)
			{
				lfrs = await QaComm.DoAcquisitions(1, ct.Token, true);
				if (lfrs == null || lfrs.TimeRslt == null || lfrs.FreqRslt == null)
					return new();
				FrequencyHistory.Add(lfrs.FreqRslt);
			}
			{
				lfrs = await QaComm.DoAcquisitions(1, ct.Token, true);
				if (lfrs == null || lfrs.TimeRslt == null || lfrs.FreqRslt == null)
					return new();
				lfrs.FreqRslt = CalculateAverages(lfrs.FreqRslt, msr.Averages);
			}

			PageData.TimeRslt = lfrs.TimeRslt;
			PageData.FreqRslt = lfrs.FreqRslt;
			var ga = CalculateGain(showfreq, lfrs, ttype == TestingType.Response); // gain,phase or gain1,gain2
            return ga;
		}

		public Rect GetDataBounds()
		{
			var vm = PageData.ViewModel;    // measurement settings
			var vmr = PageData.GainFrequencies; // test data
			var msd = PageData.GainData;

			if (vmr == null || vmr.Length == 0 || msd == null)
				return Rect.Empty;

			Rect rrc = new Rect(0, 0, 0, 0);

            rrc.X = vmr.Min();
            rrc.Width = vmr.Max() - rrc.X;
			var ttype = vm.GetTestingType(vm.TestType);
			if (ttype == TestingType.Response)
			{
                if (vm.ShowLeft)
                {
                    rrc.Y = msd.Min(x => x.Real);
                    rrc.Height = msd.Max(x => x.Real) - rrc.Y;
                    if (vm.ShowRight)
                    {
                        rrc.Y = Math.Min(rrc.Y, msd.Min(x => x.Imaginary));
                        rrc.Height = Math.Max(rrc.Height, msd.Max(x => x.Imaginary) - rrc.Y);
                    }
                }
				else if (vm.ShowRight)
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
            else if(PageData.GainData != null && PageData.GainData.Length > 0)
			{   // impedance
				double rref = ViewSettings.AmplifierLoad;
				var minL = PageData.GainData.Min(x => ToImpedance(x).Magnitude);
				var maxL = PageData.GainData.Max(x => ToImpedance(x).Magnitude);
				var minZohms = rref * minL;
				var maxZohms = rref * maxL;
                rrc.Y = minZohms;
                rrc.Height = maxZohms - minZohms;
			}
			return rrc;
		}

		public ValueTuple<double, double, double> LookupX(double freq)
        {
			if(PageData.GainFrequencies == null || PageData.GainData == null)
				return ValueTuple.Create(0.0, 0.0, 0.0);

			var freqs = PageData.GainFrequencies;
			ValueTuple<double, double, double> tup = ValueTuple.Create(1.0,1.0,1.0);
			if (freqs != null && freqs.Length > 0)
            {
                var values = PageData.GainData;
                // find nearest frequency from list
                var bin = freqs.Count(x => x < freq)-1;    // find first freq less than me
                if (bin == -1)
                    bin = 0;
                var fnearest = freqs[bin];
                if (bin < (freqs.Length-1) && Math.Abs(freq - fnearest) > Math.Abs(freq - freqs[bin + 1]))
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
							var ohms = rref * ToImpedance(PageData.GainData[bin]).Magnitude;
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
		private async Task<bool> RunFreqTest(MyDataTab page, double[] stepBinFrequencies, double voltagedBV)
        {
            var vm = page.ViewModel;
			// Check if cancel button pressed
			if (ct.IsCancellationRequested)
                return false;

            WaveGenerator.SetEnabled(true);		// enable the generator
			WaveGenerator.SetGen2(0, 0, false); // disable the second wave

			var ttype = vm.GetTestingType(vm.TestType);
			var genVolt = Math.Pow(10, voltagedBV / 20);

			try
			{
				page.GainData = []; // new list of complex data
				page.GainFrequencies = []; // new list of frequencies
				if (ct.IsCancellationRequested)
                    return false;
                for (int steps = 0; steps < stepBinFrequencies.Length; steps++)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    var dfreq = stepBinFrequencies[steps];
                    if (dfreq > 0)
                        WaveGenerator.SetGen1(dfreq, genVolt, true);
                    else
                        WaveGenerator.SetGen1(1000, genVolt, false);

                    if (vm.Averages > 0)
                    {
                        List<Complex> readings = new();
                        for (int j = 0; j < vm.Averages; j++)
                        {
							await showMessage(string.Format($"Checking + {dfreq:0} Hz at {genVolt:0.###}V"));   // need a delay to actually see it
                            var ga = await GetGain(dfreq, vm, ttype);
                            readings.Add(ga);
                        }
                        var total = Complex.Zero;
                        foreach (var f in readings)
                        {
                            total += f;
                        }
						page.GainData = page.GainData.Append(total / vm.Averages).ToArray();
					}
                    else
                    {
                        await showMessage(string.Format("Checking + {0:0}", dfreq));   // need a delay to actually see it
                        var ga = await GetGain(dfreq, vm, ttype);
						page.GainData = page.GainData.Append(ga).ToArray();
                    }
                    if (page.FreqRslt != null)
                    {
                        QaLibrary.PlotMiniFftGraph(fftPlot, page.FreqRslt, true, false);
                        QaLibrary.PlotMiniTimeGraph(timePlot, page.TimeRslt, dfreq, true, false);
                    }
					page.GainFrequencies = page.GainFrequencies.Append(dfreq).ToArray();
					UpdateGraph(false);
                    if (!vm.IsTracking)
                    {
                        vm.RaiseMouseTracked("track");
                    }
                }
			}
            catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
            return true;
		}

		private async Task<(LeftRightSeries?, Complex[], Complex[])> RunChirpAcquire(MyDataTab page, double voltagedBV)
		{
			var vm = page.ViewModel;

			var startf = MathUtil.ToDouble(vm.StartFreq) / 3;
			var endf = MathUtil.ToDouble(vm.EndFreq) * 3;
			endf = Math.Min(endf, vm.SampleRateVal / 2);
			var genv = vm.ToGenVoltage(vm.Gen1Voltage, [], GEN_INPUT, LRGains?.Left);
			var chirpy = Chirps.ChirpVp((int)vm.FftSizeVal, vm.SampleRateVal, genv, startf, endf, 0.8);
			LeftRightSeries lfrs = await QaComm.DoAcquireUser(1, ct.Token, chirpy, chirpy, false);
			if (lfrs?.TimeRslt == null)
				return (null,[],[]);
			page.TimeRslt = lfrs.TimeRslt;
			if (ct.IsCancellationRequested)
				return (null, [], []);

			Complex[] leftFft = [];
			Complex[] rightFft = [];
			var flength = lfrs.TimeRslt.Left.Length / 2;        // we want half this since freq is symmetric

			var ttype = vm.GetTestingType(vm.TestType);
			if (ttype == TestingType.Response)
			{
				var lft = Chirps.NormalizeChirpCplx(chirpy, genv, (lfrs.TimeRslt.Left, lfrs.TimeRslt.Right));
				leftFft = lft.Item1;
				rightFft = lft.Item2;
			}
			else
			{
				var window = new FftSharp.Windows.Rectangular();    // best?
																	// Left channel
				double[] lftF = window.Apply(lfrs.TimeRslt.Left, true);
				leftFft = FFT.Forward(lftF);

				double[] rgtF = window.Apply(lfrs.TimeRslt.Right, true);
				rightFft = FFT.Forward(rgtF);
			}

			leftFft = leftFft.Take(flength).ToArray();
			rightFft = rightFft.Take(flength).ToArray();
			var lrfs = new LeftRightFrequencySeries();
			// set the freq values because ?
			lrfs.Left = leftFft.Select(x => x.Magnitude).ToArray();
			lrfs.Right = rightFft.Select(x => x.Magnitude).ToArray();
			lrfs.Df = 1.0 / vm.SampleRateVal;
			lfrs.FreqRslt = lrfs;
			return (lfrs, leftFft, rightFft);
		}

		/// <summary>
		/// Determine the gain curve based on measurement start,end frequency
		/// </summary>
		/// <param name="voltagedBV">the sine generator voltage</param>
		/// <returns></returns>
		private async Task<bool> RunChirpTest(MyDataTab page, double voltagedBV)
		{
			var vm = page.ViewModel;
			// Check if cancel button pressed
			if (ct.IsCancellationRequested)
				return false;

			var ttype = vm.GetTestingType(vm.TestType);

			try
			{
				// manually average the complex data here
				var rca = await RunChirpAcquire(page, voltagedBV);
				LeftRightSeries lfrs = rca.Item1 ?? new();
				var leftFft = rca.Item2;
				var rightFft = rca.Item3;
				if(vm.Averages > 1 && lfrs.FreqRslt != null)
				{
					lfrs.FreqRslt.Left = lfrs.FreqRslt.Left.Select(x => x * x).ToArray();
					lfrs.FreqRslt.Right = lfrs.FreqRslt.Right.Select(x => x * x).ToArray();	// sum of squares
					for (int i=1; i<vm.Averages; i++)
					{
						rca = await RunChirpAcquire(page, voltagedBV);
						if(rca.Item1 != null && rca.Item1.FreqRslt != null)
						{
							lfrs.FreqRslt.Left = lfrs.FreqRslt.Left.Zip(rca.Item1.FreqRslt.Left, (x, y) => x + y * y).ToArray();
							lfrs.FreqRslt.Right = lfrs.FreqRslt.Left.Zip(rca.Item1.FreqRslt.Right, (x, y) => x + y * y).ToArray();
						}
						leftFft = leftFft.Zip(rca.Item2, (x, y) => x + y).ToArray();
						rightFft = rightFft.Zip(rca.Item3, (x, y) => x + y).ToArray();
					}
					lfrs.FreqRslt.Left = lfrs.FreqRslt.Left.Select(x => Math.Sqrt(x / vm.Averages)).ToArray();
					lfrs.FreqRslt.Right = lfrs.FreqRslt.Right.Select(x => Math.Sqrt(x / vm.Averages)).ToArray();
					leftFft = leftFft.Select(x => x / vm.Averages).ToArray();
					rightFft = rightFft.Select(x => x / vm.Averages).ToArray();
				}

				if (lfrs.FreqRslt == null || lfrs.TimeRslt == null || leftFft.Length == 0)
					return false;

				var flength = lfrs.FreqRslt.Left.Length;
				var m2 = Math.Sqrt(2);
				var nca2 = (int)(0.01 + 1 / lfrs.TimeRslt.dt);      // total time in tics = sample rate
				var df = nca2 / (double)flength / 2;                // ???

                // trim the three vectors to the frequency range of interest
				page.GainFrequencies = Enumerable.Range(0, leftFft.Length).Select(x => x * df).ToArray();
                var gfr = page.GainFrequencies;
				// restrict the data to only the frequency spectrum
				var startf = MathUtil.ToDouble(vm.StartFreq) / 3;
				var endf = MathUtil.ToDouble(vm.EndFreq) * 3;
				var trimf = gfr.Count(x => x < startf);
                var trimEnd = gfr.Count(x => x <= endf) - trimf;
                gfr = gfr.Skip(trimf).Take(trimEnd).ToArray();
                var mx = leftFft.Skip(trimf).Take(trimEnd).ToArray();
                var mref = rightFft.Skip(trimf).Take(trimEnd).ToArray();
                // format the gain vectors as desired
                page.GainFrequencies = gfr;
				switch (ttype)
                {
					case TestingType.Response:
						// left, right are magnitude. left uses right as reference
						page.GainData = mx.Zip(mref,
							(l, r) => { return new Complex(l.Magnitude, r.Magnitude); }).ToArray();
						break;
					case TestingType.Gain:
						// here complex value is the fft data left / right
						page.GainData = mx.Zip(mref, (l, r) => { return l / r; }).ToArray();
                        break;
					case TestingType.Impedance:
						// here complex value is the fft data left / right
						page.GainData = mx.Zip(mref, (l, r) => { return l / r; }).ToArray();
						break;
				}

				UpdateGraph(false);
				if (!vm.IsTracking)
				{
					vm.RaiseMouseTracked("track");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return true;
		}

        private Complex CalculateGain(double dFreq, LeftRightSeries data, bool showBoth)
        {
            Complex gain = new();
            if(showBoth)
				gain = QaMath.CalculateDualGain(dFreq, data);
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
			}
			return title;
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
					myPlot.YLabel("dBV");
					myPlot.Axes.Right.Label.Text = string.Empty;
					break;
				case TestingType.Gain:
					PlotUtil.AddPhasePlot(myPlot);
					myPlot.YLabel("dB");
					myPlot.Axes.Right.Label.Text = "Phase (Deg)";
					break;
				case TestingType.Impedance:
					PlotUtil.AddPhasePlot(myPlot);
					myPlot.YLabel("|Z| Ohms");
					myPlot.Axes.Right.Label.Text = "Phase (Deg)";
					break;
			}
			UpdatePlotTitle();
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
		void PlotValues(MyDataTab page, int measurementNr, bool isMain)
        {
			ScottPlot.Plot myPlot = frqrsPlot.ThePlot;
			var frqrsVm = MyVModel;

			if (page.GainData == null || page.GainFrequencies == null)
                return;

            var freqX = page.GainFrequencies;
            var gainY = page.GainData;
			if (gainY.Length == 0 || freqX.Length == 0)
				return;

			if (freqX[0] == 0)
                freqX[0] = 1e-6;    // so can log10

            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            float lineWidth = frqrsVm.ShowThickLines ? _Thickness : 1;
            float markerSize = frqrsVm.ShowPoints ? lineWidth + 3 : 1;

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
					legendname = "dBV";
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
			plot.Color = GraphUtil.GetPaletteColor(measurementNr * 2);
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
				plot.Color = GraphUtil.GetPaletteColor(measurementNr * 2 + 1);
				plot.MarkerSize = markerSize;
				plot.LinePattern = LinePattern.Solid;
			}

			frqrsPlot.Refresh();
        }

		public void UpdateGraph(bool settingsChanged)
        {
			DataUtil.ReflectOtherSet(OtherTabs, MyVModel.OtherSetList);
			frqrsPlot.ThePlot.Remove<Marker>();             // Remove all current lines
			frqrsPlot.ThePlot.Remove<Scatter>();             // Remove all current lines
			var frqsrVm = MyVModel;

			int resultNr = 0;

            switch(frqsrVm.GetTestingType(frqsrVm.TestType))
            {
                case TestingType.Response:
					frqsrVm.PlotFormat = "dBV";
                    break;
				case TestingType.Impedance:
					frqsrVm.PlotFormat = "Ohms";
					break;
				case TestingType.Gain:
					frqsrVm.PlotFormat = "SPL";
					break;
			}

            if (settingsChanged)
            {
                InitializePlot();
            }

			PlotValues(PageData, resultNr++, true);  // frqsrVm.GraphType);
			if (OtherTabs.Count > 0)
			{
				foreach (var other in OtherTabs)
				{
					if (other != null && other.Show != 0)
						PlotValues(other, resultNr++, false);
				}
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

            if (PageData != null && PageData.FreqRslt != null && PageData.TimeRslt.Left.Length > 0)
            {
                BandwidthData bandwidthData3dB = new BandwidthData();
                BandwidthData bandwidthData1dB = new BandwidthData();
                if (PageData.ViewModel.LeftChannel)
                {
                    bandwidthData3dB.Left = CalculateBandwidth(-3, PageData.FreqRslt.Left, PageData.FreqRslt.Df);
                    bandwidthData1dB.Left = CalculateBandwidth(-1, PageData.FreqRslt.Left, PageData.FreqRslt.Df);
                }

                if (PageData.ViewModel.RightChannel)
                {
                    bandwidthData3dB.Right = CalculateBandwidth(-3, PageData.FreqRslt.Right, PageData.FreqRslt.Df);
                    bandwidthData1dB.Right = CalculateBandwidth(-1, PageData.FreqRslt.Right, PageData.FreqRslt.Df);
                }

                // Draw bandwidth lines
                float lineWidth = frqrsVm.ShowThickLines ? _Thickness : 1;

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
            if (PageData != null && PageData.GainData != null)
            {
                // Gain BW
                if (PageData.ViewModel.LeftChannel)
                {
                    var gainBW3dB = CalculateBandwidth(-3, PageData.GainData.Select(x => x.Magnitude).ToArray(), PageData.FreqRslt?.Df ?? 1);        // Volts is gain

                    var gainBW1dB = CalculateBandwidth(-1, PageData.GainData.Select(x => x.Magnitude).ToArray(), PageData.FreqRslt?.Df ?? 1);

                    // Draw bandwidth lines
                    var colors = new GraphColors();
                    float lineWidth = frqrsVm.ShowThickLines ? _Thickness : 1;

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
            float lineWidth = frqrsVm.ShowThickLines ? _Thickness : 1;

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
