using Newtonsoft.Json;
using QA40xPlot.QA430;
using System.ComponentModel;

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
		public string SupplyTip { get => "Default 15V or (list of) values. Delimit + - with '|' (optional)"; }

		// simple ask if we have a qa430. don't bother raising property change
		// this is set when the start button is pushed
		[JsonIgnore]
		public bool HasQA430 { get; set; }

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
	}
}
