using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Diagnostics;
using System.Threading.Channels;

namespace QA40xPlot.BareMetal
{
	public class WaveGenerator
	{
		public GenWaveform GenParams { get; private set; }
		public GenWaveform Gen2Params { get; private set; }
		public bool IsEnabled { get; set; }
		public WaveChannels Channels { get => GenParams.Channels;
										set => GenParams.Channels = value;
										}

		public static WaveGenerator Singleton = new WaveGenerator();

		public WaveGenerator()
		{
			GenParams = new GenWaveform()
			{
				Name = "Sine",
				Frequency = 1000,
				FreqEnd = 20000,
				Voltage = 0.5,
				Enabled = false,
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
		private static void SetParams(GenWaveform gwf, double freq, double volts, bool ison, WaveChannels channels = WaveChannels.Both)
		{
			gwf.Name = "Sine";
			gwf.Voltage = volts;
			gwf.Frequency = freq;
			gwf.Enabled = ison;
			gwf.Channels = channels;
		}

		public static void SetGen2(double freq, double volts, bool ison, string name=  "Sine")
		{
			SetParams(WaveGenerator.Singleton.Gen2Params, freq, volts, ison);
			WaveGenerator.Singleton.Gen2Params.Name = name;
		}

		public static void SetGen1(double freq, double volts, bool ison, string name = "Sine")
		{
			SetParams(WaveGenerator.Singleton.GenParams, freq, volts, ison);
			WaveGenerator.Singleton.GenParams.Name = name;
		}

		public static void SetChannels(WaveChannels channels)
		{
			WaveGenerator.Singleton.Channels = channels;
		}

		public static void SetEnabled(bool ison)
		{
			WaveGenerator.Singleton.IsEnabled = ison;
		}

		public static void Clear()
		{
			var vw = WaveGenerator.Singleton;
			vw.IsEnabled = false;
			vw.GenParams.Enabled = false;
			vw.Gen2Params.Enabled = false;
		}

		public static (double[], double[]) GeneratePair(uint sampleRate, uint sampleSize)
		{
			var dx = Generate(sampleRate, sampleSize);
			var how = WaveGenerator.Singleton.Channels;
			double[] blank = [];
			if(how != WaveChannels.Both)
			{
				// if debug distortion is enabled, set the crosstalk channel to Addon%
				blank = new double[sampleSize];
				if(ViewSettings.AddonDistortion > 0)
				{
					blank = dx.Select(x => x * ViewSettings.AddonDistortion/100).ToArray();
				}
			}
			switch(how)
			{
				case WaveChannels.Left:
					return (dx, blank);
				case WaveChannels.Right:
					return (blank, dx);
				case WaveChannels.Both:
					return (dx, dx);
				default: // WaveChannels.Neither
					return (blank, blank);
			}
		}

		public static double[] Generate(uint sampleRate, uint sampleSize)
		{
			var vw = WaveGenerator.Singleton;
			var waveSample = new GenWaveSample()
			{
				SampleRate = (int)sampleRate,
				SampleSize = (int)sampleSize
			};

			double[] wave;
			if (vw.IsEnabled && (vw.GenParams.Enabled || vw.Gen2Params.Enabled))
			{
				GenWaveform[] waves = [];
				if (vw.GenParams.Enabled && vw.Gen2Params.Enabled)
					waves = [vw.GenParams, vw.Gen2Params];
				else if (vw.GenParams.Enabled)
					waves = [vw.GenParams];
				else
					waves = [vw.Gen2Params];
				//Debug.Assert(waves.Min(x=>x.Voltage) > 0, "Voltage is zero");
				//Debug.Assert(waves.Min(x=>x.Frequency) > 0, "Frequency is zero");
				wave = QaMath.CalculateWaveform(waves, waveSample);
			}
			else
			{
				wave = new double[waveSample.SampleSize];
			}
			return wave;
		}
	}
}
