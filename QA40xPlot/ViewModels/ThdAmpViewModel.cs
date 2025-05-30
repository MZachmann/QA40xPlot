using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public class ThdAmpViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> StartVoltages { get => new List<string> { "0.0001", "0.0002", "0.0005", "0.001", "0.002", "0.005", "0.01", "0.02", "0.05", "0.1", "0.2", "0.5" }; }
		public static List<String> EndVoltages { get => new List<string> { "1", "2", "5", "10", "20", "50", "100", "200" }; }
		private static ThdAmpViewModel MyVModel { get => ViewSettings.Singleton.ThdAmp; }

		private ActThdAmplitude MyAction { get => actThd; }
		private ActThdAmplitude actThd { get; set; }
		private PlotControl actPlot {  get; set; }

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
		#endregion

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "UpdateGraph":
					MyAction?.UpdateGraph(true);
					break;
				case "DsHeading":
				case "DsRepaint":
					MyAction?.UpdateGraph(true);
					break;
				case "DsName":
					MyAction?.UpdatePlotTitle();
					break;
				case "PlotFormat":
					// we may need to change the axis
					ToShowRange = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Collapsed : Visibility.Visible;
					ToShowdB = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Visible : Visibility.Collapsed;
					RaisePropertyChanged("GraphUnit");
					MyAction?.UpdateGraph(true);
					break;
				case "GenDirection":
					MyAction?.UpdateGraph(true);
					break;
				case "XAxisType":
				case "GraphStartVolts":
				case "GraphEndVolts":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
					MyAction?.UpdateGraph(true);
					break;
				case "ShowOtherLeft":
				case "ShowOtherRight":
				case "ShowRight":
				case "ShowLeft":
				case "ShowTabInfo":
					ShowInfos();
					MyAction?.UpdateGraph(false);
					break;
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
					MyAction?.UpdateGraph(false);
					break;
				default:
					break;
			}
		}

		// here param is the id of the tab to remove from the othertab list
		public override void DoDeleteIt(string param)
		{
			var id = MathUtil.ToInt(param, -1);
			var fat = OtherSetList.FirstOrDefault(x => x.Id == id);
			if (fat != null)
			{
				OtherSetList.Remove(fat);
				MyAction.DeleteTab(id);
			}
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = ViewSettings.Singleton.ThdAmp;
			vm.actThd?.DoMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewSettings.Singleton.ThdAmp;
			vm.actThd?.DoCancel();
		}

		private void ShowInfos()
		{
			if (actAbout != null)
				actAbout.Visibility = ShowTabInfo ? Visibility.Visible : Visibility.Hidden;
		}

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2, TabAbout tAbout)
		{
			actThd = new ActThdAmplitude(plot, plot1, plot2);
			SetupMainPlot(plot);
			actPlot = plot;
			actAbout = tAbout;
			MyVModel.LinkAbout(actThd.PageData.Definition);
			ShowInfos();

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
				await MyVModel.actThd.LoadFromFile(filename, isLoad);
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

		private void SaveItTab()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaTAmp{0}", FileAddon()), // Default file name
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
					MyAction.SaveToFile(filename);
				}
			}
		}

		private void OnFitToData(object? parameter)
		{
			var bounds = MyAction.GetDataBounds();
			switch (parameter)
			{
				case "XM":  // X magnitude
					// calculate the bounds here. X is provided in input or output volts/power
					this.GraphStartVolts = bounds.Left.ToString("G2");
					this.GraphEndVolts = (bounds.Left + bounds.Right).ToString("G2");
					break;
				case "YP":  // Y percents
					{
						var xp = bounds.Y + bounds.Height;  // max Y value
						var bot = GraphUtil.ReformatLogValue(PlotFormat, bounds.Y, xp);
						bot = Math.Pow(10, Math.Max(-7, Math.Floor(bot)));  // nearest power of 10
						var top = Math.Floor(GraphUtil.ReformatLogValue(PlotFormat, xp, xp));
						top = Math.Pow(10, Math.Min(3, top));
						this.RangeTop = top.ToString("0.##########");
						this.RangeBottom = bot.ToString("0.##########");
					}
					break;
				case "YM":  // Y magnitude
					{
						var bot = GraphUtil.ReformatLogValue(PlotFormat, bounds.Y, bounds.Y + bounds.Height);
						this.RangeBottomdB = bot.ToString("0");
						var top = GraphUtil.ReformatLogValue(PlotFormat, bounds.Y + bounds.Height, bounds.Y + bounds.Height);
						this.RangeTopdB = Math.Ceiling(top).ToString("0");
					}
					break;
				default:
					break;
			}
			MyAction?.UpdateGraph(true);
		}


		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var thdAmpVm = ViewSettings.Singleton.ThdAmp;
			thdAmpVm.DoMouse(sender, e);
		}

		// this always uses the 'global' format so others work too
		private static string FormatValue(double d1, double dMax)
		{
			var vm = MyVModel;
			var x = GraphUtil.ReformatValue(vm.PlotFormat, d1, dMax);
			return GraphUtil.PrettyPrint(x, vm.PlotFormat);
		}


		private static string FormatCursor(ThdColumn column)
		{
			var vm = MyVModel;
			string sout = "Mag: " + FormatValue(column.Mag, column.Mag) + Environment.NewLine;
			if (vm.ShowTHD)
				sout += "THD: " + FormatValue(column.THD, column.Mag) + Environment.NewLine;
			if (vm.ShowNoiseFloor)
				sout += "Noise: " + FormatValue(column.Noise, column.Mag) + Environment.NewLine;
			if (vm.ShowD2)
				sout += "D2: " + FormatValue(column.D2, column.Mag) + Environment.NewLine;
			if (vm.ShowD3)
				sout += "D3: " + FormatValue(column.D3, column.Mag) + Environment.NewLine;
			if (vm.ShowD4)
				sout += "D4: " + FormatValue(column.D4, column.Mag) + Environment.NewLine;
			if (vm.ShowD5)
				sout += "D5: " + FormatValue(column.D5, column.Mag) + Environment.NewLine;
			if (vm.ShowD6)
				sout += "D6+: " + FormatValue(column.D6P, column.Mag) + Environment.NewLine;
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

			ZValue = string.Empty;
			var zv = MyAction.LookupX(FreqValue);
			if (zv.Length > 0)
			{
				FreqShow = MathUtil.FormatLogger(zv[0].GenVolts) + "->" + MathUtil.FormatLogger(zv[0].Mag);
				foreach (var item in zv)
				{
					if (item != null)
					{
						ZValue += FormatCursor(item);
						ZValue += "------------" + Environment.NewLine;
					}
				}
			}
			else
			{
				FreqShow = "";
			}
		}

		~ThdAmpViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public ThdAmpViewModel()
		{
			Name = "ThdAmp";
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
			Task.Delay(1000).ContinueWith(t => { MyAction?.UpdateGraph(true); });
		}
	}
}

