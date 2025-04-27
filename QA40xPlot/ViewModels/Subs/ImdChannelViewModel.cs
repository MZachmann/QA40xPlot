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
	public class ImdChannelViewModel : FloorViewModel
	{
		//public string ChannelId { get; set; }
		private double _Gen1F = 0;         // type of alert
		public double Gen1F
		{
			get => _Gen1F; 
			set => SetProperty(ref _Gen1F, value);
		}
		private double _Gen2F = 0;         // type of alert
		public double Gen2F
		{
			get => _Gen2F;
			set => SetProperty(ref _Gen2F, value);
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

		private ImdStepChannel? _MyStep = null;         // type of alert
		[JsonIgnore]
		public ImdStepChannel? MyStep
		{
			get => _MyStep;
			set => SetProperty(ref _MyStep, value);
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
		private double _ThdIndB;
		public double ThdIndB
		{
			get => _ThdIndB;
			set => SetProperty(ref _ThdIndB, value);
		}
		private double _ThdInPercent;
		public double ThdInPercent
		{
			get => _ThdInPercent;
			set => SetProperty(ref _ThdInPercent, value);
		}
		private double _NoiseFloordBV;
		public double NoiseFloordBV
		{
			get => _NoiseFloordBV;
			set => SetProperty(ref _NoiseFloordBV, value);
		}
		private bool _ShowDataPercents;
		public bool ShowDataPercents
		{
			get => _ShowDataPercents;
			set => SetProperty(ref _ShowDataPercents, value);
		}

		public ImdChannelViewModel() 
		{ 
		}

		public void CalculateChannelValues(ImdStepChannel step, double gen1f, double gen2f, bool showPercent)
		{
			MyStep = step;
			Gen1F = gen1f;
			Gen2F = gen2f;
			SNRatio = step.Snr_dB;
			ENOB = (SNRatio - 1.76) / 6.02;
			ThdIndB = step.Thd_dB;	// shown
			ThdInPercent = 100*Math.Pow(10, step.Thd_dB / 20);	// shown
			ShowDataPercents = showPercent;
			NoiseFloordBV = QaLibrary.ConvertVoltage(step.TotalNoiseFloor_V, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
		}
	}
}
