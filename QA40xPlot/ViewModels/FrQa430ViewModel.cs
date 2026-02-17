using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using QA40xPlot.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Input;

public class FrQa430ViewModel : OpampViewModel
{
	public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
	public static List<String> Smoothings { get => new List<string> { "0", "0.001", "0.01", "0.05", "0.1", "0.25" }; }
	public static List<String> PhaseList { get => new List<string> { "360", "180", "90", "0", "-90", "-180", "-360" }; }
	public static List<String> TestTypes { get => new List<string> { "CMRR", "PSRR", "GBW", "Noise" }; }
	[JsonIgnore]
	public override List<string> AxisList { get; } = new List<string> { "XF", "YM", "PH" };

	private ActFrQa430 MyAction { get => actFreq; }
	private PlotControl actPlot { get; set; }
	private ActFrQa430 actFreq { get; set; }
	[JsonIgnore]
	public override RelayCommand DoRun { get => new RelayCommand(RunIt); }
	[JsonIgnore]
	public override RelayCommand DoStart { get => new RelayCommand(StartIt); }
	[JsonIgnore]
	public override RelayCommand DoStop { get => new RelayCommand(StopIt); }
	[JsonIgnore]
	public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }
	[JsonIgnore]
	public RelayCommand<object> DoFitToData { get => new RelayCommand<object>(OnFitToData); }
	[JsonIgnore]
	public RelayCommand<object> DoViewToFit { get => new RelayCommand<object>(OnViewToFit); }
	[JsonIgnore]
	public AsyncRelayCommand DoLoadTab { get => new AsyncRelayCommand(LoadItTab); }
	[JsonIgnore]
	public AsyncRelayCommand DoGetTab { get => new AsyncRelayCommand(GetItTab); }
	[JsonIgnore]
	public RelayCommand DoSaveTab { get => new RelayCommand(SaveItTab); }

	/// <summary>
	/// the static model that applies to the GUI at least...
	/// </summary>
	private static FrQa430ViewModel MyVModel { get => ViewSettings.Singleton.FrQa430Vm; }

	#region Setters and Getters
	private bool _UseMUseMicCorrection = false;
	public bool UseMicCorrection
	{
		get { return _UseMUseMicCorrection; }
		set { SetProperty(ref _UseMUseMicCorrection, value); }
	}

	private bool _IsChirp = true;
	public bool IsChirp
	{
		get { return _IsChirp; }
		set { SetProperty(ref _IsChirp, value); }
	}
	// use riaa preemphasis on the signal
	private bool _IsRiaa = false;
	public bool IsRiaa
	{
		get { return _IsRiaa; }
		set { SetProperty(ref _IsRiaa, value); }
	}

	private string _ZReference = string.Empty;
	public string ZReference
	{
		get => _ZReference;
		set => SetProperty(ref _ZReference, value);
	}

	private string _TestType = TestTypes[0];
	public string TestType
	{
		get => _TestType;
		set { SetProperty(ref _TestType, value); } // this is used to set the tab header
	}

	private string _StartFreq = string.Empty;
	public string StartFreq
	{
		get => _StartFreq;
		set => SetProperty(ref _StartFreq, value);
	}

	private string _EndFreq = string.Empty;
	public string EndFreq
	{
		get => _EndFreq;
		set => SetProperty(ref _EndFreq, value);
	}

	private uint _StepsOctave;         // type of alert
	public uint StepsOctave
	{
		get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
	}

	private string _Smoothing = string.Empty;         // type of alert
	public string Smoothing
	{
		get => _Smoothing;
		set => SetProperty(ref _Smoothing, value);
	}
	[JsonIgnore]
	public double SmoothingVal { get => MathUtil.ToDouble(Smoothing, 0); }

	private bool _RightChannel;         // type of alert
	public bool RightChannel
	{
		get => _RightChannel; set => SetProperty(ref _RightChannel, value);
	}

	private bool _LeftChannel;         // type of alert
	public bool LeftChannel
	{
		get => _LeftChannel; set => SetProperty(ref _LeftChannel, value);
	}

	private bool _ExpandPhaseAxis = true;       // expand the XAxis section?
	public bool ExpandPhaseAxis
	{
		get => _ExpandPhaseAxis;
		set => SetProperty(ref _ExpandPhaseAxis, value);
	}

	private string _PhaseBottom = "-180";
	public string PhaseBottom
	{
		get { return _PhaseBottom; }
		set => SetProperty(ref _PhaseBottom, value);
	}

	private string _PhaseTop = "180";
	public string PhaseTop
	{
		get { return _PhaseTop; }
		set => SetProperty(ref _PhaseTop, value);
	}

	private bool _ShowGroupDelay = false;
	public bool ShowGroupDelay
	{
		get => _ShowGroupDelay;
		set => SetProperty(ref _ShowGroupDelay, value);
	}

	private bool _ShowPoints;
	public bool ShowPoints
	{
		get => _ShowPoints;
		set => SetProperty(ref _ShowPoints, value);
	}

	private bool _ShowPercent;
	public bool ShowPercent
	{
		get => _ShowPercent;
		set => SetProperty(ref _ShowPercent, value);
	}

	private AcquireStep _QA430Cfg;
	[JsonIgnore]
	public AcquireStep QA430Cfg
	{
		get => _QA430Cfg;
		set => SetProperty(ref _QA430Cfg, value);
	}

	#endregion

	public TestingType GetTestingType(string type)
	{
		return (TestingType)Enum.Parse(typeof(TestingType), type);
	}

	private void SetVMForTest()
	{
		// set the tab header as we change type
		if (ViewSettings.Singleton != null && ViewSettings.Singleton.MainVm != null && ViewSettings.Singleton.MainVm.Frqa430Hdr != null)
		{
			ViewSettings.Singleton.MainVm.Frqa430Hdr = TestType;
		}

		SupplySummary = "15";

		switch (GetTestingType(TestType))
		{
			case TestingType.CMRR:
				PlotFormat = "dB";
				Gen1Voltage = "8";
				GenVoltageUnit = "mV";
				StartFreq = "20";
				EndFreq = "20000";
				SampleRate = "96000";
				FftSize = "32K";
				IsChirp = false;
				Smoothing = "0.01";
				Averages = 1;
				IsRiaa = false;
				StepsOctave = 4;
				break;
			case TestingType.InputZ:
				PlotFormat = "Ohms";
				Gen1Voltage = "0.5";
				GenVoltageUnit = "V";
				StartFreq = "20";
				EndFreq = "20000";
				SampleRate = "96000";
				FftSize = "256K";
				IsChirp = true;
				Smoothing = "0.01";
				Averages = 3;
				IsRiaa = false;
				break;
			case TestingType.PSRR:
				PlotFormat = "dB";
				Gen1Voltage = "0.1";
				GenVoltageUnit = "V";
				SupplySummary = "8";
				StartFreq = "20";
				EndFreq = "20000";
				SampleRate = "96000";
				FftSize = "64K";
				IsChirp = false;
				Smoothing = "0.01";
				Averages = 1;
				IsRiaa = false;
				break;
			case TestingType.GBW:
				PlotFormat = "dB";
				Gen1Voltage = "0.5";
				GenVoltageUnit = "mV";
				StartFreq = "200";
				EndFreq = "90000";
				SampleRate = "192000";
				FftSize = "256K";
				IsChirp = true;
				Smoothing = "0.01";
				Averages = 3;
				IsRiaa = false;
				break;
			case TestingType.Noise:
				PlotFormat = "dBV";
				Gen1Voltage = "0";
				GenVoltageUnit = "mV";
				StartFreq = ViewSettings.MinNoiseFrequency.ToString();
				EndFreq = ViewSettings.NoiseBandwidth.ToString();
				SampleRate = "192000";
				FftSize = "256K";
				IsChirp = true;
				Smoothing = "0.3";
				Averages = 1;
				IsRiaa = false;
				break;
		}

	}

	// the property change is used to trigger repaints of the graph
	private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case "DsPinAll":
				MyAction.PinAll(MainPlot.ThePlot, this);
				break;
			case "DsFitAll":
				MyAction.FitAll(MainPlot.ThePlot, this);
				break;
			case "DsSnapshot":
				MyAction.AddSnapshotPlot();
				break;
			case "DsPlotColors":
				MyAction?.DrawPlotLines(0);
				break;
			case "UpdateGraph":
				MyAction?.UpdateGraph(true);
				break;
			case "DsHeading":
			case "DsRepaint":
				MyAction?.UpdateGraph(true);
				break;
			case "DsName":
				MyAction?.UpdatePlotTitle();
				break;
			case "PlotFormat":
				// we may need to change the axis
				{
					var ttype = GetTestingType(MyVModel.TestType);
					var isdb = ttype == TestingType.Noise;
					ToShowRange = isdb ? Visibility.Collapsed : Visibility.Visible;
					ToShowdB = isdb ? Visibility.Visible : Visibility.Collapsed;
				}
				MyAction?.UpdateGraph(true);
				break;
			case "ShowTabInfo":
				if (actAbout != null)
				{
					actAbout.Visibility = ShowTabInfo ? Visibility.Visible : Visibility.Hidden;
					var fvm = actAbout.DataContext as FloorViewModel;
					if (fvm != null)
						fvm.ThemeBkgd = ViewSettings.Singleton.MainVm.ThemeBkgd; ;
				}
				break;
			case "Voltage":
			case "AmpLoad":
			case "OutPower":
			case "GenDirection":
				//MyAction?.UpdateGeneratorParameters();
				break;
			case "TestType":
				SetVMForTest();
				MyAction?.UpdateGraph(true);
				break;
			case "ShowGroupDelay":
			case "ShowPhase":
				MyAction?.UpdateGraph(true);
				break;
			case "GraphStartX":
			case "GraphEndX":
			case "RangeBottomdB":
			case "RangeBottom":
			case "RangeTopdB":
			case "RangeTop":
			case "PhaseTop":
			case "PhaseBottom":
			case "Range2Top":
			case "Range2Bottom":
				MyAction?.UpdateGraph(false, e.PropertyName);
				break;
			case "ShowRight":
			case "ShowLeft":
			case "ShowThickLines":
			case "StepsOctave":
			case "ShowPoints":
				MyAction?.UpdateGraph(false);
				break;
			default:
				OpampPropertyChanged(sender, e);
				break;

		}
	}

	public void SetAction(PlotControl plot, PlotControl plot2, PlotControl plot3, TabAbout TAbout)
	{
		LinkPlots(plot, plot2, plot3);
		actFreq = new ActFrQa430(this);
		actAbout = TAbout;
		actPlot = plot;
		MyVModel.LinkAbout(actFreq.PageData.Definition);
	}

	// here param is the id of the tab to remove from the othertab list
	public override void DoDeleteIt(string param)
	{
		var id = MathUtil.ToInt(param, -1);
		var fat = MyVModel.OtherSetList.FirstOrDefault(x => x.Id == id);
		if (fat != null)
		{
			MyVModel.OtherSetList.Remove(fat);
			MyAction.DeleteTab(id);
		}
	}

	private void RunIt()
	{
		// Implement the logic to start the measurement process
		actFreq?.RunMeasurement(true);
	}

	private void StartIt()
	{
		HasQA430 = QA430Model.BeginQA430Op();
		// Implement the logic to start the measurement process
		actFreq?.RunMeasurement(false);
	}

	private void StopIt()
	{
		actFreq?.DoCancel();
	}

	public DataBlob? GetFftData()
	{
		return MyAction.CreateExportData();
	}

	private static async Task LoadItTab()
	{
		await DoGetLoad(MyVModel.MyAction, PlotFileFilter, true);
	}

	private static async Task GetItTab()
	{
		await DoGetLoad(MyVModel.MyAction, PlotFileFilter, false);
	}


	private void SaveItTab()
	{
		string prefix = "QaFile";
		var ttype = GetTestingType(MyVModel.TestType);
		if (ttype == TestingType.CMRR)
			prefix = "QaCmrr";
		else if (ttype == TestingType.InputZ)
			prefix = "QaInputZ";
		else if (ttype == TestingType.GBW)
			prefix = "QaGbw";
		else if (ttype == TestingType.PSRR)
			prefix = "QaPsrr";

		var fname = GetSavePltName(prefix);

		// Process save file dialog box results
		if (fname.Length > 0)
		{
			actFreq.SaveToFile(fname);
		}
	}

	private void OnViewToFit(object? parameter)
	{
		var pram = parameter as string;
		if (pram == null)
			return;
		MyAction.PinGraphRange(pram);
	}

	public override void OnFitToData(object? parameter)
	{
		MyAction.FitToData(this, parameter, null);
		MyAction.UpdateGraph(false, PlotUtil.AxisParameter(parameter));
	}

	// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
	// here's the tracker event handler
	private static void DoMouseTracked(object sender, MouseEventArgs e)
	{
		var freqVm = MyVModel;
		freqVm.DoMouse(sender, e);
	}

	public override void UpdateMouseCursor(double freq, double dbvV)
	{
		if (freq <= 0)
			return;
		FreqValue = freq;
		// return (freq, real, imag, group_delay)
		var zv = MyAction.LookupX(FreqValue);
		if (zv.Item1 <= 1)
			return;
		var ttype = GetTestingType(TestType);
		FreqShow = zv.Item1.ToString("0.# Hz");
		switch (ttype)
		{
			case TestingType.Crosstalk:
				ZValue = "Left: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dB") + Environment.NewLine + "Right: " + (20 * Math.Log10(zv.Item3)).ToString("0.## dB");
				break;
			case TestingType.Noise:
				{
					ZValue = "Left: " + GraphUtil.PrettyPrint(zv.Item2, PlotFormat) + Environment.NewLine +
						"Right: " + GraphUtil.PrettyPrint(zv.Item3, PlotFormat);
				}
				break;
			case TestingType.Impedance:
				{

					ZValue = "Z: " + MathUtil.FormatUnits(zv.Item2, "|Z|") + ", " + MathUtil.FormatPhase(zv.Item3);
					if (zv.Item4 != 0.0)
					{
						ZValue += Environment.NewLine + "GDelay:" + MathUtil.FormatUnits(zv.Item4 / 1000, "S");
					}
					var x = Complex.FromPolarCoordinates(zv.Item2, Math.PI * zv.Item3 / 180);
					ZValue += Environment.NewLine + "X: " + MathUtil.FormatUnits(x.Real, "R") + ", " + MathUtil.FormatUnits(x.Imaginary, "I");
					var res = MathUtil.FormatResistance(x.Real);
					string sadd = string.Empty;
					if (x.Imaginary >= 0)
					{
						var ind = MathUtil.FormatInductance(x.Imaginary / (2 * Math.PI * zv.Item1));       // imaginary over 2piF
						sadd = res + " + " + ind;
					}
					else
					{
						var cap = MathUtil.FormatCapacitance(-1 / (2 * Math.PI * zv.Item1 * x.Imaginary));       // 2piF / imaginary
						sadd = res + " + " + cap;
					}
					ZValue += Environment.NewLine + sadd;
				}
				break;
			case TestingType.GBW:
			case TestingType.CMRR:
			case TestingType.PSRR:
				ZValue = "G: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dB") + Environment.NewLine + "  " + zv.Item3.ToString("0.## Deg");
				if (zv.Item4 != 0.0)
				{
					ZValue += Environment.NewLine + "GDelay:" + MathUtil.FormatUnits(zv.Item4 / 1000, "S");
				}
				break;
		}
		Debug.Assert(ReferenceEquals(this, MyVModel), "Not vmodel in this");
	}

	private void DoMouse(object sender, MouseEventArgs e)
	{
		SetMouseTrack(e);
		if (IsTracking)
		{
			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			FreqValue = Math.Pow(10, cord.Item1); // frequency
			LookX = FreqValue;   // cache the frequency here
		}
		UpdateMouseCursor(FreqValue, 0);
	}

	~FrQa430ViewModel()
	{
		PropertyChanged -= CheckPropertyChanged;
		MouseTracked -= DoMouseTracked;
	}

	public FrQa430ViewModel()
	{
		_Name = "Frqa430";
		MyId = $"{_Name}:{_MyIndex}";
		PropertyChanged += CheckPropertyChanged;
		MouseTracked += DoMouseTracked;

		actPlot = default!;
		actFreq = default!;

		LeftWidth = 95;
		RightWidth = 50;

		GraphStartX = "20";
		GraphEndX = "20000";
		StepsOctave = 1;
		LeftChannel = true;
		RightChannel = false;
		RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
		RangeBottom = "0.001";
		Range2Top = "1";             // when graphing group delay
		Range2Bottom = "-1";

		ShowThickLines = true;
		ShowPercent = false;
		ShowPhase = true;

		RangeTopdB = "20";
		RangeBottomdB = "-180";

		Gen1Voltage = "0.1";
		Smoothing = Smoothings[0];
		ShowPoints = true;
		TestType = TestTypes[0];    // this messes up if we start at impedance and set to impedance later so ??

		PlotFormat = "dBV";
		ToShowRange = Visibility.Collapsed;
		ToShowdB = Visibility.Visible;

		StartFreq = "20";
		EndFreq = "20000";
		ZReference = "10";

		// make a few things happen to synch the gui
		Task.Delay(1000).ContinueWith(_ =>
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				RaisePropertyChanged(nameof(TestType));
				MyAction?.UpdateGraph(true);
			});
		});
	}
}
