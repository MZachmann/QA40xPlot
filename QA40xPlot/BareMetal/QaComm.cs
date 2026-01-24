using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Diagnostics;

namespace QA40xPlot.BareMetal
{
	public class QaComm
	{
		public static IODevice MyIoDevice { get; private set; } = new IODevUSB();

		public static void SetIODevice(string name)
		{
			if (MyIoDevice.Name != name)
			{
				Debug.WriteLine($"Changing IO device from {MyIoDevice.Name} to {name}");
				MyIoDevice.Close(true);
				MyIoDevice = IODevice.IOFactory(name);
			}
		}

		/// <summary>
		/// is the hardware device connected to the usb bus?
		/// </summary>
		/// <returns></returns>
		public static async ValueTask<bool> CheckDeviceConnected()
		{
			return await MyIoDevice.CheckDeviceConnected();
		}

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
			var ubend = MyIoDevice.SetInputRange(range);
			return;
		}

		public static async ValueTask SetOutputRange(int range)
		{
			await MyIoDevice.SetOutputRange(range);
		}
		public static async ValueTask<double> GetDCVoltage()
		{
			return await MyIoDevice.GetDCVolts();
		}
		public static async ValueTask<double> GetDCCurrent()
		{
			return await MyIoDevice.GetDCAmps();
		}
		public static async ValueTask<double> GetTemperature()
		{
			return await MyIoDevice.GetTemperature();
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
		public static int GetInputRange()
		{
			return MyIoDevice.GetInputRange();
		}
		public static int GetOutputRange()
		{
			return MyIoDevice.GetOutputRange();
		}
		public static uint GetSampleRate()
		{
			return MyIoDevice.GetSampleRate();
		}
		public static uint GetFftSize()
		{
			return MyIoDevice.GetFftSize();
		}
		public static string GetWindowing()
		{
			return MyIoDevice.GetWindowing();
		}
		public static OutputSources GetOutputSource()
		{
			return MyIoDevice.GetOutputSource();
		}
		public static int GetPreBuffer()
		{
			return 2048;
		}
		public static int GetPostBuffer()
		{
			return 2048;
		}

		public static async Task<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			return await MyIoDevice.DoAcquireUser(averages, ct, dataLeft, dataRight, getFreq);
		}

		public static async Task<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			bool rslt = false;
			rslt = await MyIoDevice.InitializeDevice(sampleRate, fftsize, Windowing, attenuation);
			if (rslt)
			{
				ViewSettings.Singleton.MainVm.Temperature = await MyIoDevice.GetTemperature();
				ViewSettings.Singleton.MainVm.DCSupplyVoltage = await MyIoDevice.GetDCVolts();
				ViewSettings.Singleton.MainVm.DCSupplyCurrent = await MyIoDevice.GetDCAmps();
			}
			return rslt;
		}

		public static async Task<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq = true)
		{
			return await MyIoDevice.DoAcquisitions(averages, ct, getFreq);
		}

	}
}
