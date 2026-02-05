using QA40x.BareMetal;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Diagnostics;

namespace QA40xPlot.BareMetal
{
	/// <summary>
	/// Low level methods for the QA40x with USB
	/// </summary>
	public class IODevUSB : IODevice
	{
		private static int _InvalidInt = 1232;
		// headroom multiplier for distortion reduction (deprecated)
		private double _Headroom = 1.05;
		/// <summary>
		/// convert from generator voltage to register setting
		/// voltages are 7.943, 2.512, 0.794, 0.251 by dBV of 18, 8, -2, -12
		/// the Volt2Output list is empirically determined by looking at THD+N vs output gain at each voltage
		/// it is max generator voltage vs gain setting. -12 gain is particularly problematic
		/// this is used if MinOutputRange is set in the settings to not-blank
		/// or we're set to miniminize distortion
		/// </summary>
		private static readonly Dictionary<double, int> _Volt2Output = new() { { 7.94, 18 }, { 2.1, 8 }, { .54, -2 }, { .07, -12 } };
		private static readonly Dictionary<int, int> _Output2Reg = new() { { 18, 3 }, { 8, 2 }, { -2, 1 }, { -12, 0 } };
		private static readonly Dictionary<int, int> _Input2Reg = new() { { 0, 0 }, { 6, 1 }, { 12, 2 }, { 18, 3 }, { 24, 4 }, { 30, 5 }, { 36, 6 }, { 42, 7 } };
		private static readonly Dictionary<int, int> _Samplerate2Reg = new() { { 48000, 0 }, { 96000, 1 }, { 192000, 2 }, { 384000, 3 } }; // 384K?
																																		   // sweep parameters
		private uint _FftSize = (uint)_InvalidInt;
		private int _OutputRange = _InvalidInt;
		private int _Attenuation = _InvalidInt;
		private uint _SampleRate = (uint)_InvalidInt;
		private OutputSources _OutputSource = OutputSources.Invalid; // default to sine
		private string _Windowing = "Invalid";
		//
		private QaUsb _UsbApi = new();      // our local Usb controller

		public string Name => "USB";    // the name of the io device

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
			if (!_UsbApi.IsOpen())
			{
				Debug.WriteLine("USB device not yet open");
				var work = _UsbApi.Open();  // try to open it
				if (!work)
				{
					Debug.WriteLine("USB device not connected");
					return new ValueTask<bool>(false);
				}
			}
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
				Debug.WriteLine("Opening USB device");
				_UsbApi.Open();
				SetParametersEmpty();       // set to invalid values
			}
			return new ValueTask<bool>(true);
		}

		public ValueTask<bool> IsOpen()
		{
			return new ValueTask<bool>(_UsbApi.IsOpen());
		}

		public ValueTask Close(bool onExit)
		{
			if (_UsbApi.IsOpen())
			{
				Debug.WriteLine($"Closing USB device with {onExit}");
				_UsbApi.Close(onExit);
			}
			return ValueTask.CompletedTask;
		}

		public uint GetFftSize() { return _FftSize; }

		public int GetInputRange() { return _Attenuation; }

		public int GetOutputRange() { return _OutputRange; }

		public uint GetSampleRate() { return _SampleRate; }

		public ValueTask<double> GetDCVolts() { return new ValueTask<Double>(_UsbApi.ReadRegister(17) / 1000.0); }

		public ValueTask<double> GetDCAmps() { return new ValueTask<Double>(_UsbApi.ReadRegister(18)); }

		public ValueTask<double> GetTemperature() { return new ValueTask<Double>(_UsbApi.ReadRegister(22) / 10.0); }

		public OutputSources GetOutputSource() { return _OutputSource; }

		public string GetWindowing() { return _Windowing; }

		public ValueTask SetFftSize(uint range)
		{
			// here we just store this for the other clients
			if (range != _FftSize)
				Debug.WriteLine($"FFtsize set to {range} from {_FftSize}");
			_FftSize = range;
			return ValueTask.CompletedTask;
		}

		public ValueTask SetInputRange(int range)
		{
			if (range == _Attenuation)
				return ValueTask.CompletedTask;

			_Attenuation = range;
			Debug.Assert(!ViewSettings.IsUseREST, "SetupInput using REST.");
			if (!_Input2Reg.TryGetValue(range, out int val))
				throw new ArgumentException("Invalid input gain value.");
			_UsbApi.WriteRegister(5, (byte)val);
			Debug.WriteLine($"Attenuation set to {range} dB with {val}");
			return ValueTask.CompletedTask;
		}

		public ValueTask SetOutputRange(int range)
		{
			if (range == _OutputRange)
				return ValueTask.CompletedTask;
			_OutputRange = range;
			Debug.Assert(!ViewSettings.IsUseREST, "SetupOut using REST.");
			if (!_Output2Reg.TryGetValue(range, out int val))
				throw new ArgumentException("Invalid output gain value.");
			_UsbApi.WriteRegister(6, (byte)val);
			Debug.WriteLine($"Output full scale set to {range} dB with {val}");
			return ValueTask.CompletedTask;
		}

		public ValueTask SetSampleRate(uint range)
		{
			if (range == _SampleRate)
				return ValueTask.CompletedTask;
			_SampleRate = range;
			Debug.Assert(!ViewSettings.IsUseREST, "SetSampleRate using REST.");
			if (!_Samplerate2Reg.TryGetValue((int)range, out int val))
				throw new ArgumentException("Invalid sample rate value.");
			_UsbApi.WriteRegister(9, (byte)val);
			Debug.WriteLine($"Sample rate set to {range} Hz");
			return ValueTask.CompletedTask;
		}

		public ValueTask SetOutputSource(OutputSources source)
		{
			// nothing to do here
			if (_OutputSource != source)
				Debug.WriteLine($"Output source set to {source} from {_OutputSource}");

			_OutputSource = source;
			return ValueTask.CompletedTask;
		}

		public ValueTask SetWindowing(string windowing)
		{
			if (_Windowing != windowing)
				Debug.WriteLine($"Windowing set to {windowing} from {_Windowing}");
			_Windowing = windowing;
			return ValueTask.CompletedTask;
		}

		/// <summary>
		/// given a voltage return the dbV value for the output register
		/// empirical testing indicates the top 25% of the range has way higher distortion so
		/// lets keep this to at most 75% of full scale or so
		/// this allocates some headroom to reduce distortion
		/// </summary>
		/// <param name="maxOut"></param>
		/// <returns></returns>
		int DetermineOutput(double maxOut)
		{
			// Find the smallest output setting that is greater than or equal to maxOut
			// since maxout is a peak voltage, convert to rms
			var maxrms = maxOut * 0.7;  // the rms voltage to produce this peak voltage
			bool useVolts = ViewSettings.Singleton.SettingsVm.MinOutputRange.Length != 0 ||
				ViewSettings.Singleton.SettingsVm.OutputMethod.Length == 0 ||
				ViewSettings.Singleton.SettingsVm.OutputMethod.Contains("stort");
			if (useVolts)
			{
				// new method with hardcoded voltage limits
				foreach (var kvp in _Volt2Output.Reverse())  // since the list decreases
				{
					if (kvp.Key > maxrms)
					{
						Debug.WriteLine($"For inv of {maxrms} use {kvp.Key}");
						return kvp.Value;
					}
				}
			}
			else
			{
				// old method by just using the lowest possible gain
				var maxdbv = 20 * Math.Log10(_Headroom * maxrms);        // get it in db
				foreach (var kvp in _Output2Reg.Reverse())  // since the list decreases
				{
					if (kvp.Key > maxdbv)
					{
						Debug.WriteLine($"For inv of {maxOut} use {kvp.Key}");
						return kvp.Key;
					}
				}
			}
			// no headroom for largest one if all else fails
			var maxkvp = _Output2Reg.First();
			if (maxkvp.Key > 20 * Math.Log10(1.05 * maxrms))
				return maxkvp.Key;
			// output is too high for us? arg
			return -2; // Default to -2dB if no suitable value is found
		}

		/// <summary>
		/// This is the workhorse acquisition method used by virtually everything
		/// It provides two channels of output wave 
		/// then returns time and (optionally) frequency results
		/// </summary>
		/// <param name="averages">Number of times to acquire the data. it then averages the time domain data before calculating the fft</param>
		/// <param name="ct">a cancellation token to enable external cancel</param>
		/// <param name="dataLeft">left channel output</param>
		/// <param name="dataRight">right channel output</param>
		/// <param name="getFreq">bool to determine whether to calculate the fft freq data</param>
		/// <returns>a LeftRightSeries with time data and possibly freq data</returns>
		public async ValueTask<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			if (!_UsbApi.IsOpen())
				return new LeftRightSeries();

			Debug.Assert(_SampleRate != _InvalidInt, "Sample rate not set");
			Debug.Assert(_FftSize != _InvalidInt, "FFT size not set");
			Debug.Assert(_Attenuation != _InvalidInt, "Attenuation not set");
			Debug.Assert(_Windowing != "Invalid", "Windows not set");
			Debug.Assert(_OutputSource != OutputSources.Invalid, "Output source not set");

			LeftRightSeries lrfs = new LeftRightSeries();
			var dpt = new double[dataLeft.Length];
			List<AcqResult> runList = new List<AcqResult>();
			// set the output amplitude to support the data
			var maxOut = Math.Max(dataLeft.Max(), dataRight.Max());
			var minOut = Math.Min(dataLeft.Min(), dataRight.Min());
			maxOut = Math.Max(Math.Abs(maxOut), Math.Abs(minOut));  // maximum output voltage
																	// don't bother setting output amplitude if we have no output
			var mlevel = DetermineOutput((maxOut > 0) ? maxOut : 1e-8); // the setting for our voltage + 10%
			// if mlevel is tiny we're doing a noise analysis so let the output range be
			//var minRange = (maxOut <= 1e-8) ? -12 : (int)MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.MinOutputRange, -12);
			var minRange = (int)MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.MinOutputRange, -12);
			mlevel = Math.Max(mlevel, minRange); // noop for now but keep this line in for testing
			await SetOutputRange(mlevel);   // set the output voltage

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
			// if it's a QA402 the data comes in with channels 'backwards' so we flip them
			if ("QA402" == QaLowUsb.GetDeviceModel())
			{
				var x = lrfs.TimeRslt.Left;
				lrfs.TimeRslt.Left = lrfs.TimeRslt.Right;
				lrfs.TimeRslt.Right = x;
			}
			var gain = MathUtil.ToDouble(ViewSettings.ExternalGain, 0);
			if (gain != 0.0)
			{
				gain = 1.0 / QaLibrary.ConvertVoltage(gain, Data.E_VoltageUnit.dBV, Data.E_VoltageUnit.Volt); // linearize
				lrfs.TimeRslt.Left = lrfs.TimeRslt.Left.Select(x => x * gain).ToArray();
				lrfs.TimeRslt.Right = lrfs.TimeRslt.Right.Select(x => x * gain).ToArray();
			}
			//
			if (ct.IsCancellationRequested)
				return lrfs;

			if (getFreq)
			{
				lrfs.FreqRslt = QaMath.CalculateSpectrum(lrfs.TimeRslt, _Windowing);
			}
			return lrfs;        // Only one measurement
		}

		// note that averages refers to time series acquisitions
		public async ValueTask<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq)
		{
			if (!_UsbApi.IsOpen())
				return new LeftRightSeries();
			var ffts = GetFftSize();
			var srate = GetSampleRate();
			var datapt = WaveGenerator.GenerateBoth(srate, ffts);
			var lrfs = await DoAcquireUser(averages, ct, datapt.Item1, datapt.Item2, getFreq);
			Debug.WriteLine($"Acquire at SampleRate={GetSampleRate()}, FftSize={GetFftSize()}, Windowing={GetWindowing()}, Attenuation={GetInputRange()}`");

			return lrfs;
		}

		public async ValueTask<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			try
			{
				if (!_UsbApi.IsOpen())
				{
					_UsbApi.Open();
				}
				var isConnected = await CheckDeviceConnected();
				if (!isConnected)
				{
					Debug.WriteLine("USB device not connected");
					return false;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error opening USB device: {ex.Message}");
				return false;
			}
			await QaComm.SetFftSize(fftsize);
			await QaComm.SetSampleRate(sampleRate);
			await QaComm.SetWindowing(Windowing);
			await QaComm.SetInputRange(attenuation);
			await QaComm.SetOutputSource(OutputSources.Off);

			Debug.Assert(_SampleRate == sampleRate, "Sample rate not set");
			Debug.Assert(_FftSize == fftsize, "FFT size not set");
			Debug.Assert(_Attenuation == attenuation, "Attenuation not set");
			Debug.Assert(_Windowing == Windowing, "Windows not set");

			return true;
		}
	}
}
