using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Data
{
	public class GenWaveform
	{
		public string Name { get; set; } = string.Empty;	// the type of waveform
		public double Frequency { get; set; }
		public double FreqEnd { get; set; }
		public double Voltage { get; set; }
		public bool Enabled { get; set; }
	}

	public class GenWaveSample
	{
		public int SampleRate { get; set; }
		public int SampleSize { get; set; }
	}
}
