using QA40xPlot.Libraries;
using Newtonsoft.Json;

namespace QA40xPlot.Data
{
    public class ThdFrequencyStep
    {
        public double FundamentalFrequency { get; set; }
        public double GeneratorVoltage { get; set; }
		[JsonIgnore]
		public ThdFrequencyStepChannel Left {  get; set; }
		[JsonIgnore]
		public ThdFrequencyStepChannel Right { get; set; }

        public LeftRightFrequencySeries? fftData { get; set; }
        public LeftRightTimeSeries? timeData { get; set; }

        public ThdFrequencyStep() {
            FundamentalFrequency = 100;
            GeneratorVoltage = 1.0;
            Left = new ThdFrequencyStepChannel();
            Right = new ThdFrequencyStepChannel();
        }
    }
}           
    