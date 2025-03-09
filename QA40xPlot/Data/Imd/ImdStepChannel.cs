using System.Collections.Generic;
using Newtonsoft.Json;

namespace QA40xPlot.Data
{
    public class ImdStepChannel
    {
        public double Fundamental1_V { get; set; }
        public double Fundamental1_dBV { get; set; }
		public double Fundamental2_V { get; set; }
		public double Fundamental2_dBV { get; set; }
		public double Gain_dB { get; set; }
        public List<HarmonicData> Harmonics { 
            get; 
            set; }
        public double Thd_Percent { get; set; }
		public double Thd_PercentN { get; set; }
		public double Thd_dB { get; set; }
        public double Thd_dBN { get; set; }
		public double Snr_dB { get; set; }
		public double D6Plus_dBV { get; set; }
        public double ThdPercent_D6plus { get; set; }
        public double ThdDbD6plus { get; set; }
        public double Power_Watt { get; set; }
        public double Average_NoiseFloor_V { get; set; }
        public double Average_NoiseFloor_dBV { get; set; }
        public double TotalNoiseFloor_V { get; set; }

		public ImdStepChannel() {
            Harmonics = [];
        }
    }
}           
    