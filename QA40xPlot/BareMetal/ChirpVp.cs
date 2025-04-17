using System.Numerics;

namespace QA40xPlot.BareMetal
{
	public static class Chirps
	{
		public static (double[], double[]) ChirpVp(int totalBufferLength, double fs, double amplitudeVpk, double f1 = 20, double f2 = 20000, double pct = 0.6)
		{
			// Calculate the length of the chirp in samples
			int chirpLengthSamples = (int)(totalBufferLength * pct);

			// Duration of the chirp in seconds
			double T = chirpLengthSamples / fs;

			// Time array for the chirp duration
			double[] t = Enumerable.Range(0, chirpLengthSamples).Select(i => i / fs).ToArray();

			// Chirp rate (logarithmic)
			double R = Math.Log(f2 / f1);

			// Generate the chirp signal
			double[] chirpSignal = t.Select(time => amplitudeVpk * Math.Sin((2 * Math.PI * f1 * T / R) * (Math.Exp(time * R / T) - 1))).ToArray();

			// Calculate the start and end indices of the window
			int windowStart = 0;
			int windowEnd = chirpLengthSamples;
			int rampLength = chirpLengthSamples / 10; // Fixed ramp-up and ramp-down length

			// Apply window function
			double[] window = GenerateWindow(chirpSignal.Length, windowStart, windowEnd, rampLength, rampLength);
			chirpSignal = chirpSignal.Select((value, index) => value * window[index]).ToArray();

			// Calculate the required padding length
			int padding = totalBufferLength - chirpSignal.Length;

			// Pad the chirp signal with zeros to fit the total buffer length
			double[] paddedChirp = chirpSignal.Concat(new double[padding]).ToArray();

			// Time array for the padded chirp
			double[] paddedT = Enumerable.Range(0, paddedChirp.Length).Select(i => i / fs).ToArray();

			// Scaling factor for the inverse filter
			double[] k = paddedT.Select(time => Math.Exp(time * R / T)).ToArray();

			// Generate the inverse filter by reversing and scaling the chirp signal
			double[] inverseFilter = paddedChirp.Reverse().Select((value, index) => value / k[index]).ToArray();

			return (paddedChirp, inverseFilter);
		}

		public static (double[], double[], double[], double[]) NormalizeAndComputeFft(
			double[] chirp, double[] inverseFilter, double targetSampleRate, bool applyWindow = false,
			double windowStartTime = 0.005, double windowEndTime = 0.01,
			double rampUpTime = 0.0001, double rampDownTime = 0.001)
		{
			// Compute impulse response by convolving DUT chirp with inverse filter
			double[] ir = FftConvolve(chirp, inverseFilter);

			// Determine window parameters in samples
			int windowStart = (int)(windowStartTime * targetSampleRate);
			int windowEnd = (int)(windowEndTime * targetSampleRate);
			int rampUp = (int)(rampUpTime * targetSampleRate);
			int rampDown = (int)(rampDownTime * targetSampleRate);

			// Generate window
			int peakIndex = Array.IndexOf(ir, ir.Max());
			int bufferSize = ir.Length;
			double[] window = GenerateWindow(bufferSize, peakIndex - windowStart, peakIndex + windowEnd, rampUp, rampDown);

			// Apply window to IR if selected
			if (applyWindow)
			{
				ir = ir.Select((value, index) => value * window[index]).ToArray();
			}

			// Compute FFT of the impulse response
			Complex[] fftDut = FFT(ir);
			fftDut = fftDut.Take(fftDut.Length / 2).ToArray(); // Take the positive frequency components

			// Compute frequency bins
			double[] freq = Enumerable.Range(0, chirp.Length / 2).Select(i => i * targetSampleRate / chirp.Length).ToArray();

			// Normalize FFT by the length of the FFT
			double[] fftDutNormalized = fftDut.Select(c => c.Magnitude / fftDut.Length).ToArray();

			// Convert to dBV
			double[] fftDutDb = fftDutNormalized.Select(value => 20 * Math.Log10(value)).ToArray();

			return (freq, fftDutDb, ir, window);
		}

		private static double[] GenerateWindow(int length, int start, int end, int rampUp, int rampDown)
		{
			double[] window = new double[length];
			for (int i = 0; i < length; i++)
			{
				if (i < start || i > end)
				{
					window[i] = 0;
				}
				else if (i < start + rampUp)
				{
					window[i] = (double)(i - start) / rampUp;
				}
				else if (i > end - rampDown)
				{
					window[i] = (double)(end - i) / rampDown;
				}
				else
				{
					window[i] = 1;
				}
			}
			return window;
		}

		private static double[] FftConvolve(double[] signal, double[] filter)
		{
			int n = signal.Length + filter.Length - 1;
			Complex[] signalFft = FFT(signal, n);
			Complex[] filterFft = FFT(filter, n);
			Complex[] resultFft = signalFft.Zip(filterFft, (s, f) => s * f).ToArray();
			return IFFT(resultFft).Select(c => c.Real).ToArray();
		}

		private static Complex[] FFT(double[] signal, int n = -1)
		{
			// Implement FFT logic or use a library
			throw new NotImplementedException();
		}

		private static Complex[] IFFT(Complex[] signal)
		{
			// Implement IFFT logic or use a library
			throw new NotImplementedException();
		}
	}
}