#define USEQA430

using QA40xPlot.ViewModels;
using QA40xPlot.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static QA40xPlot.QA430.QA430Model;

namespace QA40xPlot.QA430
{
	internal class QA430Model : FloorViewModel
	{
		const int RelayRegister = 5;

		internal enum OpampNegInputs : ushort { AnalyzerTo499, GndTo4p99, Open, AnalyzerTo4p99k, GndTo499 }
		internal enum OpampPosInputs : ushort { Analyzer, Gnd, AnalyzerTo100k }
		internal enum OpampPosNegConnects : ushort { Open, R49p9, Short }
		internal enum OpampFeedbacks : ushort { Short, R4p99k }
		internal enum LoadOptions : ushort { Open, R2000, R604, R470 }
		internal enum PsrrOptions : ushort { BothPsrrInputsGrounded, HiRailToAnalyzer, LowRailToAnalyzer, BothRailsToAnalyzer }
		internal enum OpampConfigOptions : ushort { Custom, Config1, Config2, Config3a, Config3b, Config3c, Config4a, Config4b, Config5a, Config5b, Config6a, Config6b, Config7a, Config7b, Config8a, Config8b };
		// also
		// +/- supply voltages
		// use fixed rails
		// current sense enable
		// supply enable

		public static List<string> NegInputs { get; } = new() { "Signal|499", "Gnd|4.99", "Open", "Signal|4.99K", "Gnd|499" };
		public static List<string> PosInputs { get; } = new() { "Signal", "Gnd", "Signal|100K" };
		public static List<string> PosNegConnects { get; } = new() { "Open", "49.9", "Short" };
		public static List<string> Feedbacks { get; } = new() { "Short", "4.99K" };
		public static List<string> Loads { get; } = new() { "Open", "2000", "604", "470" };
		public static List<string> Psrrs { get; } = new() { "None", "Hi Rail", "Low Rail", "Both Rails" };
		public static List<string> RailVoltages { get; } = new() { "1", "2", "5", "10", "12", "14.4" };
		public static List<string> NegRailVoltages { get; } = new() { "-1", "-2", "-5", "-10", "-12", "-14.4" };
		public static List<string> ConfigOptions { get; } = Enum.GetNames(typeof(OpampConfigOptions)).ToList();

		//public static List<string> NegInputs { get; } = Enum.GetNames(typeof(OpampNegInputs)).ToList();
		//public static List<string> PosInputs { get; } = Enum.GetNames(typeof(OpampPosInputs)).ToList();
		//public static List<string> PosNegConnects { get; } = Enum.GetNames(typeof(OpampPosNegConnects)).ToList();
		//public static List<string> Feedbacks { get; } = Enum.GetNames(typeof(OpampFeedbacks)).ToList();
		//public static List<string> Loads { get; } = Enum.GetNames(typeof(LoadOptions)).ToList();
		//public static List<string> Psrrs { get; } = Enum.GetNames(typeof(PsrrOptions)).ToList();

		#region Properties
		internal QA430Info? MyWindow { get; set; } = null;

		public ImageSource ConfigImage { 
			get { 
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
		//these must all be initialized to not the initial values
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
		#endregion

		public QA430Model()
		{
			// set up a listener for when any property changes
			PropertyChanged += DoPropertyChanged;
		}

		~QA430Model()
		{
			PropertyChanged -= DoPropertyChanged;
		}

		/// <summary>
		/// high level start up QA430 controller and model
		/// initializes to config6a
		/// </summary>
		/// <returns>connected USB to QA430?</returns>
		internal static async Task<bool> BeginQA430Op()
		{
#if USEQA430
			var x = QA430.Qa430Usb.Singleton;
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
				var didStart = await model.SetDefaults();
				if (didStart)
				{
					var uu = new QA430Info();
					uu.Show();
					model.MyWindow = uu;
					ViewSettings.Singleton.MainVm.HasQA430 = true;
				}
			}

			return y;
#else
			return false;
#endif
		}

		internal static void EndQA430Op()
		{
			var x = QA430.Qa430Usb.Singleton;
			x?.Close(true);
			if (x?.QAModel.MyWindow != null)
			{
				x.QAModel.MyWindow.AllowClose = true;
				x.QAModel.MyWindow.Close();
				x.QAModel.MyWindow = null;
			}
		}

		internal async Task<bool> SetDefaults()
		{
			try
			{
				ShowRegisters("Beginning default");
				//Hw.ResetAllRelays();	// set the internal relays byte to zero
				EnableSupply = false;
				EnableCurrentSense = false;
				PosRailVoltage = "1.0";
				NegRailVoltage = "-1.0";
				UseFixedRails = false;
				await Hw.WaitForRelays();   // let the relays settle

				ShowRegisters("Voltage set");

				OpampNegInput = (short)OpampNegInputs.Open;
				OpampPosInput = (short)OpampPosInputs.Analyzer;
				OpampFeedback = (short)OpampFeedbacks.R4p99k;
				OpampPosNegConnect = (short)OpampPosNegConnects.Open;
				// don't allow analyzer to drive rails
				PsrrOption = (short)PsrrOptions.BothPsrrInputsGrounded;
				LoadOption = (short)LoadOptions.Open;
				await Hw.WaitForRelays();
				ShowRegisters("Relays set");    // sb E00C,11,11,1

				EnableSupply = true;
				await Hw.WaitForRelays();
				ShowRegisters("Voltage enabled");    // sb E00C,11,11,1
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in SetDefaults: {ex.Message}");
			}

			return false;
		}

		public void ShowRegisters(string also = "")
		{
			var r5 = Qa430Usb.Singleton.ReadRegister(5);
			var r8 = Qa430Usb.Singleton.ReadRegister(8);
			var r9 = Qa430Usb.Singleton.ReadRegister(9);
			var r10 = Qa430Usb.Singleton.ReadRegister(10);
			Debug.WriteLine(also + $":QA430 Registers: 5={r5:x} 8={r8:x} 9={r9:x} 10={r10:x}");
		}

		/// <summary>
		/// 60 dB gain for BW measurement
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig1()
		{
			// Select 4.99 ohm series
			OpampNegInput = (short)OpampNegInputs.GndTo4p99;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		/// <summary>
		/// 60 dB gain for CMRR measurement
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig2()
		{
			int targetGain = 20;
			if (targetGain == 0)
			{
				OpampFeedback = (short)OpampFeedbacks.Short;
				OpampNegInput = (short)OpampNegInputs.Open;
			}
			else if (targetGain == 20)
			{
				OpampFeedback = (short)OpampFeedbacks.R4p99k;
				OpampNegInput = (short)OpampNegInputs.GndTo499;
			}
			else if (targetGain == 60)
			{
				OpampFeedback = (short)OpampFeedbacks.R4p99k;
				OpampNegInput = (short)OpampNegInputs.GndTo4p99;
			}
			else
			{
				// Bad gain
				throw new Exception("Bad gain in SEtOpampConfig2()");
			}
			OpampPosNegConnect = (short)OpampPosNegConnects.Short;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		/// <summary>
		///  60 dB Gain for Offset and Noise Measurement
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig3a()
		{
			OpampNegInput = (short)OpampNegInputs.GndTo4p99;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Gnd;
		}

		/// <summary>
		/// 20 dB Gain for Offset and Noise Measurement
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig3b()
		{
			OpampNegInput = (short)OpampNegInputs.GndTo499;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Gnd;
		}

		/// <summary>
		/// 0 dB Gain for Offset and Noise Measurement
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig3c()
		{
			OpampNegInput = (short)OpampNegInputs.Open;
			OpampFeedback = (short)OpampFeedbacks.Short;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Gnd;
		}

		/// <summary>
		/// SigGain = 10, Dist Gain = 10
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig4a()
		{
			OpampNegInput = (short)OpampNegInputs.GndTo499;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		/// <summary>
		/// SigGain = 10, Dist Gain = 110
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig4b()
		{
			OpampNegInput = (short)OpampNegInputs.GndTo499;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.R49p9;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		/// <summary>
		/// Sig Gain = -10, Dist Gain = 100
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig5a()
		{
			OpampNegInput = (short)OpampNegInputs.AnalyzerTo499;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Gnd;
		}

		/// <summary>
		/// Sig Gain = -10, Dist Gain = 110
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig5b()
		{
			OpampNegInput = (short)OpampNegInputs.AnalyzerTo499;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.R49p9;
			OpampPosInput = (short)OpampPosInputs.Gnd;
		}

		/// <summary>
		/// Sig gain = 1, distortion gain = 1
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig6a()
		{
			OpampNegInput = (short)OpampNegInputs.Open;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		/// <summary>
		/// Sig gain = 1, distortion gain = 101
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig6b()
		{
			OpampNegInput = (short)OpampNegInputs.Open;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.R49p9;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		/// <summary>
		/// Sig gain = 1, distortion gain = 1
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig7a()
		{
			OpampNegInput = (short)OpampNegInputs.AnalyzerTo4p99k;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Gnd;
		}

		/// <summary>
		/// Sig gain = 1, distortion gain = 101
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig7b()
		{
			OpampNegInput = (short)OpampNegInputs.AnalyzerTo4p99k;
			OpampFeedback = (short)OpampFeedbacks.R4p99k;
			OpampPosNegConnect = (short)OpampPosNegConnects.R49p9;
			OpampPosInput = (short)OpampPosInputs.Gnd;
		}

		/// <summary>
		/// Input Linearity and Noise
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig8a()
		{
			OpampNegInput = (short)OpampNegInputs.Open;
			OpampFeedback = (short)OpampFeedbacks.Short;
			OpampPosNegConnect = (short)OpampPosNegConnects.Open;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		/// <summary>
		/// Input Linearity and Noise
		/// </summary>
		/// <param name="writeVal"></param>
		/// <returns></returns>
		internal void SetOpampConfig8b()
		{
			OpampNegInput = (short)OpampNegInputs.Open;
			OpampFeedback = (short)OpampFeedbacks.Short;
			OpampPosNegConnect = (short)OpampPosNegConnects.R49p9;
			OpampPosInput = (short)OpampPosInputs.Analyzer;
		}

		internal void ExecConfiguration(short iId)
		{
			switch (iId)
			{
				case (short)OpampConfigOptions.Config1:
					SetOpampConfig1();
					break;
				case (short)OpampConfigOptions.Config2:
					SetOpampConfig2();
					break;
				case (short)OpampConfigOptions.Config3a:
					SetOpampConfig3a();
					break;
				case (short)OpampConfigOptions.Config3b:
					SetOpampConfig3b();
					break;
				case (short)OpampConfigOptions.Config3c:
					SetOpampConfig3c();
					break;
				case (short)OpampConfigOptions.Config4a:
					SetOpampConfig4a();
					break;
				case (short)OpampConfigOptions.Config4b:
					SetOpampConfig4b();
					break;
				case (short)OpampConfigOptions.Config5a:
					SetOpampConfig5a();
					break;
				case (short)OpampConfigOptions.Config5b:
					SetOpampConfig5b();
					break;
				case (short)OpampConfigOptions.Config6a:
					SetOpampConfig6a();
					break;
				case (short)OpampConfigOptions.Config6b:
					SetOpampConfig6b();
					break;
				case (short)OpampConfigOptions.Config7a:
					SetOpampConfig7a();
					break;
				case (short)OpampConfigOptions.Config7b:
					SetOpampConfig7b();
					break;
				case (short)OpampConfigOptions.Config8a:
					SetOpampConfig8a();
					break;
				case (short)OpampConfigOptions.Config8b:
					SetOpampConfig8b();
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
				default:
					break;
			}
		}
	}
}
