using Newtonsoft.Json;

namespace QA40xPlot.ViewModels
{
	// this add some stuff used by all the opamp tests
	public abstract class OpampViewModel : BaseViewModel
	{
		public static List<string> QaLoads { get; } = new List<string>()
		{
			"Open", "2000 Ω", "604 Ω", "470 Ω"
		};
		public static List<string> QaGains { get; } = new List<string>()
		{
			"1", "-1", "10", "-10"
		};
		[JsonIgnore]
		public string SupplyTip { get => "Enter supply values. May use | to delimit +-. Default 15V"; }

		// simple ask if we have a qa430. don't bother raising property change
		// this is set when the start button is pushed
		[JsonIgnore]
		public bool HasQA430 { get; set; }

		// when this is saved it shows the current settings
		private string _SupplySummary = string.Empty;
		// the value is set only when we load a configuration so parse it
		public string SupplySummary
		{
			get => _SupplySummary;
			set => SetProperty(ref _SupplySummary, value);
		}

		// when this is saved it shows the current settings
		private string _VoltSummary = string.Empty;
		// the value is set only when we load a configuration so parse it
		public string VoltSummary
		{
			get => _VoltSummary;
			set => SetProperty(ref _VoltSummary, value);
		}

		// when this is saved it shows the current settings
		private string _LoadSummary = string.Empty;
		// the value is set only when we load a configuration so parse it
		public string LoadSummary
		{
			get => _LoadSummary;
			set => SetProperty(ref _LoadSummary, value);
		}

		// when this is saved it shows the current settings
		private string _GainSummary = string.Empty;
		// the value is set only when we load a configuration so parse it
		public string GainSummary
		{
			get => _GainSummary;
			set => SetProperty(ref _GainSummary, value);
		}
	}
}
