using LibUsbDotNet;
using LibUsbDotNet.Main;
using QA40x_BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Diagnostics;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40xPlot.BareMetal
{
	public class QaAnalyzer
	{
		public AnalyzerParams? Params { get; private set; }

		public GenWaveform GenParams { get; private set; }
		public GenWaveform Gen2Params { get; private set; }

		public byte[] CalData { get; private set; } // readonly
		public (double,double)[] FCalData { get; private set; } // readonly

		// the usb device we talk to
		public UsbDevice? Device { get; private set; }

		// the reader/writer pairs for register and data endpoints
		public UsbEndpointReader? RegisterReader { get; private set; }
		public UsbEndpointWriter? RegisterWriter { get; private set; }
		public UsbEndpointReader? DataReader { get; private set; }
		public UsbEndpointWriter? DataWriter { get; private set; }

		public QaAnalyzer()
		{
			Params = null;
			Device = null;
			CalData = [];
			FCalData = [];
			GenParams = new GenWaveform()
			{
				Name = "Sine",
				Frequency = 1000,
				FreqEnd = 20000,
				Voltage = 0.5,
				Enabled = false
			};
			Gen2Params = new GenWaveform()
			{
				Name = "Sine",
				Frequency = 1000,
				FreqEnd = 20000,
				Voltage = 0.5,
				Enabled = false
			};
		}

		public void SetParams(AnalyzerParams? analyzerParams)
		{
			if (analyzerParams == null)
				throw new ArgumentNullException(nameof(analyzerParams));
			// Set input/output levels and sample rate
			Control.SetInput(analyzerParams.MaxInputLevel);
			Control.SetOutput(analyzerParams.MaxOutputLevel);
			Control.SetSampleRate(analyzerParams.SampleRate);
			Params = analyzerParams;
		}

		public void SetSampleRate(int sampleRate)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			Control.SetSampleRate(sampleRate);
			Params.SampleRate = sampleRate;
			// bufsize is leftout.length and usbBufSize is the size of the usb buffer
			// bufSize * 8 must be >= to usbBufSize, and bufSize * 8 must be an integer multiple of usbBufSize
			Params.PostBuffer = 4096;
			Params.PreBuffer = 16384;
		}

		public void SetInput(int level)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			Control.SetInput(level);
			Params.MaxInputLevel = level;
		}

		public void SetOutput(int level)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			Control.SetOutput(level);
			Params.MaxOutputLevel = level;
		}
		
		public AnalyzerParams? Init(
			int sampleRate = 48000,
			int maxInputLevel = 42,
			int maxOutputLevel = 18,
			int preBuf = 2048,
			int postBuf = 2048,
			int fftSize = 16384,
			OutputSources outputSource = OutputSources.Sine,
			string windowType = "Hann")
		{
			// Attempt to open QA402 or QA403 device
			Device = QaLowUsb.AttachDevice();
			RegisterReader = Device.OpenEndpointReader(ReadEndpointID.Ep01);
			RegisterWriter = Device.OpenEndpointWriter(WriteEndpointID.Ep01);
			DataReader = Device.OpenEndpointReader(ReadEndpointID.Ep02);
			DataWriter = Device.OpenEndpointWriter(WriteEndpointID.Ep02);

			CalData = Control.LoadCalibration();
			// convert to doubles from byte stream
			var incals = Enumerable.Range(0, 7).Select(x => x * 6);
			var fcals = incals.Select(x => Control.GetAdcCal(CalData, x)).ToList();
			incals = Enumerable.Range(0, 3).Select(x => 18 - x * 10);
			var gcals = incals.Select(x => Control.GetDacCal(CalData, x)).ToList();
			fcals.AddRange(gcals);
			FCalData = fcals.ToArray();		// doubles instead of bytes mainly for debugging
			Debug.WriteLine($"Calibration data: {string.Join(", ", FCalData)}");

			// Load calibration data
			var newParams = new AnalyzerParams(sampleRate, maxInputLevel, maxOutputLevel, preBuf, postBuf, fftSize, OutputSources.Off, windowType);
			SetParams(newParams);

			return Params ?? null;
		}

		public void Close(bool OnExit)
		{
			try
			{
				DataReader?.Dispose();
				DataWriter?.Dispose();
				RegisterReader?.Dispose();
				RegisterWriter?.Dispose();
				QaLowUsb.DetachDevice(OnExit);
				Device = null;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"An error occurred during cleanup: {e.Message}");
			}
		}

	}

}