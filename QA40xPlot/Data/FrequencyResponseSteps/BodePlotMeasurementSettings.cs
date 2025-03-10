
using QA40xPlot.Libraries;

namespace QA40xPlot.Data
{
    public class BodePlotMeasurementSettings
    {
        public uint SampleRate { get; set; }
        public uint FftSize { get; set; }
        public E_GeneratorType GeneratorType { get; set; }
        public double GeneratorAmplitude { get; set; }
        public E_VoltageUnit GeneratorAmplitudeUnit { get; set;}
        public uint StartFrequency { get; set; }
        public uint EndFrequency { get; set; }
        public uint StepsPerOctave { get; set; }


        public BodePlotMeasurementSettings Copy()
        {
            return (BodePlotMeasurementSettings)MemberwiseClone();
        }
    }
}           
    