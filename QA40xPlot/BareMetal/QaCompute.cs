
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
		internal struct EnbwMath
		{
			public double Enbw;
			public double WindowSize;
			public string WindowType;
			public EnbwMath(double enbw, double windowSize, string windowType)
			{
				Enbw = enbw;
				WindowSize = windowSize;
				WindowType = windowType;
			}
		}

		/// <summary>
		/// Get the maximum value from a range of frequencies
		/// </summary>
		/// <param name="signalFreqLin"></param>
		/// <param name="frequencies"></param>
		/// <param name="minFreq"></param>
		/// <param name="maxFreq"></param>
		/// <returns></returns>
		internal static double GetMaxInRange(double[] signalFreqLin, double[] frequencies, double minFreq, double maxFreq)
		{
			double maxi = 0.0;
			for(int i=0; i<signalFreqLin.Length; i++)
			{
				if (frequencies[i] >= minFreq && frequencies[i] <= maxFreq)
				{
					if( maxi < signalFreqLin[i])
						maxi = signalFreqLin[i];
				}
			}
			return maxi;
		}

		internal static LeftRightPair GetSnrImdDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double fund2Freq)
		{
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var thdLeft = ComputeImdSnrRatio(windowing, ffs, lrs.Df, fundFreq, fund2Freq, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeImdSnrRatio(windowing, ffs, lrs.Df, fundFreq, fund2Freq, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetSnrDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double minFreq, double maxFreq)
		{
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var thdLeft = ComputeSnrRatio(windowing, ffs, lrs.Df, fundFreq, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeSnrRatio(windowing, ffs, lrs.Df, fundFreq, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetThdDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double minFreq, double maxFreq)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental,
			// int numHarmonics = 5, bool debug = false
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var thdLeft = ComputeThdLinear(windowing, ffs, lrs.Df, fundFreq, 5, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeThdLinear(windowing, ffs, lrs.Df, fundFreq, 5, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetThdnDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double minFreq, double maxFreq)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental, double notchOctaves = 0.5,
			// double startFreq = 20.0, double stopFreq = 20000.0, bool debug = false
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var thdLeft = ComputeThdnLinear(windowing, ffs, lrs.Df, fundFreq);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeThdnLinear(windowing, ffs, lrs.Df, fundFreq);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static double ComputeSnrRatio(string windowing, double[] signalFreqLin, double df, double fundamental, bool debug = false)
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
			double fundamentalRms = ComputeRmsF(signalFreqLin, df, notchLowerBound, notchUpperBound, windowing);

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsF(signalFreqLin, df, 20, notchLowerBound, windowing);
			double rmsAboveNotch = ComputeRmsF(signalFreqLin, df, notchUpperBound, 20000, windowing);
			double noiseRms = Math.Sqrt(Math.Pow(rmsBelowNotch, 2) + Math.Pow(rmsAboveNotch, 2));

			if (debug)
			{
				Debug.WriteLine($"RMS Below Notch: {rmsBelowNotch:F6}");
				Debug.WriteLine($"RMS Above Notch: {rmsAboveNotch:F6}");
				Debug.WriteLine($"Noise RMS: {noiseRms:F6}");
			}

			return fundamentalRms / noiseRms;
		}

		internal static double ComputeImdSnrRatio(string windowing, double[] signalFreqLin, double df, double fundamental, double fundamental2, bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			//var notchOctaves = 0.5; // aes-17 2015 standard notch
			var notchOctaves = 0.05; // my preferred notch much tighter and more realistic nowadays
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = ComputeRmsF(signalFreqLin, df, notchLowerBound, notchUpperBound, windowing);
			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsF(signalFreqLin, df, 20, notchLowerBound, windowing);
			double rmsAboveNotch = ComputeRmsF(signalFreqLin, df, notchUpperBound, Math.Max(20000, notchUpperBound*1.5), windowing);

			if (fundamental2 > 0.0)
			{
				notchLowerBound = fundamental2 / Math.Pow(2, notchOctaves);
				notchUpperBound = fundamental2 * Math.Pow(2, notchOctaves);
				var fundamental2Rms = ComputeRmsF(signalFreqLin, df, notchLowerBound, notchUpperBound, windowing);
				rmsAboveNotch = Math.Sqrt(Math.Pow(rmsAboveNotch, 2) - Math.Pow(fundamental2Rms, 2));
				fundamentalRms = Math.Sqrt(Math.Pow(fundamentalRms, 2) + Math.Pow(fundamental2Rms, 2));
			}
			double noiseRms = Math.Sqrt(Math.Pow(rmsBelowNotch, 2) + Math.Pow(rmsAboveNotch, 2));

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
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
		internal static double ComputeThdLinear(string windowing, double[] signalFreqLin, double df, double fundamental, int numHarmonics = 5, bool debug = false)
		{
			var maxFreq = df * signalFreqLin.Length;

			double fundamentalAmplitude = QaMath.MagAtFreq(signalFreqLin, df, fundamental);

			// Debugging: Show the peak amplitude in dB
			if (debug)
			{
				double fundamentalAmplitudeDb = 20 * Math.Log10(fundamentalAmplitude);
				Debug.WriteLine($"Fundamental Amplitude: {fundamentalAmplitude:F6} (Linear), {fundamentalAmplitudeDb:F2} dB");
			}

			// Calculate the sum of squares of the harmonic amplitudes
			double harmonicAmplitudesSqSum = 0.0;
			for (int n = 2; n <= numHarmonics; n++)
			{
				double harmonicFreq = n * fundamental;
				if (harmonicFreq > maxFreq)
					break;

				double harmonicAmplitude = QaMath.MagAtFreq(signalFreqLin, df, harmonicFreq);
				harmonicAmplitudesSqSum += Math.Pow(harmonicAmplitude, 2);

				// Debugging: Show the harmonic amplitude in dB and the bins being examined
				if (debug)
				{
					double harmonicAmplitudeDb = 20 * Math.Log10(harmonicAmplitude);
					Debug.WriteLine($"{n}x Harmonic Amplitude: {harmonicAmplitude:F6} (Linear), {harmonicAmplitudeDb:F2} dB");
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
		internal static double ComputeThdnLinear(string windowing, double[] signalFreqLin, double df, double fundamental, double notchOctaves = 0.5, double startFreq = 20.0, double stopFreq = 20000.0, bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = ComputeRmsF(signalFreqLin, df, notchLowerBound, notchUpperBound, windowing);

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsF(signalFreqLin, df, startFreq, notchLowerBound, windowing);
			double rmsAboveNotch = ComputeRmsF(signalFreqLin, df, notchUpperBound, stopFreq, windowing);
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

		// calculate the noise from 20..20000Hz
		internal static LeftRightPair CalculateNoise(string windowing, LeftRightFrequencySeries? lrfs)
		{
			if (lrfs == null)
				return new LeftRightPair(1e-20, 1e-20);

			var noiseLeft = QaCompute.ComputeRmsF(lrfs.Left, lrfs.Df, 20, 20000, windowing);
			var noiseRight = QaCompute.ComputeRmsF(lrfs.Right, lrfs.Df, 20, 20000, windowing);
			// calculate the noise floor
			return new LeftRightPair(noiseLeft, noiseRight);
		}

		internal static List<EnbwMath> EnbwMaths = new List<EnbwMath>();

		// caching version of calculateEnbw
		internal static double ComputeEnbw(string windowType, uint windowSize)
		{
			// check if we already have this in the list
			var em = EnbwMaths.FirstOrDefault(x => x.WindowType == windowType && x.WindowSize == windowSize);
			if (em.WindowType == windowType)
				return em.Enbw;
			// calculate the ENBW
			var enbw = CalculateEnbw(windowType, windowSize);
			EnbwMaths.Add(new EnbwMath(enbw, windowSize, windowType));
			return enbw;
		}

		// the equivalent noise bandwidth (ENBW) of a window
		// we need to divide by this to get actual RMS power from the windowed FFT result
		internal static double CalculateEnbw(string windowType, uint windowSize)
		{
			var wdw = QaMath.GetWindowType(windowType);
			if(wdw != null)
			{
				var n = windowSize; // number of points in the window
				var u = wdw.Create((int)n, true); // create a window of 1024 points
				var ww = n * u.Sum(x => x * x) / (u.Sum() * u.Sum());
				// here ww is in power, so in voltage it's sqrt
				return Math.Sqrt(ww);
			}
			return 1.0;
		}

		/// <summary>
		/// return the RMS voltage of a time signal
		/// </summary>
		/// <param name="signalTime"></param>
		/// <returns></returns>
		internal static double ComputeRmsTime(double[] signalTime)
		{
			return Math.Sqrt(signalTime.Sum(x => x * x) / signalTime.Length); // rms voltage
		}

		/// <summary>
		/// calculate the total power of a frequency signal in usual linear frequency format
		/// </summary>
		/// <param name="signalFreqLin"></param>
		/// <param name="df">the bin size</param>
		/// <param name="lowerBound">lower frequency</param>
		/// <param name="upperBound">upper frequency</param>
		/// <returns>the equivalent rms voltage in this fft chunk via the total power</returns>
		internal static double ComputeRmsF(double[] signalFreqLin, double df, double lowerBound, double upperBound, string windowing)
		{
			double sum = 0;
			try
			{
				var lb = Math.Max(1,(int)(lowerBound / df));
				var ub = Math.Min(signalFreqLin.Length-1, (int)(upperBound / df));
				for (int i = lb; i < ub; i++)
				{
					sum += signalFreqLin[i] * signalFreqLin[i];
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in ComputeRmsF: {ex.Message}");
			}
			return Math.Sqrt(sum) / CalculateEnbw(windowing, (uint)signalFreqLin.Length); // RMS calculation
		}

		/// <summary>
		/// calculate the total power of a frequency signal
		/// </summary>
		/// <param name="signalFreqLin"></param>
		/// <param name="frequencies"></param>
		/// <param name="lowerBound"></param>
		/// <param name="upperBound"></param>
		/// <returns></returns>
		//internal static double ComputeRmsFreq(double[] signalFreqLin, double[] frequencies, double lowerBound, double upperBound)
		//{
		//	// *****************************************************************
		//	// note that this is ~20 times faster than the Freq2 version below...
		//	// *****************************************************************
		//	double sum = 0;
		//	for (int i = 0; i < signalFreqLin.Length; i++)
		//	{
		//		if (frequencies[i] >= lowerBound && frequencies[i] <= upperBound)
		//		{
		//			sum += signalFreqLin[i] * signalFreqLin[i];
		//		}
		//	}
		//	return Math.Sqrt(sum); // RMS calculation

		//}
	}
}