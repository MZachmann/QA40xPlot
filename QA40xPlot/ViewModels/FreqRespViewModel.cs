using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using QA40xPlot.Views;
using System.Windows;
using System.ComponentModel;
using System.Text.Json;

public class FreqRespViewModel : BaseViewModel
{
	public List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
	public List<String> MeasureTypes { get => new List<string> { "Input Voltage", "Output Voltage" }; }
	public List<String> GenAmplitudes { get => new List<string> { "0.05", "0.1", "0.25", "0.5", "0.75", "1", "2", "5" }; }
	public List<String> Smoothings { get => new List<string> { "None", "1/24", "1/6" }; }
	private ActFrequencyResponse actFreq { get;  set; }
	public RelayCommand DoStart { get => new RelayCommand(StartIt); }
	public RelayCommand DoStop { get => new RelayCommand(StopIt); }
	public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }


	#region Setters and Getters
	private string _Smoothing;         // type of alert
	public string Smoothing
	{
		get => _Smoothing;
		set => SetProperty(ref _Smoothing, value);
	}

	private string _Gen1Voltage;         // type of alert
	public string Gen1Voltage
	{
		get => _Gen1Voltage;
		set => SetProperty(ref _Gen1Voltage, value);
	}

	private string _GraphStartFreq;         // type of alert
	public string GraphStartFreq
	{
		get => _GraphStartFreq;
		set => SetProperty(ref _GraphStartFreq, value);
	}

	private string _GraphEndFreq;         // type of alert
	public string GraphEndFreq
	{
		get => _GraphEndFreq;
		set =>
			SetProperty(ref _GraphEndFreq, value);
	}

	private uint _Averages;         // type of alert
	public uint Averages
	{
		get => _Averages; set => SetProperty(ref _Averages, value);
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
	private int _MeasureType;
	public int MeasureType
	{
		get => _MeasureType;
		set => SetProperty(ref _MeasureType, value);
	}

	private double _GeneratorAmplitude;
	public double GeneratorAmplitude
	{
		get => _GeneratorAmplitude;
		set => SetProperty(ref _GeneratorAmplitude, value);
	}
	private int _GeneratorUnits;
	public int GeneratorUnits
	{
		get => _GeneratorUnits;
		set => SetProperty(ref _GeneratorUnits, value);
	}
	private string _rangeTop;
	public string RangeTop
	{
		get { return _rangeTop; }
		set => SetProperty(ref _rangeTop, value);
	}

	private string _rangeBottom;
	public string RangeBottom
	{
		get { return _rangeBottom; }
		set => SetProperty(ref _rangeBottom, value);
	}
	private int _rangeTopdB;
	public int RangeTopdB
	{
		get { return _rangeTopdB; }
		set => SetProperty(ref _rangeTopdB, value);
	}

	private int _rangeBottomdB;
	public int RangeBottomdB
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

	private bool _ShowMarkers = true;
	public bool ShowMarkers
	{
		get => _ShowMarkers;
		set => SetProperty(ref _ShowMarkers, value);
	}

	private bool _ShowPowerMarkers = false;
	public bool ShowPowerMarkers
	{
		get => _ShowPowerMarkers;
		set => SetProperty(ref _ShowPowerMarkers, value);
	}

	private bool _ShowPercent;
	public bool ShowPercent
	{
		get => _ShowPercent;
		set => SetProperty(ref _ShowPercent, value);
	}

	private bool _ShowLeft;
	public bool ShowLeft
	{
		get => _ShowLeft;
		set => SetProperty(ref _ShowLeft, value);
	}

	private bool _ShowRight;
	public bool ShowRight
	{
		get => _ShowRight;
		set => SetProperty(ref _ShowRight, value);
	}
	private bool _ShowGain;
	public bool ShowGain
	{
		get => _ShowGain;
		set => SetProperty(ref _ShowGain, value);
	}

	private string _SampleRate;
	public string SampleRate
	{
		get => _SampleRate;
		set => SetProperty(ref _SampleRate, value);
	}
	private string _FftSize;
	public string FftSize
	{
		get => _FftSize;
		set => SetProperty(ref _FftSize, value);
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
	public Visibility ToShowRange
	{
		get => _ToShowRange;
		set => SetProperty(ref _ToShowRange, value);
	}
	private Visibility _ToShowdB;
	public Visibility ToShowdB
	{
		get => _ToShowdB;
		set => SetProperty(ref _ToShowdB, value);
	}
	#endregion

	// the property change is used to trigger repaints of the graph
	private void CheckPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case "GeneratorUnits":
				actFreq?.UpdateGeneratorVoltageDisplay();
				break;
			case "Voltage":
			case "AmpLoad":
			case "OutPower":
			case "MeasureType":
			case "VoltageUnits":
				//actFreq?.UpdateGeneratorParameters();
				break;
			case "ShowPercent":
				ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
				ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
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
			case "ShowMarkers":
			case "ShowGain":
				actFreq?.UpdateGraph(true);
				break;
			default:
				break;
		}
	}

	public void SetAction(PlotControl plot)
	{
		FrequencyResponseData data = new FrequencyResponseData();
		actFreq = new ActFrequencyResponse(ref data, plot);
	}

	private static void StartIt(object parameter)
	{
		// Implement the logic to start the measurement process
		var vm = QA40xPlot.ViewModels.ViewSettings.Singleton.FreqRespVm;
		vm.actFreq.StartMeasurement();
	}

	private static void StopIt(object parameter)
	{
		var vm = QA40xPlot.ViewModels.ViewSettings.Singleton.FreqRespVm;
		vm.actFreq.DoCancel();
	}

	~FreqRespViewModel()
	{
		PropertyChanged -= CheckPropertyChanged;
	}

	public string SerializeAll()
	{
		string jsonString = JsonSerializer.Serialize(this);
		//Console.WriteLine(jsonString);
		return jsonString;
	}

	public FreqRespViewModel()
	{
		PropertyChanged += CheckPropertyChanged;

		GraphStartFreq = "20";
		GraphEndFreq = "20000";
		Averages = 1;
		LeftChannel = true;
		RightChannel = false;
		MeasureType = 2;
		GeneratorUnits = (int)E_VoltageUnit.Volt;
		RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
		RangeBottom = "0.001";

		ShowThickLines = true;
		ShowSummary = true;
		ShowPercent = true;
		ShowLeft = true;
		ShowRight = false;

		SampleRate = "96000";
		FftSize = "64K";

		RangeTopdB = 20;
		RangeBottomdB = -180;

		ShowMarkers = true;
		ShowPowerMarkers = false;

		ToShowRange = Visibility.Visible;
		ToShowdB = Visibility.Visible;
		// make a few things happen to synch the gui
		Task.Delay(1000).ContinueWith(t => { ShowPercent = false; MeasureType = 0; });

		GeneratorAmplitude = -20;
		Gen1Voltage = QaLibrary.ConvertVoltage(GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)GeneratorUnits).ToString();
		Smoothing = "None";
		Show3dBBandwidth_L = true;
		Show3dBBandwidth_R = false;
		Show1dBBandwidth_L = false;
		Show1dBBandwidth_R = false;
	}
}
