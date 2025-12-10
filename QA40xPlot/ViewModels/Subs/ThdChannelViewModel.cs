using Newtonsoft.Json;
using QA40xPlot.Data;
using System.Windows;

namespace QA40xPlot.ViewModels
{
	public class ThdChannelViewModel : FloorViewModel
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

		private List<HarmonicData> _Harmonics = new List<HarmonicData>();         // type of alert
		public List<HarmonicData> Harmonics
		{
			get => _Harmonics;
			set => SetProperty(ref _Harmonics, value);
		}

		private double _GeneratorVolts = 0;         // type of alert
		public double GeneratorVolts
		{
			get => _GeneratorVolts;
			set => SetProperty(ref _GeneratorVolts, value);
		}

		private double _FundamentalFrequency = 0;         // type of alert
		public double FundamentalFrequency
		{
			get => _FundamentalFrequency;
			set => SetProperty(ref _FundamentalFrequency, value);
		}

		private double _FundamentalVolts = 0;         // type of alert
		public double FundamentalVolts
		{
			get => _FundamentalVolts;
			set => SetProperty(ref _FundamentalVolts, value);
		}

		private double _SNRatio = 0;         // type of alert
		public double SNRatio
		{
			get => _SNRatio;
			set => SetProperty(ref _SNRatio, value);
		}

		private double _SinaddB = 0;         // type of alert
		public double SinaddB
		{
			get => _SinaddB;
			set => SetProperty(ref _SinaddB, value);
		}

		private double _ENOB = 0;         // type of alert
		public double ENOB
		{
			get => _ENOB;
			set => SetProperty(ref _ENOB, value);
		}

		private string _NoiseFloorView = "0";
		[JsonIgnore]
		public string NoiseFloorView
		{
			get => _NoiseFloorView;
			set => SetProperty(ref _NoiseFloorView, value);
		}
		private string _AmplitudeView = "0";
		[JsonIgnore]
		public string AmplitudeView
		{
			get => _AmplitudeView;
			set => SetProperty(ref _AmplitudeView, value);
		}
		private double _ThdNInV;
		public double ThdNInV
		{
			get => _ThdNInV;
			set => SetProperty(ref _ThdNInV, value);
		}
		private double _TotalW;
		public double TotalW
		{
			get => _TotalW;
			set => SetProperty(ref _TotalW, value);
		}
		private double _TotalV;
		public double TotalV
		{
			get => _TotalV;
			set => SetProperty(ref _TotalV, value);
		}
		private double _GaindB;
		public double GaindB
		{
			get => _GaindB;
			set => SetProperty(ref _GaindB, value);
		}
		private double _ThdInV;
		public double ThdInV
		{
			get => _ThdInV;
			set => SetProperty(ref _ThdInV, value);
		}
		private double _ThdIndB;
		public double ThdIndB
		{
			get => _ThdIndB;
			set => SetProperty(ref _ThdIndB, value);
		}
		private double _ThdNIndB;
		public double ThdNIndB
		{
			get => _ThdNIndB;
			set => SetProperty(ref _ThdNIndB, value);
		}
		private double _ThdInPercent;
		public double ThdInPercent
		{
			get => _ThdInPercent;
			set => SetProperty(ref _ThdInPercent, value);
		}
		private double _ThdNInPercent;
		public double ThdNInPercent
		{
			get => _ThdNInPercent;
			set => SetProperty(ref _ThdNInPercent, value);
		}
		private double _NoiseFloorV;
		public double NoiseFloorV
		{
			get => _NoiseFloorV;
			set => SetProperty(ref _NoiseFloorV, value);
		}
		private double _NoiseFloordBV;
		public double NoiseFloordBV
		{
			get => _NoiseFloordBV;
			set => SetProperty(ref _NoiseFloordBV, value);
		}
		private double _NoiseFloorPct;
		public double NoiseFloorPct
		{
			get => _NoiseFloorPct;
			set => SetProperty(ref _NoiseFloorPct, value);
		}
		private bool _ShowDataPercents;
		public bool ShowDataPercents
		{
			get => _ShowDataPercents;
			set => SetProperty(ref _ShowDataPercents, value);
		}

		public ThdChannelViewModel()
		{
			Harmonics = new List<HarmonicData>();
			// just to remove null reference issues during initial display
			for (int i = 0; i < 10; i++)
				Harmonics.Add(new HarmonicData());
		}

		public void CalculateChannelValues(double Fundamental, bool showPercent)
		{
			FundamentalFrequency = Fundamental;
			//SNRatio = step.Snr_dB;
			//ENOB = (SNRatio - 1.76) / 6.02;
			//ThdInV = step.Total_V * step.Thd_Percent / 100;
			//ThdIndB = step.Thd_dB;
			//ThdInPercent = 100*Math.Pow(10, step.Thd_dB / 20);
			//ThdNInV = step.Total_V * step.Thd_PercentN / 100;
			//ShowDataPercents = showPercent;
			//NoiseFloordBV = QaLibrary.ConvertVoltage(step.TotalNoiseFloor_V, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			//NoiseFloorPct = 100 * step.TotalNoiseFloor_V / step.Total_V;
		}
	}
}
