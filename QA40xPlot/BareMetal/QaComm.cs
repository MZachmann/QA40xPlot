using QA40x.BareMetal;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Data;

namespace QA40xPlot.BareMetal
{
	public class QaComm
	{
		private static IODevice MyIoDevice { get; set; } = new IODevUSB();

		public static async ValueTask Close(bool onExit)
		{
			await MyIoDevice.Close(onExit);
		}

		public static async ValueTask SetSampleRate(uint range)
		{
			await MyIoDevice.SetSampleRate(range);
		}

		public static async ValueTask SetInputRange(int range)
		{
			await MyIoDevice.SetInputRange(range);
		}

		public static async ValueTask SetOutputRange(int range)
		{
			await MyIoDevice.SetOutputRange(range);
		}
		public static async ValueTask SetFftSize(uint range)
		{
			await MyIoDevice.SetFftSize(range);
		}
		public static async ValueTask SetOutputSource(OutputSources range)
		{
			await MyIoDevice.SetOutputSource(range);
		}
		public static async ValueTask SetWindowing(string range)
		{
			await MyIoDevice.SetWindowing(range);
		}
		public static async ValueTask<int> GetInputRange()
		{
			return await MyIoDevice.GetInputRange();
		}
		public static async ValueTask<int> GetOutputRange()
		{
			return await MyIoDevice.GetOutputRange();
		}
		public static async ValueTask<uint> GetSampleRate()
		{
			return await MyIoDevice.GetSampleRate();
		}
		public static async ValueTask<uint> GetFftSize()
		{
			return await MyIoDevice.GetFftSize();
		}
		public static async ValueTask<string> GetWindowing()
		{
			return await MyIoDevice.GetWindowing();
		}
		public static async ValueTask<OutputSources> GetOutputSource()
		{
			return await MyIoDevice.GetOutputSource();
		}
		public static int GetPreBuffer()
		{
			return 16384;
		}
		public static int GetPostBuffer()
		{
			return 4096;
		}

		public static async Task<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			if (ViewSettings.IsUseREST)
				return await QaREST.DoAcquireUser(ct, dataLeft, getFreq);
			return await MyIoDevice.DoAcquireUser(averages, ct, dataLeft, dataRight, getFreq);
		}

		private static bool _WasInitialized = false;
		public static async Task<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			bool rslt = false;
			if (ViewSettings.IsUseREST)
			{
				// rest doesn't support much windowing but we don't use it anyway so...
				rslt = await QaREST.InitializeDevice(sampleRate, fftsize, "Hann", attenuation, !_WasInitialized);
			}
			else
				rslt = await MyIoDevice.InitializeDevice(sampleRate, fftsize, Windowing, attenuation);
			_WasInitialized = true;
			return rslt;
		}

		public static async Task<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq = true)
		{
			if(!ViewSettings.IsUseREST)
				return await MyIoDevice.DoAcquisitions(averages, ct, getFreq);
			else
			{
				var ffts = await GetFftSize();
				var datapt = new double[ffts];
				var osource = await GetOutputSource();
				var srate = await GetSampleRate();
				if (osource == OutputSources.Sine)
				{
					var gp1 = WaveGenerator.Singleton.GenParams;
					var gp2 = WaveGenerator.Singleton.Gen2Params;
					double dt = 1.0 / srate;
					if (gp1?.Enabled == true)
					{
						datapt = datapt.Select((x, index) => x + (gp1.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp1.Frequency * dt * index)).ToArray();
					}
					if (gp2?.Enabled == true)
					{
						datapt = datapt.Select((x, index) => x + (gp2.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp2.Frequency * dt * index)).ToArray();
					}
				}

				return await QaREST.DoAcquireUser(ct, datapt, getFreq);
			}
		}

	}
}
