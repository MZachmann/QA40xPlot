using FftSharp;
using QA40xPlot.Libraries;
using System.Numerics;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40xPlot.BareMetal
{
	public static class Chirps
	{
		/// <summary>
		/// creates an inverse filter
		/// </summary> 
		/// <param name="totalBufferLength">amount of buffer to fill</param>
		/// <param name="fs">sampling frequency</param>
		/// <param name="amplitudeVpk">peak voltage of signal</param>
		/// <param name="f1">start freq</param>
		/// <param name="f2">end freq</param>
		/// <param name="pct">amount of buffer to fill with signal 1.0 == all</param>
		/// <returns>(chirp,inverse)</returns>
		public static double[] ChirpInverse(double[] chirpSignal, int totalBufferLength, double fs, double amplitudeVrms, double f1 = 20, double f2 = 20000, double pct = 0.6)
		{
			// Calculate the length of the chirp in samples
			int chirpLengthSamples = (int)(totalBufferLength * pct);

			// Duration of the chirp in seconds
			double T = chirpLengthSamples / fs;

			// Chirp rate (logarithmic)
			double R = Math.Log(f2 / f1);

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

			return inverseFilter;
		}

		/// <summary>
		/// creates a chirp signal and an inverse filter
		/// </summary> 
		/// <param name="totalBufferLength">amount of buffer to fill</param>
		/// <param name="fs">sampling frequency</param>
		/// <param name="amplitudeVpk">peak voltage of signal</param>
		/// <param name="f1">start freq</param>
		/// <param name="f2">end freq</param>
		/// <param name="pct">amount of buffer to fill with signal 1.0 == all</param>
		/// <returns>(chirp,inverse)</returns>
		public static double[] ChirpVp(int totalBufferLength, double fs, double amplitudeVrms, double f1 = 20, double f2 = 20000, double pct = 0.6)
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
			var vpk = amplitudeVrms * Math.Sqrt(2);
			double[] chirpSignal = t.Select(time => vpk * Math.Sin((2 * Math.PI * f1 * T / R) * (Math.Exp(time * R / T) - 1))).ToArray();

			// Calculate the required padding length
			int padding = totalBufferLength - chirpSignal.Length;

			// Pad the chirp signal with zeros to fit the total buffer length
			double[] paddedChirp = chirpSignal.Concat(new double[padding]).ToArray();

			return paddedChirp;
		}

		public static (Complex[], Complex[]) NormalizeChirpCplx(string windowing, double[] chirp, double Vrms, (double[]? leftData, double[]? rightData) rdata)
		{
			Complex[] leftFft = [];
			Complex[] rightFft = [];

			 var window = QaMath.GetWindowType(windowing);    // best?

			double[] inp = window.Apply(chirp, true);  // the input signal
			var chirpFft = FFT.Forward(inp);
			var cmax = Vrms;// * Math.Sqrt(2) / 2;     // not sure why the /2 here - rectangular window?

			// Left channel

			chirpFft = chirpFft.Select(x => (x.Real != 0 || x.Imaginary != 0) ? x : new Complex(1e-10,0)).ToArray(); // avoid divide by zero

			if (rdata.leftData != null)
			{
				double[] lftWdw = window.Apply(rdata.leftData, true);
				var lFft = FFT.Forward(lftWdw);
				lFft = lFft.Select((x, index) => cmax * x / chirpFft[index]).ToArray();
				leftFft = lFft.Take(lFft.Length / 2).ToArray();
			}

			if (rdata.rightData != null)
			{
				double[] rgtWdw = window.Apply(rdata.rightData, true);
				var rFft = FFT.Forward(rgtWdw);
				rFft = rFft.Select((x, index) => cmax * x / chirpFft[index]).ToArray();
				rightFft = rFft.Take(rFft.Length / 2).ToArray();
			}
			return (leftFft, rightFft);
		}

		public static (double[], double[]) NormalizeChirpDbl(string windowing, double[] chirp, double Vrms, (double[]? leftData, double[]? rightData) rdata)
		{
			var cplx = NormalizeChirpCplx(windowing, chirp, Vrms, rdata);
			double[] leftFft = cplx.Item1.Select(x => x.Magnitude).ToArray();
			double[] rightFft = cplx.Item2.Select(x => x.Magnitude).ToArray();
			return (leftFft, rightFft);
		}

		/// <summary>
		/// Normalizes the impulse response and computes the FFT of the DUT chirp.
		/// </summary>
		/// <param name="chirp">The chirp signal from the device under test (DUT).</param>
		/// <param name="inverseFilter">The inverse filter to be convolved with the chirp</param>
		/// <param name="targetSampleRate"></param>
		/// <param name="applyWindow">defaults to false</param>
		/// <param name="windowStartTime">The time before the IR peak for the window to start (in seconds)</param>
		/// <param name="windowEndTime"> The time after the IR peak for the window to stop (in seconds)</param>
		/// <param name="rampUpTime">The ramp-up time of the window (in seconds)</param>
		/// <param name="rampDownTime">The ramp-down time of the window (in seconds)</param>
		/// <returns>freq bins, fftDutV array, ir_dut impulse response, window</returns>
		public static (double[], Complex[], double[], double[]) NormalizeAndComputeFft(
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
			Complex[] fftDut = MyFft(ir);
			fftDut = fftDut.Take(fftDut.Length / 2).ToArray(); // Take the positive frequency components

			// Compute frequency bins
			double[] freq = Enumerable.Range(0, fftDut.Length).Select(i => i * targetSampleRate / chirp.Length).ToArray();

			// Normalize FFT by the length of the FFT
			Complex[] fftDutNormalized = fftDut.Select(c => c / fftDut.Length).ToArray();

			return (freq, fftDutNormalized, ir, window);
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
			Complex[] signalFft = MyFft(signal);
			Complex[] filterFft = MyFft(filter);
			Complex[] resultFft = signalFft.Zip(filterFft, (s, f) => s * f).ToArray();
			return IFFT(resultFft).Select(c => c.Real).ToArray();
		}

		private static Complex[] MyFft(double[] signal)
		{
			// Implement FFT logic
			Complex[] spectra = FftSharp.FFT.Forward(signal).ToArray();
			return spectra;
		}

		private static Complex[] IFFT(Complex[] signal)
		{
			// Implement IFFT logic or use a library
			var toif = (Complex[])signal.Clone();
			FftSharp.FFT.Inverse(toif);
			return toif;
		}
	}
}