using QA40xPlot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using QA40xPlot.Libraries;

namespace QA40xPlot.ViewModels
{
	public class ScopeInfoViewModel : FloorViewModel
	{
		[JsonIgnore]
		public bool IsLeft { get; set; } = false; // true = left, false = right

		private List<HarmonicData> _Harmonics = new List<HarmonicData>();         // type of alert
		public List<HarmonicData> Harmonics
		{
			get => _Harmonics;
			set => SetProperty(ref _Harmonics, value);
		}

		private System.Windows.Media.Brush _BorderColor = System.Windows.Media.Brushes.Green;
		[JsonIgnore]
		public System.Windows.Media.Brush BorderColor
		{
			get => _BorderColor;
			set => SetProperty(ref _BorderColor, value);
		}

		private double _Generator1Volts = 0;         // type of alert
		public double Generator1Volts
		{
			get => _Generator1Volts;
			set => SetProperty(ref _Generator1Volts, value);
		}

		private double _Fundamental1Frequency = 0;         // type of alert
		public double Fundamental1Frequency
		{
			get => _Fundamental1Frequency;
			set => SetProperty(ref _Fundamental1Frequency, value);
		}

		private double _Fundamental1Volts = 0;         // type of alert
		public double Fundamental1Volts
		{
			get => _Fundamental1Volts;
			set => SetProperty(ref _Fundamental1Volts, value);
		}

		private double _Generator2Volts = 0;         // type of alert
		public double Generator2Volts
		{
			get => _Generator2Volts;
			set => SetProperty(ref _Generator2Volts, value);
		}

		private double _Fundamental2Frequency = 0;         // type of alert
		public double Fundamental2Frequency
		{
			get => _Fundamental2Frequency;
			set => SetProperty(ref _Fundamental2Frequency, value);
		}

		private double _Fundamental2Volts = 0;         // type of alert
		public double Fundamental2Volts
		{
			get => _Fundamental2Volts;
			set => SetProperty(ref _Fundamental2Volts, value);
		}

		private double _SNRatio = 0;         // type of alert
		public double SNRatio
		{
			get => _SNRatio;
			set => SetProperty(ref _SNRatio, value);
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
		private string _Amplitude1View = "0";
		[JsonIgnore]
		public string Amplitude1View
		{
			get => _Amplitude1View;
			set => SetProperty(ref _Amplitude1View, value);
		}
		private string _Amplitude2View = "0";
		[JsonIgnore]
		public string Amplitude2View
		{
			get => _Amplitude2View;
			set => SetProperty(ref _Amplitude2View, value);
		}
		private string _AmplitudesView = "0";
		[JsonIgnore]
		public string AmplitudesView
		{
			get => _AmplitudesView;
			set => SetProperty(ref _AmplitudesView, value);
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
		private double _Gain1dB;
		public double Gain1dB
		{
			get => _Gain1dB;
			set => SetProperty(ref _Gain1dB, value);
		}
		private double _Gain2dB;
		public double Gain2dB
		{
			get => _Gain2dB;
			set => SetProperty(ref _Gain2dB, value);
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

		public ScopeInfoViewModel()
		{
		}

	}
}
