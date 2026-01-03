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
using System.Windows.Controls;
using System.Windows.Input;


namespace QA40xPlot.ViewModels
{
	public class SpectrumViewModel : BaseViewModel, ICloneable
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }

		private ActSpectrum MyAction { get => actSpec; }
		private static SpectrumViewModel MyVModel { get => ViewSettings.Singleton.SpectrumVm; }
		private PlotControl actPlot { get; set; }
		private ActSpectrum actSpec { get; set; }
		private ThdChannelInfo actInfoLeft { get; set; }
		private ThdChannelInfo actInfoRight { get; set; }
		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }
		[JsonIgnore]
		public RelayCommand<object> DoFitToData { get => new RelayCommand<object>(OnFitToData); }
		[JsonIgnore]
		public RelayCommand<object> DoViewToFit { get => new RelayCommand<object>(OnViewToFit); }
		[JsonIgnore]
		public AsyncRelayCommand DoLoadTab { get => new AsyncRelayCommand(LoadIt); }
		[JsonIgnore]
		public AsyncRelayCommand DoGetTab { get => new AsyncRelayCommand(GetIt); }
		[JsonIgnore]
		public RelayCommand DoSaveTab { get => new RelayCommand(SaveIt); }
		[JsonIgnore]
		public RelayCommand DoWaveSelect { get => new RelayCommand(WaveSelect); }

		#region Setters and Getters

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

		private int _InputRange;
		public int InputRange
		{
			get => _InputRange;
			set => SetProperty(ref _InputRange, value);
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
				case "PlotFormat":
					// we may need to change the axis
					ToShowRange = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Collapsed : Visibility.Visible;
					ToShowdB = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Visible : Visibility.Collapsed;
					RaisePropertyChanged("GraphUnit");
					MyAction?.UpdateGraph(true);
					break;
				case "ShowTabInfo":
				case "ShowSummary":
					ShowInfos();
					break;
				case "ShowDataPercent":
					MyAction?.UpdateGraph(false);
					break;
				case "ShowRight":
				case "ShowLeft":
					ShowInfos();
					MyAction?.UpdateGraph(false);
					break;
				case "GraphStartX":
				case "GraphEndX":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
					MyAction?.UpdateGraph(true);
					break;
				case "ShowThickLines":
				case "ShowMarkers":
				case "ShowPowerMarkers":
					MyAction?.UpdateGraph(false);
					break;
				default:
					break;
			}
		}

		public DataBlob? GetFftData()
		{
			return MyAction?.CreateExportData();
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

		public void SetAction(UserControl myWnd, PlotControl plot, ThdChannelInfo info, ThdChannelInfo info2, TabAbout about)
		{
			actSpec = new ActSpectrum(plot);
			actInfoLeft = info;
			actInfoRight = info2;
			info.SetDataContext(ViewSettings.Singleton.ChannelLeft);
			info2.SetDataContext(ViewSettings.Singleton.ChannelRight);
			about.SetDataContext(ViewSettings.Singleton.TabDefs);
			MyVModel.actAbout = about;
			SetupMainPlot(plot);
			MyVModel.LinkAbout(MyAction.PageData.Definition);
			actPlot = plot;
			ShowInfos();
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = MyVModel;
			vm.actSpec?.DoMeasurement();
		}

		private static void StopIt()
		{
			var vm = MyVModel;
			vm.actSpec?.DoCancel();
		}

		private static async Task LoadIt()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, true);
		}

		private static async Task GetIt()
		{
			await DoGetLoad(MyVModel.MyAction, PlotFileFilter, false);
		}

		private void WaveSelect()
		{
			var vu = WaveGenerator.FindWaveFileName(GenWavFile);
			if (vu != null && vu.Length > 0)
			{
				GenWavFile = vu;
			}
		}

		private static void SaveIt()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaSpectrum{0}", FileAddon()), // Default file name
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
					var vm = MyVModel;
					vm.actSpec.SaveToFile(filename);
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

			var zv = MyAction.LookupXY(FreqValue, ypos, ShowRight && !ShowLeft);
			var valdBV = GraphUtil.ValueToPlot(this, zv.Item2, zv.Item3);
			if (!GraphUtil.IsPlotFormatLog(PlotFormat))
			{
				valdBV = Math.Log10(valdBV);
			}
			// - this may be too slow, but for now....
			if (MyMark != null)
			{
				actPlot.ThePlot.Remove(MyMark);
				MyMark = null;
			}
			MyMark = actPlot.ThePlot.Add.Marker(Math.Log10(zv.Item1), valdBV,
				MarkerShape.FilledDiamond, GraphUtil.PtToPixels(6), ScottPlot.Colors.Red);
			actPlot.Refresh();

			FreqShow = zv.Item1.ToString("0.# Hz");
			var valvolt = MathUtil.FormatVoltage(zv.Item2);
			var valunit = GraphUtil.PrettyPlotValue(this, zv.Item2, zv.Item3);
			var valpercent = GraphUtil.PrettyPlotValue("dBV", zv.Item2);
			ZValue = $"{valunit}" + Environment.NewLine +
				$"{valpercent}" + Environment.NewLine +
				$"{valvolt}";
		}

		~SpectrumViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public SpectrumViewModel()
		{
			_Name = "Spectrum";
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			this.actPlot = default!;
			this.actInfoLeft = default!;
			this.actInfoRight = default!;
			this.actSpec = default!;

			LeftWidth = 80;  // reset the width of the left column
			RightWidth = 50; // reset the width of the right column

			GraphStartX = "20";
			GraphEndX = "20000";
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowDataPercent = true;

			InputRange = 0;
			RangeTopdB = "20";
			RangeBottomdB = "-180";

			ShowMarkers = false;
			ShowPowerMarkers = false;

			Gen1Waveform = "Sine";
			Gen1Voltage = "0.1";
			Gen1Frequency = "1000";
			UseGenerator = true;

			Attenuation = 42;

			ToShowRange = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Collapsed : Visibility.Visible;
			ToShowdB = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Visible : Visibility.Collapsed;

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
