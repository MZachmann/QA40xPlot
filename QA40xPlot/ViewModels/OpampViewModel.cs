using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QA40xPlot.QA430;
using System.ComponentModel;
using System.Windows.Media.Media3D;

namespace QA40xPlot.ViewModels
{
	// this add some stuff used by all the opamp tests
	public abstract class OpampViewModel : BaseViewModel
	{
		[JsonIgnore]
		public RelayCommand SelectAll { get => new RelayCommand(DoSelectAll); }
		[JsonIgnore]
		public RelayCommand SelectNone { get => new RelayCommand(DoSelectNone); }

		public static List<string> QaLoads { get; } = new List<string>()
		{
			"Open", "2000 Ω", "604 Ω", "470 Ω"
		};
		public static List<string> QaGains { get; } = new List<string>()
		{
			"1", "-1", "10", "-10"
		};
		[JsonIgnore]
		public string SupplyTip { get => "Default 15V or (list of) values. Delimit + - with '|' (optional)"; }

		// simple ask if we have a qa430. don't bother raising property change
		// this is set when the start button is pushed
		[JsonIgnore]
		public bool HasQA430 { get; set; }

		//we're not using those so don't save it... always false iow
		private bool _DeembedDistortion = false;
		[JsonIgnore]
		public bool DeembedDistortion
		{
			get => _DeembedDistortion;
			set => SetProperty(ref _DeembedDistortion, value);
		}

		private bool _UseHighDistortion = true;
		public bool UseHighDistortion
		{
			get => _UseHighDistortion;
			set => SetProperty(ref _UseHighDistortion, value);
		}

		// when this is saved it shows the current settings
		private string _SupplySummary = "15";
		public string SupplySummary
		{
			get => _SupplySummary;
			set => SetProperty(ref _SupplySummary, value);
		}

		// when this is saved it shows the current settings
		private string _VoltSummary = "0.1";
		public string VoltSummary
		{
			get => _VoltSummary;
			set => SetProperty(ref _VoltSummary, value);
		}

		private string _FreqSummary = "1000";
		public string FreqSummary
		{
			get => _FreqSummary;
			set => SetProperty(ref _FreqSummary, value);
		}

		// when this is saved it shows the current settings
		private string _LoadSummary = "Open";
		public string LoadSummary
		{
			get => _LoadSummary;
			set => SetProperty(ref _LoadSummary, value);
		}

		// when this is saved it shows the current settings
		private string _GainSummary = "1";
		public string GainSummary
		{
			get => _GainSummary;
			set => SetProperty(ref _GainSummary, value);
		}

		// this is version specific but needed for data parsing
		public int _SweepColumnCount = 13;	// this never changes but can be set on loads
		public int SweepColumnCount
		{
			get => _SweepColumnCount;
			set => SetProperty(ref _SweepColumnCount, value);
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
		private bool _ShowNoiseFloor;
		public bool ShowNoiseFloor
		{
			get => _ShowNoiseFloor;
			set => SetProperty(ref _ShowNoiseFloor, value);
		}
		private bool _ShowD2;
		public bool ShowD2
		{
			get => _ShowD2;
			set => SetProperty(ref _ShowD2, value);
		}
		private bool _ShowD3;
		public bool ShowD3
		{
			get => _ShowD3;
			set => SetProperty(ref _ShowD3, value);
		}
		private bool _ShowD4;
		public bool ShowD4
		{
			get => _ShowD4;
			set => SetProperty(ref _ShowD4, value);
		}
		private bool _ShowD5;
		public bool ShowD5
		{
			get => _ShowD5;
			set => SetProperty(ref _ShowD5, value);
		}
		private bool _ShowD6;
		public bool ShowD6
		{
			get => _ShowD6;
			set => SetProperty(ref _ShowD6, value);
		}


		public static void CheckQA430(OpampViewModel ovm)
		{
			ovm.HasQA430 = QA430Model.BeginQA430Op();
		}

		// the property change is used to trigger repaints of the graph
		protected void OpampPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "CheckQA430":
					CheckQA430(this);
					RaisePropertyChanged("HasQA430");
					break;
			}
		}

		// create variale list based on qa430 settings
		public static List<AcquireStep> EnumerateVariables(OpampViewModel vm)
		{
			var variables = new List<AcquireStep>();
			// now do the measurement stuff
			try
			{
				// enumerate the sweeps we are going to do
				var step = new AcquireStep() { Cfg = "Config6b", Load = QA430Model.LoadOptions.Open, Gain = 1, Distgain = 101, SupplyP = 15, SupplyN = 15 };    // unity 6b with 101 dist gain
				if( !vm.HasQA430 )
				{
					step = new AcquireStep() { Cfg = "", Load = QA430Model.LoadOptions.Open, Gain = 1, Distgain = 1, SupplyP = 15, SupplyN = 15 };    // unity 6a with 1 dist gain
				}
				else if( !vm.UseHighDistortion)
				{
					step = new AcquireStep() { Cfg = "Config6a", Load = QA430Model.LoadOptions.Open, Gain = 1, Distgain = 1, SupplyP = 15, SupplyN = 15 };    // unity 6a with 1 dist gain
				}
				variables.Add(step);

				QA430Model? model430 = vm.HasQA430 ? Qa430Usb.Singleton?.QAModel : null;
				if (model430 != null)
				{
					variables = model430.ExpandLoadOptions(variables, vm.LoadSummary) ?? variables;
					variables = model430.ExpandGainOptions(variables, vm.GainSummary, vm.UseHighDistortion) ?? variables;
					variables = model430.ExpandSupplyOptions(variables, vm.SupplySummary) ?? variables;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("EnumerateVariables exception: " + ex.Message);
			}
			return variables;
		}

		public async Task<string> ExecuteModel(AcquireStep myConfig, string lastCfg)
		{
			QA430Model? model = HasQA430 ? Qa430Usb.Singleton?.QAModel : null;
			if (model != null)
			{
				if (myConfig.Cfg != lastCfg && myConfig.Cfg.Length > 0)
				{
					model.SetOpampConfig(myConfig.Cfg);
					lastCfg = myConfig.Cfg;
				}
				model.LoadOption = (short)myConfig.Load;
				if (myConfig.SupplyP < 15)
				{
					model.NegRailVoltage = (-myConfig.SupplyN).ToString();
					model.PosRailVoltage = myConfig.SupplyP.ToString();
					model.UseFixedRails = false;
				}
				else
					model.UseFixedRails = true;
				// now that the QA430 relays are set, wait a bit...
				await model.WaitForQA430Relays();
			}
			return lastCfg;
		}

		protected void DoSelectAll()
		{
			DoSelection(true);
		}

		protected void DoSelectNone()
		{
			DoSelection(false);
		}

		protected void DoSelection(bool isOn)
		{
			ShowTHD = isOn;
			ShowTHDN = isOn;
			ShowMagnitude = isOn;
			//ShowPhase = isOn;	// not yet implemented so don't enable it
			ShowNoise = isOn;
			ShowNoiseFloor = isOn;
			ShowD2 = isOn;
			ShowD3 = isOn;
			ShowD4 = isOn;
			ShowD5 = isOn;
			ShowD6 = isOn;
		}
	}
}
