
namespace QA40xPlot.BareMetal
{
	public static class QaCompute
	{
		public static double ComputeThdLinear(double[] signalFreqLin, double[] frequencies, double fundamental, int numHarmonics = 5, bool debug = false)
		{
			const double windowHzPm = 10.0; // Set the window for searching the actual fundamental and harmonics

			// Find the actual fundamental frequency within the specified window
			double lowerBound = fundamental - windowHzPm;
			double upperBound = fundamental + windowHzPm;
			var fundamentalIndices = frequencies
				.Select((freq, index) => (freq, index))
				.Where(f => f.freq >= lowerBound && f.freq <= upperBound)
				.Select(f => f.index)
				.ToArray();

			if (fundamentalIndices.Length == 0)
			{
				throw new ArgumentException($"No fundamental frequency found within the specified window ({lowerBound} Hz to {upperBound} Hz).");
			}

			int fundamentalIdx = fundamentalIndices.OrderByDescending(idx => signalFreqLin[idx]).First();
			double fundamentalAmplitude = signalFreqLin[fundamentalIdx];

			// Debugging: Show the peak amplitude in dB
			if (debug)
			{
				double fundamentalAmplitudeDb = 20 * Math.Log10(fundamentalAmplitude);
				Console.WriteLine($"Fundamental Frequency: {frequencies[fundamentalIdx]:F2} Hz (bin {fundamentalIdx})");
				Console.WriteLine($"Fundamental Amplitude: {fundamentalAmplitude:F6} (Linear), {fundamentalAmplitudeDb:F2} dB");
			}

			// Calculate the sum of squares of the harmonic amplitudes
			double harmonicAmplitudesSqSum = 0.0;
			for (int n = 2; n <= numHarmonics; n++)
			{
				double harmonicFreq = n * fundamental;
				double lowerBoundHarmonic = harmonicFreq - windowHzPm;
				double upperBoundHarmonic = harmonicFreq + windowHzPm;

				var harmonicIndices = frequencies
					.Select((freq, index) => (freq, index))
					.Where(f => f.freq >= lowerBoundHarmonic && f.freq <= upperBoundHarmonic)
					.Select(f => f.index)
					.ToArray();

				if (harmonicIndices.Length == 0)
				{
					if (debug)
					{
						Console.WriteLine($"No harmonic indices found within the specified window for {n}x harmonic.");
					}
					continue;
				}

				int harmonicIdx = harmonicIndices.OrderByDescending(idx => signalFreqLin[idx]).First();
				double harmonicAmplitude = harmonicIdx < signalFreqLin.Length ? signalFreqLin[harmonicIdx] : 0.0;
				harmonicAmplitudesSqSum += Math.Pow(harmonicAmplitude, 2);

				// Debugging: Show the harmonic amplitude in dB and the bins being examined
				if (debug)
				{
					double harmonicAmplitudeDb = 20 * Math.Log10(harmonicAmplitude);
					Console.WriteLine($"{n}x Harmonic Frequency: {harmonicFreq:F2} Hz (closest bin {harmonicIdx})");
					Console.WriteLine($"{n}x Harmonic Amplitude: {harmonicAmplitude:F6} (Linear), {harmonicAmplitudeDb:F2} dB");

					// Additional debugging: Show the amplitudes of the bins around the harmonic
					for (int offset = -2; offset <= 2; offset++)
					{
						int idx = harmonicIdx + offset;
						if (idx >= 0 && idx < signalFreqLin.Length)
						{
							double amplitude = signalFreqLin[idx];
							double amplitudeDb = 20 * Math.Log10(amplitude);
							Console.WriteLine($"Bin {idx} Frequency: {frequencies[idx]:F2} Hz, Amplitude: {amplitude:F6} (Linear), {amplitudeDb:F2} dB");
						}
					}
				}
			}

			// Compute THD
			double thd = Math.Sqrt(harmonicAmplitudesSqSum) / fundamentalAmplitude;

			// Debugging: Show THD computation details
			if (debug)
			{
				Console.WriteLine($"Sum of Squares of Harmonic Amplitudes: {harmonicAmplitudesSqSum:F6}");
				Console.WriteLine($"THD: {thd:F6} (Linear)");
			}

			return thd;
		}

		public static double ComputeThdnLinear(double[] signalFreqLin, double[] frequencies, double fundamental, double notchOctaves = 0.5, double startFreq = 20.0, double stopFreq = 20000.0, bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);

			if (debug)
			{
				Console.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = ComputeRmsFreq(signalFreqLin, frequencies, notchLowerBound, notchUpperBound);

			if (debug)
			{
				Console.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsFreq(signalFreqLin, frequencies, startFreq, notchLowerBound);
			double rmsAboveNotch = ComputeRmsFreq(signalFreqLin, frequencies, notchUpperBound, stopFreq);
			double noiseRms = Math.Sqrt(Math.Pow(rmsBelowNotch, 2) + Math.Pow(rmsAboveNotch, 2));

			if (debug)
			{
				Console.WriteLine($"RMS Below Notch: {rmsBelowNotch:F6}");
				Console.WriteLine($"RMS Above Notch: {rmsAboveNotch:F6}");
				Console.WriteLine($"Noise RMS: {noiseRms:F6}");
			}

			// Calculate THDN
			double thdn = noiseRms / fundamentalRms;

			if (debug)
			{
				Console.WriteLine($"THDN: {thdn:F6} (Linear)");
			}

			return thdn;
		}

		public static double ComputeRmsFreq(double[] signalFreqLin, double[] frequencies, double lowerBound, double upperBound)
		{
			var indices = frequencies
				.Select((freq, index) => (freq, index))
				.Where(f => f.freq >= lowerBound && f.freq <= upperBound)
				.Select(f => f.index)
				.ToArray();

			if (indices.Length == 0)
			{
				return 0.0;
			}

			double sumSquares = indices.Sum(idx => Math.Pow(signalFreqLin[idx], 2));
			return Math.Sqrt(sumSquares / indices.Length);
		}
	}
}