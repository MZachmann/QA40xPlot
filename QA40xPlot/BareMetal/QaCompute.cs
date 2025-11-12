
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Interop;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40xPlot.BareMetal
{
	public static class QaCompute
	{
		internal enum E_ImdMethod
		{
			CCIF, // CCIF method for intermodulation distortion
			Power, // Power method for intermodulation distortion
			DIN // DIN method for intermodulation distortion
		}

		internal static E_ImdMethod GetImdMethod(double[] fundFreqs)
		{
			Debug.Assert(fundFreqs.Length == 2, "fundFreqs should have exactly two frequencies for IMD calculation.");
			var ratio = fundFreqs[1] / fundFreqs[0];
			if (ratio < 2)
				return E_ImdMethod.CCIF;
			else if (ratio < 7)
				return E_ImdMethod.Power;
			else
				return E_ImdMethod.DIN;
		}

		internal static double GetImdDenom(E_ImdMethod method, double vf1, double vf2)
		{
			switch (method)
			{
				case E_ImdMethod.CCIF:
					return vf1 + vf2; // CCIF method uses F2+F1 as the denominator
				case E_ImdMethod.Power:
					return Math.Sqrt(vf1 * vf1 + vf2 * vf2); // Power method uses sqrt(F2^2+F1^2) as the denominator
				case E_ImdMethod.DIN:
					return vf2; // DIN method uses F2 as the denominator
				default:
					throw new ArgumentException("Invalid IMD method specified.");
			}
		}

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

		internal static LeftRightPair GetSnrImdDb(string windowing, LeftRightFrequencySeries lrs, double[] fundFreqs, double minFreq, double maxFreq, string weighting)
		{
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var thdLeft = ComputeImdSnrRatio(windowing, ffs, lrs.Df, fundFreqs, minFreq, maxFreq, weighting, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeImdSnrRatio(windowing, ffs, lrs.Df, fundFreqs, minFreq, maxFreq, weighting, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetSnrDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double minFreq, double maxFreq, string weighting)
		{
			if (lrs == null)
				return new(200,200);

			if(minFreq >= maxFreq)
			{
				Debug.WriteLine("minFreq must be less than maxFreq in GetSnrDb");
				return new(200, 200);
			}

			var ffs = lrs.Left;
			var thdLeft = ComputeSnrRatio(windowing, ffs, lrs.Df, fundFreq, minFreq, maxFreq, weighting, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeSnrRatio(windowing, ffs, lrs.Df, fundFreq, minFreq, maxFreq, weighting, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetSinadDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double minFreq, double maxFreq, string weighting)
		{
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var thdLeft = ComputeSinadRatio(windowing, ffs, lrs.Df, fundFreq, minFreq, maxFreq, weighting, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeSinadRatio(windowing, ffs, lrs.Df, fundFreq, minFreq, maxFreq, weighting, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static LeftRightPair GetThdDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double minFreq, double maxFreq)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental,
			// int numHarmonics = 5, bool debug = false
			if (lrs == null)
				return new(0,0);

			var ffs = lrs.Left;
			var thdLeft = ComputeThdLinear(windowing, ffs, lrs.Df, fundFreq, 7, false);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeThdLinear(windowing, ffs, lrs.Df, fundFreq, 7, false);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		// there are essentially three ways to compute IMD based on f2/f1 ratio
		// 1. if f2/f1 < 2 use the CCIF method. DFD2,DFD3 plus IMD using the power method
		//      reference level is F2+F1
		// 2. if 2 < f2/f1 < 7 use the power method and report IMD
		//      reference level is sqrt(F2^2+F1^2)
		// 3. if f2/f1 > 7 use the DIN method and report MD2,MD3 and IMDdin
		//      reference level is F2
		internal static LeftRightPair GetImdDb(string windowing, LeftRightFrequencySeries lrs, double[] fundFreqs, double minFreq, double maxFreq)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental,
			// int numHarmonics = 5, bool debug = false
			if (lrs == null)
				return new();

			Debug.Assert(fundFreqs.Length == 2, "fundFreqs should have exactly two frequencies for IMD calculation.");

			var imdType = GetImdMethod(fundFreqs);  // such as E_ImdMethod.CCIF, E_ImdMethod.Power, or E_ImdMethod.DIN

			var ffs = lrs.Left;
			var imdLeft = ComputeImdLinear(windowing, ffs, lrs.Df, fundFreqs, imdType, false);
			imdLeft = QaLibrary.ConvertVoltage(imdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var imdRight = ComputeImdLinear(windowing, ffs, lrs.Df, fundFreqs, imdType, false);
			imdRight = QaLibrary.ConvertVoltage(imdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(imdLeft, imdRight);
		}

		internal static LeftRightPair GetImdnDb(string windowing, LeftRightFrequencySeries lrs, double[] fundFreqs, double minFreq, double maxFreq, string weighting)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental,
			// int numHarmonics = 5, bool debug = false
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var imdLeft = ComputeImdnLinear(windowing, ffs, lrs.Df, fundFreqs, 3, minFreq, maxFreq, weighting, false);
			imdLeft = QaLibrary.ConvertVoltage(imdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var imdRight = ComputeImdnLinear(windowing, ffs, lrs.Df, fundFreqs, 3, minFreq, maxFreq, weighting, false);
			imdRight = QaLibrary.ConvertVoltage(imdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(imdLeft, imdRight);
		}

		internal static LeftRightPair GetThdnDb(string windowing, LeftRightFrequencySeries lrs, double fundFreq, double minFreq, double maxFreq, string weighting)
		{
			// double[] signalFreqLin, double[] frequencies, double fundamental, double notchOctaves = 0.5,
			// double startFreq = 20.0, double stopFreq = 20000.0, bool debug = false
			if (lrs == null)
				return new();

			var ffs = lrs.Left;
			var notchOct = fundFreq > 1000 ? 0.2 : 0.5;	// at low frequencies it is not enough data points at 0.2
			var thdLeft = ComputeThdnLinear(windowing, ffs, lrs.Df, fundFreq, notchOct, minFreq, maxFreq, weighting);
			thdLeft = QaLibrary.ConvertVoltage(thdLeft, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			ffs = lrs.Right;
			var thdRight = ComputeThdnLinear(windowing, ffs, lrs.Df, fundFreq, notchOct, minFreq, maxFreq, weighting);
			thdRight = QaLibrary.ConvertVoltage(thdRight, E_VoltageUnit.Volt, E_VoltageUnit.dBV);

			return new(thdLeft, thdRight);
		}

		internal static double ComputeSnrRatio(string windowing, double[] signalFreqLin, double df, double fundamental, double minFreq, double maxFreq, string weighting, bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			var notchOctaves = 0.5; // aes-17 2015 standard notch
			//var notchOctaves = 0.15; // my preferred notch much tighter and more realistic nowadays
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);
			double[] weights = GetWeights(0, signalFreqLin.Length, df, weighting);
			double[] weighted = signalFreqLin.Select((x, i) => x * weights[i]).ToArray();

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = QaMath.MagAtFreq(signalFreqLin, df, fundamental);
			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsF(weighted, df, minFreq, notchLowerBound, windowing);
			double rmsAboveNotch = ComputeRmsF(weighted, df, notchUpperBound, maxFreq, windowing);
			double noiseRms = Math.Sqrt(Math.Pow(rmsBelowNotch, 2) + Math.Pow(rmsAboveNotch, 2));

			if (debug)
			{
				Debug.WriteLine($"RMS Below Notch: {rmsBelowNotch:F6}");
				Debug.WriteLine($"RMS Above Notch: {rmsAboveNotch:F6}");
				Debug.WriteLine($"Noise RMS: {noiseRms:F6}");
			}

			return fundamentalRms / noiseRms;
		}

		internal static double ComputeSinadRatio(string windowing, double[] signalFreqLin, double df, double fundamental, double minFreq, double maxFreq, string weighting, bool debug = false)
		{
			if(minFreq >= maxFreq)
			{
				Debug.WriteLine("minFreq must be less than maxFreq in ComputeSinadRatio");
				return 200.0;
			}
			// Calculate notch filter bounds in Hz
			var notchOctaves = 0.5; // aes-17 2015 standard notch
			//var notchOctaves = 0.15; // my preferred notch much tighter and more realistic nowadays
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);
			double[] weights = GetWeights(0, signalFreqLin.Length, df, weighting);
			double[] weighted = signalFreqLin.Select((x, i) => x * weights[i]).ToArray();

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = QaMath.MagAtFreq(weighted, df, fundamental);
			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsF(weighted, df, minFreq, notchLowerBound, windowing);
			double rmsAboveNotch = ComputeRmsF(weighted, df, notchUpperBound, maxFreq, windowing);
			double rmsTotal = ComputeRmsF(weighted, df, minFreq, maxFreq, windowing);

			if (debug)
			{
				Debug.WriteLine($"RMS Below Notch: {rmsBelowNotch:F6}");
				Debug.WriteLine($"RMS Above Notch: {rmsAboveNotch:F6}");
			}

			return rmsTotal / Math.Sqrt(rmsAboveNotch * rmsAboveNotch + rmsBelowNotch * rmsBelowNotch);
		}

		internal static double ComputeImdSnrRatio(string windowing, double[] signalFreqLin, double df, double[] fundamentals, double minFreq, double maxFreq, string weighting, bool debug = false)
		{
			var notchOctaves = 0.15; // tight notch for intermods
			var notches = new List<double>();
			double[] weights = GetWeights(0, signalFreqLin.Length, df, weighting);
			double[] weighted = signalFreqLin.Select((x, i) => x * weights[i]).ToArray();
			// fundamentals must be monotone
			double lastFreq = fundamentals[0] - 1;
			foreach (var freq in fundamentals)
			{
				// Calculate notch filter bounds in Hz
				double notchLowerBound = freq / Math.Pow(2, notchOctaves);
				double notchUpperBound = freq * Math.Pow(2, notchOctaves);
				notches.Add(notchLowerBound);
				notches.Add(notchUpperBound);
				if (debug)
				{
					Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
					Debug.Assert(freq > lastFreq, "Fundamentals list must be monotone");
				}
				lastFreq = freq;
			}

			// signal without noise boundaries
			double fundamentalRmsSq = 0;
			foreach (var freq in fundamentals)
			{
				var rmsv = QaMath.MagAtFreq(weighted, df, freq);
				fundamentalRmsSq += rmsv * rmsv;
			}

			double fundamentalRms = Math.Sqrt(fundamentalRmsSq);

			for(int i=0; i<notches.Count; i += 2)
			{
				double rmsNotch = ComputeRmsF(weighted, df, notches[i], notches[i+1], windowing);
			}

			// now get total noise+distortion outside of the notch areas
			double totalNoiseSq = 0.0;
			double startFreq = minFreq;
			double stopFreq = maxFreq;
			if (startFreq < notches[0])
			{
				double rmsNotch = ComputeRmsF(weighted, df, startFreq, notches[0], windowing);
				totalNoiseSq += rmsNotch * rmsNotch;    // squared
			}
			for (int i = 1; i < fundamentals.Length; i++)
			{
				// get the voltage outside the notch
				double rmsNotch = ComputeRmsF(weighted, df, notches[2 * i - 1], notches[2 * i], windowing);
				totalNoiseSq += rmsNotch * rmsNotch;    // squared
				if (debug)
					Debug.WriteLine($"{rmsNotch} noise inside notch #{i}");
			}
			if (notches[notches.Count - 1] < stopFreq)
			{
				double rmsNotch = ComputeRmsF(weighted, df, notches[notches.Count - 1], stopFreq, windowing);
				totalNoiseSq += rmsNotch * rmsNotch;    // squared
			}

			var noiseRms = Math.Sqrt(totalNoiseSq);


			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
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
		/// Compute the total intermodulation distrtion
		/// </summary>
		/// <param name="windowing">windowing to use</param>
		/// <param name="signalFreqLin">the signal frequencies as an array</param>
		/// <param name="df">the delta freqeuncy</param>
		/// <param name="fundamentals">the array of fundamental frequencies</param>
		/// <param name="numHarmonics">number of harmonics to keep</param>
		/// <param name="debug"></param>
		/// <returns></returns>
		internal static double ComputeImdLinear(string windowing, double[] signalFreqLin, double df, double[] fundamentals, E_ImdMethod method, bool debug = false)
		{
			var maxFreq = df * signalFreqLin.Length;
			double[] fundVolts = new double[fundamentals.Length];
			fundVolts[0] = QaMath.MagAtFreq(signalFreqLin, df, fundamentals[0]);
			fundVolts[1] = QaMath.MagAtFreq(signalFreqLin, df, fundamentals[1]);

			// the denominator depends on the method of calculation
			double fundamentalAmplitude = GetImdDenom(method, fundVolts[0], fundVolts[1]);
			List<List<double>> sidebands = new();
			var fh = fundamentals[1]; // F2
			var fl = fundamentals[0]; // F1
			switch (method)
			{ 	
				case E_ImdMethod.CCIF:
					// CCIF method uses F2+F1 as the denominator
					sidebands.Add(new List<double> { 2*fl - fh, 2 * fh - fl });
					sidebands.Add(new List<double> { fh - fl });
					break;
				case E_ImdMethod.Power:
					// Power method uses sqrt(F2^2+F1^2) as the denominator
					sidebands.Add(new List<double> { fh - fl });
					sidebands.Add(new List<double> { fh + fl });
					sidebands.Add(new List<double> { fl - 2*fh });
					sidebands.Add(new List<double> { fl + 2*fh });
					sidebands.Add(new List<double> { fh + 2*fl });
					sidebands.Add(new List<double> { fh - 2*fl });
					break;
				case E_ImdMethod.DIN:
					// DIN method uses F2 as the denominator
					sidebands.Add(new List<double> { fh - fl, fh + fl });
					sidebands.Add(new List<double> { fh - 2*fl, fh + 2*fl });
					break;
				default:
					throw new ArgumentException("Invalid IMD method specified.");
			}

			// Calculate the sum of squares of the harmonic amplitudes
			double harmonicAmplitudesSqSum = 0.0;
			foreach (var band in sidebands)
			{
				double harmonicAmplitude = 0;
				foreach (double fv in band)
				{
						if (fv < maxFreq && fv > 10)
						{
							harmonicAmplitude += QaMath.MagAtFreq(signalFreqLin, df, fv);
							// Debugging: Show the harmonic amplitude in dB and the bins being examined
							Debug.WriteLine($"{fv:0} Intermod Amplitude: {MathUtil.FormatVoltage(harmonicAmplitude)} ");
						}
				}
				harmonicAmplitudesSqSum += harmonicAmplitude * harmonicAmplitude;
			}
			// Compute THD
			double imd = Math.Sqrt(harmonicAmplitudesSqSum) / fundamentalAmplitude;
			return imd;
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
		internal static double ComputeThdnLinear(string windowing, double[] signalFreqLin, double df, double fundamental, double notchOctaves = 0.5, double startFreq = 20.0, double stopFreq = 20000.0, string weighting = "Z", bool debug = false)
		{
			// Calculate notch filter bounds in Hz
			double notchLowerBound = fundamental / Math.Pow(2, notchOctaves);
			double notchUpperBound = fundamental * Math.Pow(2, notchOctaves);
			double[] weights = GetWeights(0, signalFreqLin.Length, df, weighting);
			double[] weighted = signalFreqLin.Select((x, i) => x * weights[i]).ToArray();

			if (debug)
			{
				Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
			}

			// Calculate RMS of the fundamental within the notch
			double fundamentalRms = QaMath.MagAtFreq(signalFreqLin, df, fundamental);

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// Calculate RMS of the signal outside the notch
			double rmsBelowNotch = ComputeRmsF(weighted, df, startFreq, notchLowerBound, windowing);
			double rmsAboveNotch = ComputeRmsF(weighted, df, notchUpperBound, stopFreq, windowing);
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
		internal static double ComputeImdnLinear(string windowing, double[] signalFreqLin, double df, double[] fundamentals, double notchOctaves = 0.5, double startFreq = 20.0, double stopFreq = 20000.0, string weighting="Z", bool debug = false)
		{
			var notches = new List<double>();
			// fundamentals must be monotone
			double lastFreq = fundamentals[0] - 1;
			double fundamentalRmsSq = 0;
			double[] weights = GetWeights(0, signalFreqLin.Length, df, weighting);
			double[] weighted = signalFreqLin.Select((x, i) => x * weights[i]).ToArray();
			foreach(var freq in fundamentals)
			{
				// Calculate notch filter bounds in Hz
				double notchLowerBound = freq / Math.Pow(2, notchOctaves);
				double notchUpperBound = freq * Math.Pow(2, notchOctaves);
				notches.Add(notchLowerBound);
				notches.Add(notchUpperBound);
				if (debug)
				{
					Debug.WriteLine($"Notch Filter Bounds: {notchLowerBound:F2} Hz to {notchUpperBound:F2} Hz");
					Debug.Assert(freq > lastFreq, "Fundamentals list must be monotone");
				}
				lastFreq = freq;
				var vfund = QaMath.MagAtFreq(weighted, df, freq);
				// only use last fundamental
				fundamentalRmsSq = vfund * vfund;
			}
			notches.Sort();

			// Calculate RMS of the total fundamentals
			double fundamentalRms = Math.Sqrt(fundamentalRmsSq);

			if (debug)
			{
				Debug.WriteLine($"Fundamental RMS: {fundamentalRms:F6}");
			}

			// now get total noise+distortion outside of the notch areas
			double totalNoiseSq = 0.0;
			if(startFreq < notches[0])
			{
				double rmsNotch = ComputeRmsF(weighted, df, startFreq, notches[0], windowing);
				totalNoiseSq += rmsNotch * rmsNotch;	// squared
			}
			for (int i=1; i<fundamentals.Length; i++)
			{
				// get the voltage outside the notch
				double rmsNotch = ComputeRmsF(weighted, df, notches[2*i-1], notches[2*i], windowing);
				totalNoiseSq += rmsNotch * rmsNotch;    // squared
				if (debug)
					Debug.WriteLine($"{rmsNotch} noise inside notch #{i}");
			}
			if (notches[notches.Count-1] < stopFreq)
			{
				double rmsNotch = ComputeRmsF(weighted, df, notches[notches.Count - 1], stopFreq, windowing);
				totalNoiseSq += rmsNotch * rmsNotch;    // squared
			}

			var noiseRms = Math.Sqrt(totalNoiseSq);
			// Calculate THDN
			double thdn = noiseRms / fundamentalRms;

			if (debug)
			{
				Debug.WriteLine($"THDN: {thdn:F6} Noise: {noiseRms:F6} and Fundamental: {fundamentalRms} (Linear)");
			}

			return thdn;
		}

		// calculate the noise from 20..20000Hz
		internal static LeftRightPair CalculateNoise(string windowing, LeftRightFrequencySeries? lrfs, string weighting)
		{
			if (lrfs == null)
				return new LeftRightPair(1e-20, 1e-20);

			var maxf = ViewSettings.NoiseBandwidth;
			var noiseLeft = QaCompute.ComputeRmsF(lrfs.Left, lrfs.Df, 20, maxf, windowing, weighting);
			var noiseRight = QaCompute.ComputeRmsF(lrfs.Right, lrfs.Df, 20, maxf, windowing, weighting);
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

		public static double CwCalc(double f)
		{
			const double rt1 = 20.598997; // C-weighting constants
			const double rt2 = 12194.217;
			var f1 = f*f + rt1 * rt1;     //fron ansi standard
			var f2 = f*f + rt2 * rt2;
			var ux = (rt2 * rt2 * f * f) / (f1 * f2);     // C-weighting formula
			return ux;
		}

		public static double AwCalc(double f)
		{
			const double rt1 = 107.65265; // C-weighting constants
			const double rt2 = 737.86223;
			var f1 = f * f + rt1 * rt1;     //fron ansi standard
			var f2 = f * f + rt2 * rt2;
			var ux = (f * f) / Math.Sqrt(f1 * f2);     // C-weighting formula
			return ux;
		}

		public static double[] CWeightCalc(int start, int length, double df)
		{
			var rtrn = new double[length];
			var rt1000 = CwCalc(1000.0); // C-weighting at 1000Hz
			for (int i = 0; i < length; i++)
			{
				var ux = CwCalc((start+i)*df);     // C-weighting formula
				rtrn[i] = ux / rt1000;
			}
			return rtrn;
		}

		public static double[] AWeight(int start, int length, double df)
		{
			var rtrn = new double[length];
			var rt1000 = AwCalc(1000.0);		// A-weighting offset at 1000Hz
			var cw = CWeightCalc(start, length, df);   // get the C-weighting values
			for (int i = 0; i < length; i++)
			{
				var ux = cw[i] * AwCalc((start + i) * df);     // C-weighting formula
				rtrn[i] = ux / rt1000;
			}
			return rtrn;
		}

		public static double[] CWeight(int start, int length, double df)
		{
			var rtrn = CWeightCalc(start, length, df);   // get the C-weighting values
			return rtrn;
		}

		public static double[] GetWeights(int start, int length, double df, string weighting)
		{
			switch (weighting)
			{
				case "A":
					return AWeight(start, length, df);
				case "C":
					return CWeight(start, length, df);
				default:
					return Enumerable.Range(0, length).Select(x => 1.0).ToArray(); // no weighting
			}
		}

		/// <summary>
		/// calculate the total power of a frequency signal in usual linear frequency format
		/// </summary>
		/// <param name="signalFreqLin"></param>
		/// <param name="df">the bin size</param>
		/// <param name="lowerBound">lower frequency</param>
		/// <param name="upperBound">upper frequency</param>
		/// <returns>the equivalent rms voltage in this fft chunk via the total power</returns>
		internal static double ComputeRmsF(double[] signalFreqLin, double df, double lowerBound, double upperBound, string windowing, string Weighting = "")
		{
			double sum = 0;
			if(lowerBound >= upperBound)
				return 0.0;
			var mx = Math.Max(lowerBound, upperBound);
			lowerBound = Math.Min(lowerBound, upperBound);
			upperBound = mx; // make sure lowerBound is always less than upperBound

			try
			{
				var lb = Math.Max(1,(int)(lowerBound / df));
				var ub = Math.Min(signalFreqLin.Length-1, (int)(upperBound / df));
				if( Weighting.Length == 0 || Weighting == "Z")
				{
					for (int i = lb; i < ub; i++)
					{
						sum += signalFreqLin[i] * signalFreqLin[i];
					}
				}
				else
				{
					double[] wgt = GetWeights(lb, ub - lb, df, Weighting);
					for (int i = lb; i < ub; i++)
					{
						sum += signalFreqLin[i] * signalFreqLin[i] * wgt[i-lb] * wgt[i - lb];
					}
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