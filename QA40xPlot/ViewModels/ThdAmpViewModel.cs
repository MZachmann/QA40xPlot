using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace QA40xPlot.ViewModels
{
	public class ThdAmpViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> StartVoltages { get => new List<string> { "0.0001", "0.0002", "0.0005", "0.001", "0.002", "0.005", "0.01", "0.02", "0.05", "0.1", "0.2", "0.5" }; }
		public static List<String> EndVoltages { get => new List<string> { "1", "2", "5", "10", "20", "50", "100", "200" }; }

		private ActThdAmplitude actThd { get; set; }
		private PlotControl actPlot {  get; set; }

		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand<object> DoFitToData { get => new RelayCommand<object>(OnFitToData); }

		#region Setters and Getters
		private string _StartVoltage = string.Empty;         // type of alert
		public string StartVoltage
		{
			get => _StartVoltage; set => SetProperty(ref _StartVoltage, value);
		}

		private string _EndVoltage = string.Empty;         // type of alert
		public string EndVoltage
		{
			get => _EndVoltage; set => SetProperty(ref _EndVoltage, value);
		}

		private string _StartPower = string.Empty;         // type of alert
		public string StartPower
		{
			get => _StartPower; set => SetProperty(ref _StartPower, value);
		}

		private string _EndPower = string.Empty;         // type of alert
		public string EndPower
		{
			get => _EndPower; set => SetProperty(ref _EndPower, value);
		}

		private string _TestFreq = string.Empty;         // type of alert
		public string TestFreq
		{
			get => _TestFreq;
			set => SetProperty(ref _TestFreq, value);
		}

		private string _GraphStartVolts = string.Empty;         // type of alert
		public string GraphStartVolts
		{
			get => _GraphStartVolts;
			set => SetProperty(ref _GraphStartVolts, value);
		}

		private string _GraphEndVolts = string.Empty;         // type of alert
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
		private string _Windowing = string.Empty;
		public string WindowingMethod
		{
			get => _Windowing;
			set => SetProperty(ref _Windowing, value);
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
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "GenDirection":
					//UpdateGeneratorParameters();
					actThd?.UpdateGraph(true);
					break;
				case "StartVoltage":
					//actThd?.UpdateStartVoltageDisplay();
					break;
				case "EndVoltage":
					//actThd?.UpdateEndVoltageDisplay();
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
					actThd?.UpdateGraph(true);
					break;
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
					actThd?.UpdateGraph(false);
					break;
				default:
					break;
			}
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = ViewSettings.Singleton.ThdAmp;
			vm.actThd?.StartMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewSettings.Singleton.ThdAmp;
			vm.actThd?.DoCancel();
		}

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2)
		{
			ThdAmplitudeData data = new ThdAmplitudeData();
			actThd = new ActThdAmplitude(ref data, plot, plot1, plot2);
			SetupMainPlot(plot);
			actPlot = plot;
		}

		private void OnFitToData(object? parameter)
		{
			var bounds = actThd.GetDataBounds();
			switch (parameter)
			{
				case "XM":  // X magnitude
							// calculate the bounds here
					this.GraphStartVolts = bounds.Left.ToString("0.##");
					this.GraphEndVolts = bounds.Right.ToString("0.##");
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
					this.RangeTopdB = Math.Ceiling((20 * Math.Log10(Math.Max(1e-14, bounds.Height + bounds.Y)))).ToString("0");
					break;
				default:
					break;
			}
			actThd?.UpdateGraph(false);
		}


		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var thdAmpVm = ViewSettings.Singleton.ThdAmp;
			thdAmpVm.DoMouse(sender, e);
		}

		private string FormatValue(double value)
		{
			if( ! ShowPercent)
				return MathUtil.FormatLogger(value) + " dB";
			return MathUtil.FormatPercent(Math.Pow(10,value/20 + 2)) + " %";
		}

		private string FormatColumn( ThdColumn column )
		{
			var vm = ViewSettings.Singleton.ThdAmp;
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

			if( vm.ShowTHD)
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

		private void DoMouse(object sender, MouseEventArgs e)
		{
			SetMouseTrack(e);
			if (IsTracking)
			{
				var p = e.GetPosition(actPlot);
				var cord = ConvertScottCoords(actPlot, p.X, p.Y);
				FreqValue = Math.Pow(10, cord.Item1); // amplitude actually
			}
			var zv = actThd.LookupX(FreqValue);
			ZValue = string.Empty;
			if( zv.Item1 != null )
			{
				FreqShow = MathUtil.FormatVoltage(zv.Item1.GenVolts) + " -> " + MathUtil.FormatVoltage(zv.Item1.Mag);
				if (zv.Item2 != null)
					ZValue += "Left: " + Environment.NewLine;
				ZValue += FormatColumn( zv.Item1 );
			}
			if (zv.Item2 != null)
			{
				if (zv.Item1 == null)
					FreqShow = MathUtil.FormatVoltage(zv.Item2.GenVolts) + " -> " + MathUtil.FormatVoltage(zv.Item2.Mag);
				else
					ZValue += "Right: " + Environment.NewLine;
				ZValue += FormatColumn(zv.Item2);
			}
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

			actPlot = default!;
			actThd = default!;

			TestFreq = "1000";
			GraphStartVolts = "0.002";
			GraphEndVolts = "10";
			StepsOctave = 1;

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

			WindowingMethod = "Hann";
			RangeTopdB = "20";
			RangeBottomdB = "-180";

			StartVoltage = "0.1";
			EndVoltage = "1";
			StartPower = "0.5";
			EndPower = "5";

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;          
			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { actThd?.UpdateGraph(true); });
		}
	}
}

