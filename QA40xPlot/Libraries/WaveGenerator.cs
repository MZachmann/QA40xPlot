using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using System.Diagnostics;
using System.Windows;
namespace QA40xPlot.Libraries
{
	public class WaveContainer
	{
		public static WaveContainer Singleton = new WaveContainer();

		public WaveGenerator LeftWaveGen = new WaveGenerator();
		public WaveGenerator RightWaveGen = new WaveGenerator();

		public WaveChannels Channels = WaveChannels.Left;

		public static WaveGenerator LeftWave { get => Singleton.LeftWaveGen; }
		public static WaveGenerator RightWave { get => Singleton.RightWaveGen; }
		public static WaveGenerator TheWave(bool isLeft)
		{
			return isLeft ? LeftWave : RightWave;
		}

		public static void SetChannels(WaveChannels channels)
		{
			WaveContainer.Singleton.Channels = channels;
		}

		public static void SetOff()
		{
			SetChannels(WaveChannels.Neither);
		}

		public static void SetMono()
		{
			SetChannels(WaveChannels.Left);
		}

		public static void SetStereo()
		{
			SetChannels(WaveChannels.Both);
		}

		// this determines which channels become output
		public static bool IsStereo()
		{
			return WaveContainer.Singleton.Channels == WaveChannels.Both;
		}

		/// <summary>
		/// this determines if the first generator is enabled in
		/// each of the WaveGenerator pair. This assumes no second oscillator (scope)
		/// </summary>
		/// <param name="bLeft">enable left</param>
		/// <param name="bRight">enable right</param>
		public static void SetEnabled(bool bLeft, bool bRight)
		{
			Singleton.LeftWaveGen.GenParams.Enabled = bLeft;
			Singleton.RightWaveGen.GenParams.Enabled = bRight;
		}

		/// <summary>
		/// read the enabled states
		/// </summary>
		/// <returns></returns>
		//public static (bool,bool) GetEnabled()
		//{
		//	var ab = Singleton.LeftWaveGen.GenParams.Enabled;
		//	var bb = Singleton.RightWaveGen.GenParams.Enabled;
		//	return (ab, bb);
		//}

	}

	public class WaveGenerator
	{
		public GenWaveform GenParams { get; private set; }
		public GenWaveform Gen2Params { get; private set; }

		public WaveGenerator()
		{
			GenParams = new GenWaveform()
			{
				Name = "Sine",
				Frequency = 1000,
				FreqEnd = 20000,
				Voltage = 0.5,
				Enabled = false,
				WaveFile = string.Empty,
			};
			Gen2Params = new GenWaveform()
			{
				Name = "Sine",
				Frequency = 1000,
				FreqEnd = 20000,
				Voltage = 0.5,
				Enabled = false,
				WaveFile = string.Empty,
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
			gwf.WaveFile = string.Empty;
		}

		public static void SetGen2(bool isLeft, double freq, double volts, bool ison, string name = "Sine")
		{
			var single = WaveContainer.TheWave(isLeft);
			SetParams(single.Gen2Params, freq, volts, ison);
			single.Gen2Params.Name = name;
		}

		public static void SetGen1(bool isLeft, double freq, double volts, bool ison, string name = "Sine")
		{
			var single = WaveContainer.TheWave(isLeft);
			SetParams(single.GenParams, freq, volts, ison);
			single.GenParams.Name = name;
		}

		public static void SetWaveFile(bool isLeft, string fileName)
		{
			var single = WaveContainer.TheWave(isLeft);
			single.GenParams.WaveFile = fileName;
		}

		public static void Clear(bool isLeft)
		{
			var vw = WaveContainer.TheWave(isLeft);
			// turn off both waveforms
			vw.GenParams.Enabled = false;
			vw.Gen2Params.Enabled = false;
		}

		public static (double[], double[]) GenerateBoth(uint sampleRate, uint sampleSize)
		{
			var channels = WaveContainer.Singleton.Channels;
			switch(channels)
			{
				case WaveChannels.Neither:
					var blank = new double[sampleSize];
					return (blank, blank);
				case WaveChannels.Left:
					{
						var dx = Generate(true, sampleRate, sampleSize);
						return (dx, dx);
					}
				case WaveChannels.Right:
					{
						var dx = Generate(false, sampleRate, sampleSize);
						return (dx, dx);
					}
				case WaveChannels.Both:
					{
						var dx = Generate(true, sampleRate, sampleSize);
						var dy = Generate(false, sampleRate, sampleSize);
						return (dx, dy);
					}
				default:
					break;
			}
			var black = new double[sampleSize];
			return (black, black);
		}

		public static double[] Generate(bool isLeft, uint sampleRate, uint sampleSize)
		{
			var single = WaveContainer.TheWave(isLeft);
			var vw = single;
			var waveSample = new GenWaveSample()
			{
				SampleRate = sampleRate,
				SampleSize = sampleSize
			};

			double[] wave;
			if (vw.GenParams.Enabled || vw.Gen2Params.Enabled)
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
				if(vw.GenParams.Name == "WaveFile")
				{
					wave = ReadWaveFile(vw.GenParams.WaveFile, sampleRate, sampleSize, vw.GenParams.Voltage);
				}
				else
				{
					wave = QaMath.CalculateWaveform(waves, waveSample);
				}
			}
			else
			{
				wave = new double[waveSample.SampleSize];
			}
			return wave;
		}

		public static string FindWaveFileName(string initial)
		{
			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
			{
				FileName = initial, // Default file name
				DefaultExt = ".wav", // Default file extension
				Filter = "Wave files|*.wav|All files|*.*"  // Filter files by extension
			};

			// Show save file dialog box
			bool? result = openFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true )
			{
				// open document
				return openFileDialog.FileName;
			}
			return string.Empty;
		}

		public struct WaveCache
		{
			string name;
			uint samplerate;
			uint samplesize;
			double[] sampledata;

			public double[]? GetCache(string wave, uint rate, uint sizes)
			{
				if(wave == name && rate == samplerate && sizes == samplesize)
				{
					return sampledata;
				}
				name = wave;
				samplerate = rate; 
				samplesize = sizes;
				return null;
			}
			public void SetCache(double[] newdata)
			{
				sampledata = newdata;
			}

		}

		// read a wave file and return the first channel contents as a double array
		private static double[] CreateWaveArray(string waveName, uint sampleRate, uint sampleSize)
		{
			double[] outdata = [];
			try
			{
				List<float> outstream = new();
				using (var wavFileReader = new WaveFileReader(waveName))
				{
					var resampler = new WdlResamplingSampleProvider(wavFileReader.ToSampleProvider(), (int)sampleRate);
					var monoSource = resampler.ToMono(1, 0);    // ignore right channel if there is one
					{
						float[] dataOutput = new float[1024];
						while (true)
						{
							int dataRead = monoSource.Read(dataOutput, 0, dataOutput.Length);
							if (dataRead == 0)
								break;
							if(dataRead == dataOutput.Length)
								outstream.AddRange(dataOutput);
							else
								outstream.AddRange(dataOutput.Take(dataRead));
						}

						outdata = outstream.Select(x => (double)x).ToArray(); // This is raw PCM data
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Wave loader error:", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// if undersize, zero-pad to longer
			if (outdata.Length < sampleSize)
			{
				var u = outdata.ToList();
				u = u.Concat(new double[sampleSize - outdata.Length]).ToList();
				outdata = u.ToArray();
			}
			// shrink if oversize
			if (outdata.Length > sampleSize)
			{
				outdata = outdata.Take((int)sampleSize).ToArray();
			}
			return outdata;
		}

		public static WaveCache _waveCache;

		public static double[] ReadWaveFile(string waveName, uint sampleRate, uint sampleSize, double vMax = 1.0)
		{
			double[] outdata = [0];
			var otd = _waveCache.GetCache(waveName, sampleRate, sampleSize);
			if (otd != null)
				outdata = otd;
			else
			{
				outdata = CreateWaveArray(waveName, sampleRate, sampleSize);
				_waveCache.SetCache(outdata);
			}

			// scale to the output RMS voltage
			var mx = vMax * Math.Sqrt(2) / Math.Max(0.01, outdata.Max(Math.Abs));
			var vout = outdata.Select(x => x * mx).ToArray();
			return vout;
		}

		public static void WriteWaveFile(string waveName, uint sampleRate, uint sampleSize, double[] waveData)
		{
			try
			{
				List<float> outstream = new();
				WaveFormat waveFormat = new WaveFormat((int)sampleRate, 32, 1);
				using (var wavFileWriter = new WaveFileWriter(waveName, waveFormat))
				{
					var ada = waveData.Select(x => (Int32)x).ToArray();
					byte[] byteArray = new byte[ada.Length * sizeof(Int32)];
					Buffer.BlockCopy(ada, 0, byteArray, 0, byteArray.Length);
					wavFileWriter.Write(byteArray, 0, byteArray.Length);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Wave loader error:", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
	}

}
