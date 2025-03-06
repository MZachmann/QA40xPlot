using QA40xPlot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QA40xPlot.ViewModels
{
	public class ChannelViewModel : BaseViewModel
	{
		//public string ChannelId { get; set; }
		private double _FundamentalFrequency = 0;         // type of alert
		public double FundamentalFrequency
		{
			get => _FundamentalFrequency; 
			set => SetProperty(ref _FundamentalFrequency, value);
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

		private ThdFrequencyStepChannel? _MyStep = null;         // type of alert
		public ThdFrequencyStepChannel? MyStep
		{
			get => _MyStep;
			set => SetProperty(ref _MyStep, value);
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

		public ChannelViewModel() 
		{ 
		}

		public void CalculateChannelValues(ThdFrequencyStepChannel step, double Fundamental)
		{
			MyStep = step;
			FundamentalFrequency = Fundamental;
			SNRatio = step.Snr_dB;
			ENOB = (SNRatio - 1.76) / 6.02;
			ThdIndB = step.Thd_dB;
			ThdInPercent = 100*Math.Pow(10, step.Thd_dB / 20);
		}
	}
}
