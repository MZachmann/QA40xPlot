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
	public class ThdFreqViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }

		private ActThdFrequency MyAction { get => actThd; }
		private PlotControl actPlot { get; set; }
		private ActThdFrequency actThd { get; set; }

		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }
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
		private static ThdFreqViewModel MyVModel { get => new(); }

		#region Setters and Getters

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
		private bool _ShowTHDN;
		public bool ShowTHDN
		{
			get => _ShowTHDN;
			set => SetProperty(ref _ShowTHDN, value);
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
			//var vm = ViewSettings.Singleton.ThdFreq;
			//vm.actThd?.DoMeasurement();
		}

		private static void StopIt()
		{
			//var vm = ViewSettings.Singleton.ThdFreq;
			//vm.actThd?.DoCancel();
		}

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
				case "GraphStartX":
				case "GraphEndX":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
					MyAction?.UpdateGraph(true);
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
					break;
			}
		}

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2, TabAbout tAbout)
		{
			actThd = new ActThdFrequency(plot, plot1, plot2);
			SetupMainPlot(plot);
			actPlot = plot;
			actAbout = tAbout;
			MyVModel.LinkAbout(actThd.PageData.Definition);
			ShowInfos();
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

		// this always uses the 'global' format so others work too
		private static string FormatValue(double d1, double dMax)
		{
			var vm = MyVModel;
			var x = GraphUtil.ValueToPlot(vm.PlotFormat, d1, dMax);
			return GraphUtil.PrettyPrint(x, vm.PlotFormat);
		}

		private static string FormatCursor(ThdColumn column)
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

		private static async Task LoadItTab()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, true);
		}

		private static async Task GetItTab()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, false);
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
				if (filename.Length > 1)
				{
					MyVModel.actThd.SaveToFile(filename);
				}
			}
		}

		private void OnViewToFit(object? parameter)
		{
			var pram = parameter as string;
			if (pram == null)
				return;
			MyAction.PinGraphRange(pram);
		}

		private void OnFitToData(object? parameter)
		{
			var frslt = MyAction.PageData.FreqRslt;
			MyAction?.ActFitToData(this, parameter, ShowLeft ? frslt?.Left : frslt?.Right);
			MyAction?.UpdateGraph(true);
		}

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			//var thdFreqVm = ViewSettings.Singleton.ThdFreq;
			//thdFreqVm.DoMouse(sender, e);
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
			ZValue = string.Empty;
			var zv = MyAction.LookupX(FreqValue);
			if (zv.Length > 0)
			{
				FreqShow = MathUtil.FormatLogger(zv[0].Freq);
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

		~ThdFreqViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public ThdFreqViewModel()
		{
			_Name = "ThdFreq";
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			this.actPlot = default!;
			this.actThd = default!;

			LeftWidth = 95;  // reset the width of the left column
			RightWidth = 50; // reset the width of the right column

			StartFreq = "20";
			EndFreq = "20000";
			GraphStartX = "20";
			GraphEndX = "20000";
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

			ToShowWindowing = Visibility.Collapsed;
			// make a few things happen to synch the gui. don't await this.
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
