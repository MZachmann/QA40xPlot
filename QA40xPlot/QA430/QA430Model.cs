#define USEQA430

using QA40xPlot.ViewModels;
using QA40xPlot.Views;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
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
		// also
		// +/- supply voltages
		// use fixed rails
		// current sense enable
		// supply enable

		public static List<string> NegInputs { get; } = new(){"Signal|499", "Gnd|4.99", "Open", "Signal|4.99K", "Gnd|499" };
		public static List<string> PosInputs { get; } = new() { "Signal", "Gnd", "Signal|100K" };
		public static List<string> PosNegConnects { get; } = new() { "Open", "49.9", "Short" };
		public static List<string> Feedbacks { get; } = new() { "Short", "4.99K" };
		public static List<string> Loads { get; } = new() { "Open", "2000", "604", "470" };
		public static List<string> Psrrs { get; } = new() { "None", "Hi Rail", "Low Rail", "Both Rails" };
		public static List<string> RailVoltages { get; } = new() { "1", "2", "5", "10", "12", "14.4" };
		public static List<string> NegRailVoltages { get; } = new() { "-1", "-2", "-5", "-10", "-12", "-14.4" };

		//public static List<string> NegInputs { get; } = Enum.GetNames(typeof(OpampNegInputs)).ToList();
		//public static List<string> PosInputs { get; } = Enum.GetNames(typeof(OpampPosInputs)).ToList();
		//public static List<string> PosNegConnects { get; } = Enum.GetNames(typeof(OpampPosNegConnects)).ToList();
		//public static List<string> Feedbacks { get; } = Enum.GetNames(typeof(OpampFeedbacks)).ToList();
		//public static List<string> Loads { get; } = Enum.GetNames(typeof(LoadOptions)).ToList();
		//public static List<string> Psrrs { get; } = Enum.GetNames(typeof(PsrrOptions)).ToList();

		#region Properties
		private QA430Info? MyWindow { get; set; } = null;

		//these must all be initialized to not the initial values
		private short _OpampNegInput;
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
		{	get
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
			if(x.IsOpen())
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
				if(didStart)
				{
					var uu = new QA430Info();
					uu.Show();
					model.MyWindow = uu;
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
			if(x?.QAModel.MyWindow != null)
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
				await Hw.WaitForRelays();	// let the relays settle

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
				default:
					break;
			}
		}
	}
}
