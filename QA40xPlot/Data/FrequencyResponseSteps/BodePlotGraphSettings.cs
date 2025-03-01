
namespace QA40xPlot.Data
{
    public class BodePlotGraphSettings
    {
        public bool ShowGain { get; set; }
        public bool ShowPhase { get; set; }
        public bool ThickLines { get; set; }
        public bool ShowDataPoints { get; set; }
        public double GainTop { get; set; }
        public double GainBottom { get; set; }
        public double PhaseTop { get; set; }
        public double PhaseBottom { get; set; }
        public uint FrequencyRange_Start { get; set; }
        public uint FrequencyRange_End { get; set; }
        public bool Show1dBBandwidth { get; set; }
        public bool Show3dBBandwidth { get; set; }
    }
}           
    