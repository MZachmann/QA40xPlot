using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public class FreqSweepViewModel :OpampViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }

		private ActFreqSweep MyAction { get => actFreq; }
		private PlotControl actPlot { get; set; }
		private ActFreqSweep actFreq { get; set; }

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
		[JsonIgnore]
		public RelayCommand DoUpdateLoad { get => new RelayCommand(UpdateLoad); }
		[JsonIgnore]
		public RelayCommand DoUpdateGain { get => new RelayCommand(UpdateGain); }
		private static FreqSweepViewModel MyVModel { get => ViewSettings.Singleton.FreqVm; }

		#region Setters and Getters
		private bool _VaryLoad = false;
		public bool VaryLoad
		{
			get => _VaryLoad;
			set { SetProperty(ref _VaryLoad, value); RaisePropertyChanged("LoadSummary"); }
		}

		private bool _VaryGain = false;
		public bool VaryGain
		{
			get => _VaryGain;
			set { SetProperty(ref _VaryGain, value); RaisePropertyChanged("GainSummary"); }
}

		private bool _VarySupply = false;
		public bool VarySupply
		{
			get => _VarySupply;
			set => SetProperty(ref _VarySupply, value);
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
		private bool _ShowPhase;
		public bool ShowPhase
		{
			get => _ShowPhase;
			set => SetProperty(ref _ShowPhase, value);
		}
		private bool _ShowNoise;
		public bool ShowNoise
		{
			get => _ShowNoise;
			set => SetProperty(ref _ShowNoise, value);
		}

		#endregion

		private static void StartIt()
		{
			var nowHave = QA430Model.BeginQA430Op();
			if (!nowHave)
			{
				MessageBox.Show("QA-430 device not connected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			// Implement the logic to start the measurement process
			var vm = ViewSettings.Singleton.FreqVm;
			vm.actFreq?.DoMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewSettings.Singleton.FreqVm;
			vm.actFreq?.DoCancel();
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
			var x = GraphUtil.ReformatValue(vm.PlotFormat, d1, dMax);
			return GraphUtil.PrettyPrint(x, vm.PlotFormat);
		}

		private static string FormatCursor(FreqSweepDot dot)
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
				await vm.actFreq.LoadFromFile(filename, isLoad);
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

		public static void UpdateGain()
		{
			MyVModel.RaisePropertyChanged("GainSummary");
		}

		public static void UpdateLoad()
		{
			MyVModel.RaisePropertyChanged("LoadSummary");
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
				FileName = String.Format("QaOpamp{0}", FileAddon()), // Default file name
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
					MyVModel.actFreq.SaveToFile(filename);
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
			var bounds = MyAction.GetDataBounds();
			switch (parameter)
			{
				case "XF":  // X frequency
					this.GraphStartX = bounds.Left.ToString("0");
					this.GraphEndX = bounds.Right.ToString("0");
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
			if(zv.Length > 0)
			{
				FreqShow = MathUtil.FormatLogger(zv[0].Column.Freq);
				if(! ShowMagnitude)
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
			Name = "FreqSweep";
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
			Task.Delay(1000).ContinueWith(t => { MyAction?.UpdateGraph(true); });
		}
	}
}
