using QA40xPlot.Libraries;
namespace QA40xPlot.Data
{
    public class ThdFrequencyMeasurementSettings
    {
        public uint SampleRate { get; set; }
        public uint FftSize { get; set; }
        public int InputRange { get; set; }
        public Windowing WindowingFunction { get; set; }
        public uint StartFrequency { get; set; }
        public uint EndFrequency { get; set; }
        public uint StepsPerOctave { get; set; }
        public E_GeneratorType GeneratorType { get; set; }
        public double GeneratorAmplitude { get; set; }
        public E_VoltageUnit GeneratorAmplitudeUnit { get; set; }
        public uint Averages { get; set; }
        public double Load { get; set; }       
        public double AmpOutputPower { get; set; }
        public double AmpOutputAmplitude { get; set; }      
        public E_VoltageUnit AmpOutputAmplitudeUnit { get; set; }
        public bool EnableLeftChannel { get; set; }
        public bool EnableRightChannel { get; set; }

        public ThdFrequencyMeasurementSettings Copy()
        {
            return (ThdFrequencyMeasurementSettings)MemberwiseClone();
        }
    }
}           
    