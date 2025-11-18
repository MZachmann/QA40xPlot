using Newtonsoft.Json;

namespace QA40xPlot.ViewModels
{
	public class ScopeInfoViewModel : FloorViewModel
	{
		[JsonIgnore]
		public bool IsLeft { get; set; } = false; // true = left, false = right

		private System.Windows.Media.Brush _BorderColor = System.Windows.Media.Brushes.Green;
		[JsonIgnore]
		public System.Windows.Media.Brush BorderColor
		{
			get => _BorderColor;
			set => SetProperty(ref _BorderColor, value);
		}

		private string _PlotFormat = "dBm"; // dBm, dBv, Volts, mVolts, uVolts
		[JsonIgnore]
		public string PlotFormat
		{
			get => _PlotFormat;
			set => SetProperty(ref _PlotFormat, value);
		}

		private double _Generator1Volts = 0;
		public double Generator1Volts
		{
			get => _Generator1Volts;
			set => SetProperty(ref _Generator1Volts, value);
		}

		private double _TotalVolts = 0;         // total signal level
		public double TotalVolts
		{
			get => _TotalVolts;
			set => SetProperty(ref _TotalVolts, value);
		}

		private double _Fundamental1Frequency = 0;
		public double Fundamental1Frequency
		{
			get => _Fundamental1Frequency;
			set => SetProperty(ref _Fundamental1Frequency, value);
		}

		private double _Fundamental1Volts = 0;
		public double Fundamental1Volts
		{
			get => _Fundamental1Volts;
			set => SetProperty(ref _Fundamental1Volts, value);
		}

		private double _Generator2Volts = 0;
		public double Generator2Volts
		{
			get => _Generator2Volts;
			set => SetProperty(ref _Generator2Volts, value);
		}

		private double _Fundamental2Frequency = 0;
		public double Fundamental2Frequency
		{
			get => _Fundamental2Frequency;
			set => SetProperty(ref _Fundamental2Frequency, value);
		}

		private double _Fundamental2Volts = 0;
		public double Fundamental2Volts
		{
			get => _Fundamental2Volts;
			set => SetProperty(ref _Fundamental2Volts, value);
		}

		private double _SNRatio = 0;
		public double SNRatio
		{
			get => _SNRatio;
			set => SetProperty(ref _SNRatio, value);
		}


		private double _MaxVolts = 0;
		public double MaxVolts
		{
			get => _MaxVolts;
			set => SetProperty(ref _MaxVolts, value);
		}


		private double _MinVolts = 0;
		public double MinVolts
		{
			get => _MinVolts;
			set => SetProperty(ref _MinVolts, value);
		}

		private double _PtPVolts = 0;
		public double PtPVolts
		{
			get => _PtPVolts;
			set => SetProperty(ref _PtPVolts, value);
		}

		public ScopeInfoViewModel()
		{
		}

	}
}
