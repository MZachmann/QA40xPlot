using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using ScottPlot;
using ScottPlot.Plottables;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public class ScopeViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }
		public static List<String> TimeSteps { get => new List<string> { "0", ".1", ".5", "1", "5", "10", "20", "50", "100", "200", "500", "1000", "5000", "10000" }; }
		[JsonIgnore]
		public override List<string> AxisList { get; } = new List<string> { "XT", "YP" };

		private ActScope MyAction { get => actScope; }
		private static ScopeViewModel MyVModel { get => ViewSettings.Singleton.ScopeVm; }
		private PlotControl actPlot { get; set; }
		private ActScope actScope { get; set; }
		private ScopeInfo actInfoLeft { get; set; }
		private ScopeInfo actInfoRight { get; set; }
		[JsonIgnore]
		public override RelayCommand DoRun { get => new RelayCommand(RunIt); }
		[JsonIgnore]
		public override RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public override RelayCommand DoStop { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }
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
		[JsonIgnore]
		public RelayCommand DoWaveSelect { get => new RelayCommand(WaveSelect); }

		#region Setters and Getters

		private string _Gen1Waveform = string.Empty;
		public string Gen1Waveform
		{
			get => _Gen1Waveform;
			set => SetProperty(ref _Gen1Waveform, value);
		}

		private string _Gen2Waveform = string.Empty;
		public string Gen2Waveform
		{
			get => _Gen2Waveform;
			set => SetProperty(ref _Gen2Waveform, value);
		}

		private bool _ShowPoints = false;
		public bool ShowPoints
		{
			get => _ShowPoints;
			set => SetProperty(ref _ShowPoints, value);
		}

		private string _Gen1Frequency = string.Empty;
		public string Gen1Frequency
		{
			get => _Gen1Frequency;
			set => SetProperty(ref _Gen1Frequency, value);
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

		private string _GenWavFile = string.Empty;
		public string GenWavFile
		{
			get => _GenWavFile;
			set => SetProperty(ref _GenWavFile, value);
		}

		private bool _ShowMarkers = false;
		public bool ShowMarkers
		{
			get => _ShowMarkers;
			set => SetProperty(ref _ShowMarkers, value);
		}

		private int _InputRange;
		public int InputRange
		{
			get => _InputRange;
			set => SetProperty(ref _InputRange, value);
		}

		private bool _UseGenerator2;
		public bool UseGenerator2
		{
			get => _UseGenerator2;
			set => SetProperty(ref _UseGenerator2, value);
		}
		#endregion

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "DSPlotColors":
					MyAction?.DrawPlotLines(0);
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
				case "ShowResiduals":
					ChangeResidualScale();
					break;
				case "ShowTabInfo":
				case "ShowSummary":
					ShowInfos();
					break;
				case "ShowRight":
				case "ShowLeft":
					ShowInfos();
					MyAction?.UpdateGraph(false);
					break;
				case "PlotFormat":
					// we may need to change the axis
					//ToShowRange = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Collapsed : Visibility.Visible;
					//ToShowdB = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Visible : Visibility.Collapsed;
					RaisePropertyChanged("GraphUnit");
					MyAction?.UpdateGraph(true);
					break;
				case "GraphStartX":
				case "GraphEndX":
				case "RangeBottom":
				case "RangeTop":
					MyAction?.UpdateGraph(false, e.PropertyName);
					break;
				case "ShowThickLines":
				case "ShowMarkers":
				case "ShowPoints":
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

		public DataBlob? GetFftData()
		{
			return MyAction?.CreateExportData();
		}

		public void SetAction(PlotControl plot, ScopeInfo info, ScopeInfo info2, TabAbout tinfo)
		{
			actScope = new ActScope(plot);
			actAbout = tinfo;
			actInfoLeft = info;
			actInfoRight = info2;
			info.SetDataContext(ViewSettings.Singleton.ScopeInfoLeft);
			info2.SetDataContext(ViewSettings.Singleton.ScopeInfoRight);
			SetupMainPlot(plot);
			actPlot = plot;
			MyVModel.LinkAbout(MyAction.PageData.Definition);
			ShowInfos();

		}

		private void RunIt()
		{
			// Implement the logic to run repeatedly
			actScope?.DoMeasurement(true);
		}

		private void StartIt()
		{
			// Implement the logic to run the measurement process once
			actScope?.DoMeasurement(false);
		}

		private void StopIt()
		{
			actScope?.DoCancel();
		}

		private static async Task LoadItTab()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, true);
		}

		private static async Task GetItTab()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, false);
		}

		private void ChangeResidualScale()
		{
			MyAction?.DoShowResiduals(ShowResiduals);
		}

		private void WaveSelect()
		{
			var vu = WaveGenerator.FindWaveFileName(GenWavFile);
			if (vu != null && vu.Length > 0)
			{
				GenWavFile = vu;
			}
		}

		private void SaveItTab()
		{
			var fname = GetSavePltName("QaScope");
			// Process save file dialog box results
			if (fname.Length > 0)
			{
				actScope.SaveToFile(fname);
			}
		}

		private void ShowInfos()
		{
			uint frames = 0;
			if (ShowLeft)
				frames++;
			if (ShowRight)
				frames++;
			var seen = (OtherSetList == null) ? 0 : OtherSetList.Count(x => x.IsOnR) + OtherSetList.Count(x => x.IsOnL);
			frames += (uint)seen;
			if (actInfoLeft != null)
				actInfoLeft.Visibility = (ShowSummary && frames > 0) ? Visibility.Visible : Visibility.Hidden;
			if (actInfoRight != null)
				actInfoRight.Visibility = (ShowSummary && frames > 1) ? Visibility.Visible : Visibility.Hidden;
			if (actAbout != null)
			{
				actAbout.Visibility = ShowTabInfo ? Visibility.Visible : Visibility.Hidden;
				var fvm = actAbout.DataContext as FloorViewModel;
				if (fvm != null)
					fvm.ThemeBkgd = ViewSettings.Singleton.MainVm.ThemeBkgd; ;
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
			var bounds = MyAction.GetDataBounds();
			switch (parameter)
			{
				case "XT":  // X time
					this.GraphStartX = bounds.Left.ToString("0.###");
					this.GraphEndX = bounds.Right.ToString("0.###");
					break;
				case "YP":  // Y magnitude
					if(Math.Abs(bounds.Height) > 1e-2)
					{
						this.RangeBottom = (bounds.Y).ToString("0.###");
						this.RangeTop = (bounds.Height + bounds.Y).ToString("0.###");
					}
					else
					{
						this.RangeBottom = (bounds.Y).ToString("0.#####");
						this.RangeTop = (bounds.Height + bounds.Y).ToString("0.#####");
					}
					break;
				default:
					break;
			}
			MyAction?.UpdateGraph(false, PlotUtil.AxisParameter(parameter));
		}

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object? sender, MouseEventArgs e)
		{
			var specVm = ViewSettings.Singleton.ScopeVm;
			specVm.DoMouse(sender, e);
		}

		private static Marker? MyMark = null;
		private void DoMouse(object? sender, MouseEventArgs e)
		{
			SetMouseTrack(e);
			// it's too laggy while it's running....
			if (MainVm.IsRunning || !IsTracking)
				return;

			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			var xpos = cord.Item1;
			var ypos = cord.Item2;
			FreqValue = xpos;
			LookX = xpos;
			LookY = ypos;
			UpdateMouseCursor(xpos, ypos);
		}

		public override void UpdateMouseCursor(double xpos, double ypos)
		{
			if (xpos <= 1)
				return;
			var zv = MyAction.LookupXY(xpos, ypos, ShowRight && !ShowLeft);
			// - this may be too slow, but for now....
			if (MyMark != null)
			{
				actPlot.ThePlot.Remove(MyMark);
				MyMark = null;
			}
			MyMark = actPlot.ThePlot.Add.Marker(zv.Item1, zv.Item2,
				MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), ScottPlot.Colors.Red);
			actPlot.Refresh();

			FreqShow = zv.Item1.ToString("0.### mS");
			var valvolt = MathUtil.FormatVoltage(zv.Item2);
			ZValue = $"{valvolt}";
		}

		~ScopeViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public ScopeViewModel()
		{
			_Name = "Scope";
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			actPlot = default!;
			actInfoLeft = default!;
			actInfoRight = default!;
			actScope = default!;

			LeftWidth = 80;  // reset the width of the left column
			RightWidth = 50; // reset the width of the right column

			GraphStartX = "0";
			GraphEndX = "10";
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "-1";

			ShowThickLines = true;
			InputRange = 0;

			ShowMarkers = false;

			Gen1Voltage = "0.1";
			Gen2Voltage = "0.2";
			Gen1Frequency = "1000";
			Gen2Frequency = "2000";
			Gen1Waveform = "Sine";
			Gen2Waveform = "Sine";
			UseGenerator1 = true;
			UseGenerator2 = false;

			Attenuation = 42;
			ToShowAverages = Visibility.Collapsed;

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
