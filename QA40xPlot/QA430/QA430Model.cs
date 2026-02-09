using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using QA40xPlot.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static QA40xPlot.QA430.QA430Model;

namespace QA40xPlot.QA430
{
	public struct QA430Config
	{
		public readonly OpampNegInputs CfgNegInput;
		public readonly OpampPosInputs CfgPosInput;
		public readonly OpampPosNegConnects CfgPosNegConnect;
		public readonly OpampFeedbacks CfgFeedback;
		public readonly int CfgGain = 1;
		public readonly int CfgDistgain = 1;
		public readonly string Name;
		public QA430Config(string name, OpampNegInputs ni, OpampPosInputs pi, OpampPosNegConnects pnc, OpampFeedbacks fb, int gain, int distgain)
		{
			Name = name;
			CfgNegInput = ni;
			CfgPosInput = pi;
			CfgPosNegConnect = pnc;
			CfgFeedback = fb;
			CfgGain = gain;
			CfgDistgain = distgain;
		}
	}

	// cfg,load,supply are used to set the QA430 for acquisition steps
	// Gain, Distgain are just passed to the app for internal math 
	public struct AcquireStep
	{
		public string Cfg;      // configuration name
		public LoadOptions Load; // load option
		public int Gain;        // signal gain for use by app
		public int Distgain;    // distortion gain for use by app
		public double SupplyP;  // supply positive voltage
		public double SupplyN;  // supply negative voltage
		public double GenVolt { get; set; } // generator voltage (freq sweep)
		public string GenXFmt { get; set; } // format the swept value
		public double GenFrequency { get; set; } // generator frequency (amp sweep)

		public AcquireStep(AcquireStep asIn)
		{
			Cfg = asIn.Cfg;
			Load = asIn.Load;
			Gain = asIn.Gain;
			Distgain = asIn.Distgain;
			SupplyP = asIn.SupplyP;
			SupplyN = asIn.SupplyN;
			GenVolt = asIn.GenVolt;
			GenXFmt = asIn.GenXFmt;
			GenFrequency = asIn.GenFrequency;
		}

		public string ToSuffix(bool addVolt, bool hasQa430)
		{
			string sout = string.Empty;
			var aload = QA430Model.FormatLoad(Load);
			if (hasQa430)
			{
				if (SupplyN == SupplyP)
					sout = $".{aload};{SupplyP}VDC;Gain={Gain}";
				else
					sout = $".{aload};+{SupplyP}|-{SupplyN}VDC;Gain={Gain}";
			}
			if (addVolt)
			{
				sout += "@" + GenXFmt;
			}
			return sout;
		}
	};

	public class QA430Model : FloorViewModel
	{
		const int RelayRegister = 5;

		// choice values for combo boxes
		public enum OpampNegInputs : ushort { AnalyzerTo499, GndTo4p99, Open, AnalyzerTo4p99k, GndTo499 }
		public enum OpampPosInputs : ushort { Analyzer, Gnd, AnalyzerTo100k }
		public enum OpampPosNegConnects : ushort { Open, R49p9, Short }
		public enum OpampFeedbacks : ushort { Short, R4p99k }
		public enum LoadOptions : ushort { Open, R2000, R604, R470 }
		public enum PsrrOptions : ushort { BothPsrrInputsGrounded, HiRailToAnalyzer, LowRailToAnalyzer, BothRailsToAnalyzer }
		public enum OpampConfigOptions : ushort { Custom, Config1, Config2, Config3a, Config3b, Config3c, Config4a, Config4b, Config5a, Config5b, Config6a, Config6b, Config7a, Config7b, Config8a, Config8b };
		// also
		// +/- supply voltages
		// use fixed rails
		// current sense enable
		// supply enable
		public static List<string> DistortionConfigs { get; } = ["Config6b", "Config7b", "Config4b", "Config5b"];
		public static List<string> LowDistortionConfigs { get; } = ["Config6a", "Config7a", "Config4a", "Config5a"];

		// choice names for combo boxes
		public static List<string> NegInputs { get; } = new() { "Signal + 499 Ω", "Gnd + 4.99 Ω", "Open", "Signal + 4.99K Ω", "Gnd + 499 Ω" };
		public static List<string> PosInputs { get; } = new() { "Signal", "Gnd", "Signal + 100K Ω" };
		public static List<string> PosNegConnects { get; } = new() { "Open", "49.9 Ω", "Short" };
		public static List<string> Feedbacks { get; } = new() { "Short", "4.99K Ω" };
		public static List<string> Loads { get; } = new() { "Open", "2000 Ω", "604 Ω", "470 Ω" };
		public static List<string> Psrrs { get; } = new() { "None", "Hi Rail", "Low Rail", "Both Rails" };
		public static List<string> RailVoltages { get; } = new() { "1", "2", "5", "10", "12", "14.4" };
		public static List<string> NegRailVoltages { get; } = new() { "-1", "-2", "-5", "-10", "-12", "-14.4" };
		public static List<string> ConfigOptions { get; } = Enum.GetNames(typeof(OpampConfigOptions)).ToList();

		private readonly Task? RefreshTask;
		private bool RefreshTaskCancel = false;

		static readonly QA430Config C1 = new("Config1", OpampNegInputs.GndTo4p99, OpampPosInputs.Analyzer,
					OpampPosNegConnects.Open, OpampFeedbacks.R4p99k, 0, 0);
		static readonly QA430Config C2a = new("Config2a", OpampNegInputs.Open, OpampPosInputs.Analyzer,
					OpampPosNegConnects.Short, OpampFeedbacks.Short, 0, 0);
		static readonly QA430Config C2b = new("Config2b", OpampNegInputs.GndTo499, OpampPosInputs.Analyzer,
					OpampPosNegConnects.Short, OpampFeedbacks.R4p99k, 0, 0);
		static readonly QA430Config C2c = new("Config2", OpampNegInputs.GndTo4p99, OpampPosInputs.Analyzer,
					OpampPosNegConnects.Short, OpampFeedbacks.R4p99k, 0, 0);    // note the Config2 name
		static readonly QA430Config C3a = new("Config3a", OpampNegInputs.GndTo4p99, OpampPosInputs.Gnd,
					OpampPosNegConnects.Open, OpampFeedbacks.R4p99k, 0, 0);
		static readonly QA430Config C3b = new("Config3b", OpampNegInputs.GndTo499, OpampPosInputs.Gnd,
					OpampPosNegConnects.Open, OpampFeedbacks.R4p99k, 0, 0);
		static readonly QA430Config C3c = new("Config3c", OpampNegInputs.Open, OpampPosInputs.Gnd,
					OpampPosNegConnects.Open, OpampFeedbacks.Short, 0, 0);
		static readonly QA430Config C4a = new("Config4a", OpampNegInputs.GndTo499, OpampPosInputs.Analyzer,
					OpampPosNegConnects.Open, OpampFeedbacks.R4p99k, 10, 10);
		static readonly QA430Config C4b = new("Config4b", OpampNegInputs.GndTo499, OpampPosInputs.Analyzer,
					OpampPosNegConnects.R49p9, OpampFeedbacks.R4p99k, 10, 110);
		static readonly QA430Config C5a = new("Config5a", OpampNegInputs.AnalyzerTo499, OpampPosInputs.Gnd,
					OpampPosNegConnects.Open, OpampFeedbacks.R4p99k, -10, 10);
		static readonly QA430Config C5b = new("Config5b", OpampNegInputs.AnalyzerTo499, OpampPosInputs.Gnd,
					OpampPosNegConnects.R49p9, OpampFeedbacks.R4p99k, -10, 110);
		static readonly QA430Config C6a = new("Config6a", OpampNegInputs.Open, OpampPosInputs.Analyzer,
					OpampPosNegConnects.Open, OpampFeedbacks.R4p99k, 1, 1);
		static readonly QA430Config C6b = new("Config6b", OpampNegInputs.Open, OpampPosInputs.Analyzer,
					OpampPosNegConnects.R49p9, OpampFeedbacks.R4p99k, 1, 101);
		static readonly QA430Config C7a = new("Config7a", OpampNegInputs.AnalyzerTo4p99k, OpampPosInputs.Gnd,
					OpampPosNegConnects.Open, OpampFeedbacks.R4p99k, -1, 1);
		static readonly QA430Config C7b = new("Config7b", OpampNegInputs.AnalyzerTo4p99k, OpampPosInputs.Gnd,
					OpampPosNegConnects.R49p9, OpampFeedbacks.R4p99k, -1, 101);
		static readonly QA430Config C8a = new("Config8a", OpampNegInputs.Open, OpampPosInputs.Analyzer,
					OpampPosNegConnects.Open, OpampFeedbacks.Short, 0, 0);
		static readonly QA430Config C8b = new("Config8b", OpampNegInputs.Open, OpampPosInputs.AnalyzerTo100k,
					OpampPosNegConnects.Open, OpampFeedbacks.Short, 0, 0);
		static readonly QA430Config[] AllConfigs = new[] { C1, C2a, C2b, C2c, C3a, C3b, C3c, C4a, C4b, C5a, C5b, C6a, C6b, C7a, C7b, C8a, C8b };

		#region Properties

		internal QA430Info? MyWindow { get; set; } = null;

		[JsonIgnore]
		public RelayCommand ShowConfigurations { get => new RelayCommand(OnShowConfigs); }

		// this is used by the gui to show the current config photo
		public ImageSource ConfigImage
		{
			get
			{
				var logo = new BitmapImage();
				logo.BeginInit();
				var src = @"/QA40xPlot;component/Images/QA430Configs/" + ConfigOptions[OpampConfigOption] + ".png";
				logo.UriSource = new Uri(src, UriKind.Relative);
				logo.EndInit();
				return logo;
			}
		}

		//these must all be initialized to not the initial values
		private short _OpampConfigOption = 10;
		public short OpampConfigOption
		{
			get => _OpampConfigOption;
			set { SetProperty(ref _OpampConfigOption, value); RaisePropertyChanged("ConfigImage"); }
		}

		private short _OpampNegInput = 10;
		public short OpampNegInput
		{
			get => _OpampNegInput;
			set => SetProperty(ref _OpampNegInput, value);
		}
		private short _OpampPosInput = 10;
		public short OpampPosInput
		{
			get => _OpampPosInput;
			set => SetProperty(ref _OpampPosInput, value);
		}
		private short _OpampPosNegConnect = 10;
		public short OpampPosNegConnect
		{
			get => _OpampPosNegConnect;
			set => SetProperty(ref _OpampPosNegConnect, value);
		}
		private short _OpampFeedback = 10;
		public short OpampFeedback
		{
			get => _OpampFeedback;
			set => SetProperty(ref _OpampFeedback, value);
		}
		private short _Load = 10;
		public short LoadOption
		{
			get => _Load;
			set => SetProperty(ref _Load, value);
		}
		private short _Psrr = 10;
		public short PsrrOption
		{
			get => _Psrr;
			set => SetProperty(ref _Psrr, value);
		}

		public double PosRailVoltageValue
		{
			get
			{
				if (double.TryParse(PosRailVoltage, out double v))
				{
					return v;
				}
				return 1.0;
			}
		}
		public string _PosRailVoltage = "3.14159";
		public string PosRailVoltage
		{
			get => _PosRailVoltage;
			set => SetProperty(ref _PosRailVoltage, value);
		}

		public double NegRailVoltageValue
		{
			get
			{
				if (double.TryParse(NegRailVoltage, out double v))
				{
					return v;
				}
				return 1.0;
			}
		}
		public string _NegRailVoltage = "-3.14159";
		public string NegRailVoltage
		{
			get => _NegRailVoltage;
			set => SetProperty(ref _NegRailVoltage, value);
		}
		// these bools must differ from the defaults with current code
		public bool _EnableSupply = true;
		public bool EnableSupply
		{
			get => _EnableSupply;
			set => SetProperty(ref _EnableSupply, value);
		}
		public bool _EnableCurrentSense = true;
		public bool EnableCurrentSense
		{
			get => _EnableCurrentSense;
			set => SetProperty(ref _EnableCurrentSense, value);
		}
		public bool _UseFixedRails = true;
		public bool UseFixedRails
		{
			get => _UseFixedRails;
			set => SetProperty(ref _UseFixedRails, value);
		}

		// things we read from the QA430
		[JsonIgnore]
		public string CurrentSenseHiValue
		{
			get
			{
				var val = Hw.GetHiSideSupplyCurrent();
				return MathUtil.FormatCurrent(val);
			}
		}

		[JsonIgnore]
		public string CurrentSenseLowValue
		{
			get
			{
				var val = Hw.GetLowSideSupplyCurrent();
				return MathUtil.FormatCurrent(val);
			}
		}

		[JsonIgnore]
		public string OffsetVoltage
		{
			get
			{
				var val = Hw.GetOffsetVoltage();
				return MathUtil.FormatVoltage(val);
			}
		}

		[JsonIgnore]
		public string UsbVoltage
		{
			get
			{
				var val = Hw.GetUsbVoltage();
				return MathUtil.FormatVoltage(val);
			}
		}
		#endregion

		/// <summary>
		/// pop up the configuration image gallery
		/// </summary>
		private void OnShowConfigs()
		{
			var wd = new QA430ShowConfigs();
			var rslt = wd.ShowDialog();
			if (rslt ?? false)
			{
				var uname = wd.ConfigName;
				OpampConfigOption = (short)ConfigOptions.IndexOf(uname);
			}
		}

		/// <summary>
		/// this keeps updating the 4 input variables
		/// we stop when the app exits
		/// </summary>
		/// <returns></returns>
		private async Task MonitorQA430Timer()
		{
			var RefreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
			while (await RefreshTimer.WaitForNextTickAsync())
			{
				// Place function in here..
				if (RefreshTaskCancel)
					break;
				if (ViewSettings.Singleton.MainVm.HasQA430 && ViewSettings.Singleton.MainVm.ShowQA430)
					RefreshVars();
			}
		}

		public QA430Model()
		{
			// set up a listener for when any property changes
			// we could embed them into the setproperty line but 
			// this puts all the 'set option' code in one place
			PropertyChanged += DoPropertyChanged;
			RefreshTask = MonitorQA430Timer();
		}

		~QA430Model()
		{
			PropertyChanged -= DoPropertyChanged;
			KillTimer();
		}

		public async Task WaitForQA430Relays()
		{
			// wait for the QA430 to settle after a relay change
			await Hw.WaitForRelays();
		}

		/// <summary>
		/// high level start up QA430 controller and model
		/// initializes to config6a
		/// </summary>
		/// <returns>connected USB to QA430?</returns>
		internal static bool BeginQA430Op()
		{
			if (!ViewSettings.Singleton.SettingsVm.EnableQA430)
			{
				// not wanted
				Debug.WriteLine("QA430 not wanted by user");
				return false;
			}
			var x = Qa430Usb.Singleton;
			if (x.IsOpen())
			{
				// already open
				Debug.WriteLine("QA430 was already open");
				return true;
			}
			var y = x?.InitializeConnection() ?? false;
			if (y)
			{
				// we have a QA430 connected. Initialize it to a known state
				var model = Qa430Usb.Singleton.QAModel;
				var didStart = model.SetDefaults();
				if (didStart)
				{
					var uu = new QA430Info();
					uu.Show();
					model.MyWindow = uu;
					ViewSettings.Singleton.MainVm.HasQA430 = true;
					// default to 6a and set the dialog display after rendering
					uu.ContentRendered += (s, e) => { model.OpampConfigOption = (short)OpampConfigOptions.Config6a; };
				}
			}

			return y;
		}

		internal static void EndQA430Op()
		{
			// close the usb connection
			var qausb = Qa430Usb.Singleton;
			if (qausb == null)
				return;
			qausb.QAModel.KillTimer();
			// close and clear the window if it exists
			var wnd = qausb.QAModel.MyWindow;
			if (wnd != null)
			{
				wnd.AllowClose = true;
				wnd.Close();
				qausb.QAModel.MyWindow = null;
			}
			qausb.Close(true);
		}

		internal void KillTimer()
		{
			RefreshTaskCancel = true;
		}

		/// <summary>
		/// the 4 sense values get refreshed every second, but because the property
		/// is readonly it requires a push (RaisePropertyChanged)
		/// </summary>
		internal void RefreshVars()
		{
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				RaisePropertyChanged(nameof(UsbVoltage));
				RaisePropertyChanged(nameof(OffsetVoltage));
				RaisePropertyChanged(nameof(CurrentSenseLowValue));
				RaisePropertyChanged(nameof(CurrentSenseHiValue));
			});

		}

		internal bool SetDefaults()
		{
			try
			{
				ShowRegisters("Beginning default");
				//Hw.ResetAllRelays();    // set the internal relays byte to zero
				Thread.Sleep(50);
				EnableSupply = false;
				EnableCurrentSense = false;
				PosRailVoltage = "1.0";
				NegRailVoltage = "-1.0";
				UseFixedRails = false;

				Thread.Sleep(250); 
				ShowRegisters("Voltage set");

				OpampNegInput = (short)OpampNegInputs.Open;
				OpampPosInput = (short)OpampPosInputs.Analyzer;
				OpampFeedback = (short)OpampFeedbacks.R4p99k;
				OpampPosNegConnect = (short)OpampPosNegConnects.Open;
				UseFixedRails = true;   // not using this is just painful, so...
										// don't allow analyzer to drive rails
				PsrrOption = (short)PsrrOptions.BothPsrrInputsGrounded;
				LoadOption = (short)LoadOptions.Open;

				ShowRegisters("Relays set");    // sb E00C,11,11,1

				EnableSupply = true;
				ShowRegisters("Voltage enabled");    // sb E00C,11,11,1
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in SetDefaults: {ex.Message}");
			}

			return false;
		}

		internal async Task PrepareDefault()
		{
			SetOpampConfig("Config6a");
			UseFixedRails = true;
			LoadOption = (short)QA430Model.LoadOptions.Open;
			// now that the QA430 relays are set, wait a bit...
			await WaitForQA430Relays();
		}

		public void ShowRegisters(string also = "")
		{
			var qausb = Qa430Usb.Singleton;
			var r5 = qausb.ReadRegister(5);
			var r8 = qausb.ReadRegister(8);
			var r9 = qausb.ReadRegister(9);
			var r10 = qausb.ReadRegister(10);
			Debug.WriteLine(also + $":QA430 Registers: 5={r5:x} 8={r8:x} 9={r9:x} 10={r10:x}");
		}

		private void SetConfiguration(OpampNegInputs negin, OpampFeedbacks fdbk, OpampPosNegConnects opn, OpampPosInputs opinp)
		{
			// Select 4.99 ohm series
			OpampNegInput = (short)negin;
			OpampFeedback = (short)fdbk;
			OpampPosNegConnect = (short)opn;
			OpampPosInput = (short)opinp;
			ShowRegisters("SetConfiguration"); // for debugging write register configuration
		}

		public LoadOptions FindLoad(int Loadval)
		{
			switch (Loadval)
			{
				default:
					Debug.WriteLine($"Unknown load value {Loadval}, defaulting to Open");
					return LoadOptions.Open;
				case 1000000:
					return LoadOptions.Open;
				case 2000:
					return LoadOptions.R2000;
				case 604:
					return LoadOptions.R604;
				case 470:
					return LoadOptions.R470;
			}
		}

		public static string FormatLoad(LoadOptions load)
		{
			if (load == LoadOptions.Open)
				return "Open";
			else if (load == LoadOptions.R2000)
				return "2000Ω";
			else if (load == LoadOptions.R604)
				return "604Ω";
			else if (load == LoadOptions.R470)
				return "470Ω";
			else
				return "Unknown";
		}

		public List<AcquireStep> ExpandLoadOptions(List<AcquireStep> srcSteps, string whom)
		{
			var items = SelectItemList.ParseList(whom).Where(x => x.IsSelected).ToList();
			if (items == null || items.Count == 0)
				return srcSteps;    // nothing selected

			List<AcquireStep> newSteps = new List<AcquireStep>();
			foreach (var item in items)
			{
				var vname = item.Name;
				if (vname.Contains("Open", StringComparison.InvariantCultureIgnoreCase))
					vname = "Open";
				else
				{
					vname = "R" + vname.TrimEnd('Ω').Trim();
				}
				object? value = null;
				Enum.TryParse(typeof(LoadOptions), vname, true, out value);
				if (value == null)
					continue;
				var newlist = srcSteps.Select(s =>
				{
					var sp = new AcquireStep(s);
					sp.Load = (LoadOptions)value;
					return sp;
				}).ToList();
				newSteps.AddRange(newlist);
			}
			return newSteps;
		}

		private QA430Config? GainToConfig(int gainValue, bool useHighDistortion)
		{
			QA430Model? model430 = Qa430Usb.Singleton?.QAModel;
			var configSet = useHighDistortion ? DistortionConfigs : LowDistortionConfigs;
			foreach (var config in configSet)
			{
				QA430Config? cfg = model430?.FindOpampConfig(config);
				if (cfg?.CfgGain == gainValue)
				{
					return cfg;
				}
			}
			return model430?.FindOpampConfig("Config6b"); // default
		}

		public List<AcquireStep> ExpandGainOptions(List<AcquireStep> srcSteps, string gainOpts, bool useHigh)
		{
			var gainSet = SelectItemList.ParseList(gainOpts).Where(x => x.IsSelected).ToList();        // convert to a list of items
			if (gainSet == null || gainSet.Count == 0)
			{
				return srcSteps;
			}
			List<AcquireStep> newSteps = new List<AcquireStep>();
			foreach (var opt in gainSet)
			{
				var gain = MathUtil.ToDouble(opt.Name, -150);
				var cfg = GainToConfig((int)gain, useHigh);
				if (cfg == null)
					continue;
				foreach (var step in srcSteps)
				{
					// for different gain we --
					// have to change the configuration name and the distortion gain
					var stp = new AcquireStep(step);
					stp.Cfg = cfg?.Name ?? string.Empty;
					stp.Gain = (int)gain;
					stp.Distgain = cfg?.CfgDistgain ?? 1;
					newSteps.Add(stp);
				}
			}

			return newSteps;
		}

		public List<AcquireStep> ExpandSupplyOptions(List<AcquireStep> srcSteps, string supplyOpts)
		{
			var supplySet = SelectItemList.ParseList(supplyOpts).Where(x => x.IsSelected).ToList();
			if (supplySet == null || supplySet.Count == 0)
			{
				return srcSteps;
			}
			List<(double, double)> supplies = new();
			// parse the entries
			var names = supplySet.Select(x => x.Name).ToList();
			foreach (var voltage in names)
			{
				var avolt = voltage.Split(['|', '_', ':']);
				double voltp = 15;
				if (avolt.Length > 0)
					voltp = MathUtil.ToDouble(avolt[0], 15);
				var voltn = voltp;
				if (avolt.Length > 1)
					voltn = MathUtil.ToDouble(avolt[1], 15);
				supplies.Add((voltp, voltn));
			}

			List<AcquireStep> newSteps = new List<AcquireStep>();
			foreach (var step in srcSteps)
			{
				foreach (var supplyOpt in supplies)
				{
					var stp = new AcquireStep(step);
					stp.SupplyP = supplyOpt.Item1;
					stp.SupplyN = supplyOpt.Item2;
					newSteps.Add(stp);
				}
			}
			return newSteps;
		}

		public void SetOpampConfig(string configName)
		{
			var config = FindOpampConfig(configName);
			SetConfiguration(config.CfgNegInput, config.CfgFeedback, config.CfgPosNegConnect, config.CfgPosInput);
			// setting configuration sets to custom so fix the image display
			OpampConfigOption = (short)Math.Max(0, ConfigOptions.IndexOf(configName));
		}

		internal QA430Config FindOpampConfig(string configName)
		{
			var config = AllConfigs.FirstOrDefault(c => c.Name == configName);
			return config;
		}

		internal void ExecConfiguration(short iId)
		{
			switch (iId)
			{
				case (short)OpampConfigOptions.Config1:
					SetOpampConfig("Config1");
					break;
				case (short)OpampConfigOptions.Config2:
					SetOpampConfig("Config2");  // 60db variant
					break;
				case (short)OpampConfigOptions.Config3a:
					SetOpampConfig("Config3a");
					break;
				case (short)OpampConfigOptions.Config3b:
					SetOpampConfig("Config3b");
					break;
				case (short)OpampConfigOptions.Config3c:
					SetOpampConfig("Config3c");
					break;
				case (short)OpampConfigOptions.Config4a:
					SetOpampConfig("Config4a");
					break;
				case (short)OpampConfigOptions.Config4b:
					SetOpampConfig("Config4b");
					break;
				case (short)OpampConfigOptions.Config5a:
					SetOpampConfig("Config5a");
					break;
				case (short)OpampConfigOptions.Config5b:
					SetOpampConfig("Config5b");
					break;
				case (short)OpampConfigOptions.Config6a:
					SetOpampConfig("Config6a");
					break;
				case (short)OpampConfigOptions.Config6b:
					SetOpampConfig("Config6b");
					break;
				case (short)OpampConfigOptions.Config7a:
					SetOpampConfig("Config7a");
					break;
				case (short)OpampConfigOptions.Config7b:
					SetOpampConfig("Config7b");
					break;
				case (short)OpampConfigOptions.Config8a:
					SetOpampConfig("Config8a");
					break;
				case (short)OpampConfigOptions.Config8b:
					SetOpampConfig("Config8b");
					break;
				default:
					break;
			}
		}

		// this essentially handles the Set side of the properties
		// and adjusts the QA430 to match the property values
		private void DoPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "OpampNegInput":
					Hw.SetNegInput((OpampNegInputs)OpampNegInput);
					break;
				case "OpampPosInput":
					Hw.SetPosInput((OpampPosInputs)OpampPosInput);
					break;
				case "OpampPosNegConnect":
					Hw.SetPosNegConnect((OpampPosNegConnects)OpampPosNegConnect);
					break;
				case "OpampFeedback":
					Hw.SetFeedback((OpampFeedbacks)OpampFeedback);
					break;
				case "LoadOption":
					Hw.SetLoad((LoadOptions)LoadOption);
					break;
				case "PsrrOption":
					Hw.SetPsrr((PsrrOptions)PsrrOption);
					break;
				case "PosRailVoltage":
					Hw.SetPositiveRailVoltage(PosRailVoltageValue);
					break;
				case "NegRailVoltage":
					Hw.SetNegativeRailVoltage(NegRailVoltageValue);
					break;
				case "EnableSupply":
					Hw.OpampSupplyEnable(EnableSupply);
					break;
				case "EnableCurrentSense":
					Hw.EnableSplitCurrentSense(EnableCurrentSense);
					break;
				case "UseFixedRails":
					Hw.SetPowerRails(UseFixedRails);
					break;
				case "OpampConfigOption":
					ExecConfiguration(OpampConfigOption);
					break;
				case "ConfigImage":
					// used internally
					break;
				case nameof(UsbVoltage):
				case nameof(OffsetVoltage):
				case nameof(CurrentSenseLowValue):
				case nameof(CurrentSenseHiValue):
					break;
				default:
					Debug.WriteLine($"Unknown property change {e.PropertyName}");
					break;
			}
		}
	}
}
