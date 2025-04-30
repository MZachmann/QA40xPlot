using QA40xPlot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.BareMetal
{
	public class WaveGenerator
	{
		public GenWaveform GenParams { get; private set; }
		public GenWaveform Gen2Params { get; private set; }

		public static WaveGenerator Singleton = new WaveGenerator();

		public WaveGenerator()
		{
			GenParams = new GenWaveform()
			{
				Name = "Sine",
				Frequency = 1000,
				FreqEnd = 20000,
				Voltage = 0.5,
				Enabled = false
			};
			Gen2Params = new GenWaveform()
			{
				Name = "Sine",
				Frequency = 1000,
				FreqEnd = 20000,
				Voltage = 0.5,
				Enabled = false
			};
		}

		/// <summary>
		/// set a waveform to sine with parameters as given
		/// </summary>
		/// <param name="gwf">the genwaveform</param>
		/// <param name="freq"></param>
		/// <param name="volts"></param>
		/// <param name="ison"></param>
		private static void SetParams(GenWaveform gwf, double freq, double volts, bool ison)
		{
			gwf.Name = "Sine";
			gwf.Voltage = volts;
			gwf.Frequency = freq;
			gwf.Enabled = ison;
		}


		public static void SetGen2(double freq, double volts, bool ison)
		{
			SetParams(WaveGenerator.Singleton.Gen2Params, freq, volts, ison);
		}

		public static void SetGen1(double freq, double volts, bool ison)
		{
			SetParams(WaveGenerator.Singleton.GenParams, freq, volts, ison);
		}

	}
}
