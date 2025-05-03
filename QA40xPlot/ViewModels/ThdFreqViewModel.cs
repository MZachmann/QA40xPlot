using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System;
using Microsoft.Win32;

namespace QA40xPlot.ViewModels
{
	public class ThdFreqViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }

		private PlotControl actPlot { get; set; }
		private ActThdFrequency actThd { get; set; }

		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand<object> DoFitToData { get => new RelayCommand<object>(OnFitToData); }
		[JsonIgnore]
		public AsyncRelayCommand DoLoadTab { get => new AsyncRelayCommand(LoadItTab); }
		[JsonIgnore]
		public AsyncRelayCommand DoGetTab { get => new AsyncRelayCommand(GetItTab); }
		[JsonIgnore]
		public RelayCommand DoSaveTab { get => new RelayCommand(SaveItTab); }
		private static ThdFreqViewModel MyVModel { get => ViewSettings.Singleton.ThdFreq; }

		#region Setters and Getters
		[JsonIgnore]
		public string GraphUnit
		{
			get => GraphUtil.GetFormatSuffix(PlotFormat);
		}

		private string _GenVoltage = string.Empty;
		public string GenVoltage
		{
			get => _GenVoltage; set => SetProperty(ref _GenVoltage, value);
		}

		private string _StartFreq = string.Empty;
		public string StartFreq
		{
			get => _StartFreq;
			set => SetProperty(ref _StartFreq, value);
		}

		private string _EndFreq = string.Empty;
		public string EndFreq
		{
			get => _EndFreq;
			set => SetProperty(ref _EndFreq, value);
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
			set => SetProperty(ref _GraphEndFreq, value);
		}

		private uint _StepsOctave;
		public uint StepsOctave
		{
			get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
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

		#endregion

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = ViewSettings.Singleton.ThdFreq;
			vm.actThd?.DoMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewSettings.Singleton.ThdFreq;
			vm.actThd?.DoCancel();
		}

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "PlotFormat":
					// we may need to change the axis
					ToShowRange = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Collapsed : Visibility.Visible;
					ToShowdB = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Visible : Visibility.Collapsed;
					OnPropertyChanged("GraphUnit");
					actThd?.UpdateGraph(true);
					break;
				case "Voltage":
				case "OutPower":
				case "GenDirection":
				case "VoltageUnits":
					//actThd?.UpdateGeneratorParameters();
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

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2)
		{
			ThdFrequencyData data = new ThdFrequencyData();
			actThd = new ActThdFrequency(plot, plot1, plot2);
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

		private static async Task DoGetLoad(bool isLoad)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				FileName = string.Empty, // Default file name
				DefaultExt = ".zip", // Default file extension
				Filter = PlotFileFilter // Filter files by extension
			};

			// Show save file dialog box
			bool? result = openFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// open document
				string filename = openFileDialog.FileName;
				var vm = MyVModel;
				await vm.actThd.LoadFromFile(filename, isLoad);
			}
		}

		private static async Task LoadItTab()
		{
			await DoGetLoad(true);
		}

		private static async Task GetItTab()
		{
			await DoGetLoad(false);
		}


		private static string FileAddon()
		{
			DateTime now = DateTime.Now;
			string formattedDate = $"{now:yyyy-MM-dd_HH-mm-ss}";
			return formattedDate;
		}

		private static void SaveItTab()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaTFreq{0}", FileAddon()), // Default file name
				DefaultExt = ".plt", // Default file extension
				Filter = PlotFileFilter // Filter files by extension
			};

			// Show save file dialog box
			bool? result = saveFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = saveFileDialog.FileName;
				if (filename.Count() > 1)
				{
					var vm = MyVModel;
					vm.actThd.SaveToFile(filename);
				}
			}
		}

		private void OnFitToData(object? parameter)
		{
			var bounds = actThd.GetDataBounds();
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
			var thdFreqVm = ViewSettings.Singleton.ThdFreq;
			thdFreqVm.DoMouse(sender, e);
		}

		private void DoMouse(object sender, MouseEventArgs e)
		{
			SetMouseTrack(e);
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
			Name = "ThdFreq";
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			PropertyChanged += CheckPropertyChanged;

			this.actPlot = default!;
			this.actThd = default!;

			StartFreq = "20";
			EndFreq = "20000";
			GraphStartFreq = "20";
			GraphEndFreq = "20000";
			StepsOctave = 1;
			LeftChannel = true;
			RightChannel = false;
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowPoints = false;
			ShowPercent = false;

			ShowTHD = true;
			ShowMagnitude = true;
			ShowD2 = true;
			ShowD3 = true;
			ShowD4 = true;
			ShowD5 = true;
			ShowD6 = true;
			ShowNoiseFloor = true;

			RangeTopdB = "20";
			RangeBottomdB = "-180";

			GenVoltage = "0.10";

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;          
			// make a few things happen to synch the gui. don't await this.
			Task.Delay(1000).ContinueWith(t => { actThd?.UpdateGraph(true); });
		}
	}
}
