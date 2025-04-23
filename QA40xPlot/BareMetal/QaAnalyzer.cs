
using LibUsbDotNet;
using LibUsbDotNet.Main;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Diagnostics;

namespace QA40xPlot.BareMetal
{
	public class QaAnalyzer
	{
		public AnalyzerParams? Params { get; private set; }

		public GenWaveform GenParams { get; private set; }
		public GenWaveform Gen2Params { get; private set; }

		public byte[] CalData { get; private set; } // readonly

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
			if( Params?.MaxInputLevel != analyzerParams.MaxInputLevel)
				Control.SetInput(analyzerParams.MaxInputLevel);
			if (Params?.MaxOutputLevel != analyzerParams.MaxOutputLevel)
				Control.SetOutput(analyzerParams.MaxOutputLevel);
			if (Params?.SampleRate != analyzerParams.SampleRate)
				Control.SetSampleRate(analyzerParams.SampleRate);
			Params = analyzerParams;
		}

		public void SetSampleRate(int sampleRate)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			if (Params.SampleRate != sampleRate)
			{
				Control.SetSampleRate(sampleRate);
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
				Control.SetInput(level);
			Params.MaxInputLevel = level;
		}

		public void SetOutput(int level)
		{
			if (Params == null)
				throw new InvalidOperationException("Analyzer parameters not initialized.");
			if (Params.MaxOutputLevel != level)
				Control.SetOutput(level);
			Params.MaxOutputLevel = level;
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
			//RegisterReader?.Reset();
			RegisterWriter = Device.OpenEndpointWriter(WriteEndpointID.Ep01);
			//RegisterWriter?.Reset();
			DataReader = Device.OpenEndpointReader(ReadEndpointID.Ep02);
			//DataReader?.Reset();
			DataWriter = Device.OpenEndpointWriter(WriteEndpointID.Ep02);
			//DataWriter?.Reset();

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