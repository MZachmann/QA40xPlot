
using LibUsbDotNet;
using LibUsbDotNet.Main;
using QA40xPlot.Libraries;
using System.Diagnostics;

namespace QA40xPlot.BareMetal
{
	public class SineGen
	{
		public double Frequency { get; set; }
		public double Voltage { get; set; }
		public bool IsOn { get; set; }
	}

	public class QaAnalyzer
	{
		public AnalyzerParams? Params { get; private set; }

		public SineGen GenParams { get; private set; } 

		public byte[] CalData { get; private set; } // readonly

		// the usb device we talk to
		public UsbDevice? Device { get; private set; }

		// some control methods for the device
		public Control? Control { get; private set; }

		// the reader/writer pairs for register and data endpoints
		public UsbEndpointReader? RegisterReader { get; private set; }
		public UsbEndpointWriter? RegisterWriter { get; private set; }
		public UsbEndpointReader? DataReader { get; private set; }
		public UsbEndpointWriter? DataWriter { get; private set; }

		public QaAnalyzer()
		{
			Params = null;
			Device = null;
			Control = null;
			CalData = [];
			GenParams = new SineGen
			{
				Frequency = 1000,
				Voltage = 0.5,
				IsOn = false
			};
		}

		public void SetParams(AnalyzerParams? analyzerParams)
		{
			if (analyzerParams == null)
				throw new ArgumentNullException(nameof(analyzerParams));
			// Set input/output levels and sample rate
			if( Params?.MaxInputLevel != analyzerParams.MaxInputLevel)
				Control?.SetInput(analyzerParams.MaxInputLevel);
			if (Params?.MaxOutputLevel != analyzerParams.MaxOutputLevel)
				Control?.SetOutput(analyzerParams.MaxOutputLevel);
			if (Params?.SampleRate != analyzerParams.SampleRate)
				Control?.SetSampleRate(analyzerParams.SampleRate);
			Params = analyzerParams;
		}

		public void SetSampleRate(int sampleRate)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			if (Params.SampleRate != sampleRate)
			{
				Control?.SetSampleRate(sampleRate);
			}
			Params.SampleRate = sampleRate;
			Params.PostBuffer = 2048 * sampleRate / 48000;
			Params.PreBuffer = 2048 * sampleRate / 48000;
		}

		public void SetInput(int level)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			if (Params.MaxInputLevel != level)
				Control?.SetInput(level);
			Params.MaxInputLevel = level;
		}

		public void SetOutput(int level)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			if (Params.MaxOutputLevel != level)
				Control?.SetOutput(level);
			Params.MaxOutputLevel = level;
		}
		
		public void SetGenParams(double frequency, double voltage, bool isOn)
		{
			GenParams.Frequency = frequency;
			GenParams.Voltage = Math.Pow(10, voltage/20);
			GenParams.IsOn = isOn;
		}

		public AnalyzerParams? Init(
			int sampleRate = 48000,
			int maxInputLevel = 0,
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

			Control = new Control(this);
			CalData = Control.LoadCalibration();

			// Load calibration data
			var newParams = new AnalyzerParams(sampleRate, maxInputLevel, maxOutputLevel, preBuf, postBuf, fftSize, OutputSources.Off, windowType);
			SetParams(newParams);

			return Params ?? null;
		}

		public void Close()
		{
			try
			{
				DataReader?.Dispose();
				DataWriter?.Dispose();
				RegisterReader?.Dispose();
				RegisterWriter?.Dispose();
				var iuDevice = Device as IUsbDevice;
				iuDevice?.ReleaseInterface(0);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"An error occurred during cleanup: {e.Message}");
			}
		}

	}

}