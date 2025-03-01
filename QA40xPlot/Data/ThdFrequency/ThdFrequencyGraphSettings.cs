
namespace QA40xPlot.Data
{
    public class ThdFrequencyGraphSettings
    {
        public E_ThdFreq_GraphType GraphType { get; set; }         
        public bool ShowLeftChannel { get; set; }
        public bool ShowRightChannel { get; set; }
        public bool ShowTHD {  get; set; }
        public bool ShowMagnitude { get; set; }
        public bool ShowD2 { get; set; }
        public bool ShowD3 { get; set; }
        public bool ShowD4 { get; set; }
        public bool ShowD5 { get; set; }
        public bool ShowD6 { get; set; }
        public bool ShowNoiseFloor { get; set; }
        public bool ThickLines { get; set; }
        public bool ShowDataPoints { get; set; }
        public double DbRangeTop { get; set; }
        public double DbRangeBottom { get; set; }
        public double D_PercentTop { get; set; }
        public double D_PercentBottom { get;set; }
        public uint FrequencyRange_Start { get; set; }
        public uint FrequencyRange_End { get; set; }

    }
}           
    