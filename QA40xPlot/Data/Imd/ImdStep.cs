using QA40xPlot.Libraries;
using Newtonsoft.Json;

namespace QA40xPlot.Data
{
	public class ImdStep
	{
		public double Gen1Freq { get; set; }
		public double Gen2Freq { get; set; }
		public double Gen1Volts { get; set; }
		public double Gen2Volts { get; set; }
		public ImdStepChannel Left { get; set; }
		public ImdStepChannel Right { get; set; }

		[JsonIgnore]
		public LeftRightFrequencySeries fftData { get; set; }
		[JsonIgnore]
		public LeftRightTimeSeries timeData { get; set; }

		public ImdStep()
		{
			Left = new ImdStepChannel();
			Right = new ImdStepChannel();
		}
	}
}
