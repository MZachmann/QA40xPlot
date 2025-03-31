using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using QA40xPlot.Views;
using System.Windows;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Interop;


public class FreqRespViewModel : BaseViewModel
{
	public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
	public static List<String> Smoothings { get => new List<string> { "None", "1/24", "1/6" }; }
	public static List<String> TestTypes { get => new List<string> { "Response", "Impedance", "Gain" }; }

	private PlotControl actPlot { get; set; }
	private ActFrequencyResponse actFreq { get;  set; }
	[JsonIgnore]
	public RelayCommand DoStart { get => new RelayCommand(StartIt); }
	[JsonIgnore]
	public RelayCommand DoStop { get => new RelayCommand(StopIt); }
	[JsonIgnore]
	public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }
	[JsonIgnore]
	public RelayCommand<object> DoFitToData { get => new RelayCommand<object>(OnFitToData); }
	[JsonIgnore]
	private static FreqRespViewModel MyVModel { get => ViewSettings.Singleton.FreqRespVm; }
	[JsonIgnore]
	public bool IsNotChirp { get => !_IsChirp; }

	#region Setters and Getters
	private bool _IsChirp;
	public bool IsChirp
	{
		get { return _IsChirp; }
		set { SetProperty(ref _IsChirp, value); OnPropertyChanged("IsNotChirp"); }
	}

	private string _ZReference = string.Empty;
	public string ZReference
	{
		get => _ZReference;
		set => SetProperty(ref _ZReference, value);
	}

	private string _GraphUnit = string.Empty;
	[JsonIgnore]
	public string GraphUnit
	{
		get => _GraphUnit;
		set => SetProperty(ref _GraphUnit, value);
	}

	private string _TestType = string.Empty;
	public string TestType
	{
		get => _TestType;
		set => SetProperty(ref _TestType, value);
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

	private string _Gen1Voltage = string.Empty;         // type of alert
	public string Gen1Voltage
	{
		get => _Gen1Voltage;
		set => SetProperty(ref _Gen1Voltage, value);
	}

	private string _GraphStartFreq = string.Empty;         // type of alert
	public string GraphStartFreq
	{
		get => _GraphStartFreq;
		set => SetProperty(ref _GraphStartFreq, value);
	}

	private string _GraphEndFreq = string.Empty;         // type of alert
	public string GraphEndFreq
	{
		get => _GraphEndFreq;
		set =>
			SetProperty(ref _GraphEndFreq, value);
	}

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
	private string _rangeTop = string.Empty;
	public string RangeTop
	{
		get { return _rangeTop; }
		set => SetProperty(ref _rangeTop, value);
	}

	private string _rangeBottom = string.Empty;
	public string RangeBottom
	{
		get { return _rangeBottom; }
		set => SetProperty(ref _rangeBottom, value);
	}
	private string _rangeTopdB = string.Empty;
	public string RangeTopdB
	{
		get { return _rangeTopdB; }
		set => SetProperty(ref _rangeTopdB, value);
	}

	private string _rangeBottomdB = string.Empty;
	public string RangeBottomdB
	{
		get { return _rangeBottomdB; }
		set => SetProperty(ref _rangeBottomdB, value);
	}

	private bool _ShowThickLines;
	public bool ShowThickLines
	{
		get => _ShowThickLines;
		set => SetProperty(ref _ShowThickLines, value);
	}

	private bool _ShowPoints;
	public bool ShowPoints
	{
		get => _ShowPoints;
		set => SetProperty(ref _ShowPoints, value);
	}

	private bool _ShowSummary = true;
	public bool ShowSummary
	{
		get => _ShowSummary;
		set => SetProperty(ref _ShowSummary, value);
	}

	private bool _ShowPercent;
	public bool ShowPercent
	{
		get => _ShowPercent;
		set => SetProperty(ref _ShowPercent, value);
	}

	private bool _Show3dBBandwidth_L;
	public bool Show3dBBandwidth_L
	{
		get => _Show3dBBandwidth_L;
		set => SetProperty(ref _Show3dBBandwidth_L, value);
	}
	private bool _Show3dBBandwidth_R;
	public bool Show3dBBandwidth_R
	{
		get => _Show3dBBandwidth_R;
		set => SetProperty(ref _Show3dBBandwidth_R, value);
	}

	private bool _Show1dBBandwidth_L;
	public bool Show1dBBandwidth_L
	{
		get => _Show1dBBandwidth_L;
		set => SetProperty(ref _Show1dBBandwidth_L, value);
	}
	private bool _Show1dBBandwidth_R;
	public bool Show1dBBandwidth_R
	{
		get => _Show1dBBandwidth_R;
		set => SetProperty(ref _Show1dBBandwidth_R, value);
	}
	private Visibility _ToShowRange;
	[JsonIgnore]
	public Visibility ToShowRange
	{
		get => _ToShowRange;
		set => SetProperty(ref _ToShowRange, value);
	}
	private Visibility _ToShowdB;
	[JsonIgnore]
	public Visibility ToShowdB
	{
		get => _ToShowdB;
		set => SetProperty(ref _ToShowdB, value);
	}
	#endregion

	public TestingType GetTestingType(string type)
	{
		var vm = MyVModel;
		return (TestingType)TestTypes.IndexOf(type);
	}

	// the property change is used to trigger repaints of the graph
	private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case "Voltage":
			case "AmpLoad":
			case "OutPower":
			case "GenDirection":
			case "VoltageUnits":
				//actFreq?.UpdateGeneratorParameters();
				break;
			case "ShowPercent":
				ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
				ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
				actFreq?.UpdateGraph(true);
				break;
			case "TestType":
				// set the tab header as we change type
				if( ViewSettings.Singleton != null && ViewSettings.Singleton.Main != null && ViewSettings.Singleton.Main.FreqRespHdr != null)
				{
					ViewSettings.Singleton.Main.FreqRespHdr = TestType;
				}
				actFreq?.UpdateGraph(true);
				break;
			case "GraphStartFreq":
			case "GraphEndFreq":
			case "RangeBottomdB":
			case "RangeBottom":
			case "RangeTopdB":
			case "RangeTop":
			case "ShowRight":
			case "ShowLeft":
			case "ShowThickLines":
			case "StepsOctave":
				actFreq?.UpdateGraph(true);
				break;
			default:
				break;
		}
	}

	public void SetAction(PlotControl plot, PlotControl plot2, PlotControl plot3)
	{
		FrequencyResponseData data = new FrequencyResponseData();
		actFreq = new ActFrequencyResponse(ref data, plot, plot2, plot3);
		SetupMainPlot(plot);
		actPlot = plot;
	}

	private static void StartIt()
	{
		// Implement the logic to start the measurement process
		var vm = MyVModel;
		vm.actFreq.StartMeasurement();
	}

	private static void StopIt()
	{
		var vm = MyVModel;
		vm.actFreq.DoCancel();
	}

	public DataBlob? GetFftData()
	{
		var vm = MyVModel;
		return vm.actFreq.CreateExportData();
	}

	private void OnFitToData(object? parameter)
	{
		var bounds = actFreq.GetDataBounds();
		switch (parameter)
		{
			case "XF":  // X frequency
				this.GraphStartFreq = bounds.Left.ToString("0");
				this.GraphEndFreq = bounds.Right.ToString("0");
				break;
			case "YP":  // Y percent
				var xp = bounds.Y + bounds.Height;  // max Y value
				var bot = ((100 * bounds.Y) / xp);  // bottom value in percent
				bot = Math.Pow(10, Math.Max(-7, Math.Floor(Math.Log10(bot))));  // nearest power of 10
				this.RangeTop = "100";  // always 100%
				this.RangeBottom = bot.ToString("0.##########");
				break;
			case "YM":  // Y magnitude
				var ttype = GetTestingType(TestType);
				if (ttype == TestingType.Impedance)
				{
					this.RangeBottomdB = bounds.Y.ToString("0");
					this.RangeTopdB = (bounds.Height + bounds.Y).ToString("0");
				}
				else
				{
					this.RangeBottomdB = (20 * Math.Log10(Math.Max(1e-14, bounds.Y))).ToString("0");
					this.RangeTopdB = Math.Ceiling((20 * Math.Log10(Math.Max(1e-14, bounds.Height + bounds.Y)))).ToString("0");
				}
				break;
			default:
				break;
		}
		actFreq?.UpdateGraph(false);
	}

	// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
	// here's the tracker event handler
	private static void DoMouseTracked(object sender, MouseEventArgs e)
	{
		var freqVm = MyVModel;
		freqVm.DoMouse(sender, e);
	}

	private void DoMouse(object sender, MouseEventArgs e)
	{
		SetMouseTrack(e);
		if (IsTracking)
		{
			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			FreqValue = Math.Pow(10, cord.Item1); // frequency
		}
		var zv = actFreq.LookupX(FreqValue);
		var ttype = GetTestingType(TestType);
		FreqShow = zv.Item1.ToString("0.# Hz");
		switch ( ttype)
		{
			case TestingType.Response:
				ZValue = "Left: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dB") + Environment.NewLine + "Right: " + (20 * Math.Log10(zv.Item3)).ToString("0.## dB");
				break;
			case TestingType.Impedance:
				{

					ZValue = "Z: " + zv.Item2.ToString("0.## Ohms") + Environment.NewLine + "  " + zv.Item3.ToString("0.## Deg");
				}
				break;
			case TestingType.Gain:
				ZValue = "G: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dB") + Environment.NewLine + "  " + zv.Item3.ToString("0.## Deg");
				break;
		}
	}

	~FreqRespViewModel()
	{
		PropertyChanged -= CheckPropertyChanged;
		MouseTracked -= DoMouseTracked;
	}

	public FreqRespViewModel()
	{
		PropertyChanged += CheckPropertyChanged;
		MouseTracked += DoMouseTracked;

		actPlot = default!;
		actFreq = default!;

		GraphStartFreq = "20";
		GraphEndFreq = "20000";
		StepsOctave = 1;
		LeftChannel = true;
		RightChannel = false;
		RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
		RangeBottom = "0.001";

		ShowThickLines = true;
		ShowSummary = true;
		ShowPercent = false;
		ShowLeft = true;
		ShowRight = false;

		RangeTopdB = "20";
		RangeBottomdB = "-180";

		Gen1Voltage = "0.1";
		Smoothing = "None";
		Show3dBBandwidth_L = true;
		Show3dBBandwidth_R = false;
		Show1dBBandwidth_L = false;
		Show1dBBandwidth_R = false;
		ShowPoints = true;
		TestType = "";	// this messes up if we start at impedance and set to impedance later so ??

		GraphUnit = "dBV";

		StartFreq = "20";
		EndFreq = "20000";

		ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
		ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;      
		// make a few things happen to synch the gui
		Task.Delay(1000).ContinueWith(t => { actFreq?.UpdateGraph(true); });
	}
}
