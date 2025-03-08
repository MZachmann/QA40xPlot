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
using Windows.Storage.BulkAccess;

namespace QA40xPlot.ViewModels
{
	public class ImdViewModel : BaseViewModel
	{
		public List<String> WindowingTypes { get => new List<string> { "Rectangle", "Hann", "FlatTop" }; }
		public List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public List<String> MeasureTypes { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }
		public List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }
		public List<String> GenAmplitudes { get => new List<string> { "0.05", "0.1", "0.25", "0.5", "0.75", "1", "2", "5" }; }
		public List<String> StartFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500" }; }
		public List<String> IntermodTypes { get => new List<string> { "Custom", "SMPTE (60Hz.7KHz 4:1)", "DIN (250Hz.8KHz 4:1",
			"CCIF (19KHz.20KHz 1:1)", "AES-17 MD (41Hz.7993Hz 4:1)", "AES-17 DFD (18KHz.20KHz 1:1)",
			"TDFD Phono (3005Hz.4462Hz 1:1)" }; }
		private ActImd actImd { get;  set; }
		private ChannelInfo actInfo { get;  set; }
		public RelayCommand SetAttenuate { get => new RelayCommand(SetAtten); }
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }
		public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }

		#region Setters and Getters
		private bool _ShowChannelInfo = false;
		public bool ShowChannelInfo
		{
			get => _ShowChannelInfo;
			set => SetProperty(ref _ShowChannelInfo, value);
		}

		private double _AmpLoad;         // type of alert
		public double AmpLoad
		{
			get => _AmpLoad; set => SetProperty(ref _AmpLoad, value);
		}
		private double _OutVoltage;         // type of alert
		public double OutVoltage
		{
			get => _OutVoltage; set => SetProperty(ref _OutVoltage, value);
		}
		private string _Gen1Frequency;
		public string Gen1Frequency
		{
			get => _Gen1Frequency;
			set => SetProperty(ref _Gen1Frequency, value);
		}

		private string _Gen1Voltage;
		public string Gen1Voltage
		{
			get => _Gen1Voltage;
			set => SetProperty(ref _Gen1Voltage, value);
		}
		private string _Gen2Frequency;
		public string Gen2Frequency
		{
			get => _Gen2Frequency;
			set => SetProperty(ref _Gen2Frequency, value);
		}

		private string _Gen2Voltage;
		public string Gen2Voltage
		{
			get => _Gen2Voltage;
			set => SetProperty(ref _Gen2Voltage, value);
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
			set => 
				SetProperty(ref _GraphEndFreq, value);
		}

		private uint _StepsOctave;         // type of alert
		public uint StepsOctave
		{
			get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
		}

		private uint _Averages;
		public uint Averages
		{
			get => _Averages;
			set => SetProperty(ref _Averages, value);
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
		private double _Attenuation;
		public double Attenuation
		{
			get => _Attenuation;
			set => SetProperty(ref _Attenuation, value);
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

		private bool _ShowSummary = true;
		public bool ShowSummary
		{
			get => _ShowSummary;
			set => SetProperty(ref _ShowSummary, value);
		}

		private bool _ShowMarkers = false;
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
		private string _SampleRate = String.Empty;
		public string SampleRate
		{
			get => _SampleRate;
			set => SetProperty(ref _SampleRate, value);
		}
		private string _FftSize = String.Empty;
		public string FftSize
		{
			get => _FftSize;
			set => SetProperty(ref _FftSize, value);
		}
		private string _Windowing = String.Empty;
		public string WindowingMethod
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
		private bool _AttenCheck;
		public bool AttenCheck
		{
			get => _AttenCheck;
			set => SetProperty(ref _AttenCheck, value);
		}
		private bool _UseGenerator;
		public bool UseGenerator
		{
			get => _UseGenerator;
			set => SetProperty(ref _UseGenerator, value);
		}
		private bool _UseGenerator2;
		public bool UseGenerator2
		{
			get => _UseGenerator2;
			set => SetProperty(ref _UseGenerator2, value);
		}
		private bool _IsImdCustom;
		public bool IsImdCustom
		{
			get => _IsImdCustom;
			set => SetProperty(ref _IsImdCustom, value);
		}

		private string _IntermodType;
		public string IntermodType
		{
			get => _IntermodType;
			set => SetProperty(ref _IntermodType, value);
		}
		
		#endregion



		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Gen1Voltage":
					if(! IsImdCustom)
					{
						// synchronize voltage 2
						SetImType();
					}
					break;
				case "OutputUnits":
					actImd?.UpdateAmpOutputVoltageDisplay();
					break;
				case "GeneratorUnits":
					actImd?.UpdateGeneratorVoltageDisplay();
					break;
				case "Voltage":
				case "MeasureType":
				case "VoltageUnits":
					actImd?.UpdateGeneratorParameters();
					break;
				case "ShowPercent":
					ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
					ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
					actImd?.UpdateGraph(true);
					break;
				case "ShowSummary":
					ShowChannelInfo = ShowSummary;
					if (actInfo != null)
						actInfo.Visibility = ShowSummary ? Visibility.Visible : Visibility.Hidden;
					break;
				case "IntermodType":
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
				case "ShowThickLines":
				case "ShowMarkers":
				case "ShowPowerMarkers":
					actImd?.UpdateGraph(true);
					break;
				default:
					break;
			}
		}

		public void SetAction(PlotControl plot, ChannelInfo info)
		{
			ImdData data = new ImdData();
			actImd = new ActImd(ref data, plot);
			actInfo = info;
		}

		private static void SetAtten(object parameter)
		{
			var vm = ViewSettings.Singleton.ImdVm;
			var atten = MathUtil.ParseTextToDouble(parameter.ToString(), vm.Attenuation);
			vm.Attenuation = atten;
		}

		private static void StartIt(object parameter)
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.ImdVm;
			vm.actImd.StartMeasurement();
		}

		private static void StopIt(object parameter)
		{
			var vm = ViewModels.ViewSettings.Singleton.ImdVm;
			vm.actImd.DoCancel();
		}

		public string SerializeAll()
		{
			string jsonString = JsonSerializer.Serialize(this);
			//Console.WriteLine(jsonString);
			return jsonString;
		}

		private void ExecIm(int df1, int df2, int divisor)
		{
			var ax = IntermodType;
			this.Gen2Voltage = (Convert.ToDouble(this.Gen1Voltage) / divisor).ToString();
			this.Gen1Frequency = df1.ToString();
			this.Gen2Frequency = df2.ToString();
			IntermodType = ax;
		}

		public void SetImType()
		{
			var tt = IntermodType;
			if (tt == null)
				return;

			IsImdCustom = (tt == "Custom");
			// if custom, we're done
			if (IsImdCustom)
				return;

			if( tt.Contains("SMPTE "))
			{
				ExecIm(60, 7000, 4);
			}
			else if(tt.Contains("DIN "))
			{
				ExecIm(250, 8000, 4);
			}
			else if (tt.Contains("CCIF "))
			{
				ExecIm(19000, 20000, 1);
			}
			else if (tt.Contains("AES-17 MD"))
			{
				ExecIm(41, 7993, 4);
			}
			else if (tt.Contains("AES-17 DFD"))
			{
				ExecIm(18000, 20000, 1);
			}
			else if (tt.Contains("TDFD Phono"))
			{
				ExecIm(3005, 4462, 1);
			}
		}

		~ImdViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
		}

		public ImdViewModel()
		{
			PropertyChanged += CheckPropertyChanged;

			OutVoltage = 0.5;
			GraphStartFreq = "20";
			GraphEndFreq = "20000";
			StepsOctave = 1;
			Averages = 1;
			MeasureType = 2;
			OutputUnits = 0;
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowSummary = true;
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

			SampleRate = "96000";
			FftSize = "64K";
			WindowingMethod = "Hann";

			InputRange = 0;
			RangeTopdB = 20;
			RangeBottomdB = -180;

			ShowMarkers = false;
			ShowPowerMarkers = false;

			ToShowRange = Visibility.Visible;
			ToShowdB = Visibility.Visible;
			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { ShowPercent = false; MeasureType = 0; });

			ReadVoltage = true;
			ReadOutPower = true;
			ReadOutVoltage = true;
			AmpOutputAmplitude = -20;         // this is the unitless (dbV) amplitude of the amplifier output
			GeneratorUnits = (int)E_VoltageUnit.Volt;
			GeneratorAmplitude = -20;       // this is the unitless (dbV) amplitude of the generator
			Gen1Voltage = QaLibrary.ConvertVoltage(GeneratorAmplitude, E_VoltageUnit.dBV, (E_VoltageUnit)GeneratorUnits).ToString();
			Gen2Voltage = "0.1";
			Gen2Frequency = "2000";
			MeasureType = 0;
			Gen1Frequency = "1000";
			UseGenerator = false;
			UseGenerator2 = false;

			Attenuation = 42;
			AttenCheck = true;

			IntermodType = "Custom";
			IsImdCustom = true;
		}
	}
}
