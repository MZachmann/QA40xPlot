using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using ScottPlot;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.ViewModels
{
	public class ThdAmpViewModel : BaseViewModel
	{
		public List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public List<String> StartVoltages { get => new List<string> { "0.0001", "0.0002", "0.0005", "0.001", "0.002", "0.005", "0.01", "0.02", "0.05", "0.1", "0.2", "0.5" }; }
		public List<String> EndVoltages{ get => new List<string> {"1","2","5","10","20","50","100","200" }; }
		public List<String> StartPercents { get => new List<string> { "100", "10", "1", "0.1", "0.01" }; }
		public List<String> EndPercents { get => new List<string> { "0.1", "0.01", "0.001", "0.0001", "0.00001", "0.000001" }; }
		public List<String> XAxisTypes { get => new List<string> {"Input Voltage", "Output Voltage", "Output Power" }; }
		public ActThdAmplitude actThd { get; private set; }

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

		private double _StartVoltageUnits;         // type of alert
		public double StartVoltageUnits
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

		private double _EndVoltageUnits;         // type of alert
		public double EndVoltageUnits
		{
			get => _EndVoltageUnits; set => SetProperty(ref _EndVoltageUnits, value);
		}

		private double _AmpLoad;         // type of alert
		public double AmpLoad
		{
			get => _AmpLoad; set => SetProperty(ref _AmpLoad, value);
		}

		private double _OutPower;         // type of alert
		public double OutPower
		{
			get => _OutPower; set => SetProperty(ref _OutPower, value);
		}

		private double _OutVoltage;         // type of alert
		public double OutVoltage
		{
			get => _OutVoltage; set => SetProperty(ref _OutVoltage, value);
		}

		private double _TestFreq;         // type of alert
		public double TestFreq
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
		private int _OutputUnits;
		public int OutputUnits
		{
			get => _OutputUnits;
			set => SetProperty(ref _OutputUnits, value);
		}
		private double _AmpOutputAmplitude;
		public double AmpOutputAmplitude
		{
			get => _AmpOutputAmplitude;
			set => SetProperty(ref _AmpOutputAmplitude, value);
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
		private Libraries.Windowing _Windowing;
		public Libraries.Windowing WindowingMethod
		{
			get => _Windowing;
			set => SetProperty(ref _Windowing, value);
		}
		private int _InputRange;
		public int InputRange
		{
			get => _InputRange;
			set => SetProperty(ref _InputRange, value);
		}
		private int _XAxisType;
		public int XAxisType
		{
			get => _XAxisType;
			set => SetProperty(ref _XAxisType, value);
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
		private bool _ReadVoltage;
		public bool ReadVoltage
		{
			get => _ReadVoltage;
			set => SetProperty(ref _ReadVoltage, value);
		}
		private bool _ReadOutPower;
		public bool ReadOutPower
		{
			get => _ReadOutPower;
			set => SetProperty(ref _ReadOutPower, value);
		}
		private bool _ReadOutVoltage;
		public bool ReadOutVoltage
		{
			get => _ReadOutVoltage;
			set => SetProperty(ref _ReadOutVoltage, value);
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

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2)
		{
			ThdAmplitudeData data = new ThdAmplitudeData();
			actThd = new ActThdAmplitude(ref data, plot, plot1, plot2);
		}

		~ThdAmpViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
		}

		public ThdAmpViewModel()
		{
			PropertyChanged += CheckPropertyChanged;

			StartVoltage = 0.03;
			StartVoltageUnits = 1;
			EndVoltage = 1;
			EndVoltageUnits = 1;

			AmpLoad = 8;
			OutPower = 0.5;
			OutVoltage = 0.5;
			TestFreq = 20;
			GraphStartVolts = "0.002";
			GraphEndVolts = "10";
			StepsOctave = 1;
			Averages = 1;
			LeftChannel = true;
			RightChannel = false;
			MeasureType = 2;
			OutputUnits = 0;
			GeneratorUnits = 0;
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowPoints = false;
			ShowPercent = true;
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
			FftSize = 65536;
			WindowingMethod = 0;

			InputRange = 0;
			RangeTopdB = 20;
			RangeBottomdB = -150;

			ToShowRange = Visibility.Visible;
			ToShowdB = Visibility.Visible;
			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { ShowPercent = false; MeasureType = 0; });

			ReadVoltage = true;
			ReadOutPower = true;
			ReadOutVoltage = true;

			XAxisType = 0;
			StartAmplitude = -10;       // this is the unitless (dbV) amplitude of the generator
			EndAmplitude = 0;       // this is the unitless (dbV) amplitude of the generator
			EndVoltage = QaLibrary.ConvertVoltage(EndAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
			StartVoltage = QaLibrary.ConvertVoltage(StartAmplitude, E_VoltageUnit.dBV, E_VoltageUnit.Volt);
		}
	}
}
