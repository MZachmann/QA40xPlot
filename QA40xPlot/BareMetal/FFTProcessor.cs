using FftSharp;
using System;
using System.Linq;

namespace QA40xPlot.BareMetal
{
	public class FFTProcessor
	{
		private readonly AnalyzerParams _params;
		private double[] _timeSeries;
		private double[] _windowedTimeSeries;
		private double[] _fftData;

		public FFTProcessor(AnalyzerParams parameters)
		{
			_params = parameters;
			_fftData = [];
			_timeSeries = [];
			_windowedTimeSeries = [];
		}

		public FFTProcessor FftForward(double[] signal)
		{
			// Compute the forward FFT of the given signal
			_timeSeries = signal;
			_windowedTimeSeries = signal.Zip(_params.Window, (s, w) => s * w).ToArray();
			var fftResult = FFTInternal.RealFFT(_windowedTimeSeries); // Assuming FFT.RealFFT is implemented
			_fftData = fftResult.Select(x => (Math.Abs(x) / (_params.FFTSize / 2)) / Math.Sqrt(2)).ToArray();

			return this;
		}

		public FFTProcessor ApplyAcf()
		{
			// Apply the Amplitude Correction Factor (ACF) to the FFT data
			if (_fftData != null)
			{
				_fftData = _fftData.Select(x => x * _params.ACF).ToArray();
			}

			return this;
		}

		public FFTProcessor ApplyEcf()
		{
			// Apply the Energy Correction Factor (ECF) to the FFT data
			if (_fftData != null)
			{
				_fftData = _fftData.Select(x => x * _params.ECF).ToArray();
			}

			return this;
		}

		public FFTProcessor ToDbv()
		{
			// Convert FFT data to dBV (decibels relative to 1 volt)
			_fftData = Helpers.LinearArrayToDbV(_fftData);
			return this;
		}

		public FFTProcessor ToDbu()
		{
			// Convert FFT data to dBu (decibels relative to 0.775 volts)
			_fftData = Helpers.LinearArrayToDbU(_fftData);
			return this;
		}

		public double[] GetResult()
		{
			// Get the current FFT data
			return _fftData;
		}

		public double[] GetFrequencies()
		{
			// Get the frequency bins corresponding to the FFT
			return FFTInternal.FrequencyBins(_params.FFTSize, _params.SampleRate); // Assuming FFT.FrequencyBins is implemented
		}
	}

	public static class Helpers
	{
		public static double[] LinearArrayToDbV(double[] value)
		{
			return value.Select(x => 20 * Math.Log10(x)).ToArray();
		}

		public static double[] LinearArrayToDbU(double[] value)
		{
			return value.Select(x => 20 * Math.Log10(x / 0.775)).ToArray();
		}
	}

	public static class FFTInternal
	{
		public static double[] RealFFT(double[] data)
		{
			var window = new FftSharp.Windows.Hanning();
			double[] windowed_measured = window.Apply(data, true);   // true == normalized by # elements
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);
			return spectrum_measured.Select(x => x.Magnitude).ToArray();
		}

		public static double[] FrequencyBins(int fftSize, double sampleRate)
		{
			return Enumerable.Range(0, fftSize / 2 + 1)
							 .Select(i => i * sampleRate / fftSize)
							 .ToArray();
		}
	}
}
