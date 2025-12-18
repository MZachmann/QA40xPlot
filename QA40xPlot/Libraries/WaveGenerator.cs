using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;

namespace QA40xPlot.Libraries
{
	public class WaveGenerator
	{
		public static WaveGenerator LeftWaves = new WaveGenerator();
		public static WaveGenerator RightWaves = new WaveGenerator();
		public static WaveGenerator TheWave(bool isLeft)
		{
			return isLeft ? LeftWaves : RightWaves;
		}

		public GenWaveform GenParams { get; private set; }
		public GenWaveform Gen2Params { get; private set; }
		public bool IsEnabled { get; set; }
		public WaveChannels Channels
		{
			get => GenParams.Channels;
			set => GenParams.Channels = value;
		}

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

		public static void SetGen2(bool isLeft, double freq, double volts, bool ison, string name = "Sine")
		{
			var single = TheWave(isLeft);
			SetParams(single.Gen2Params, freq, volts, ison);
			single.Gen2Params.Name = name;
		}

		public static void SetGen1(bool isLeft, double freq, double volts, bool ison, string name = "Sine")
		{
			var single = TheWave(isLeft);
			SetParams(single.GenParams, freq, volts, ison);
			single.GenParams.Name = name;
		}

		public static void SetChannels(bool isLeft, WaveChannels channels)
		{
			var single = TheWave(isLeft);
			single.Channels = channels;
		}

		public static void SetEnabled(bool isLeft, bool ison)
		{
			var single = TheWave(isLeft);
			single.IsEnabled = ison;
		}

		public static void Clear(bool isLeft)
		{
			var single = TheWave(isLeft);
			var vw = single;
			vw.IsEnabled = false;
			vw.GenParams.Enabled = false;
			vw.Gen2Params.Enabled = false;
		}

		public static (double[], double[]) GenerateBoth(uint sampleRate, uint sampleSize)
		{
			var dx = Generate(true, sampleRate, sampleSize);
			// if no right generator - duplicate
			if (!TheWave(false).IsEnabled)
				return (dx, dx);
			var dy = Generate(false, sampleRate, sampleSize);
			return (dx, dy);
		}

		public static (double[], double[]) GeneratePair(bool isLeft, uint sampleRate, uint sampleSize)
		{
			var single = TheWave(isLeft);
			var dx = Generate(isLeft, sampleRate, sampleSize);
			var how = single.Channels;
			double[] blank = [];
			if (how != WaveChannels.Both)
			{
				// if debug distortion is enabled, set the crosstalk channel to Addon%
				blank = new double[sampleSize];
				if (ViewSettings.AddonDistortion > 0)
				{
					blank = dx.Select(x => x * ViewSettings.AddonDistortion / 100).ToArray();
				}
			}
			switch (how)
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

		public static double[] Generate(bool isLeft, uint sampleRate, uint sampleSize)
		{
			var single = TheWave(isLeft);
			var vw = single;
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
