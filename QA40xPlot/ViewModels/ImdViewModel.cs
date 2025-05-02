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
using Microsoft.Win32;

namespace QA40xPlot.ViewModels
{
	public class ImdViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> GenFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "2000", "5000", "10000" }; }
		public static List<String> IntermodTypes { get => new List<string> { "Custom", "SMPTE (60Hz.7KHz 4:1)", "DIN (250Hz.8KHz 4:1",
			"CCIF (19KHz.20KHz 1:1)", "AES-17 MD (41Hz.7993Hz 4:1)", "AES-17 DFD (18KHz.20KHz 1:1)",
			"TDFD Phono (3005Hz.4462Hz 1:1)" }; }
		private ActImd actImd { get;  set; }
		private PlotControl actPlot { get; set; }
		private ImdChannelInfo actInfoLeft { get;  set; }
		private ImdChannelInfo actInfoRight { get; set; }
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
		public RelayCommand DoLoad { get => new RelayCommand(LoadIt); }
		[JsonIgnore]
		public RelayCommand DoSave { get => new RelayCommand(SaveIt); }

		private static ImdViewModel MyVModel { get => ViewSettings.Singleton.ImdVm; }


		#region Setters and Getters

		[JsonIgnore]
		public string AttenColor
		{
			get => DoAutoAttn ? "#1800f000" : "Transparent";
		}
		[JsonIgnore]
		public string GraphUnit
		{
			get => GraphUtil.GetFormatSuffix(PlotFormat);
		}

		private bool _DoAutoAttn = false;
		public bool DoAutoAttn
		{
			get { return _DoAutoAttn; }
			set { if(SetProperty(ref _DoAutoAttn, value)) OnPropertyChanged("AttenColor"); }
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
			set => 
				SetProperty(ref _GraphEndFreq, value);
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
		private bool _UseGenerator2;
		public bool UseGenerator2
		{
			get => _UseGenerator2;
			set => SetProperty(ref _UseGenerator2, value);
		}
		private bool _IsImdCustom;
		public bool IsImdCustom
		{
			get => _IsImdCustom;
			set => SetProperty(ref _IsImdCustom, value);
		}

		private string _IntermodType = string.Empty;
		public string IntermodType
		{
			get => _IntermodType;
			set => SetProperty(ref _IntermodType, value);
		}

		#endregion


		public DataBlob? GetFftData()
		{
			return actImd.CreateExportData();
		}

		[JsonIgnore]
		public double GenDivisor
		{   // this gets set whenever we set the imd type
			get => GetImDivisor();
		}

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "GenDirection":
				case "Gen1Voltage":
					// synchronize voltage 2
					SetImType();
					break;
				case "PlotFormat":
					// we may need to change the axis
					ToShowRange = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Collapsed : Visibility.Visible;
					ToShowdB = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Visible : Visibility.Collapsed; 
					OnPropertyChanged("GraphUnit");
					actImd?.UpdateGraph(true);
					break;
				case "ShowTabInfo":
				case "ShowSummary":
					ShowInfos();
					break;
				case "ShowRight":
				case "ShowLeft":
					ShowInfos();
					actImd?.UpdateGraph(true);
					break;
				case "IntermodType":
				case "GraphStartFreq":
				case "GraphEndFreq":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
					actImd?.UpdateGraph(true);
					break;
				case "ShowThickLines":
				case "ShowMarkers":
				case "ShowPowerMarkers":
					actImd?.UpdateGraph(false);
					break;
				default:
					break;
			}
		}

		public void SetAction(PlotControl plot, ImdChannelInfo info, ImdChannelInfo info2, TabAbout tinfo)
		{
			actImd = new ActImd(plot);
			actInfoLeft = info;
			actInfoRight = info2;
			actAbout = tinfo;
			SetupMainPlot(plot);
			actPlot = plot;
		}

		private static void SetAtten(object? parameter)
		{
			var vm = ViewSettings.Singleton.ImdVm;
			var atten = MathUtil.ToDouble(parameter?.ToString() ?? string.Empty, vm.Attenuation);
			vm.Attenuation = atten;
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = ViewSettings.Singleton.ImdVm;
			vm?.actImd?.DoMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewSettings.Singleton.ImdVm;
			vm?.actImd?.DoCancel();
		}

		private static void LoadIt()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				FileName = string.Empty, // Default file name
				DefaultExt = ".plt", // Default file extension
				Filter = "Plot files|*.plt|All files|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = openFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// open document
				string filename = openFileDialog.FileName;
				var vm = MyVModel;
				vm.actImd.LoadFromFile(filename).Wait();
			}
		}

		private static string FileAddon()
		{
			DateTime now = DateTime.Now;
			string formattedDate = $"{now:yyyy-MM-dd_HH-mm-ss}";
			return formattedDate;
		}

		private static void SaveIt()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaImd{0}", FileAddon()), // Default file name
				DefaultExt = ".plt", // Default file extension
				Filter = "Plot files|*.plt|All files|*.*" // Filter files by extension
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
					vm.actImd.SaveToFile(filename);
				}
			}
		}

		private void OnFitToData(object? parameter)
		{
			var bounds = actImd.GetDataBounds();
			switch (parameter)
			{
				case "XF":  // X frequency
					this.GraphStartFreq = bounds.Left.ToString("0");
					this.GraphEndFreq = bounds.Right.ToString("0");
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
			actImd?.UpdateGraph(false);
		}

		private void ShowInfos()
		{
			if (actInfoLeft != null)
				actInfoLeft.Visibility = (ShowSummary && ShowLeft) ? Visibility.Visible : Visibility.Hidden;
			if (actInfoRight != null)
				actInfoRight.Visibility = (ShowSummary && ShowRight) ? Visibility.Visible : Visibility.Hidden;
			if (actAbout != null)
				actAbout.Visibility = ShowTabInfo ? Visibility.Visible : Visibility.Hidden;
		}

		private void ExecIm(int df1, int df2, int divisor)
		{
			var ax = IntermodType;
			var tt = ToDirection(GenDirection);
			if( tt == E_GeneratorDirection.OUTPUT_POWER)
			{
				Gen2Voltage = (MathUtil.ToDouble(this.Gen1Voltage) / (divisor*divisor)).ToString();
			}
			else
			{
				Gen2Voltage = (MathUtil.ToDouble(this.Gen1Voltage) / divisor).ToString();
			}
			Gen1Frequency = df1.ToString();
			Gen2Frequency = df2.ToString();
			IntermodType = ax;
		}

		public double GetImDivisor()
		{
			var tt = IntermodType;
			if (tt == null)
				return 1;

			IsImdCustom = (tt == "Custom");
			// if custom, we're done
			if (IsImdCustom)
			{
				return 1;
			}

			if (tt.Contains("SMPTE "))
			{
				return 4;
			}
			else if (tt.Contains("DIN "))
			{
				return 4;
			}
			else if (tt.Contains("CCIF "))
			{
				return 1;
			}
			else if (tt.Contains("AES-17 MD"))
			{
				return 4;
			}
			else if (tt.Contains("AES-17 DFD"))
			{
				return 1;
			}
			else if (tt.Contains("TDFD Phono"))
			{
				return 1;
			}
			return 1;
		}

		public void SetImType()
		{
			var tt = IntermodType;
			if (tt == null)
				return;

			IsImdCustom = (tt == "Custom");
			// if custom, we're done
			if (IsImdCustom)
			{
				return;
			}

			if ( tt.Contains("SMPTE "))
			{
				ExecIm(60, 7000, 4);
			}
			else if(tt.Contains("DIN "))
			{
				ExecIm(250, 8000, 4);
			}
			else if (tt.Contains("CCIF "))
			{
				ExecIm(19000, 20000, 1);
			}
			else if (tt.Contains("AES-17 MD"))
			{
				ExecIm(41, 7993, 4);
			}
			else if (tt.Contains("AES-17 DFD"))
			{
				ExecIm(18000, 20000, 1);
			}
			else if (tt.Contains("TDFD Phono"))
			{
				ExecIm(3005, 4462, 1);
			}
		}

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var imdVm = ViewSettings.Singleton.ImdVm;
			imdVm.DoMouse(sender, e);
		}

		private static Marker? MyMark = null;
		private void DoMouse(object sender, MouseEventArgs e)
		{
			SetMouseTrack(e);

			if (IsRunning || !IsTracking || actImd == null || actPlot == null)
				return;

			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			var xpos = cord.Item1;
			var ypos = cord.Item2;
			FreqValue = Math.Pow(10, xpos);

			var zv = actImd.LookupXY(FreqValue, ypos, ShowRight && !ShowLeft);
			var valdBV = GraphUtil.ReformatValue(PlotFormat, zv.Item2, zv.Item3);
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
			var valunit = GraphUtil.DoValueFormat(PlotFormat, zv.Item2, zv.Item3);
			var valpercent = GraphUtil.DoValueFormat("dBV", zv.Item2, zv.Item3);
			ZValue = $"{valunit}" + Environment.NewLine +
				$"{valpercent}" + Environment.NewLine +
				$"{valvolt}";
		}

		~ImdViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		 
		}

		public ImdViewModel()
		{
			Name = "Intermodulation";
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			// eliminate warnings
			this.actPlot = default!;
			this.actInfoLeft = default!;
			this.actInfoRight = default!;
			this.actImd = default!;

			GraphStartFreq = "20";
			GraphEndFreq = "20000";
			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowSummary = true;
			ShowDataPercent = true;
			ShowLeft = true;
			ShowRight = false;

			InputRange = 0;
			RangeTopdB = "20";
			RangeBottomdB = "-180";

			ShowMarkers = false;
			ShowPowerMarkers = false;

			Gen1Voltage = "0.1";
			Gen2Voltage = "0.1";
			Gen2Frequency = "20000";
			Gen1Frequency = "19000";
			UseGenerator = false;
			UseGenerator2 = false;

			Attenuation = 42;

			IntermodType = "Custom";
			IsImdCustom = true;

			PlotFormat = "dBV";

			ToShowRange = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Collapsed : Visibility.Visible;
			ToShowdB = GraphUtil.IsPlotFormatLog(PlotFormat) ? Visibility.Visible : Visibility.Collapsed; 

			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { actImd?.UpdateGraph(true); });
		}
	}
}
