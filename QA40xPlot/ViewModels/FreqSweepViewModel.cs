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
	public class FreqSweepViewModel : OpampViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }

		private ActFreqSweep MyAction { get => actFreq; }
		private PlotControl actPlot { get; set; }
		private ActFreqSweep actFreq { get; set; }

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

		private static FreqSweepViewModel MyVModel { get => ViewSettings.Singleton.FreqVm; }

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

		#endregion

		private void StartIt()
		{
			// Implement the logic to start the measurement process
			HasQA430 = QA430Model.BeginQA430Op();
			actFreq?.DoMeasurement();
		}

		private void StopIt()
		{
			actFreq?.DoCancel();
		}

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "DSPlotColors":
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
				case "ShowPhase":
				case "ShowNoise":
				case "ShowNoiseFloor":
				case "ShowPoints":
				case "ShowD2":
				case "ShowD3":
				case "ShowD4":
				case "ShowD5":
				case "ShowD6":
				case "ShowThickLines":
					MyAction?.UpdateGraph(false);
					break;
				default:
					OpampPropertyChanged(sender, e);
					break;
			}
		}

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2, TabAbout tAbout)
		{
			actFreq = new ActFreqSweep(plot, plot1, plot2);
			SetupMainPlot(plot);
			actPlot = plot;
			actAbout = tAbout;
			MyVModel.LinkAbout(actFreq.PageData.Definition);
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

		private static string FormatCursor(SweepDot dot)
		{
			var vm = MyVModel;
			var column = dot.Column;

			string sout = "--" + dot.Label + Environment.NewLine;
			if (vm.ShowMagnitude)
				sout += "Mag: " + FormatValue(column.Mag, column.Mag) + Environment.NewLine;
			if (vm.ShowPhase)
				sout += "Phase: " + FormatValue(column.Phase, column.Mag) + Environment.NewLine;
			if (vm.ShowTHDN)
				sout += "THD+N: " + FormatValue(column.THDN, column.Mag) + Environment.NewLine;
			if (vm.ShowTHD)
				sout += "THD: " + FormatValue(column.THD, column.Mag) + Environment.NewLine;
			if (vm.ShowNoise)
				sout += "Noise: " + FormatValue(column.Noise, column.Mag) + Environment.NewLine;
			if (vm.ShowNoiseFloor)
				sout += "Floor: " + FormatValue(column.NoiseFloor, column.Mag) + Environment.NewLine;
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

		private void SaveItTab()
		{
			var fname = GetSavePltName("QaFswp");

			// Process save file dialog box results
			if (fname.Length > 0)
			{
				actFreq.SaveToFile(fname);
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
			var thdFreqVm = ViewSettings.Singleton.FreqVm;
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
			ZValue = string.Empty;
			var zv = MyAction.LookupX(FreqValue);
			if (zv.Length > 0)
			{
				FreqShow = MathUtil.FormatLogger(zv[0].Column.Freq);
				if (!ShowMagnitude)
					ZValue += "Mag: " + FormatValue(zv[0].Column.Mag, zv[0].Column.Mag) + Environment.NewLine;
				foreach (var item in zv)
				{
					if (item != null)
					{
						ZValue += FormatCursor(item);
					}
				}
			}
			else
			{
				FreqShow = "";
			}
		}

		~FreqSweepViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public FreqSweepViewModel()
		{
			_Name = "FreqSweep";
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			this.actPlot = default!;
			this.actFreq = default!;

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
			ShowPhase = false;
			ShowNoise = false;
			ShowTHDN = false;

			RangeTopdB = "20";
			RangeBottomdB = "-180";

			GenVoltage = "0.10";

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
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
