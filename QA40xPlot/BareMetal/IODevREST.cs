using QA40xPlot.Libraries;


namespace QA40xPlot.BareMetal
{
	internal class IODevREST : IODevice
	{
		// we keep the locals so we can answer the Get method requests
		private uint _FftSize = 0;
		private int _OutputRange = 0;
		private int _Attenuation = 0;
		private uint _SampleRate = 0;
		private string _Windowing = "Hann";
		private OutputSources _OutputSource = OutputSources.Invalid; // default to sine

		public string Name => "REST";    // the name of the io device

		public ValueTask<bool> IsOpen()
		{
			return new ValueTask<bool>(QaREST.CheckDeviceConnected());
		}

		public async ValueTask<bool> CheckDeviceConnected()
		{
			return await QaREST.CheckDeviceConnected();
		}

		public ValueTask<bool> Open()
		{
			return new ValueTask<bool>(true);
		}

		public ValueTask Close(bool onExit)
		{
			return ValueTask.CompletedTask;
		}

		public ValueTask<uint> GetFftSize() { return new ValueTask<uint>(_FftSize); }

		public ValueTask<int> GetInputRange() { return new ValueTask<int>(_Attenuation); }

		public ValueTask<int> GetOutputRange() { return new ValueTask<int>(_OutputRange); }

		public ValueTask<uint> GetSampleRate() { return new ValueTask<uint>(_SampleRate); }

		public ValueTask<OutputSources> GetOutputSource() {  return new ValueTask<OutputSources>(_OutputSource); }

		public ValueTask<string> GetWindowing() { return new ValueTask<string>(_Windowing); }

		public ValueTask<bool> IsServerRunning()
		{
			return new ValueTask<bool>(QaREST.IsServerRunning());
		}

		public async ValueTask SetFftSize(uint range)
		{
			_FftSize = range;
			await Qa40x.SetBufferSize(range);
		}

		public async ValueTask SetInputRange(int range)
		{
			_Attenuation = range;
			await Qa40x.SetInputRange(range);
		}

		public ValueTask SetOutputRange(int range)
		{
			// this isn't a thing with REST afaik
			_OutputRange = range;
			return ValueTask.CompletedTask;
		}

		public async ValueTask SetSampleRate(uint range)
		{
			_SampleRate = range;
			await Qa40x.SetSampleRate(range);
		}

		public async ValueTask SetOutputSource(OutputSources source)
		{
			_OutputSource = source;
			await Qa40x.SetOutputSource(source);
		}


		public async ValueTask SetWindowing(string windowing)
		{
			_Windowing = windowing;
			await Qa40x.SetWindowing(windowing);
		}

		public async ValueTask<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			return await QaREST.DoAcquireUser(ct, dataLeft, getFreq);
		}

		public async ValueTask<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq)
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

		public async ValueTask<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			return await QaREST.InitializeDevice(sampleRate, fftsize, "Hann", attenuation);	// ignore whatever is passed in here for windowing
		}

	}
}