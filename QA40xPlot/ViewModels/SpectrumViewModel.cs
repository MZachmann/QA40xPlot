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
	public class SpectrumViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }

		private static SpectrumViewModel MyVModel { get => ViewSettings.Singleton.SpectrumVm; }
		private PlotControl actPlot {  get; set; }
		private ActSpectrum actSpec { get;  set; }
		private ThdChannelInfo actInfoLeft { get;  set; }
		private ThdChannelInfo actInfoRight { get; set; }
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
			set { if ( SetProperty(ref _DoAutoAttn, value)) OnPropertyChanged("AttenColor"); }
		}

		private string _Gen1Waveform = string.Empty;
		public string Gen1Waveform
		{
			get => _Gen1Waveform;
			set => SetProperty(ref _Gen1Waveform, value);
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
		#endregion

		private void ShowInfos()
		{
			if (actInfoLeft != null)
				actInfoLeft.Visibility = (ShowSummary && ShowLeft) ? Visibility.Visible : Visibility.Hidden;
			if (actInfoRight != null)
				actInfoRight.Visibility = (ShowSummary && ShowRight) ? Visibility.Visible : Visibility.Hidden;
		}

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "PlotFormat":
					// we may need to change the axis
					actSpec?.UpdateGraph(true);
					break;
				case "ShowPercent":
					ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
					ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
					actSpec?.UpdateGraph(true);
					break;
				case "ShowSummary":
					ShowInfos();
					break;
				case "ShowRight":
				case "ShowLeft":
					ShowInfos();
					actSpec?.UpdateGraph(false);
					break;
				case "GraphStartFreq":
				case "GraphEndFreq":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
					actSpec?.UpdateGraph(true);
					break;
				case "ShowThickLines":
				case "ShowMarkers":
				case "ShowPowerMarkers":
					actSpec?.UpdateGraph(false);
					break;
				default:
					break;
			}
		}

		public DataBlob? GetFftData()
		{
			return actSpec?.CreateExportData();
		}

		public void SetAction(PlotControl plot, ThdChannelInfo info, ThdChannelInfo info2)
		{
			SpectrumData data = new SpectrumData();
			actSpec = new ActSpectrum(ref data, plot);
			actInfoLeft = info;
			actInfoRight = info2;
			info.SetDataContext(ViewSettings.Singleton.ChannelLeft);
			info2.SetDataContext(ViewSettings.Singleton.ChannelRight);
			SetupMainPlot(plot);
			actPlot = plot;
			ShowInfos();
		}

		private static void SetAtten(object? parameter)
		{
			var vm = MyVModel;
			var atten = MathUtil.ToDouble(parameter?.ToString() ?? string.Empty, vm.Attenuation);
			vm.Attenuation = atten;
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = MyVModel;
			vm.actSpec?.StartMeasurement();
		}

		private static void StopIt()
		{
			var vm = MyVModel;
			vm.actSpec?.DoCancel();
		}

		private void OnFitToData(object? parameter)
		{
			var bounds = actSpec.GetDataBounds();
			switch (parameter)
			{
				case "XF":  // X frequency
					this.GraphStartFreq = bounds.Left.ToString("0");
					this.GraphEndFreq = bounds.Right.ToString("0");
					break;
				case "YP":  // Y percents
					var xp = bounds.Y + bounds.Height;  // max Y value
					var bot = ((100 * bounds.Y) / xp);  // bottom value in percent
					bot = Math.Pow(10, Math.Max(-7, Math.Floor(Math.Log10(bot))));	// nearest power of 10
					this.RangeTop = "100";	// always 100%
					this.RangeBottom = bot.ToString("0.##########");
					break;
				case "YM":  // Y magnitude
					this.RangeBottomdB = (20*Math.Log10(Math.Max(1e-14,bounds.Y))).ToString("0");
					this.RangeTopdB = Math.Ceiling((20 * Math.Log10(Math.Max(1e-14, bounds.Height + bounds.Y)))).ToString("0");
					break;
				default:
					break;
			}
			actSpec?.UpdateGraph(false);
		}

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object? sender, MouseEventArgs e)
		{
			var specVm = MyVModel;
			specVm.DoMouse(sender, e);
		}

		private static Marker? MyMark = null;
		private void DoMouse(object? sender, MouseEventArgs e)
		{ 
			SetMouseTrack(e);
			// it's too laggy while it's running....
			if (IsRunning || !IsTracking)
				return;

			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			var xpos = cord.Item1;
			var ypos = cord.Item2;
			FreqValue = Math.Pow(10, xpos);

			var zv = actSpec.LookupXY(FreqValue, ypos, ShowRight && !ShowLeft);
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

		~SpectrumViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public SpectrumViewModel()
		{
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			this.actPlot = default!;
			this.actInfoLeft = default!;
			this.actInfoRight = default!;
			this.actSpec = default!;

			GraphStartFreq = "20";
			GraphEndFreq = "20000";
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowSummary = true;
			ShowPercent = false;
			ShowDataPercent = true;
			ShowLeft = true;
			ShowRight = false;

			WindowingMethod = "Hann";

			InputRange = 0;
			RangeTopdB = "20";
			RangeBottomdB = "-180";

			ShowMarkers = false;
			ShowPowerMarkers = false;

			Gen1Waveform = "Sine";
			Gen1Voltage = "0.1";
			Gen1Frequency = "1000";
			UseGenerator = false;

			Attenuation = 42;

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;

			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { actSpec?.UpdateGraph(true); });
		}
	}
}
