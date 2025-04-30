using QA40x_BareMetal;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Data;

namespace QA40xPlot.BareMetal
{
	public class QaComm
	{
		private static int _CurrentInput = 42;
		private static uint _CurrentFftSize = 32768;
		private static uint _CurrentSampleRate = 48000;
		private static OutputSources _CurrentOutputSource = OutputSources.Off;

		public static async Task SetInputRange(int range)
		{
			_CurrentInput = range;
			if (ViewSettings.IsUseREST)
				await Qa40x.SetInputRange(range);
			else
				QaUsb.SetInputRange(range);
		}
		public static void SetOutputRange(int range)
		{
			if (!ViewSettings.IsUseREST)
				QaUsb.SetOutputRange(range);
		}

		public static int GetInputRange()
		{
			return _CurrentInput;
		}

		public static async Task SetOutputSource(OutputSources source)
		{
			_CurrentOutputSource = source;
			if (ViewSettings.IsUseREST)
				await Qa40x.SetOutputSource(source);
			else
				QaUsb.SetOutputSource(source);
			var x = 12;
		}

		public static void Close(bool onExit)
		{
			if (!ViewSettings.IsUseREST)
				QaUsb.Close(onExit);
		}

		public static async Task<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			if (ViewSettings.IsUseREST)
				return await QaLibrary.DoAcquireUser(ct, dataLeft, getFreq);
			return await QaUsb.DoAcquireUser(averages, ct, dataLeft, dataRight, getFreq);
		}

		private static bool _WasInitialized = false;
		public static async Task<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			_CurrentInput = attenuation;        // save this for getinputrange
			_CurrentFftSize = fftsize;
			_CurrentSampleRate = sampleRate;
			bool rslt = false;
			if (ViewSettings.IsUseREST)
			{

				rslt = await QaLibrary.InitializeDevice(sampleRate, fftsize, Windowing, attenuation, !_WasInitialized);
			}
			else
				rslt = QaUsb.InitializeDevice(sampleRate, fftsize, Windowing, attenuation);
			_WasInitialized = true;
			return rslt;
		}

		//public static void SetSampleRate(int rate)
		//{
		//	if (ViewSettings.Singleton.SettingsVm.UseREST)
		//		QaAnalyzer.SetSampleRate(rate);
		//	else
		//		Qa40x.SetSampleRate(rate);
		//}

		public static async Task<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq = true)
		{
			if(!ViewSettings.IsUseREST)
				return await QaUsb.DoAcquisitions(averages, ct, getFreq);
			else
			{
				var datapt = new double[_CurrentFftSize];
				if (_CurrentOutputSource == OutputSources.Sine)
				{
					var gp1 = WaveGenerator.Singleton.GenParams;
					var gp2 = WaveGenerator.Singleton.Gen2Params;
					double dt = 1.0 / _CurrentSampleRate;
					if (gp1?.Enabled == true)
					{
						datapt = datapt.Select((x, index) => x + (gp1.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp1.Frequency * dt * index)).ToArray();
					}
					if (gp2?.Enabled == true)
					{
						datapt = datapt.Select((x, index) => x + (gp2.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp2.Frequency * dt * index)).ToArray();
					}
				}

				return await QaLibrary.DoAcquireUser(ct, datapt, getFreq);
			}
		}

	}
}
