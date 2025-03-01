
using QA40xPlot.Libraries;
using System.Text.Json.Serialization;

namespace QA40xPlot.Data
{
    public class BodePlotStep
    {
        public double FundamentalFrequency { get; set; }
        public double GeneratorVoltage { get; set; }
        public double MeasuredAmplitude_dBV { get; set; }

        public double ReferenceAmplitude_dBV { get; set; }

        public double Gain { get; set; }

        public double Phase_Degree { get; set; }


        [JsonIgnore]
        public LeftRightTimeSeries TimeData { get; set; }

        public BodePlotStep() {
            
        }
    }
}           
    