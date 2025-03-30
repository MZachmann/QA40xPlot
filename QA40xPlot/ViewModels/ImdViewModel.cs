using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Input;
using ScottPlot;
using ScottPlot.Plottables;
using CommunityToolkit.Mvvm.Input;

namespace QA40xPlot.ViewModels
{
	public class ImdViewModel : BaseViewModel
	{
		public static List<String> WindowingTypes { get => new List<string> { "Rectangle", "Hann", "FlatTop" }; }
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }
		public static List<String> IntermodTypes { get => new List<string> { "Custom", "SMPTE (60Hz.7KHz 4:1)", "DIN (250Hz.8KHz 4:1",
			"CCIF (19KHz.20KHz 1:1)", "AES-17 MD (41Hz.7993Hz 4:1)", "AES-17 DFD (18KHz.20KHz 1:1)",
			"TDFD Phono (3005Hz.4462Hz 1:1)" }; }
		private ActImd actImd { get;  set; }
		private PlotControl actPlot { get; set; }
		private ImdChannelInfo actInfo { get;  set; }
		[JsonIgnore]
		public RelayCommand<object> SetAttenuate { get => new RelayCommand<object>(SetAtten); }
		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand<object> DoFitToData { get => new RelayCommand<object>(OnFitToData); }

		#region Setters and Getters

		[JsonIgnore]
		public string AttenColor
		{
			get => DoAutoAttn ? "#1800f000" : "Transparent";
		}

		private bool _DoAutoAttn = false;
		public bool DoAutoAttn
		{
			get { return _DoAutoAttn; }
			set { if(SetProperty(ref _DoAutoAttn, value)) OnPropertyChanged("AttenColor"); }
		}

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
		private string _Gen1Frequency = string.Empty;
		public string Gen1Frequency
		{
			get => _Gen1Frequency;
			set => SetProperty(ref _Gen1Frequency, value);
		}

		private string _Gen1Voltage = string.Empty;
		public string Gen1Voltage
		{
			get => _Gen1Voltage;
			set => SetProperty(ref _Gen1Voltage, value);
		}

		private string _Gen2Frequency = string.Empty;
		public string Gen2Frequency
		{
			get => _Gen2Frequency;
			set => SetProperty(ref _Gen2Frequency, value);
		}

		private string _Gen2Voltage = string.Empty;
		public string Gen2Voltage
		{
			get => _Gen2Voltage;
			set => SetProperty(ref _Gen2Voltage, value);
		}
		private string _GraphStartFreq = string.Empty;
		public string GraphStartFreq
		{
			get => _GraphStartFreq;
			set => SetProperty(ref _GraphStartFreq, value);
		}

		private string _GraphEndFreq = string.Empty;
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

		private bool _ShowDataPercent;
		public bool ShowDataPercent
		{
			get => _ShowDataPercent;
			set => SetProperty(ref _ShowDataPercent, value);
		}

		private bool _ShowPercent;
		public bool ShowPercent
		{
			get => _ShowPercent;
			set => SetProperty(ref _ShowPercent, value);
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

		private string _IntermodType = string.Empty;
		public string IntermodType
		{
			get => _IntermodType;
			set => SetProperty(ref _IntermodType, value);
		}

		#endregion


		public DataBlob? GetFftData()
		{
			return actImd.CreateExportData();
		}

		[JsonIgnore]
		public double GenDivisor
		{   // this gets set whenever we set the imd type
			get => GetImDivisor();
		}


		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "GenDirection":
				case "Gen1Voltage":
					// synchronize voltage 2
					SetImType();
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

		public void SetAction(PlotControl plot, ImdChannelInfo info)
		{
			ImdData data = new ImdData();
			actImd = new ActImd(ref data, plot);
			actInfo = info;
			SetupMainPlot(plot);
			actPlot = plot;
		}

		private static void SetAtten(object? parameter)
		{
			var vm = ViewSettings.Singleton.ImdVm;
			var atten = MathUtil.ToDouble(parameter?.ToString() ?? string.Empty, vm.Attenuation);
			vm.Attenuation = atten;
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.ImdVm;
			vm?.actImd?.StartMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewModels.ViewSettings.Singleton.ImdVm;
			vm?.actImd?.DoCancel();
		}


		private void OnFitToData(object? parameter)
		{
			var bounds = actImd.GetDataBounds();
			switch (parameter)
			{
				case "XF":  // X frequency
					this.GraphStartFreq = bounds.Left.ToString("0");
					this.GraphEndFreq = bounds.Right.ToString("0");
					break;
				case "YP":  // Y percents
					var xp = bounds.Y + bounds.Height;  // max Y value
					var bot = ((100 * bounds.Y) / xp);  // bottom value in percent
					bot = Math.Pow(10, Math.Max(-7, Math.Floor(Math.Log10(bot))));  // nearest power of 10
					this.RangeTop = "100";  // always 100%
					this.RangeBottom = bot.ToString("0.##########");
					break;
				case "YM":  // Y magnitude
					this.RangeBottomdB = (20 * Math.Log10(Math.Max(1e-14, bounds.Y))).ToString("0");
					this.RangeTopdB = Math.Ceiling((20 * Math.Log10(Math.Max(1e-14, bounds.Height - bounds.Y)))).ToString("0");
					break;
				default:
					break;
			}
			actImd?.UpdateGraph(false);
		}

		private void ExecIm(int df1, int df2, int divisor)
		{
			var ax = IntermodType;
			var tt = ToDirection(GenDirection);
			if( tt == E_GeneratorDirection.OUTPUT_POWER)
			{
				Gen2Voltage = (MathUtil.ToDouble(this.Gen1Voltage) / (divisor*divisor)).ToString();
			}
			else
			{
				Gen2Voltage = (MathUtil.ToDouble(this.Gen1Voltage) / divisor).ToString();
			}
			Gen1Frequency = df1.ToString();
			Gen2Frequency = df2.ToString();
			IntermodType = ax;
		}

		public double GetImDivisor()
		{
			var tt = IntermodType;
			if (tt == null)
				return 1;

			IsImdCustom = (tt == "Custom");
			// if custom, we're done
			if (IsImdCustom)
			{
				return 1;
			}

			if (tt.Contains("SMPTE "))
			{
				return 4;
			}
			else if (tt.Contains("DIN "))
			{
				return 4;
			}
			else if (tt.Contains("CCIF "))
			{
				return 1;
			}
			else if (tt.Contains("AES-17 MD"))
			{
				return 4;
			}
			else if (tt.Contains("AES-17 DFD"))
			{
				return 1;
			}
			else if (tt.Contains("TDFD Phono"))
			{
				return 1;
			}
			return 1;
		}

		public void SetImType()
		{
			var tt = IntermodType;
			if (tt == null)
				return;

			IsImdCustom = (tt == "Custom");
			// if custom, we're done
			if (IsImdCustom)
			{
				return;
			}

			if ( tt.Contains("SMPTE "))
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

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var imdVm = ViewSettings.Singleton.ImdVm;
			imdVm.DoMouse(sender, e);
		}

		private static Marker? MyMark = null;
		private void DoMouse(object sender, MouseEventArgs e)
		{
			SetMouseTrack(e);

			if (!IsTracking || actImd == null || actPlot == null)
				return;

			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			var xpos = cord.Item1;
			var ypos = cord.Item2;
			FreqValue = Math.Pow(10, xpos); // frequency

			var zv = actImd.LookupXY(FreqValue, ypos, ShowRight && !ShowLeft);
			var valdBV = 20 * Math.Log10(zv.Item2);
			// - this may be too slow, but for now....
			if (MyMark != null)
			{
				actPlot.ThePlot.Remove(MyMark);
				MyMark = null;
			}
			var valshow = ShowPercent ? Math.Log10(zv.Item3) : valdBV;
			MyMark = actPlot.ThePlot.Add.Marker(Math.Log10(zv.Item1), valshow,
				MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), ScottPlot.Colors.Red);
			actPlot.Refresh();

			FreqShow = zv.Item1.ToString("0.# Hz");
			var valvolt = MathUtil.FormatVoltage(zv.Item2);
			var valpercent = MathUtil.FormatPercent(zv.Item3);
			ZValue = $"{valdBV:0.#} dBV" + Environment.NewLine +
				$"{valpercent} %" + Environment.NewLine +
				$"{valvolt}";
		}

		~ImdViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
			 
		}

		public ImdViewModel()
		{
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			// eliminate warnings
			this.actPlot = default!;
			this.actInfo = default!;
			this.actImd = default!;

			OutVoltage = 0.5;
			GraphStartFreq = "20";
			GraphEndFreq = "20000";
			StepsOctave = 1;
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowSummary = true;
			ShowPercent = false;
			ShowDataPercent = true;
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

			WindowingMethod = "Hann";

			InputRange = 0;
			RangeTopdB = "20";
			RangeBottomdB = "-180";

			ShowMarkers = false;
			ShowPowerMarkers = false;

			Gen1Voltage = "0.1";
			Gen2Voltage = "0.1";
			Gen2Frequency = "20000";
			Gen1Frequency = "19000";
			UseGenerator = false;
			UseGenerator2 = false;

			Attenuation = 42;

			IntermodType = "Custom";
			IsImdCustom = true;

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;

			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { actImd?.UpdateGraph(true); });
		}
	}
}
