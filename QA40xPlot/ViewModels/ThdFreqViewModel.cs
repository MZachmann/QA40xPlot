using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using ScottPlot;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.ViewModels
{
	public class ThdFreqViewModel : BaseViewModel
	{
		public List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public List<String> MeasureTypes { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }
		public List<String> StartFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500" }; }
		private ActThdFrequency actThd { get; set; }
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }

		#region Setters and Getters
		private double _GenVoltage;
		public double GenVoltage
		{
			get => _GenVoltage; set => SetProperty(ref _GenVoltage, value);
		}

		private double _AmpLoad;
		public double AmpLoad
		{
			get => _AmpLoad; set => SetProperty(ref _AmpLoad, value);
		}

		private double _OutPower;
		public double OutPower
		{
			get => _OutPower; set => SetProperty(ref _OutPower, value);
		}

		private double _OutVoltage;
		public double OutVoltage
		{
			get => _OutVoltage; set => SetProperty(ref _OutVoltage, value);
		}

		private double _StartFreq;
		public double StartFreq
		{
			get => _StartFreq;
			set => SetProperty(ref _StartFreq, value);
		}

		private double _EndFreq;
		public double EndFreq
		{
			get => _EndFreq;
			set => SetProperty(ref _EndFreq, value);
		}

		private string _GraphStartFreq;
		public string GraphStartFreq
		{
			get => _GraphStartFreq;
			set => SetProperty(ref _GraphStartFreq, value);
		}

		private string _GraphEndFreq;
		public string GraphEndFreq
		{
			get => _GraphEndFreq;
			set => SetProperty(ref _GraphEndFreq, value);
		}

		private uint _StepsOctave;
		public uint StepsOctave
		{
			get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
		}

		private uint _Averages;
		public uint Averages
		{
			get => _Averages; set => SetProperty(ref _Averages, value);
		}

		private bool _RightChannel;
		public bool RightChannel
		{
			get => _RightChannel; set => SetProperty(ref _RightChannel, value);
		}

		private bool _LeftChannel;
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

		private static void StartIt(object parameter)
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			vm.actThd.StartMeasurement();
		}

		private static void StopIt(object parameter)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			vm.actThd.DoCancel();
		}

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "OutputUnits":
					actThd?.UpdateAmpOutputVoltageDisplay();
					break;
				case "GeneratorUnits":
					actThd?.UpdateGeneratorVoltageDisplay();
					break;
				case "Voltage":
				case "AmpLoad":
				case "OutPower":
				case "MeasureType":
				case "VoltageUnits":
					actThd?.UpdateGeneratorParameters();
					break;
				case "ShowPercent":
					ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
					ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
					actThd?.UpdateGraph(true);
					break;
				case "GraphStartFreq":
				case "GraphEndFreq":
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
			ThdFrequencyData data = new ThdFrequencyData();
			actThd = new ActThdFrequency(ref data, plot, plot1, plot2);
		}


		public void OnVoltageChanged(string news)
		{
			actThd.UpdateGenAmplitude(news);
		}

		public void OnAmpVoltageChanged(string news)
		{
			actThd.UpdateAmpAmplitude(news);
		}

		public string SerializeAll()
		{
			string jsonString = JsonSerializer.Serialize(this);
			//Console.WriteLine(jsonString);
			return jsonString;
		}

		~ThdFreqViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
		}

		public ThdFreqViewModel()
		{
			PropertyChanged += CheckPropertyChanged;

			PropertyChanged += CheckPropertyChanged;

			GenVoltage = 0.03;
			AmpLoad = 8;
			OutPower = 0.5;
			OutVoltage = 0.5;
			StartFreq = 20;
			EndFreq = 20000;
			GraphStartFreq = "20";
			GraphEndFreq = "20000";
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
			RangeBottomdB = -180;

			ToShowRange = Visibility.Visible;
			ToShowdB = Visibility.Visible;
			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { ShowPercent = false; MeasureType = 0; });

			ReadVoltage = true;
			ReadOutPower = true;
			ReadOutVoltage = true;

			GeneratorAmplitude = -20;       // this is the unitless (dbV) amplitude of the generator
			AmpOutputAmplitude = -20;         // this is the unitless (dbV) amplitude of the amplifier output
			GenVoltage = QaLibrary.ConvertVoltage(GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)GeneratorUnits);
			OutVoltage = QaLibrary.ConvertVoltage(AmpOutputAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)OutputUnits);
		}
	}
}
