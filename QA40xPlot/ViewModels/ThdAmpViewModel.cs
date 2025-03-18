using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using FftSharp.Windows;
using static FreqRespViewModel;
using System.DirectoryServices.ActiveDirectory;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public class ThdAmpViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> MeasureTypes { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }
		public static List<String> StartVoltages { get => new List<string> { "0.0001", "0.0002", "0.0005", "0.001", "0.002", "0.005", "0.01", "0.02", "0.05", "0.1", "0.2", "0.5" }; }
		public static List<String> EndVoltages { get => new List<string> { "1", "2", "5", "10", "20", "50", "100", "200" }; }

		private ActThdAmplitude actThd { get; set; }
		private PlotControl actPlot {  get; set; }

		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }

		#region Setters and Getters
		private double _StartVoltage;         // type of alert
		public double StartVoltage
		{
			get => _StartVoltage; set => SetProperty(ref _StartVoltage, value);
		}

		private double _StartAmplitude;         // type of alert
		public double StartAmplitude
		{
			get => _StartAmplitude; set => SetProperty(ref _StartAmplitude, value);
		}
		private uint _StartVoltageUnits;         // type of alert
		public uint StartVoltageUnits
		{
			get => _StartVoltageUnits; set => SetProperty(ref _StartVoltageUnits, value);
		}

		private double _EndVoltage;         // type of alert
		public double EndVoltage
		{
			get => _EndVoltage; set => SetProperty(ref _EndVoltage, value);
		}

		private double _EndAmplitude;         // type of alert
		public double EndAmplitude
		{
			get => _EndAmplitude; set => SetProperty(ref _EndAmplitude, value);
		}
		private uint _EndVoltageUnits;         // type of alert
		public uint EndVoltageUnits
		{
			get => _EndVoltageUnits; set => SetProperty(ref _EndVoltageUnits, value);
		}

		private double _AmpLoad;         // type of alert
		public double AmpLoad
		{
			get => _AmpLoad; set => SetProperty(ref _AmpLoad, value);
		}

		private string _TestFreq;         // type of alert
		public string TestFreq
		{
			get => _TestFreq;
			set => SetProperty(ref _TestFreq, value);
		}

		private string _GraphStartVolts;         // type of alert
		public string GraphStartVolts
		{
			get => _GraphStartVolts;
			set => SetProperty(ref _GraphStartVolts, value);
		}

		private string _GraphEndVolts;         // type of alert
		public string GraphEndVolts
		{
			get => _GraphEndVolts;
			set =>
				SetProperty(ref _GraphEndVolts, value);
		}

		private uint _StepsOctave;         // type of alert
		public uint StepsOctave
		{
			get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
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
		private bool _ShowTHD;
		public bool ShowTHD
		{
			get => _ShowTHD;
			set => SetProperty(ref _ShowTHD, value);
		}
		private bool _ShowMagnitude;
		public bool ShowMagnitude
		{
			get => _ShowMagnitude;
			set => SetProperty(ref _ShowMagnitude, value);
		}
		private bool _ShowD2;
		public bool ShowD2
		{
			get => _ShowD2;
			set => SetProperty(ref _ShowD2, value);
		}
		private bool _ShowD3;
		public bool ShowD3
		{
			get => _ShowD3;
			set => SetProperty(ref _ShowD3, value);
		}
		private bool _ShowD4;
		public bool ShowD4
		{
			get => _ShowD4;
			set => SetProperty(ref _ShowD4, value);
		}
		private bool _ShowD5;
		public bool ShowD5
		{
			get => _ShowD5;
			set => SetProperty(ref _ShowD5, value);
		}
		private bool _ShowD6;
		public bool ShowD6
		{
			get => _ShowD6;
			set => SetProperty(ref _ShowD6, value);
		}
		private bool _ShowNoiseFloor;
		public bool ShowNoiseFloor
		{
			get => _ShowNoiseFloor;
			set => SetProperty(ref _ShowNoiseFloor, value);
		}
		private uint _SampleRate;
		public uint SampleRate
		{
			get => _SampleRate;
			set => SetProperty(ref _SampleRate, value);
		}
		private uint _FftSize;
		public uint FftSize
		{
			get => _FftSize;
			set => SetProperty(ref _FftSize, value);
		}
		private string _Windowing;
		public string WindowingMethod
		{
			get => _Windowing;
			set => SetProperty(ref _Windowing, value);
		}
		private int _XAxisType;
		public int XAxisType
		{
			get => _XAxisType;
			set => SetProperty(ref _XAxisType, value);
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

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "StartVoltage":
				case "StartVoltageUnits":
					actThd?.UpdateStartVoltageDisplay();
					break;
				case "EndVoltage":
				case "EndVoltageUnits":
					actThd?.UpdateEndVoltageDisplay();
					break;
				case "ShowPercent":
					ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
					ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
					actThd?.UpdateGraph(true);
					break;
				case "XAxisType":
				case "GraphStartVolts":
				case "GraphEndVolts":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
				case "ShowRight":
				case "ShowLeft":
				case "ShowTHD":
				case "ShowMagnitude":
				case "ShowD2":
				case "ShowD3":
				case "ShowD4":
				case "ShowD5":
				case "ShowD6":
				case "ShowNoiseFloor":
				case "ShowPoints":
				case "ShowThickLines":
					actThd?.UpdateGraph(true);
					break;
				default:
					break;
			}
		}

		private static void StartIt(object parameter)
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			vm.actThd.StartMeasurement();
		}

		private static void StopIt(object parameter)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			vm.actThd.DoCancel();
		}

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2)
		{
			ThdAmplitudeData data = new ThdAmplitudeData();
			actThd = new ActThdAmplitude(ref data, plot, plot1, plot2);
			SetupMainPlot(plot);
			actPlot = plot;
		}

		public void OnStartVoltageChanged(string news)
		{
			actThd.UpdateStartAmplitude(news);
		}

		public void OnEndVoltageChanged(string news)
		{
			actThd.UpdateEndAmplitude(news);
		}

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var thdAmpVm = ViewSettings.Singleton.ThdAmp;
			thdAmpVm.DoMouse(sender, e);
		}

		private void DoMouse(object sender, MouseEventArgs e)
		{

			if (e.LeftButton == MouseButtonState.Pressed && !IsMouseDown)
			{
				IsTracking = !IsTracking;
				IsMouseDown = true;
			}
			else
			if (e.LeftButton == MouseButtonState.Released && IsMouseDown)
			{
				IsMouseDown = false;
			}
			if (IsTracking)
			{
				var p = e.GetPosition(actPlot);
				var cord = ConvertScottCoords(actPlot, p.X, p.Y);
				FreqValue = Math.Pow(10, cord.Item1); // frequency
			}
			var zv = actThd.LookupX(FreqValue);
			//var ttype = actThd.GetTestingType(TestType);
			//FreqShow = zv.Item1.ToString("0.# Hz");
			//switch (ttype)
			//{
			//	case TestingType.Response:
			//		ZValue = "Left: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dBV") + Environment.NewLine + "Right: " + (20 * Math.Log10(zv.Item3)).ToString("0.## dBV");
			//		break;
			//	case TestingType.Impedance:
			//		ZValue = "Z: " + (20 * Math.Log10(zv.Item2)).ToString("0.## Ohms") + Environment.NewLine + "  " + zv.Item3.ToString("0.## Deg");
			//		break;
			//	case TestingType.Gain:
			//		ZValue = "G: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dB") + Environment.NewLine + "  " + zv.Item3.ToString("0.## Deg");
			//		break;
			//}
		}

		~ThdAmpViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public ThdAmpViewModel()
		{
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			AmpLoad = 8;
			TestFreq = "1000";
			GraphStartVolts = "0.002";
			GraphEndVolts = "10";
			StepsOctave = 1;
			Averages = 3;
			LeftChannel = true;
			RightChannel = false;
			MeasureType = 2;
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowPoints = false;
			ShowPercent = false;
			ShowLeft = true;
			ShowRight = false;
			ShowTHD = true;
			ShowMagnitude = true;
			ShowD2 = true;
			ShowD3 = true;
			ShowD4 = true;
			ShowD5 = true;
			ShowD6 = true;
			ShowNoiseFloor = true;

			SampleRate = 96000;
			FftSize = 65536*2;
			WindowingMethod = "Hann";
			RangeTopdB = 20;
			RangeBottomdB = -180;

			XAxisType = 0;
			StartAmplitude = -10;       // this is the unitless (dbV) amplitude of the generator
			EndAmplitude = 0;       // this is the unitless (dbV) amplitude of the generator
			StartVoltageUnits = 1;
			EndVoltageUnits = 1;
			StartVoltage = QaLibrary.ConvertVoltage(StartAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)StartVoltageUnits);
			EndVoltage = QaLibrary.ConvertVoltage(EndAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)EndVoltageUnits);

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;          
			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { actThd?.UpdateGraph(true); });
		}
	}
}

