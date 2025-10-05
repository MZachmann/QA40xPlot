using QA40xPlot.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
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
		internal enum LoadOptions : ushort { Open, R2000, R604, R330 }
		internal enum PsrrOptions : ushort { BothPsrrInputsGrounded, HiRailToAnalyzer, LowRailToAnalyzer, BothRailsToAnalyzer }

		public List<string> NegInputs { get; } = Enum.GetNames(typeof(OpampNegInputs)).ToList();
		public List<string> PosInputs { get; } = Enum.GetNames(typeof(OpampPosInputs)).ToList();
		public List<string> PosNegConnects { get; } = Enum.GetNames(typeof(OpampPosNegConnects)).ToList();
		public List<string> Feedbacks { get; } = Enum.GetNames(typeof(OpampFeedbacks)).ToList();
		public List<string> Loads { get; } = Enum.GetNames(typeof(LoadOptions)).ToList();
		public List<string> Psrrs { get; } = Enum.GetNames(typeof(PsrrOptions)).ToList();

		#region Properties

		//these must all be initialized to not the initial values
		private OpampNegInputs _OpampNegInput;
		public OpampNegInputs OpampNegInput
		{
			get => _OpampNegInput;
			set => SetProperty(ref _OpampNegInput, value);
		}
		private OpampPosInputs _OpampPosInput =  (OpampPosInputs)10;
		public OpampPosInputs OpampPosInput
		{
			get => _OpampPosInput;
			set => SetProperty(ref _OpampPosInput, value);
		}
		private OpampPosNegConnects _OpampPosNegConnect = (OpampPosNegConnects)10;
		public OpampPosNegConnects OpampPosNegConnect
		{
			get => _OpampPosNegConnect;
			set => SetProperty(ref _OpampPosNegConnect, value);
		}
		private OpampFeedbacks _OpampFeedback = (OpampFeedbacks)10;
		public OpampFeedbacks OpampFeedback
		{
			get => _OpampFeedback;
			set => SetProperty(ref _OpampFeedback, value);
		}
		private LoadOptions _Load = (LoadOptions)10;
		public LoadOptions LoadOption
		{
			get => _Load;
			set => SetProperty(ref _Load, value);
		}
		private PsrrOptions _Psrr = (PsrrOptions)10;
		public PsrrOptions PsrrOption
		{
			get => _Psrr;
			set => SetProperty(ref _Psrr, value);
		}
		public double _PosRailVoltage = 3.14159;
		public double PosRailVoltage
		{
			get => _PosRailVoltage;
			set => SetProperty(ref _PosRailVoltage, value);
		}
		public double _NegRailVoltage = -3.14159;
		public double NegRailVoltage
		{
			get => _NegRailVoltage;
			set => SetProperty(ref _NegRailVoltage, value);
		}
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
			// set up a listener for our events
			PropertyChanged += DoPropertyChanged;
		}

		 ~QA430Model()
		{
			PropertyChanged -= DoPropertyChanged;
		}

		internal async Task<bool> SetDefaults()
		{
			try
			{
				Hw.ResetAllRelays();
				EnableSupply = false;
				EnableCurrentSense = false;
				PosRailVoltage = 1.0;
				NegRailVoltage = -1.0;
				UseFixedRails = false;
				await Hw.WaitForRelays();

				Debug.WriteLine("Start default");
				ShowRegisters();

				OpampNegInput = OpampNegInputs.Open;
				OpampPosInput = OpampPosInputs.Analyzer;
				OpampFeedback = OpampFeedbacks.R4p99k;
				OpampPosNegConnect = OpampPosNegConnects.Open;
				// don't allow analyzer to drive rails
				PsrrOption = PsrrOptions.BothPsrrInputsGrounded;
				LoadOption = LoadOptions.Open;
				await Hw.WaitForRelays();
				ShowRegisters();    // sb E00C,11,11,1
				EnableCurrentSense = false;
				UseFixedRails = true;
				EnableSupply = true;

				Debug.WriteLine("End Default");
				await Hw.WaitForRelays();
				ShowRegisters();	// sb E00C,11,11,1
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in SetDefaults: {ex.Message}");
			}

			return false;
		}

		public void ShowRegisters()
		{
			var r5 = Qa430Usb.Singleton.ReadRegister(5);
			var r8 = Qa430Usb.Singleton.ReadRegister(8);
			var r9 = Qa430Usb.Singleton.ReadRegister(9);
			var r10 = Qa430Usb.Singleton.ReadRegister(10);
			Debug.WriteLine($"QA430 Registers: 5={r5:x} 8={r8:x} 9={r9:x} 10={r10:x}");
		}

		// this essentially handles the Set side of the properties
		// and adjusts the QA430 to match the property values
		private void DoPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "OpampNegInput":
					Hw.SetNegInput(OpampNegInput);
					break;
				case "OpampPosInput":
					Hw.SetPosInput(OpampPosInput);
					break;
				case "OpampPosNegConnect":
					Hw.SetPosNegConnect(OpampPosNegConnect);
					break;
				case "OpampFeedback":
					Hw.SetFeedback(OpampFeedback);
					break;
				case "LoadOption":
					Hw.SetLoad(LoadOption);
					break;
				case "PsrrOption":
					Hw.SetPsrr(PsrrOption);
					break;
				case "PosRailVoltage":
					Hw.SetPositiveRailVoltage(PosRailVoltage);
					break;
				case "NegRailVoltage":
					Hw.SetNegativeRailVoltage(NegRailVoltage);
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
