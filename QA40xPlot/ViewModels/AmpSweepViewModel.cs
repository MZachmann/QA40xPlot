using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.Views;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public class AmpSweepViewModel : OpampViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> StartVoltages { get => new List<string> { "0.0001", "0.0002", "0.0005", "0.001", "0.002", "0.005", "0.01", "0.02", "0.05", "0.1", "0.2", "0.5" }; }
		public static List<String> EndVoltages { get => new List<string> { "1", "2", "5", "10", "20", "50", "100", "200" }; }
		/// <summary>
		/// the static model that applies to the GUI at least...
		/// </summary>
		private static AmpSweepViewModel MyVModel { get => ViewSettings.Singleton.AmpVm; }
		[JsonIgnore]
		public override List<string> AxisList { get; } = new List<string> { "XM", "YM", "YP" };

		private ActAmpSweep MyAction { get => actSweep; }
		private ActAmpSweep actSweep { get; set; }

		[JsonIgnore]
		public override RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public override RelayCommand DoStop { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand<object> DoViewToFit { get => new RelayCommand<object>(OnViewToFit); }
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

		private uint _StepsOctave;         // type of alert
		public uint StepsOctave
		{
			get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
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

		#endregion

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "DsPinAll":
					MyAction.PinAll(MainPlot.ThePlot, this);
					break;
				case "DsFitAll":
					MyAction.FitAll(MainPlot.ThePlot, this);
					break;
				case "DsSnapshot":
					MyAction.AddSnapshotPlot();
					break;
				case "DsPlotColors":
					MyAction?.UpdateGraph(false);
					break;
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
				case "XAxisType":
					MyAction?.UpdateGraph(true);
					break;
				case "GraphStartX":
				case "GraphEndX":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
					MyAction?.UpdateGraph(false, e.PropertyName);
					break;
				case "ShowRight":
				case "ShowLeft":
				case "ShowTabInfo":
					ShowInfos();
					MyAction?.UpdateGraph(false);
					break;
				case "ShowTHDN":
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
					OpampPropertyChanged(sender, e);
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

		private void StartIt()
		{
			HasQA430 = QA430Model.BeginQA430Op();
			// Implement the logic to start the measurement process
			actSweep?.DoMeasurement();
		}

		private void StopIt()
		{
			actSweep?.DoCancel();
		}

		private void ShowInfos()
		{
			if (actAbout != null)
			{
				actAbout.Visibility = ShowTabInfo ? Visibility.Visible : Visibility.Hidden;
				var fvm = actAbout.DataContext as FloorViewModel;
				if (fvm != null)
					fvm.ThemeBkgd = ViewSettings.Singleton.MainVm.ThemeBkgd; ;
			}
		}

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2, TabAbout tAbout)
		{
			LinkPlots(plot, plot1, plot2);
			actSweep = new ActAmpSweep(this);
			SetupMainPlot(plot);
			actAbout = tAbout;
			MyVModel.LinkAbout(actSweep.PageData.Definition);
			ShowInfos();

		}

		private static async Task LoadItTab()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, true);
		}

		private static async Task GetItTab()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, false);
		}

		private void SaveItTab()
		{
			var fname = GetSavePltName("QaTAmp");
			// Process save file dialog box results
			if (fname.Length > 0)
			{
					MyAction.SaveToFile(fname);
			}
		}

		private void OnViewToFit(object? parameter)
		{
			var pram = parameter as string;
			if (pram == null)
				return;
			MyAction.PinGraphRange(pram);
		}

		public override void OnFitToData(object? parameter)
		{
			if (MyAction == null)
				return;
			var frslt = MyAction.PageData.FreqRslt;
			MyAction.ActFitToData(this, parameter, ShowLeft ? frslt?.Left : frslt?.Right);
			MyAction.UpdateGraph(false, PlotUtil.AxisParameter(parameter));
		}

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var thdAmpVm = ViewSettings.Singleton.AmpVm;
			thdAmpVm.DoMouse(sender, e);
		}

		// this always uses the 'global' format so others work too
		private static string FormatValue(double d1, double dMax)
		{
			var vm = MyVModel;
			var x = GraphUtil.ValueToPlot(vm.PlotFormat, d1, dMax);
			return GraphUtil.PrettyPrint(x, vm.PlotFormat);
		}

		private static string FormatCursor(SweepColumn column)
		{
			var vm = MyVModel;
			string sout = "Mag: " + FormatValue(column.Mag, column.Mag) + Environment.NewLine;
			if (vm.ShowTHDN)
				sout += "THD+N: " + FormatValue(column.THDN, column.Mag) + Environment.NewLine;
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
				var p = e.GetPosition(MainPlot);
				var cord = ConvertScottCoords(MainPlot, p.X, p.Y);
				FreqValue = Math.Pow(10, cord.Item1); // amplitude actually
			}

			ZValue = string.Empty;
			var zv = MyAction.LookupX(FreqValue);
			if (zv.Length > 0)
			{
				FreqShow = MathUtil.FormatLogger(zv[0].Column.GenVolts);
				foreach (var item in zv)
				{
					if (item != null)
					{
						ZValue += FormatCursor(item.Column);
						ZValue += "------------" + Environment.NewLine;
					}
				}
			}
			else
			{
				FreqShow = "";
			}
		}

		// the old saved amp sweeps are just not compatible at all
		public override bool IsValidLoadModel(string? name, int version)
		{
			return base.IsValidLoadModel(name, version) && (version > 1);
		}

		~AmpSweepViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public AmpSweepViewModel()
		{
			_Name = "AmpSweep";
			_Version = 2;
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			LeftWidth = 90;  // reset the width of the left column
			RightWidth = 50; // reset the width of the right column

			actSweep = default!;

			TestFreq = "1000";
			GraphStartX = "0.002";
			GraphEndX = "10";
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
			Task.Delay(1000).ContinueWith(_ =>
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					MyAction?.UpdateGraph(true);
				});
			});
		}
	}
}

