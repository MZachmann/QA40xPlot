

using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using QA40xPlot.ViewModels;
using ScottPlot;
using System.IO;

// code for dealing with windows sound audio output devices
// this keeps the data cached if possible because otherwise it's quite slow

public class SoundUtil : FloorViewModel, IDisposable
{
	public static readonly int EchoNone = 0;        // only the QA40x makes sound
	public static readonly int EchoQuiet = 1;       // only the Windows Sound device makes sound
	public static readonly int EchoBoth = 2;		// both make sound

	double[] _DataLeft { get; set; } = [];
	double[] _DataRight { get; set; } = [];
	string _DeviceName { get; set; } = string.Empty;
	int _SampleRate { get; set; } = 0;
	RawSourceWaveStream? _Provider { get; set; } = null;

	private bool _isNew = true;
	public bool IsNew
	{
		get { return _isNew; }
		set { SetProperty(ref _isNew, value); }
	}

	private bool _isShared = true;
	public bool IsShared
	{
		get { return _isShared; }
		set { SetProperty(ref _isShared, value); }
	}

	// the wasapi output device we are using
	private MMDevice? _sndDevice = null;
	public MMDevice? SndDevice
	{
		get { return _sndDevice; }
		set { SetProperty(ref _sndDevice, value); }
	}

	// the wasapi output device we are using
	private MixingSampleProvider? _sndMixer = null;
	public MixingSampleProvider? SndMixer
	{
		get { return _sndMixer; }
		set { SetProperty(ref _sndMixer, value); }
	}

	// the waveformat we're rendering
	private WaveFormat? _waveForm = null;
	public WaveFormat? WaveForm
	{
		get { return _waveForm; }
		set { SetProperty(ref _waveForm, value); }
	}

	// the built up wasapiout object based on MyDevice
	private WasapiOut? _renderer = null;
	public WasapiOut? WaveRender
	{
		get { return _renderer; }
		set { SetProperty(ref _renderer, value); }
	}

	public SoundUtil()
	{
	}

	/// <summary>
	/// returns 0=none,1=left,2=right,3=both
	/// </summary>
	/// <returns></returns>
	public static int GetChannels()
	{
		var idx = SettingsViewModel.EchoChannels.IndexOf(ViewSettings.Singleton.SettingsVm.EchoChannel);
		if (idx < 0)
			idx = 0;	// default to none
		return idx;
	}

	/// <summary>
	/// returns 0=none,1=left,2=right,3=both
	/// </summary>
	/// <returns></returns>
	public static bool HasChannel(bool isLeft)
	{
		var idx = SettingsViewModel.EchoChannels.IndexOf(ViewSettings.Singleton.SettingsVm.EchoChannel);
		if (idx < 0)
			idx = 0;    // default to none
		return 0 != (idx & (isLeft ? SettingsViewModel.EchoChannelLeft : SettingsViewModel.EchoChannelRight));
	}

	// create a new sound object if the name has changed
	// update the data if it has changed
	// set IsNew to true if we built new data or a new object, requiring WasteOne to prime it
	public static SoundUtil? CreateUtil(string deviceName, double[] leftOut, double[] rightOut, int sampleRate)
	{
		var snd = ViewSettings.Singleton.MainVm.ExternalSound;
		if (snd != null)
		{
			if(snd._DeviceName != deviceName)
			{
				snd.Dispose();
				ViewSettings.Singleton.MainVm.ExternalSound = null;
				snd = null;
			}
		}

		if(snd == null && deviceName.Length > 0)
		{
			var soundObj = new SoundUtil();
			var mydev = soundObj.InitDevice(deviceName);
			if(mydev != null)
			{
				ViewSettings.Singleton.MainVm.ExternalSound = soundObj;
				snd = soundObj;
				snd.IsNew = true;
			}
		}
		// snd not null => .ExternalSound is set to it
		if (snd != null)
		{
			snd.CreateSound(leftOut, rightOut, sampleRate);	// this may set IsNew
		}
		return snd;
	}

	/// <summary>
	/// enumerate by name all of the active audio output devices
	/// </summary>
	/// <returns>the array of names</returns>
	public static string[] EnumerateDevices()
	{
		string[] names = [];
		var enumerator = new MMDeviceEnumerator();
		foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
		{
			try
			{
				names = names.Append(wasapi.FriendlyName).ToArray();
			}
			catch
			{
				// ignore
			}
		}
		return names;
	}

	public static bool ExternalPresent()
	{
		var name = ViewSettings.Singleton.SettingsVm.EchoName;
		if (name.Length == 0)
			return false;
		var bres = ViewSettings.Singleton.SettingsVm.EchoNames.Contains(name);
		return bres;
	}

	public bool IsFormatSupported(WaveFormat wvfmt)
	{
		if(SndDevice == null) 
			return false;

		bool isSupported = SndDevice.AudioClient.IsFormatSupported(
			IsShared ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive,
			wvfmt,
			out WaveFormatExtensible closestMatchFormat);
		var cmf = closestMatchFormat;
		return isSupported;
	}

	public MMDevice? InitDevice(string name)
	{
		// find our named device
		_DeviceName = name;
		var enumerator = new MMDeviceEnumerator();
		MMDevice? myDevice = null;
		foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
		{
			try
			{
				//Debug.WriteLine($"{wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}");
				if (wasapi.FriendlyName.Contains(name))
				{
					myDevice = wasapi;
					break;
				}
			}
			catch
			{
				// ignore
			}
		}
		SndDevice = myDevice;

		CreateRender();
		return myDevice;
	}

	public IEnumerable<WaveFormat> FilterEncoding(IEnumerable<WaveFormat> waves, WaveFormatEncoding enc)
	{
		return waves.Where(wf => wf.Encoding == enc);
	}

	public IEnumerable<WaveFormat> FilterBps(IEnumerable<WaveFormat> waves, int bitsPerSample)
	{
		return waves.Where(wf => wf.BitsPerSample == bitsPerSample);
	}

	public IEnumerable<WaveFormat> FilterSampleRate(IEnumerable<WaveFormat> waves, int sampleRate)
	{
		return waves.Where(wf => wf.SampleRate == sampleRate);
	}

	/// <summary>
	/// return list contains at least the number of channels if channels < 0, exactly if channels > 0
	/// </summary>
	/// <param name="waves"></param>
	/// <param name="channels"></param>
	/// <returns></returns>
	public IEnumerable<WaveFormat> FilterChannels(IEnumerable<WaveFormat> waves, int channels)
	{
		return waves.Where(wf => (channels > 0) ? wf.Channels == channels : wf.Channels > channels);
	}

	/// <summary>
	/// find the closest available wave format to the requested parameters
	/// tries pcm first then ieee float if exclusive, only pcm if shared
	/// </summary>
	/// <param name="sampleRate"></param>
	/// <param name="Channels"></param>
	/// <param name="isShared"></param>
	/// <returns></returns>
	public WaveFormat? GetBestWaveFormat(int sampleRate, int Channels, bool isShared)
	{
		if(!isShared)
		{
			var allowExcl = FindSupportedFormats(false);
			var waves = FilterEncoding(allowExcl, WaveFormatEncoding.Pcm);
			// we have PCM formats, use one of them hopefully
			var fnd = FilterBps(waves, 32);
			if (fnd.Count() == 0)
				fnd = FilterBps(waves, 24); // try 24 bps
			if (fnd.Count() == 0)
				fnd = FilterBps(waves, 16); // try 16 bps yuck
			waves = fnd;
			if (waves.Count() > 0)
			{
				fnd = FilterSampleRate(waves, sampleRate); // try 96000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 96000); // try 96000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 48000); // try 48000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 192000); // try 192000
				if (fnd.Count() > 0)
				{
					var fnd2 = FilterChannels(fnd, Channels);
					if (fnd2.Count() > 0)
						return fnd2.First();
					fnd2 = FilterChannels(fnd, -Channels);
					if (fnd2.Count() > 0)
						return fnd2.First();
					return fnd.First(); // give up and use what we've got
				}
			}

			waves = FilterEncoding(allowExcl, WaveFormatEncoding.IeeeFloat);
			// we have ieee formats, use one of them hopefully
			fnd = FilterBps(waves, 32);
			if (fnd.Count() == 0)
				fnd = FilterBps(waves, 24); // try 24 bps
			if (fnd.Count() == 0)
				fnd = FilterBps(waves, 16); // try 16 bps yuck
			waves = fnd;
			if (waves.Count() > 0)
			{
				fnd = FilterSampleRate(waves, sampleRate); // try 96000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 96000); // try 96000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 48000); // try 48000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 192000); // try 192000
				if (fnd.Count() > 0)
				{
					var fnd2 = FilterChannels(fnd, Channels);
					if (fnd2.Count() > 0)
						return fnd2.First();
					fnd2 = FilterChannels(fnd, -Channels);
					if (fnd2.Count() > 0)
						return fnd2.First();
					return fnd.First(); // give up and use what we've got
				}
			}
		}
		else
		{
			var allowShared = FindSupportedFormats(true);
			// now allowShared is all shared format and allowedExcl is all exclusive formats
			// 1- find exclusive format with right parameters
			var waves = FilterEncoding(allowShared, WaveFormatEncoding.Pcm);
			// we have PCM formats, use one of them hopefully
			var fnd = FilterBps(waves, 32);
			if (fnd.Count() == 0)
				fnd = FilterBps(waves, 24); // try 24 bps
			if (fnd.Count() == 0)
				fnd = FilterBps(waves, 16); // try 16 bps yuck
			waves = fnd;
			if (waves.Count() > 0)
			{
				fnd = FilterSampleRate(waves, sampleRate); // try 96000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 96000); // try 96000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 48000); // try 48000
				if (fnd.Count() == 0)
					fnd = FilterSampleRate(waves, 192000); // try 192000
				if (fnd.Count() > 0)
				{
					var fnd2 = FilterChannels(fnd, Channels);
					if (fnd2.Count() > 0)
						return fnd2.First();
					fnd2 = FilterChannels(fnd, -Channels);
					if (fnd2.Count() > 0)
						return fnd2.First();
					return fnd.First(); // give up and use what we've got
				}
			}
		}

		return null;
	}

	/// <summary>
	/// create the wasapi output device based on the selected SndDevice
	/// </summary>
	/// <returns>the created device or null</returns>
	public WasapiOut? CreateRender()
	{
		try
		{
			IsShared = true;
			if (SndDevice != null)
			{
				WasapiOut? waveOut = null;
				try
				{
					waveOut = new(SndDevice, 
						IsShared ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive,
						useEventSync: true, latency: 40);

				}
				catch
				{
					waveOut = new(SndDevice, AudioClientShareMode.Shared, useEventSync: true, latency:40);
					IsShared = false;
				}
				WaveRender = waveOut;
			}
		}
		catch
		{
			WaveRender = null;
		}
		return WaveRender;

	}

	// prepare to play a sound from memory data to emulate sending a signal
	// the WaveOut it returns is still playing in a thread
	public WasapiOut? CreateSound(double[] leftOut, double[] rightOut, int sampleRate)
	{
		// check cache...
		bool isCached = false;
		if(_DataLeft.Length == leftOut.Length && _DataRight.Length == rightOut.Length && _SampleRate == sampleRate)
		{
			isCached = _DataLeft.SequenceEqual(leftOut) && _DataRight.SequenceEqual(rightOut);
		}

		// we have to figure out a waveformat that is supported by the board and usable
		// and whether it's exclusive (preferred) or shared mode
		if(! isCached)
		{
			if(WaveRender != null)
			{
				WaveRender.Dispose();
				CreateRender();
			}
			// figure out the waveformat and sharing mode
			const int bitsPerSample = 32;
			const int channels = 2;
			WaveFormat wvf = new WaveFormat(sampleRate, bitsPerSample, channels);
			IsShared = true;   // default to shared
			// check exclusive first
			var wvf2 = GetBestWaveFormat(sampleRate, channels, false);
			if (wvf2 != null)
			{
				IsShared = false;
				wvf = wvf2;
			}
			else
			{
				// now check shared if we didn't find one
				wvf2 = GetBestWaveFormat(sampleRate, channels, true);
				if (wvf2 != null)
				{
					wvf = wvf2;
				}
			}
			WaveForm = wvf;

			// if wvf isn't our requested format, we will need to resample
			// start by calculating the packed stereo data given pcm or ieee float
			_DataLeft = (double[])leftOut.Clone();
			_DataRight = (double[])rightOut.Clone();
			_SampleRate = sampleRate;
			var soundlimit = 1.0;
			var maxSound = Math.Max(leftOut.Max(), soundlimit);
			var maxv = Int32.MaxValue * 1.0 / maxSound;
			byte[] byteData = [];
			if( WaveForm.Encoding == WaveFormatEncoding.IeeeFloat)
			{
				maxv = 1.0 / maxSound;
				float[] intData = leftOut.Select(s => (float)(s * maxv)).ToArray();
				float[] intRData = rightOut.Select(s => (float)(s * maxv)).ToArray();
				float[] stereoData = new float[intData.Length * 2];
				for (int i = 0; i < intData.Length; i++)
				{
					stereoData[i * 2] = intData[i];
					stereoData[i * 2 + 1] = intRData[i];
				}
				byteData = new byte[stereoData.Length * 4];
				Buffer.BlockCopy(stereoData, 0, byteData, 0, byteData.Length);
			}
			else
			{
				maxv = Int32.MaxValue * 1.0 / maxSound;
				int[] intData = leftOut.Select(s => (int)(s * maxv)).ToArray();
				int[] intRData = rightOut.Select(s => (int)(s * maxv)).ToArray();
				int[] stereoData = new int[intData.Length * 2];
				for (int i = 0; i < intData.Length; i++)
				{
					stereoData[i * 2] = intData[i];
					stereoData[i * 2 + 1] = intRData[i];
				}
				byteData = new byte[stereoData.Length * 4];
				Buffer.BlockCopy(stereoData, 0, byteData, 0, byteData.Length);
			}

			// now pump it out if we have the actual format or resample it if not
			if(WaveForm.SampleRate != sampleRate || WaveForm.Channels != 2 || WaveForm.BitsPerSample != bitsPerSample)
			{
				var wv = new WaveFormat(sampleRate, bitsPerSample, channels);
				if(WaveForm.Encoding == WaveFormatEncoding.IeeeFloat)
					wv = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
				_Provider = new RawSourceWaveStream(new MemoryStream(byteData), wv);
				var resampler = new WdlResamplingSampleProvider(_Provider.ToSampleProvider(), wvf.SampleRate);
				WaveRender?.Init(resampler);
			}
			else
			{
				_Provider = new RawSourceWaveStream(new MemoryStream(byteData), WaveForm);
				WaveRender?.Init(_Provider);
			}
			IsNew = true;
		}
		return WaveRender;
	}

	/// <summary>
	/// find all supported formats given a share mode
	/// </summary>
	/// <param name="isShared">shared wasapi or exclusive</param>
	/// <param name="wvf">waveformat to prefer</param>
	/// <returns></returns>
	public List<WaveFormat> FindSupportedFormats(bool isShared)
	{
		List<WaveFormat> allWaves = [];
		if (SndDevice == null)
			return allWaves;
		bool isSupported = false;
		AudioClientShareMode shared = isShared ? AudioClientShareMode.Shared : AudioClientShareMode.Exclusive;
		int[] sampleRates = [192000, 96000, 48000, 44100];
		int[] channels = [4, 2, 1];
		int[] bitsPerSample = [32, 24, 16, 8];
		foreach (var sr in sampleRates)
		{
			foreach (var ch in channels)
			{
				foreach (var bps in bitsPerSample)
				{
					var waveFormat = new WaveFormat(sr, bps, ch);
					isSupported = SndDevice.AudioClient.IsFormatSupported(shared, waveFormat);
					if (isSupported)
					{
						allWaves.Add(waveFormat);
					}
					waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sr, ch);
					isSupported = SndDevice.AudioClient.IsFormatSupported(shared, waveFormat);
					if (isSupported)
					{
						allWaves.Add(waveFormat);
					}
				}
			}
		}
		return allWaves;
	}

	public void Play()
	{
		WaveRender?.Play();
	}
	public void Stop()
	{
		if (_Provider != null)
		{
			WaveRender?.Stop();
			_Provider.Position = 0;	// prepare to play again
		}
	}

	public void Dispose()
	{
		WaveRender?.Stop();
		WaveRender?.Dispose();
	}

	// when using an external DAC the first time through seems to be warmup
	// so this wastes an entire playthrough
	// sleeping a shorter time seemed to fail
	public void WasteOne(int numNotes, uint sampleRate)
	{
		Play();
		Thread.Sleep((int)(1000 * numNotes / sampleRate));  // give some time to start the sound engine
		Stop();
	}
}
