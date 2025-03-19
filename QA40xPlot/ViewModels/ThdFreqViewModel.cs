using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using static FreqRespViewModel;
using System.DirectoryServices.ActiveDirectory;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public class ThdFreqViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> MeasureTypes { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }

		private PlotControl actPlot { get; set; }
		private ActThdFrequency actThd { get; set; }

		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }

		#region Setters and Getters
		private string _GenVoltage;
		public string GenVoltage
		{
			get => _GenVoltage; set => SetProperty(ref _GenVoltage, value);
		}

		private double _AmpLoad;
		public double AmpLoad
		{
			get => _AmpLoad; set => SetProperty(ref _AmpLoad, value);
		}

		private string _OutPower;
		public string OutPower
		{
			get => _OutPower; set => SetProperty(ref _OutPower, value);
		}

		private string _OutVoltage;
		public string OutVoltage
		{
			get => _OutVoltage; set => SetProperty(ref _OutVoltage, value);
		}

		private string _StartFreq;
		public string StartFreq
		{
			get => _StartFreq;
			set => SetProperty(ref _StartFreq, value);
		}

		private string _EndFreq;
		public string EndFreq
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
			SetupMainPlot(plot);
			actPlot = plot;
		}


		private string FormatValue(double value)
		{
			if (!ShowPercent)
				return MathUtil.FormatLogger(value) + " dBV";
			return MathUtil.FormatPercent(Math.Pow(10, value / 20 + 2)) + " %";
		}

		private string FormatCursor(ThdColumn column)
		{
			var vm = ViewSettings.Singleton.ThdFreq;
			string sout = "Mag: ";
			if (ShowPercent)
			{
				var MagValue = Math.Pow(10, column.Mag / 20);
				sout += MathUtil.FormatVoltage(MagValue);
			}
			else
			{
				sout += MathUtil.FormatLogger(column.Mag) + " dBV";
			}
			sout += Environment.NewLine;

			if (vm.ShowTHD)
				sout += "THD: " + FormatValue(column.THD) + Environment.NewLine;
			if (vm.ShowNoiseFloor)
				sout += "Noise: " + FormatValue(column.Noise) + Environment.NewLine;
			if (vm.ShowD2)
				sout += "D2: " + FormatValue(column.D2) + Environment.NewLine;
			if (vm.ShowD3)
				sout += "D3: " + FormatValue(column.D3) + Environment.NewLine;
			if (vm.ShowD4)
				sout += "D4: " + FormatValue(column.D4) + Environment.NewLine;
			if (vm.ShowD5)
				sout += "D5: " + FormatValue(column.D5) + Environment.NewLine;
			if (vm.ShowD6)
				sout += "D6+: " + FormatValue(column.D6P) + Environment.NewLine;
			return sout;
		}


		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var thdFreqVm = ViewSettings.Singleton.ThdFreq;
			thdFreqVm.DoMouse(sender, e);
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
			ZValue = string.Empty;
			if (zv.Item1 != null)
			{
				FreqShow = MathUtil.FormatLogger(zv.Item1.Freq);
				if (zv.Item2 != null)
					ZValue += "Left: " + Environment.NewLine;
				ZValue += FormatCursor(zv.Item1);
			}
			if (zv.Item2 != null)
			{
				if (zv.Item1 == null)
					FreqShow = MathUtil.FormatLogger(zv.Item2.Freq);
				else
					ZValue += "Right: " + Environment.NewLine;
				ZValue += FormatCursor(zv.Item2);
			}
		}

		~ThdFreqViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;

		}

		public ThdFreqViewModel()
		{
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			PropertyChanged += CheckPropertyChanged;

			AmpLoad = 8;
			OutPower = "0.5";
			OutVoltage = "0.5";
			StartFreq = "20";
			EndFreq = "20000";
			GraphStartFreq = "20";
			GraphEndFreq = "20000";
			StepsOctave = 1;
			Averages = 1;
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
			FftSize = 65536;
			WindowingMethod = "Hann";

			InputRange = 0;
			RangeTopdB = 20;
			RangeBottomdB = -180;

			ReadVoltage = true;
			ReadOutPower = true;
			ReadOutVoltage = true;

			GenVoltage = "0.10";

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;          
			// make a few things happen to synch the gui. don't await this.
			Task.Delay(1000).ContinueWith(t => { actThd?.UpdateGraph(true); });
		}
	}
}
