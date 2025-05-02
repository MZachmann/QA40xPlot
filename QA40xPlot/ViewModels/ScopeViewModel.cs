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
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace QA40xPlot.ViewModels
{
	public class ScopeViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }
		public static List<String> AbsoluteValues { get => new List<string> { "5", "2", "1", "0.5", "0.1", "0.05", "-0.05", "-0.1", "-0.5", "-1", "-2", "-5" }; }
		public static List<String> TimeSteps { get => new List<string> { "0",".1",".5","1","5", "10", "20", "50", "100", "200", "500", "1000", "5000", "10000" }; }

		private static ScopeViewModel MyVModel { get => ViewSettings.Singleton.ScopeVm; }
		private PlotControl actPlot {  get; set; }
		private ActScope actScope { get;  set; }
		private ThdChannelInfo actInfo { get;  set; }
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
		[JsonIgnore]
		public AsyncRelayCommand DoLoadTab { get => new AsyncRelayCommand(LoadItTab); }
		[JsonIgnore]
		public AsyncRelayCommand DoGetTab { get => new AsyncRelayCommand(GetItTab); }
		[JsonIgnore]
		public RelayCommand DoSaveTab { get => new RelayCommand(SaveItTab); }

		#region Setters and Getters

		[JsonIgnore]
		public string AttenColor
		{
			get => DoAutoAttn ? "#1800f000" : "Transparent";
		}

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

		private bool _DoAutoAttn = false;
		public bool DoAutoAttn
		{
			get { return _DoAutoAttn; }
			set { if ( SetProperty(ref _DoAutoAttn, value)) OnPropertyChanged("AttenColor"); }
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
		private string _GraphStartTime = string.Empty;
		public string GraphStartTime
		{
			get => _GraphStartTime;
			set => SetProperty(ref _GraphStartTime, value);
		}

		private string _GraphEndTime = string.Empty;
		public string GraphEndTime
		{
			get => _GraphEndTime;
			set => SetProperty(ref _GraphEndTime, value);
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

		private bool _ShowThickLines;
		public bool ShowThickLines
		{
			get => _ShowThickLines;
			set => SetProperty(ref _ShowThickLines, value);
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
		private bool _UseGenerator1;
		public bool UseGenerator1
		{
			get => _UseGenerator1;
			set => SetProperty(ref _UseGenerator1, value);
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
				case "ShowOtherLeft":
				case "ShowOtherRight":
					actScope?.UpdateGraph(false);
					break;
				case "ShowTabInfo":
					if (actAbout != null)
						actAbout.Visibility = ShowTabInfo ? Visibility.Visible : Visibility.Hidden;
					break;
				case "ShowSummary":
					//if (actInfo != null)
					//	actInfo.Visibility = ShowSummary ? Visibility.Visible : Visibility.Hidden;
					break;
				case "GraphStartTime":
				case "GraphEndTime":
				case "RangeBottom":
				case "RangeTop":
					actScope?.UpdateGraph(true);
					break;
				case "ShowRight":
				case "ShowLeft":
				case "ShowThickLines":
				case "ShowMarkers":
				case "ShowPowerMarkers":
				case "ShowPoints":
					actScope?.UpdateGraph(false);
					break;
				default:
					break;
			}
		}

		public DataBlob? GetFftData()
		{
			return actScope?.CreateExportData();
		}

		public void SetAction(PlotControl plot, TabAbout tinfo)
		{
			actScope = new ActScope(plot);
			actAbout = tinfo;
			//actInfo = info;
			//info.SetDataContext(ViewSettings.Singleton.ScopeChanLeft);
			SetupMainPlot(plot);
			actPlot = plot;
			MyVModel.LinkAbout(actScope.PageData.Definition);
		}

		private static void SetAtten(object? parameter)
		{
			var vm = ViewSettings.Singleton.ScopeVm;
			var atten = MathUtil.ToDouble(parameter?.ToString() ?? string.Empty, vm.Attenuation);
			vm.Attenuation = atten;
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = ViewSettings.Singleton.ScopeVm;
			vm.actScope?.DoMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewSettings.Singleton.ScopeVm;
			vm.actScope?.DoCancel();
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
				await vm.actScope.LoadFromFile(filename, isLoad);
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
				FileName = String.Format("QaScope{0}", FileAddon()), // Default file name
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
					vm.actScope.SaveToFile(filename);
				}
			}
		}

		private void OnFitToData(object? parameter)
		{
			var bounds = actScope.GetDataBounds();
			switch (parameter)
			{
				case "XF":  // X time
					this.GraphStartTime = bounds.Left.ToString("0.###");
					this.GraphEndTime = bounds.Right.ToString("0.###");
					break;
				case "YM":  // Y magnitude
					this.RangeBottom = (bounds.Y).ToString("0.###");
					this.RangeTop = (bounds.Height + bounds.Y).ToString("0.###");
					break;
				default:
					break;
			}
			actScope?.UpdateGraph(false);
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
			if (IsRunning || !IsTracking)
				return;

			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			var xpos = cord.Item1;
			var ypos = cord.Item2;
			FreqValue = xpos;

			var zv = actScope.LookupXY(xpos, ypos, ShowRight && !ShowLeft);
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
			Name = "Scope";
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			this.actPlot = default!;
			this.actInfo = default!;
			this.actScope = default!;

			GraphStartTime = "0";
			GraphEndTime = "10";
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "-1";

			ShowThickLines = true;
			ShowLeft = true;
			ShowRight = false;

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

			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { actScope?.UpdateGraph(true); });
		}
	}
}
