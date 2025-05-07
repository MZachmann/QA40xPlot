using QA40x.BareMetal;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Diagnostics;

namespace QA40xPlot.BareMetal
{
	public class IODevUSB : IODevice
	{
		// convert from value to register setting
		private static readonly Dictionary<int, int> _Output2Reg = new() { { 18, 3 }, { 8, 2 }, { -2, 1 }, { -12, 0 } };
		private static readonly Dictionary<int, int> _Input2Reg = new() { { 0, 0 }, { 6, 1 }, { 12, 2 }, { 18, 3 }, { 24, 4 }, { 30, 5 }, { 36, 6 }, { 42, 7 } };
		private static readonly Dictionary<int, int> _Samplerate2Reg = new() { { 48000, 0 }, { 96000, 1 }, { 192000, 2 }, { 384000, 3 } }; // 384K?
		// sweep parameters
		private uint _FftSize = (uint)_InvalidInt;
		private int _OutputRange = _InvalidInt;
		private int _Attenuation = _InvalidInt;
		private uint _SampleRate = (uint)_InvalidInt;
		private OutputSources _OutputSource = OutputSources.Invalid; // default to sine
		private string _Windowing = "Hann";
		//
		private QaUsb _UsbApi = new();		// our local Usb controller

		private static int _InvalidInt = 1232;

		void SetParametersEmpty()
		{
			_FftSize = (uint)_InvalidInt;
			_OutputRange = _InvalidInt;
			_Attenuation = _InvalidInt;
			_SampleRate = (uint)_InvalidInt;
			_Windowing = "Hann";
			_OutputSource = OutputSources.Invalid; // default to sine
		}

		public ValueTask<bool> CheckDeviceConnected()
		{
			return new ValueTask<bool>(_UsbApi.VerifyConnection());
		}

		public ValueTask<bool> IsServerRunning()
		{
			return new ValueTask<bool>(true);   // always...
		}

		public ValueTask<bool> Open()
		{
			if (!_UsbApi.IsOpen())
			{
				_UsbApi.Open();
				SetParametersEmpty();       // set to invalid values
			}
			return new ValueTask<bool>(true);
		}

		public ValueTask Close(bool onExit)
		{
			if(_UsbApi != null)
			{
				_UsbApi.Close(onExit);
			}
			return new ValueTask();
		}

		public ValueTask<uint> GetFftSize() { return new ValueTask<uint>(_FftSize); }

		public ValueTask<int> GetInputRange() { return new ValueTask<int>(_Attenuation); }

		public ValueTask<int> GetOutputRange() { return new ValueTask<int>(_OutputRange); }

		public ValueTask<uint> GetSampleRate() { return new ValueTask<uint>(_SampleRate); }

		public ValueTask<OutputSources> GetOutputSource() { return new ValueTask<OutputSources>(_OutputSource); }

		public ValueTask SetFftSize(uint range)
		{
			// here we just store this for the other clients
			_FftSize = range;
			return new ValueTask();
		}

		public ValueTask SetInputRange(int range)
		{
			if (range == _Attenuation)
				return new ValueTask();

			_Attenuation = range;
			Debug.Assert(!ViewSettings.IsUseREST, "SetupInput using REST.");
			if (!_Input2Reg.TryGetValue(range, out int val))
				throw new ArgumentException("Invalid input gain value.");
			_UsbApi.WriteRegister(5, (byte)val);
			Debug.WriteLine($"Attenuation set to {range} dB with {val}");
			return new ValueTask();
		}

		public ValueTask SetOutputRange(int range)
		{
			if (range == _OutputRange)
				return new ValueTask();
			_OutputRange = range;
			Debug.Assert(!ViewSettings.IsUseREST, "SetupOut using REST.");
			if (!_Output2Reg.TryGetValue(range, out int val))
				throw new ArgumentException("Invalid output gain value.");
			_UsbApi.WriteRegister(6, (byte)val);
			Debug.WriteLine($"Output full scale set to {range} dB with {val}");
			return new ValueTask();
		}

		public ValueTask SetSampleRate(uint range)
		{
			if (range == _SampleRate)
				return new ValueTask();
			_SampleRate = range;
			Debug.Assert(!ViewSettings.IsUseREST, "SetSampleRate using REST.");
			if (!_Samplerate2Reg.TryGetValue((int)range, out int val))
				throw new ArgumentException("Invalid sample rate value.");
			_UsbApi.WriteRegister(9, (byte)val);
			Debug.WriteLine($"Sample rate set to {range} Hz");
			return new ValueTask();
		}

		public ValueTask SetOutputSource(OutputSources source)
		{
			// nothing to do here
			_OutputSource = source;
			return new ValueTask();
		}

		/// <summary>
		/// given a voltage return the dbV value for the output register
		/// </summary>
		/// <param name="maxOut"></param>
		/// <returns></returns>
		static int DetermineOutput(double maxOut)
		{
			// Find the smallest output setting that is greater than or equal to maxOut
			// since maxout is a peak voltage, convert to rms
			var maxrms = maxOut * 0.7;  // the rms voltage to produce this peak voltage
			Debug.WriteLine($"Max rms= {maxrms}");
			foreach (var kvp in _Output2Reg.Reverse())
			{
				var mvp = Math.Pow(10, kvp.Key / 20.0);
				if (mvp >= maxrms)
					return kvp.Key;
			}
			return 18; // Default to 18 dB if no suitable value is found
		}

		public async ValueTask<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			if (_UsbApi == null)
				return new LeftRightSeries();

			LeftRightSeries lrfs = new LeftRightSeries();
			var dpt = new double[dataLeft.Length];
			List<AcqResult> runList = new List<AcqResult>();
			// set the output amplitude to support the data
			var maxOut = Math.Max(dataLeft.Max(), dataRight.Max());
			var minOut = Math.Min(dataLeft.Min(), dataRight.Min());
			maxOut = Math.Max(Math.Abs(maxOut), Math.Abs(minOut));  // maximum output voltage
																	// don't bother setting output amplitude if we have no output
			var mlevel = DetermineOutput(1.1 * ((maxOut > 0) ? maxOut : 1e-8)); // the setting for our voltage + 10%
			await SetOutputRange(mlevel); // set the output voltage

			for (int rrun = 0; rrun < averages; rrun++)
			{
				try
				{
					var newData = await _UsbApi.DoStreamingAsync(ct, dataLeft, dataRight);
					if (ct.IsCancellationRequested || lrfs == null || newData.Valid == false)
						return lrfs ?? new();
					runList.Add(newData);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error: {ex.Message}");
					return lrfs ?? new();
				}
			}

			{
				lrfs.TimeRslt = new();
				if (runList.Count == 1)
				{
					lrfs.TimeRslt.Left = runList.First().Left;
					lrfs.TimeRslt.Right = runList.First().Right;
				}
				else
				{
					var left = runList.First().Left;
					var right = runList.First().Right;
					for (int i = 1; i < runList.Count; i++)
					{
						left = left.Zip(runList[i].Left, (x, y) => x + y).ToArray();
						right = right.Zip(runList[i].Right, (x, y) => x + y).ToArray();
					}
					lrfs.TimeRslt.Left = left.Select(x => x / runList.Count).ToArray();
					lrfs.TimeRslt.Right = right.Select(x => x / runList.Count).ToArray();
				}
				lrfs.TimeRslt.dt = 1.0 / _SampleRate;
			}
			if (ct.IsCancellationRequested)
				return lrfs;

			if (getFreq)
			{
				lrfs.FreqRslt = QaMath.CalculateSpectrum(lrfs.TimeRslt, _Windowing);
			}
			return lrfs;        // Only one measurement
		}

		public async ValueTask<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq)
		{
			var datapt = new double[_FftSize];
			if (_OutputSource == OutputSources.Sine)
			{
				var gp1 = WaveGenerator.Singleton.GenParams;
				var gp2 = WaveGenerator.Singleton.Gen2Params;
				double dt = 1.0 / _SampleRate;
				if (gp1?.Enabled == true)
				{
					datapt = datapt.Select((x, index) => x + (gp1.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp1.Frequency * dt * index)).ToArray();
				}
				if (gp2?.Enabled == true)
				{
					datapt = datapt.Select((x, index) => x + (gp2.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp2.Frequency * dt * index)).ToArray();
				}
			}
			var lrfs = await DoAcquireUser(averages, ct, datapt, datapt, getFreq);
			return lrfs;
		}

		public async ValueTask<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			if(!_UsbApi.IsOpen())
			{
				_UsbApi.Open();
			}
			await QaComm.SetFftSize(fftsize);
			await QaComm.SetSampleRate(sampleRate);
			await QaComm.SetWindowing(Windowing);
			await QaComm.SetInputRange(attenuation);

			Debug.Assert(_SampleRate == sampleRate, "Sample rate not set");
			Debug.Assert(_FftSize == fftsize, "FFT size not set");
			Debug.Assert(_Attenuation == attenuation, "Attenuation not set");
			Debug.Assert(_Windowing == Windowing, "Windows not set");

			return true;
		}

		public ValueTask<string> GetWindowing()
		{
			return new ValueTask<string>(_Windowing);
		}

		public ValueTask SetWindowing(string windowing)
		{
			_Windowing = windowing;
			return new ValueTask();
		}
	}
}
