
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Diagnostics;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40xPlot.BareMetal
{
	public static class QaCompute
	{
		internal static LeftRightPair GetSnrImdDb(LeftRightFrequencySeries lrs, double fundFreq, double fund2Freq)
		{
			if (lrs == null)
				return new();

			var frqs = Enumerable.Range(0, lrs.Left.Length).Select(x => x * lrs.Df).ToArray();
			var ffs = lrs.Left;
			var thdLeft = ComputeImdSnrRatio(ffs, frqs, fundFreq, fund2Freq, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeImdSnrRatio(ffs, frqs, fundFreq, fund2Freq, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetSnrDb(LeftRightSeries lrs, double fundFreq, double minFreq, double maxFreq)
		{
			if (lrs.FreqRslt == null)
				return new();

			var frqs = Enumerable.Range(0, lrs.FreqRslt.Left.Length).Select(x => x * lrs.FreqRslt.Df).ToArray();
			var ffs = lrs.FreqRslt.Left;
			var thdLeft = ComputeSnrRatio(ffs, frqs, fundFreq, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.FreqRslt.Right;
			var thdRight = ComputeSnrRatio(ffs, frqs, fundFreq, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetThdDb(LeftRightSeries lrs, double fundFreq, double minFreq, double maxFreq)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental,
			// int numHarmonics = 5, bool debug = false
			if (lrs.FreqRslt == null)
				return new();

			var frqs = Enumerable.Range(0, lrs.FreqRslt.Left.Length).Select(x => x * lrs.FreqRslt.Df).ToArray();
			var ffs = lrs.FreqRslt.Left;
			var thdLeft = ComputeThdLinear(ffs, frqs, fundFreq, 5, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.FreqRslt.Right;
			var thdRight = ComputeThdLinear(ffs, frqs, fundFreq, 5, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetThdnDb(LeftRightSeries lrs, double fundFreq, double minFreq, double maxFreq)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental, double notchOctaves = 0.5,
			// double startFreq = 20.0, double stopFreq = 20000.0, bool debug = false
			if (lrs.FreqRslt == null)
				return new();

			var frqs = Enumerable.Range(0, lrs.FreqRslt.Left.Length).Select(x => x * lrs.FreqRslt.Df).ToArray();
			var ffs = lrs.FreqRslt.Left;
			var thdLeft = ComputeThdnLinear(ffs, frqs, fundFreq);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.FreqRslt.Right;
			var thdRight = ComputeThdnLinear(ffs, frqs, fundFreq);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static double ComputeSnrRatio(double[] signalFreqLin, double[] frequencies, double fundamental, bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			//var notchOctaves = 0.5; // aes-17 2015 standard notch
			var notchOctaves = 0.1; // my preferred notch much tighter and more realistic nowadays
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = ComputeRmsFreq(signalFreqLin, frequencies, notchLowerBound, notchUpperBound);

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsFreq(signalFreqLin, frequencies, 20, notchLowerBound);
			double rmsAboveNotch = ComputeRmsFreq(signalFreqLin, frequencies, notchUpperBound, 20000);
			double noiseRms = Math.Sqrt(Math.Pow(rmsBelowNotch, 2) + Math.Pow(rmsAboveNotch, 2));

			if (debug)
			{
				Debug.WriteLine($"RMS Below Notch: {rmsBelowNotch:F6}");
				Debug.WriteLine($"RMS Above Notch: {rmsAboveNotch:F6}");
				Debug.WriteLine($"Noise RMS: {noiseRms:F6}");
			}

			return fundamentalRms / noiseRms;
		}

		internal static double ComputeImdSnrRatio(double[] signalFreqLin, double[] frequencies, double fundamental, double fundamental2, bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			//var notchOctaves = 0.5; // aes-17 2015 standard notch
			var notchOctaves = 0.1; // my preferred notch much tighter and more realistic nowadays
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = ComputeRmsFreq(signalFreqLin, frequencies, notchLowerBound, notchUpperBound);
			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsFreq(signalFreqLin, frequencies, 20, notchLowerBound);
			double rmsAboveNotch = ComputeRmsFreq(signalFreqLin, frequencies, notchUpperBound, 20000);

			if (fundamental2 > 0.0)
			{
				notchLowerBound = fundamental2 / Math.Pow(2, notchOctaves);
				notchUpperBound = fundamental2 * Math.Pow(2, notchOctaves);
				var fundamental2Rms = ComputeRmsFreq(signalFreqLin, frequencies, notchLowerBound, notchUpperBound);
				rmsAboveNotch = Math.Sqrt(Math.Pow(rmsAboveNotch, 2) - Math.Pow(fundamental2Rms, 2));
				fundamentalRms = Math.Sqrt(Math.Pow(fundamentalRms, 2) + Math.Pow(fundamental2Rms, 2));
			}
			double noiseRms = Math.Sqrt(Math.Pow(rmsBelowNotch, 2) + Math.Pow(rmsAboveNotch, 2));

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			if (debug)
			{
				Debug.WriteLine($"RMS Below Notch: {rmsBelowNotch:F6}");
				Debug.WriteLine($"RMS Above Notch: {rmsAboveNotch:F6}");
				Debug.WriteLine($"Noise RMS: {noiseRms:F6}");
			}

			return fundamentalRms / noiseRms;
		}

		/// <summary>
		/// Compute the Total Harmonic Distortion (THD) from double[] result
		/// </summary>
		/// <param name="signalFreqLin"></param>
		/// <param name="frequencies"></param>
		/// <param name="fundamental"></param>
		/// <param name="numHarmonics"></param>
		/// <param name="debug"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		internal static double ComputeThdLinear(double[] signalFreqLin, double[] frequencies, double fundamental, int numHarmonics = 5, bool debug = false)
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
				Debug.WriteLine($"Fundamental Frequency: {frequencies[fundamentalIdx]:F2} Hz (bin {fundamentalIdx})");
				Debug.WriteLine($"Fundamental Amplitude: {fundamentalAmplitude:F6} (Linear), {fundamentalAmplitudeDb:F2} dB");
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
						Debug.WriteLine($"No harmonic indices found within the specified window for {n}x harmonic.");
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
					Debug.WriteLine($"{n}x Harmonic Frequency: {harmonicFreq:F2} Hz (closest bin {harmonicIdx})");
					Debug.WriteLine($"{n}x Harmonic Amplitude: {harmonicAmplitude:F6} (Linear), {harmonicAmplitudeDb:F2} dB");

					// Additional debugging: Show the amplitudes of the bins around the harmonic
					for (int offset = -2; offset <= 2; offset++)
					{
						int idx = harmonicIdx + offset;
						if (idx >= 0 && idx < signalFreqLin.Length)
						{
							double amplitude = signalFreqLin[idx];
							double amplitudeDb = 20 * Math.Log10(amplitude);
							Debug.WriteLine($"Bin {idx} Frequency: {frequencies[idx]:F2} Hz, Amplitude: {amplitude:F6} (Linear), {amplitudeDb:F2} dB");
						}
					}
				}
			}

			// Compute THD
			double thd = Math.Sqrt(harmonicAmplitudesSqSum) / fundamentalAmplitude;

			// Debugging: Show THD computation details
			if (debug)
			{
				Debug.WriteLine($"Sum of Squares of Harmonic Amplitudes: {harmonicAmplitudesSqSum:F6}");
				Debug.WriteLine($"THD: {thd:F6} (Linear)");
			}

			return thd;
		}

		/// <summary>
		/// Computer the Total Harmonic Distortion + Noise (THDN) from double[] result
		/// </summary>
		/// <param name="signalFreqLin"></param>
		/// <param name="frequencies"></param>
		/// <param name="fundamental"></param>
		/// <param name="notchOctaves"></param>
		/// <param name="startFreq"></param>
		/// <param name="stopFreq"></param>
		/// <param name="debug"></param>
		/// <returns></returns>
		internal static double ComputeThdnLinear(double[] signalFreqLin, double[] frequencies, double fundamental, double notchOctaves = 0.5, double startFreq = 20.0, double stopFreq = 20000.0, bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = ComputeRmsFreq(signalFreqLin, frequencies, notchLowerBound, notchUpperBound);

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsFreq(signalFreqLin, frequencies, startFreq, notchLowerBound);
			double rmsAboveNotch = ComputeRmsFreq(signalFreqLin, frequencies, notchUpperBound, stopFreq);
			double noiseRms = Math.Sqrt(Math.Pow(rmsBelowNotch, 2) + Math.Pow(rmsAboveNotch, 2));

			if (debug)
			{
				Debug.WriteLine($"RMS Below Notch: {rmsBelowNotch:F6}");
				Debug.WriteLine($"RMS Above Notch: {rmsAboveNotch:F6}");
				Debug.WriteLine($"Noise RMS: {noiseRms:F6}");
			}

			// Calculate THDN
			double thdn = noiseRms / fundamentalRms;

			if (debug)
			{
				Debug.WriteLine($"THDN: {thdn:F6} (Linear)");
			}

			return thdn;
		}

		/// <summary>
		/// calculate the total power of a frequency signal
		/// </summary>
		/// <param name="signalFreqLin"></param>
		/// <param name="frequencies"></param>
		/// <param name="lowerBound"></param>
		/// <param name="upperBound"></param>
		/// <returns></returns>
		internal static double ComputeRmsFreq(double[] signalFreqLin, double[] frequencies, double lowerBound, double upperBound)
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
			return Math.Sqrt(sumSquares); // RMS calculation
		}
	}
}