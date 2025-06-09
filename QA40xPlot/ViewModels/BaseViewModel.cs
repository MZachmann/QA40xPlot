﻿using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using ScottPlot;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public abstract class BaseViewModel : FloorViewModel, IMouseTracker, ICloneable
	{
		// public INavigation ViewNavigator { get; set; }
		#region Shared Properties
		public static List<String> WindowingTypes { get => new List<string> { "Bartlett", "Blackman", "Cosine", "FlatTop", "Hamming", "Hann", "Kaiser", "Rectangular", "Tukey", "Welch" }; }
		public static List<String> EndFrequencies { get => new List<string> { "1000", "2000", "5000", "10000", "20000", "50000", "100000" }; }
		public static List<String> StartFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "5000", "10000" }; }
		public static List<String> TopDbs { get => new List<string> { "100", "50", "20", "0", "-50", "-80"  }; }
		public static List<String> BottomDbs { get => new List<string> { "0", "-50", "-100", "-120", "-140", "-160", "-180", "-200" }; }
		public static List<String> StartPercents { get => new List<string> { "1000", "100", "10", "1", "0.1", "0.01" }; }
		public static List<String> EndPercents { get => new List<string> { "0.1", "0.01", "0.001", "0.0001", "0.00001", "0.000001", "0.0000001" }; }
		public static List<String> SampleRates { get => new List<string> { "48000", "96000", "192000", "384000"}; }
		public static List<String>	FftSizes { get => new List<string> { "8K","16K", "32K", "64K", "128K", "256K", "512K", "1024K" }; }
		public static List<uint>	FftActualSizes { get => new List<uint> { 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576 }; }
		public static List<String> GenVoltages { get => new List<string> { "0.05", "0.1", "0.25", "0.5", "0.75", "1", "2", "5" }; }
		public static List<String> GenPowers { get => new List<string> { "0.05", "0.1", "0.25", "0.5", "0.75", "1", "2", "5", "10", "25" }; }
		public static List<String> MeasureVolts { get => new List<string> { "Input Voltage", "Output Voltage" }; }
		public static List<String> MeasureVoltsFull { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }
		public static List<String> Impedances { get => new List<string> { "2","4", "8", "10","16", "20", "100", "500", "1000" }; }
		public static List<String> Waveforms { get => new List<string> { "Sine", "Square", "Multitone","Impulse", "Chirp" }; }
		public static List<String> Attenuations { get => new List<string> { "0", "20", "10", "-6", "-10", "-20", "-30"}; }
		public static string PlotFileFilter { get => "Plot files|*.plt;*.zip|All files|*.*"; }
		public static string DutInfo { get => "DUT = Device Under Test"; }
		public static string DutDescript { get => "Input Voltage = DUT Input(Generator Output), Output Voltage = DUT Output(QA40x Input)"; }
		public static string AutoRangeDescript { get => "When the test is started a safe Attenuation value is calculated based on a test at 42."; }
		public static string TabInfoTip { get => "Click to set line colors and edit plot headings"; }
		public static string SummaryDataTip { get => "Click to see a box with summary test statistics"; }
		public static string MiniPlotsTip { get => "Click to show the mini plots persistently"; }
		public static List<String> ChannelList { get => new List<string> { "Left", "Right" }; }
		public static List<String> PowerFreqList { get => new List<string> { "50", "60" }; }
		public static List<String> TrueFalseList { get => new List<string> { "True", "False" }; }
		public static bool GEN_INPUT { get => true; }
		public static bool GEN_OUTPUT { get => false; }
		public static List<String> DataFormats { get => new List<string> { "SPL", "dBFS", "dBr", "dBu", "dBV", "dBW", "%", "V", "W" }; }
		public static int MaxAverages { get => 20; }    // there's not much reason for this to be in a setting, but here's an easy...
		public static List<string> PowerUnits { get => new List<string>() { "mW", "μW", "W", "dBW", "dBm" }; }
		public static List<string> VoltageUnits { get => new List<string>() { "mV", "μV", "V", "dBV", "dBmV", "dBu", "dBFS" }; }
		[JsonIgnore]
		public RelayCommand DoGetGenUnits { get => new RelayCommand(GetGenUnits); }
		[JsonIgnore]
		public RelayCommand<object> ShowMenuCommand { get => new RelayCommand<object>(ShowMenu); }
		[JsonIgnore]
		public RelayCommand<object> SetGenVolts { get => new RelayCommand<object>(DoGenVolts); }
		#endregion

		#region Output Setters and Getters
		[JsonIgnore]
		protected TabAbout actAbout { get; set; } = new();
		[JsonIgnore]
		public DataDescript DataInfo { get; set; } = new(); // set when we set the datapage via linkabout

		[JsonIgnore]
		public bool IsGenPower { get => (GenDirection == MeasureVoltsFull[2]); }
		[JsonIgnore]
		public string GenAmpDescript { get => (IsGenPower ? "Po_wer" : "_Voltage"); }
		[JsonIgnore]
		public string GenAmpUnits { get => (IsGenPower ? "W" : "V"); }
		[JsonIgnore]
		public string DsHeading { get => DataInfo.Heading; }

		[JsonIgnore]
		private ObservableCollection<DataDescript> _OtherSetList = new();
		[JsonIgnore]
		public ObservableCollection<DataDescript> OtherSetList {
			get => _OtherSetList;
			set => SetProperty(ref _OtherSetList, value);
		}

		[JsonIgnore]
		private UserControl? _MyWindow = null;
		[JsonIgnore]
		public UserControl? MyWindow
		{
			get => _MyWindow;
			set => SetProperty(ref _MyWindow, value);
		}

		private bool _ShowMiniPlots = false;
		[JsonIgnore]
		public bool ShowMiniPlots
		{
			get => _ShowMiniPlots;
			set => SetProperty(ref _ShowMiniPlots, value);
		}

		[JsonIgnore]
		public string GraphUnit
		{
			get => GraphUtil.GetFormatSuffix(PlotFormat);
		}
		private Visibility _ToShowRange;
		[JsonIgnore]
		public Visibility ToShowRange
		{
			get => _ToShowRange;
			set => SetProperty(ref _ToShowRange, value);
		}
		private Visibility _ToShowdB;
		[JsonIgnore]
		public Visibility ToShowdB
		{
			get => _ToShowdB;
			set => SetProperty(ref _ToShowdB, value);
		}
		[JsonIgnore]
		public uint FftSizeVal
		{
			get
			{
				var fl = FftSizes.IndexOf(FftSize);
				if (fl == -1)
					return FftActualSizes[0];
				return FftActualSizes[fl];
			}
		}

		[JsonIgnore]
		public uint SampleRateVal { get => MathUtil.ToUint(SampleRate); }

		private bool _IsRunning = false;         // type of alert
		[JsonIgnore]
		public bool IsRunning
		{
			get { return _IsRunning; }
			set { SetProperty(ref _IsRunning, value); IsNotRunning = !value; }
		}
		private bool _IsNotRunning = true;         // type of alert
		[JsonIgnore]
		public bool IsNotRunning
		{
			get { return _IsNotRunning; }
			private set { SetProperty(ref _IsNotRunning, value); }
		}

		private string _XPos = string.Empty;
		[JsonIgnore]
		public string XPos
		{
			get => _XPos;
			set => SetProperty(ref _XPos, value);
		}

		private string _ZValue = string.Empty;
		[JsonIgnore]
		public string ZValue
		{
			get => _ZValue;
			set => SetProperty(ref _ZValue, value);
		}

		private double _FreqValue = 0.0;
		[JsonIgnore]
		public double FreqValue
		{
			get => _FreqValue;
			set => SetProperty(ref _FreqValue, value);
		}

		private string _FreqShow = string.Empty;
		[JsonIgnore]
		public string FreqShow
		{
			get => _FreqShow;
			set => SetProperty(ref _FreqShow, value);
		}

		bool _isTracking = true;
		[JsonIgnore]
		public bool IsTracking
		{
			get { return _isTracking; }
			set { SetProperty(ref _isTracking, value); }
		}

		bool _isMouseDown = false;
		[JsonIgnore]
		public bool IsMouseDown
		{
			get { return _isMouseDown; }
			set { SetProperty(ref _isMouseDown, value); }
		}

		private bool _HasExport = false;
		[JsonIgnore]
		public bool HasExport
		{
			get { return _HasExport; }
			set { SetProperty(ref _HasExport, value); }
		}
		private bool _HasSave = false;
		[JsonIgnore]
		public bool HasSave
		{
			get { return _HasSave; }
			set { SetProperty(ref _HasSave, value); }
		}

		/// <summary>
		/// for use by the unit converters
		/// </summary>
		private string _GenVoltageUnits = "V";
		[JsonIgnore]
		public string GenVoltageUnits
		{
			get { return _GenVoltageUnits; }
			set
			{
				SetProperty(ref _GenVoltageUnits, value);
			}
		}

		/// <summary>
		/// for use by the global display section
		/// </summary>
		private string _GeneratorVoltage = "V";
		[JsonIgnore]
		public string GeneratorVoltage
		{
			get { return _GeneratorVoltage; }
			set
			{
				SetProperty(ref _GeneratorVoltage, value);
			}
		}

		#endregion

		#region Setters and Getters
		private bool _KeepMiniPlots = false;
		public bool KeepMiniPlots
		{
			get => _KeepMiniPlots;
			set { SetProperty(ref _KeepMiniPlots, value); ShowMiniPlots = value; }
		}

		private bool _ExpandGenerator = true;		// expand the generator section?
		public bool ExpandGenerator
		{
			get => _ExpandGenerator;
			set => SetProperty(ref _ExpandGenerator, value);
		}

		private bool _ExpandSampling = true;       // expand the Sampling section?
		public bool ExpandSampling
		{
			get => _ExpandSampling;
			set => SetProperty(ref _ExpandSampling, value);
		}

		private bool _ExpandYAxis = true;       // expand the YAxis section?
		public bool ExpandYAxis
		{
			get => _ExpandYAxis;
			set => SetProperty(ref _ExpandYAxis, value);
		}

		private bool _ExpandXAxis = true;       // expand the XAxis section?
		public bool ExpandXAxis
		{
			get => _ExpandXAxis;
			set => SetProperty(ref _ExpandXAxis, value);
		}

		private bool _ExpandSweep = true;       // expand the Sweep section?
		public bool ExpandSweep
		{
			get => _ExpandSweep;
			set => SetProperty(ref _ExpandSweep, value);
		}

		private bool _ExpandGlobals = true;       // expand the Sweep section?
		public bool ExpandGlobals
		{
			get => _ExpandGlobals;
			set => SetProperty(ref _ExpandGlobals, value);
		}

		private bool _ExpandGraphData = true;       // expand the GraphData section?
		public bool ExpandGraphData
		{
			get => _ExpandGraphData;
			set => SetProperty(ref _ExpandGraphData, value);
		}


		private bool _ExpandGraphOptions = true;       // expand the GraphOptions section?
		public bool ExpandGraphOptions
		{
			get => _ExpandGraphOptions;
			set => SetProperty(ref _ExpandGraphOptions, value);
		}

		private bool _ShowTabInfo = false;
		public bool ShowTabInfo
		{
			get => _ShowTabInfo;
			set => SetProperty(ref _ShowTabInfo, value);
		}

		private string _Name = string.Empty;         // name of the test
		public string Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}
		private bool _ShowSummary = false;
		public bool ShowSummary
		{
			get => _ShowSummary;
			set => SetProperty(ref _ShowSummary, value);
		}

		private string _PlotFormat = "dBV";
		public string PlotFormat
		{
			get => _PlotFormat;
			set { SetProperty(ref _PlotFormat, value); RaisePropertyChanged("GraphUnit"); }
		}

		private string _SampleRate = string.Empty;
		public string SampleRate
		{
			get => _SampleRate;
			set => SetProperty(ref _SampleRate, value);
		}
		private string _FftSize = string.Empty;
		public string FftSize
		{
			get => _FftSize;
			set => SetProperty(ref _FftSize, value);
		}
		private string _Windowing = string.Empty;
		public string WindowingMethod
		{
			get => _Windowing;
			set => SetProperty(ref _Windowing, value);
		}
		private uint _Averages;         // type of alert
		public uint Averages
		{
			get => _Averages; 
			set => SetProperty(ref _Averages, value);
		}

		private bool _ShowLeft;
		public bool ShowLeft
		{
			get => _ShowLeft;
			set => SetProperty(ref _ShowLeft, value);
		}

		private bool _ShowRight;
		public bool ShowRight
		{
			get => _ShowRight;
			set => SetProperty(ref _ShowRight, value);
		}

		private int _LeftWidth; // the width of the left channel in units
		public int LeftWidth
		{
			get => _LeftWidth;
			set => SetProperty(ref _LeftWidth, value);
		}

		private int _RightWidth; // the width of the left channel in units
		public int RightWidth
		{
			get => _RightWidth;
			set => SetProperty(ref _RightWidth, value);
		}


		/// <summary>
		/// Given an input voltage format, get a display format converter
		/// </summary>
		/// <param name="value">the data value</param>
		/// <param name="genFormat">the entry format</param>
		/// <returns>a converted double that is ready to become text</returns>
		public static string AlterDirection(string genFormat)
		{
			switch (genFormat)
			{
				// power formats
				case "mW":    // the generator has 18dBV output, the input has 32dBV maximum
					return "mV";
				case "μW":
					return "μV";
				case "W":
					return "V";
				case "dBW":
					return "dbV";
				case "dBm":
					return "dBmV";
				// voltage formats
				case "mV":
					return "mW";
				case "μV":
					return "μW";
				case "dBV":
					return "dBW";
				case "dBmV":
					return "dBm";
				case "dBu":
					return "dBW";
				case "dBFS":
					return "dBW";
			}
			return "W"; // default to volts
		}


		private string _GenDirection = string.Empty;
		public string GenDirection
		{
			get => _GenDirection;
			set {
				var oldd = IsGenPower;
				if( SetProperty(ref _GenDirection, value))
				{
					RaisePropertyChanged("GenAmpDescript");
					RaisePropertyChanged("GenAmpUnits");
					if (oldd != IsGenPower)
					{
						GenVoltageUnits = AlterDirection(GenVoltageUnits);
					}
				}
			}
		}
		private double _Attenuation;
		public double Attenuation
		{
			get => _Attenuation;
			set => SetProperty(ref _Attenuation, value);
		}
		#endregion


		private static void GetGenUnits()
		{
		}

		public void SetGeneratorVolts(string volts)
		{
			GenVoltageUnits = volts;
			RaisePropertyChanged("GenVoltage");
			RaisePropertyChanged("Gen1Voltage");
			RaisePropertyChanged("Gen2Voltage");
			RaisePropertyChanged("StartVoltage");
			RaisePropertyChanged("EndVoltage");
		}

		public static void DoGenVolts(object? parameter)
		{
			var mvm = ViewSettings.Singleton.Main.CurrentView; // the current viewmodel
			if (parameter != null && mvm != null)
			{
				mvm.SetGeneratorVolts(parameter.ToString() ?? string.Empty);
			}
		}

		/// <summary>
		/// find the closest frequency to the desired one that is a bin frequency
		/// this uses the ViewModel's fftsize and samplerate to determine the bin size
		/// </summary>
		/// <param name="freq">Frequency</param>
		/// <returns></returns>
		public double NearestBinFreq(double freq)
		{
			return QaLibrary.GetNearestBinFrequency(freq, this.SampleRateVal, this.FftSizeVal);
		}
		public double NearestBinFreq(string sfreq)
		{
			return NearestBinFreq(MathUtil.ToDouble(sfreq, 1000.0));
		}


		public static void ShowMenu(object? parameter)
		{
			var mvm = ViewSettings.Singleton.Main.CurrentView; // the current viewmodel
			if (parameter is Button button && mvm != null)
			{
				button.ContextMenu = new ContextMenu(); // Clear any previous context menu
				var dutDirection = BaseViewModel.ToDirection(mvm.GenDirection);
				var unitList = (dutDirection == E_GeneratorDirection.OUTPUT_POWER) ? BaseViewModel.PowerUnits : BaseViewModel.VoltageUnits;
				foreach (var unit in unitList)
				{
					MenuItem unitItem = new MenuItem
					{
						Header = unit,
						Command = mvm.SetGenVolts, // set the unit of measure string
						CommandParameter = unit
					};
					button.ContextMenu.Items.Add(unitItem);
				}
				button.ContextMenu.PlacementTarget = button;
				button.ContextMenu.IsOpen = true;
			}
		}

		// here param is the id of the tab to remove from the othertab list
		public virtual void DoDeleteIt(string param) { }

		/// <summary>
		/// Convert direction string to a direction type
		/// this works for 2-value and 3-value answers
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static E_GeneratorDirection ToDirection(string  direction)
		{
			var u = MeasureVoltsFull.IndexOf(direction);
			if (u == -1)
				u = 0;
			return (E_GeneratorDirection)u;
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}

		public void LinkAbout(DataDescript fmv)
		{
			if(DataInfo != null)
			{
				DataInfo.MainVm = null;	// if the last vm pointed here, empty it
			}
			fmv.MainVm = this;  // point to the primary vm for messaging
			actAbout.SetDataContext(fmv);
			DataInfo = fmv;		// and the datadescript for binding
		}

		public List<int> FindShownOthers()
		{
			var x = OtherSetList;
			var y = x.Where(x => x.IsOnL || x.IsOnR).Select(x => x.Id).ToList();
			return y;
		}

		public bool IsValidLoadModel(string name)
		{
			var synonym = new List<string> { "Spectrum", "Intermodulation", "Scope" };

			if (name == Name)
				return true;
			if (synonym.Contains(name) && synonym.Contains(Name))
				return true;
			return false;
		}

		/// <summary>
		/// Given an input voltage determine the output voltage for this channel
		/// </summary>
		/// <param name="dVoltIn">volts input (generator output)</param>
		/// <param name="binNumber">bin number(s) to check or all if empty</param>
		/// <param name="lrGains">the gain curve for this channel</param>
		/// <returns></returns>
		public static double ToGenOutVolts(double dVoltIn, int[] binNumber, double[] lrGains)
		{
			if (lrGains == null)
			{
				return dVoltIn;
			}

			var maxGain = lrGains.Max();
			if (lrGains.Length > 1)
			{
				// figure out which bins we want
				int binmin = Math.Min(lrGains.Length - 1, 10);  // skip 10 at front since df is usually about 1Hz
				int binmax = Math.Max(1, lrGains.Length - binmin);
				if (binNumber.Length > 0 && binNumber[0] != 0)
				{
					int abin = binNumber[0];                // approximate bin
					binmin = Math.Max(1, abin - 5);         // random....
					if (binNumber.Length == 2)
						binmax = binNumber[1] + 5;
					else
						binmax = abin + 6;                      // random....
					binmax = Math.Min(lrGains.Length, binmax);  // limit this
				}
				maxGain = lrGains.Skip(binmin).Take(binmax - binmin).Max();
			}

			if (maxGain <= 0.0)
				return dVoltIn;

			return dVoltIn * maxGain;   // input to output transformation
		}

		/// <summary>
		/// convert a string value in the gui to an input or output voltage
		/// based on the current generator direction
		/// </summary>
		/// <param name="amplitude">string amplitude with type depending on generator direction</param>
		/// <param name="binNumber">frequency bin of interested or 0 for all</param>
		/// <param name="isInput">dut input or dut output voltage</param>
		/// <param name="lrGains">the gain calculations for one channel</param>
		/// <returns></returns>
		public double ToGenVoltage(string amplitude, int[] binNumber, bool isInput, double[]? lrGains)
		{
			var genType = ToDirection(GenDirection);
			var vtest = MathUtil.ToDouble(amplitude, 4321);
			if (vtest == 4321)
				return 1e-5;
			if (lrGains == null || (genType == E_GeneratorDirection.INPUT_VOLTAGE && isInput))
			{
				return vtest;
			}
			var maxGain = lrGains.Max();
			if( lrGains.Length > 1)
			{
				// figure out which bins we want
				int binmin = Math.Min(lrGains.Length-1, 20);
				int binmax = Math.Max(1, lrGains.Length * 9 / 10);
				if(binNumber.Length > 0 && binNumber[0] != 0)
				{
					int abin = binNumber[0];				// approximate bin
					binmin = Math.Max(binmin, abin - 5);         // random....
					if (binNumber.Length == 2)
						abin = binNumber[1] + 5;
					else
						abin = abin + 6;						// random....
					binmax = Math.Min(abin, binmax);  // limit this
				}
				maxGain = lrGains.Skip(binmin).Take(binmax - binmin).Max();
			}
			
			if (maxGain <= 0.0)
				return vtest;

			switch (genType)
			{
				case E_GeneratorDirection.INPUT_VOLTAGE:
					if (lrGains != null && !isInput)
					{
						vtest *= maxGain; // max expected DUT output voltage
					}
					break;
				case E_GeneratorDirection.OUTPUT_VOLTAGE:
					if (lrGains != null && isInput)
						vtest /= maxGain; // expected QA40x generator voltage
					break;
				case E_GeneratorDirection.OUTPUT_POWER:
					// now vtest is actually power setting... so convert to voltage
					vtest = Math.Sqrt(vtest * ViewSettings.AmplifierLoad);  // so sqrt(power * load) = output volts
					if (lrGains != null && isInput)
						vtest /= maxGain; // expected QA40x generator voltage
					break;
			}
			return vtest;
		}

		private DateTime _DownTime = DateTime.MinValue;
		protected void SetMouseTrack(MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed && !IsMouseDown)
			{
				IsMouseDown = true;
				_DownTime = DateTime.Now;
			}
			else
			if (e.LeftButton == MouseButtonState.Released && IsMouseDown)
			{
				IsMouseDown = false;
				if ((DateTime.Now - _DownTime).TotalMilliseconds < 500)
				{
					IsTracking = !IsTracking;
				}
				_DownTime = DateTime.MinValue;
			}
		}

		public void ForceGraphDraw()
		{
			RaisePropertyChanged("ShowLeft");   // cause the right kind of repaint
		}

		public void ForceGraphUpdate()
		{
			RaisePropertyChanged("UpdateGraph");   // cause the right kind of repaint
		}

		public 	BaseViewModel()
		{
			HasExport = false;
			GenDirection = MeasureVolts[0];
			SampleRate = "96000";
			FftSize = "64K";
			Averages = 1;
			PlotFormat = "dBV";
			WindowingMethod = "Hann";
			ShowLeft = true;
			ShowRight = false;
			ShowTabInfo = false;
			ShowSummary = false;
		}

		public void SetupMainPlot(PlotControl plot)
		{
			plot.TrackMouse = true;
			plot.GrandParent = this;
		}

		// convert mouse coordinates to scottplot coordinates
		// from ScottPlot github comment https://github.com/ScottPlot/ScottPlot/issues/3514
		public static Tuple<double,double> ConvertScottCoords(PlotControl plt, double x, double y)
		{
			PresentationSource source = PresentationSource.FromVisual(plt);
			double dpiX = 1;
			double dpiY = 1;
			if (source != null)
			{
				dpiX = source.CompositionTarget.TransformToDevice.M11;
				dpiY = source.CompositionTarget.TransformToDevice.M22;
			}

			Pixel mousePixel = new(x * dpiX, y * dpiY);
			var ep = plt.ThePlot.GetCoordinates(mousePixel);
			double XPos = ep.X;
			double YPos = ep.Y;
			return Tuple.Create(XPos, YPos);
		}

		#region IMouseHandler
		public event MouseEventHandler? MouseTracked;

		// when the mouse moves we send it up to the actual view model
		protected void OnMouseTracked([CallerMemberName] string? propertyName = "")
		{
			var changed = MouseTracked;
			if (changed == null)
				return;
			var md = InputManager.Current.PrimaryMouseDevice;
			changed.Invoke(this, new MouseEventArgs(md, Environment.TickCount));
		}

		/// <summary>
		/// RaisePropertyChanged
		/// Tell the window a mouse event has happened
		/// </summary>
		/// <param name="propertyName"></param>
		public void RaiseMouseTracked(string? propertyName = null)
		{
			OnMouseTracked(propertyName);
		}

		#endregion
	}
}
