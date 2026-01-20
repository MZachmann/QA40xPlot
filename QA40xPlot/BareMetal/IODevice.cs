using QA40xPlot.Libraries;

namespace QA40xPlot.BareMetal
{
	/// <summary>
	/// this provides a way to abstract the REST/USB split effectively
	/// 
	/// </summary>
	public interface IODevice
	{
		public static IODevice IOFactory(string name)
		{
			switch (name)
			{
				case "USB": return new IODevUSB();
				case "REST": return new IODevREST();
				default: throw new ArgumentException($"Unknown device type: {name}");
			}
		}

		public string Name { get; } // the name of the io device
									// queries
		public ValueTask<bool> IsServerRunning();
		public ValueTask<bool> CheckDeviceConnected();
		// open and close
		public ValueTask<bool> IsOpen();
		public ValueTask<bool> Open();
		public ValueTask Close(bool onExit);

		// internal register stuff
		public ValueTask<double> GetDCVolts();
		public ValueTask<double> GetDCAmps();
		public ValueTask<double> GetTemperature();

		// properties. getters are not async every since we cache this stuff.
		public int GetInputRange();
		public int GetOutputRange();
		public uint GetSampleRate();
		public uint GetFftSize();
		public string GetWindowing();
		public OutputSources GetOutputSource();

		public ValueTask SetInputRange(int range);
		public ValueTask SetOutputRange(int range);
		public ValueTask SetSampleRate(uint range);
		public ValueTask SetFftSize(uint range);
		public ValueTask SetOutputSource(OutputSources source);
		public ValueTask SetWindowing(string windowing);

		// higher level methods
		public ValueTask<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation);
		public ValueTask<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq);
		public ValueTask<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq);
	}
}
