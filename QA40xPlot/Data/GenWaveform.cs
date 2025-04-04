using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Data
{
	public class GenWaveform
	{
		public string Name { get; set; } = string.Empty;
		public double Freq { get; set; }
		public double Volts { get; set; }
	}

	public class GenWaveSample
	{
		public int SampleRate { get; set; }
		public int SampleSize { get; set; }
	}
}
