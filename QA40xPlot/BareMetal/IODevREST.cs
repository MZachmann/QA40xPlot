using QA40xPlot.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.BareMetal
{
	internal class IODevREST : IODevice
	{
		private uint _FftSize = 0;
		private int _OutputRange = 0;
		private int _Attenuation = 0;
		private uint _SampleRate = 0;
		private string _Windowing = "Hann"; 
		
		ValueTask<bool> IODevice.CheckDeviceConnected()
		{
			throw new NotImplementedException();
		}

		ValueTask<bool> IODevice.Open()
		{
			throw new NotImplementedException();
		}

		ValueTask IODevice.Close(bool onExit)
		{
			throw new NotImplementedException();
		}

		ValueTask<uint> IODevice.GetFftSize() { return new ValueTask<uint>(_FftSize); }

		ValueTask<int> IODevice.GetInputRange() { return new ValueTask<int>(_Attenuation); }

		ValueTask<int> IODevice.GetOutputRange() { return new ValueTask<int>(_OutputRange); }

		ValueTask<uint> IODevice.GetSampleRate() { return new ValueTask<uint>(_SampleRate); }


		ValueTask<LeftRightSeries> IODevice.DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			throw new NotImplementedException();
		}

		ValueTask<LeftRightSeries> IODevice.DoAcquisitions(uint averages, CancellationToken ct, bool getFreq)
		{
			throw new NotImplementedException();
		}

		ValueTask<bool> IODevice.InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			throw new NotImplementedException();
		}

		ValueTask<bool> IODevice.IsServerRunning()
		{
			throw new NotImplementedException();
		}

		ValueTask IODevice.SetFftSize(uint range)
		{
			throw new NotImplementedException();
		}

		ValueTask IODevice.SetInputRange(int range)
		{
			throw new NotImplementedException();
		}

		ValueTask IODevice.SetOutputRange(int range)
		{
			throw new NotImplementedException();
		}

		ValueTask IODevice.SetSampleRate(uint range)
		{
			throw new NotImplementedException();
		}

		ValueTask<OutputSources> IODevice.GetOutputSource()
		{
			throw new NotImplementedException();
		}

		ValueTask IODevice.SetOutputSource(OutputSources source)
		{
			throw new NotImplementedException();
		}

		public ValueTask<string> GetWindowing()
		{
			throw new NotImplementedException();
		}

		public ValueTask SetWindowing(string windowing)
		{
			throw new NotImplementedException();
		}
	}
}