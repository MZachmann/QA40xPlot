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

		public uint GetFftSize() { return _FftSize; }

		public int GetInputRange() { return _Attenuation; }

		public int GetOutputRange() { return _OutputRange; }

		public uint GetSampleRate() { return _SampleRate; }

		public OutputSources GetOutputSource() { return _OutputSource; }

		public string GetWindowing() { return _Windowing; }

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
			return await QaREST.DoAcquireUser(ct, dataLeft, dataRight, getFreq);
		}

		public async ValueTask<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq)
		{
			var ffts = GetFftSize();
			var osource = GetOutputSource();
			var srate = GetSampleRate();
			var datapts = WaveGenerator.GeneratePair(srate, ffts);
			return await QaREST.DoAcquireUser(ct, datapts.Item1, datapts.Item2, getFreq);
		}

		public async ValueTask<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			return await QaREST.InitializeDevice(sampleRate, fftsize, "Hann", attenuation); // ignore whatever is passed in here for windowing
		}

	}
}