using QA40xPlot.Libraries;

namespace QA40xPlot.Data
{
    public class FrequencyResponseMeasurementSettings
    {
        public uint SampleRate { get; set; }
        public uint FftSize { get; set; }
        public Windowing WindowingFunction { get; set; }
        public E_GeneratorType GeneratorType { get; set; }
        public double GeneratorAmplitude { get; set; }
        public E_VoltageUnit GeneratorAmplitudeUnit { get; set;}
        public int SmoothDenominator { get; set; } = 48;
        public bool EnableLeftChannel { get; set; } = true;
        public bool EnableRightChannel { get; set; } = true;
        public bool RightChannelIsReference { get; set; }
        public uint Averages { get; set; } = 1;
        public uint FftResolution { get; set; } = 1;


        public FrequencyResponseMeasurementSettings Copy()
        {
            return (FrequencyResponseMeasurementSettings)MemberwiseClone();
        }
    }
}           
    