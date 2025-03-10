﻿using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;

namespace QA40xPlot.ViewModels
{
	public class SpectrumViewModel : BaseViewModel
	{
		public static List<String> WindowingTypes { get => new List<string> { "Rectangle", "Hann", "FlatTop" }; }
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> MeasureTypes { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }
		public static List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }
		public static List<String> GenAmplitudes { get => new List<string> { "0.05", "0.1", "0.25", "0.5", "0.75", "1", "2", "5" }; }

		private ActSpectrum actSpec { get;  set; }
		private ThdChannelInfo actInfo { get;  set; }
		[JsonIgnore]
		public RelayCommand SetAttenuate { get => new RelayCommand(SetAtten); }
		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }
		[JsonIgnore]
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
		#endregion



		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "OutputUnits":
					actSpec?.UpdateAmpOutputVoltageDisplay();
					break;
				case "GeneratorUnits":
					actSpec?.UpdateGeneratorVoltageDisplay();
					break;
				case "Voltage":
				case "MeasureType":
				case "VoltageUnits":
					actSpec?.UpdateGeneratorParameters();
					break;
				case "ShowPercent":
					ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
					ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
					actSpec?.UpdateGraph(true);
					break;
				case "ShowSummary":
					ShowChannelInfo = ShowSummary;
					if (actInfo != null)
						actInfo.Visibility = ShowSummary ? Visibility.Visible : Visibility.Hidden;
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
				case "ShowThickLines":
				case "ShowMarkers":
				case "ShowPowerMarkers":
					actSpec?.UpdateGraph(true);
					break;
				default:
					break;
			}
		}

		public DataBlob? GetFftData()
		{
			return actSpec.CreateExportData();
		}

		public void SetAction(PlotControl plot, ThdChannelInfo info)
		{
			SpectrumData data = new SpectrumData();
			actSpec = new ActSpectrum(ref data, plot);
			actInfo = info;
		}

		private static void SetAtten(object parameter)
		{
			var vm = ViewSettings.Singleton.SpectrumVm;
			var atten = MathUtil.ParseTextToDouble(parameter.ToString(), vm.Attenuation);
			vm.Attenuation = atten;
		}

		private static void StartIt(object parameter)
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.SpectrumVm;
			vm.actSpec.StartMeasurement();
		}

		private static void StopIt(object parameter)
		{
			var vm = ViewModels.ViewSettings.Singleton.SpectrumVm;
			vm.actSpec.DoCancel();
		}

		~SpectrumViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
		}

		public SpectrumViewModel()
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
			MeasureType = 0;
			Gen1Frequency = "1000";
			UseGenerator = false;
			UseGenerator2 = false;

			Attenuation = 42;
		}
	}
}
