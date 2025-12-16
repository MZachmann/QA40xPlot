using FftSharp;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using System.Numerics;
using System.Windows;


// A chirp is a signal of the form x(t)=Λsin(2πϕ(t))
// // where Λ is the amplitude and the function ϕ(t) is the phase of the chirp.
// If the phase has the form ϕ(t)=(1/2) * ((f1−f0) / (t1−t0)) * ((t−t0)**2) + ϕ0
//    then the signal is called a linear chirp. 
// 

namespace QA40xPlot.Libraries
{
	public static class QaMath
	{
		// convert from one frequency band to another
		public static double[] LinearApproximate(double[] fIn, double[] valIn, double[] fOut)
		{
			if (fIn.Length != valIn.Length)
				return [];
			double[] result = new double[fOut.Length];
			int idx = 0;
			int maxl = fIn.Length;
			for (int i = 0; i < fOut.Length; i++)
			{
				var f = fOut[i];
				while (idx < maxl && fIn[idx] < f)
					idx++;
				if (idx == 0)
					result[i] = valIn[0];
				else if (idx == maxl)
					result[i] = valIn[maxl - 1];
				else
				{
					var x1 = fIn[idx - 1];
					var x2 = fIn[idx];
					var y1 = valIn[idx - 1];
					var y2 = valIn[idx];
					result[i] = y1 + (y2 - y1) * (f - x1) / (x2 - x1);
				}
				//Debug.WriteLine($"for {i} using {idx} and result={result[i]}");
			}
			return result;
		}

		public static double MagAtFreq(double[] pts, double df, double dFreq)
		{
			var bin = (int)QaLibrary.GetBinOfFrequency(dFreq, df);
			if (bin >= pts.Length)
				return 1e-10;
			var ba = pts[bin];
			if (bin > 0)
				ba = Math.Max(ba, pts[bin - 1]);
			if (bin > 1)
				ba = Math.Max(ba, pts[bin - 2]);
			if (bin < (pts.Length - 1))
				ba = Math.Max(ba, pts[bin + 1]);
			if (bin < (pts.Length - 2))
				ba = Math.Max(ba, pts[bin + 2]);
			return ba;
		}

		/// <summary>
		/// get the snr from a leftright series
		/// </summary>
		public static double CalculateSNR(LeftRightSeries? lfrs, bool isLeft)
		{
			if (lfrs == null || lfrs.TimeRslt == null || lfrs.FreqRslt == null)
				return 0;

			var timeSeries = isLeft ? lfrs.TimeRslt.Left : lfrs.TimeRslt.Right;
			var freqSeries = isLeft ? lfrs.FreqRslt.Left : lfrs.FreqRslt.Right;
			var dt = lfrs.TimeRslt.dt;
			var df = lfrs.FreqRslt.Df;

			var totalV = QaCompute.ComputeRmsTime(timeSeries);
			var windowBw = 1.5; // hann
			var totalVF = Math.Sqrt(freqSeries.Select(x => x * x / windowBw).Sum()); // rms voltage
			var fundamentalV = freqSeries.Max();    // fundamental magnitude of output
			var totalV3 = freqSeries.Select(x => x * df * x * df).Sum();    // ?
			List<double> thd = new();
			var dfreq = freqSeries.ToList().IndexOf(fundamentalV) * df;
			double thdall = 0;
			for (int i = 2; i < 12; i++)
			{
				var x = MagAtFreq(freqSeries, df, dfreq * i);
				thd.Add(x / fundamentalV);      // each individual distortion component
				thdall += x * x;                // total sum of squares
			}
			var thd_pct = 100 * Math.Sqrt(thdall) / fundamentalV;   // total thd in percent
			var snr = 20 * Math.Log10(totalV / Math.Sqrt(totalV * totalV - fundamentalV * fundamentalV));

			return 0;

		}

		private static double Squares(double f)
		{
			// is freq in left or right quadrange
			var u = f % (2 * Math.PI);
			if (u < Math.PI)
				return 1;
			else
				return -1;
		}

		private static double Impulse(double f)
		{
			// is freq in left or right quadrange
			var u = f % (2 * Math.PI);
			if (u < Math.PI / 4)
				return 1;
			else
				return -1;
		}

		// for some reason the window creation eats a moderate amount
		// of CPU resources so cache it stupidly
		private static string _Windowingtype = string.Empty;
		private static FftSharp.Window _FftWindow = new FftSharp.Windows.Rectangular();

		public static FftSharp.Window GetWindowType(string windowType)
		{
			if (_Windowingtype == windowType)
				return _FftWindow;

			_Windowingtype = windowType;
			FftSharp.Window window;
			switch (windowType)
			{
				case "Bartlett":
					window = new FftSharp.Windows.Bartlett();    // best?
					break;
				case "Blackman":
					window = new FftSharp.Windows.Blackman();    // best?
					break;
				case "Cosine":
					window = new FftSharp.Windows.Cosine();    // best?
					break;
				case "FlatTop":
					window = new FftSharp.Windows.FlatTop();    // best?
					break;
				case "Hamming":
					window = new FftSharp.Windows.Hamming();    // best?
					break;
				case "Hann":
					window = new FftSharp.Windows.Hanning();    // best?
					break;
				case "Kaiser":
					window = new FftSharp.Windows.Kaiser();    // best?
					break;
				case "Rectangular":
					window = new FftSharp.Windows.Rectangular();    // best?
					break;
				case "Tukey":
					window = new FftSharp.Windows.Tukey();    // best?
					break;
				case "Welch":
					window = new FftSharp.Windows.Welch();    // best?
					break;
				default:
					window = new FftSharp.Windows.Rectangular();
					break;
			}
			_FftWindow = window;
			return window;
		}

		public static LeftRightFrequencySeries? CalculateChirpFreq(string windowing, LeftRightTimeSeries? lrts, double[] signal, double voltage, uint sampleRate, uint sampleSize)
		{
			LeftRightFrequencySeries? fs = null;
			if (lrts != null)
			{
				var norms = Chirps.NormalizeChirpDbl(windowing, signal, voltage, (lrts.Left, lrts.Right));
				fs = new();
				fs.Left = norms.Item1;
				fs.Right = norms.Item2;
				fs.Df = QaLibrary.CalcBinSize(sampleRate, sampleSize);
			}
			return fs;
		}

		public static LeftRightFrequencySeries CalculateSpectrum(LeftRightTimeSeries lrfs, string windowing)
		{
			LeftRightFrequencySeries lfs = new();
			try
			{
				var timeSeries = lrfs;
				var m2 = Math.Sqrt(2);
				// Left channel
				// only take half of the data since it's symmetric so length of freq data = 1/2 length of time data
				var window = GetWindowType(windowing);
				double[] wdwLeft = window.Apply(timeSeries.Left, true);
				System.Numerics.Complex[] specLeft = FFT.Forward(wdwLeft).Take(timeSeries.Left.Length / 2).ToArray();

				double[] wdwRight = window.Apply(timeSeries.Right, true);
				System.Numerics.Complex[] specRight = FFT.Forward(wdwRight).Take(timeSeries.Left.Length / 2).ToArray();

				lfs.Left = specLeft.Select(x => x.Magnitude * m2).ToArray();
				lfs.Right = specRight.Select(x => x.Magnitude * m2).ToArray();
				var nca2 = (int)(0.01 + 1 / timeSeries.dt);      // total time in tics = sample rate
				lfs.Df = nca2 / (double)timeSeries.Left.Length; // ???
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return lfs;
		}

		private static double[] MakeMultitone(GenWaveform gw, GenWaveSample samples)
		{
			var dvamp = gw.Voltage * Math.Sqrt(2); // rms voltage -> peak voltage
												   // is freq in left or right quadrange
			var freqs = Enumerable.Range(0, 32).Select(x => 20 * Math.Pow(2, x / 3.0)).
						Select(f => QaLibrary.GetNearestBinFrequency(f, (uint)samples.SampleRate, (uint)samples.SampleSize)).ToList();
			double[] wave = new double[samples.SampleSize];
			foreach (var f in freqs)
			{
				var theta = Enumerable.Range(0, samples.SampleSize).Select(x => 2 * Math.PI * f * x / samples.SampleRate);
				var thetav = theta.Select(f => dvamp * Math.Sin(f)).ToList();
				for (int i = 0; i < samples.SampleSize; i++)
				{
					wave[i] += thetav[i];   // accumulate the tone values
				}
			}
			// now evaluate
			return wave;
		}

		private static double[] MakeWave(GenWaveform gw, GenWaveSample samples)
		{
			if (gw.Name == "Multitone")
			{
				return MakeMultitone(gw, samples);
			}

			var dvamp = gw.Voltage * Math.Sqrt(2); // rms voltage -> peak voltage
												   // we always put frequencies in the bin so they don't leak into other bins when we sample them
			var freq = QaLibrary.GetNearestBinFrequency(gw.Frequency, (uint)samples.SampleRate, (uint)samples.SampleSize);
			// frequency vector
			var theta = Enumerable.Range(0, samples.SampleSize).Select(x => 2 * Math.PI * freq * x / samples.SampleRate);
			var totalth = theta.Max();
			// now evaluate
			switch (gw.Name)
			{
				case "":        // this is just wrong... so use sine here
				case "Sine":
					// use a bin frequency???
					return theta.Select(f => dvamp * Math.Sin(f)).ToArray();
				case "Square":
					return theta.Select(f => dvamp * Squares(f)).ToArray();
				case "Impulse":
					return theta.Select(f => dvamp * Impulse(f)).ToArray();
				case "Chirp":
					var chirpTwo = Chirps.ChirpVp(samples.SampleSize, samples.SampleRate, gw.Voltage, 20, 20000, 0.8);
					return chirpTwo;
				case "RiaaChirp":
					var chirp3 = Chirps.ChirpVp(samples.SampleSize, samples.SampleRate, gw.Voltage, 20, 20000, 0.8);
					var bq = BiquadBuilder.BuildRiaaBiquad((uint)samples.SampleRate, true);
					chirp3 = chirp3.Select(x => bq.Process(x)).ToArray();
					return chirp3;
				default:
					break;
			}
			return [];
		}

		/// <summary>
		/// calculate the waveform for up to n frequencies
		/// </summary>
		/// <param name="gw1"></param>
		/// <param name="gw2"></param>
		/// <param name="gwSample"></param>
		/// <returns></returns>
		public static double[] CalculateWaveform(GenWaveform[] gws, GenWaveSample gwSample)
		{
			var lresult = new double[gwSample.SampleSize];
			foreach (var gwi in gws)
			{
				var lr2 = MakeWave(gwi, gwSample);
				for (int i = 0; i < gwSample.SampleSize; i++)
				{
					lresult[i] += lr2[i];   // accumulate the tone values
				}
			}
			return lresult;
		}

		// create a chirp from F0 ... F1 for a total time of chirpTime
		// chirpsize must be the same as fftSize in the device
		//exponential chirp: f(t) = f0 * k^(t/T) where k=f0/f1
		// Y = 10^(Slope*X + Y-intercept)
		public static List<double> CalculateChirp(double f0, double f1, double dVolts, uint chirpSize, uint sampleRate)
		{
			double dt = 1 / (double)sampleRate; // interval time
			var lout = new List<double>();
			var k = f1 / f0;    // number of octaves
			var T = chirpSize * dt; // total time
			for (int i = 0; i < chirpSize; i++)
			{

				double t = i * dt;
				double ft = f0 * Math.Pow(k, (t / T));
				var fmulx = T / Math.Log(k);
				lout.Add((dVolts * Math.Sqrt(2)) * Math.Cos(2 * Math.PI * fmulx * ft)); // * Math.Sqrt(ft / f1));
			}
			return lout;
		}

		// given a left-right time series it finds the gain/phase at freq
		// using left input as data and right input as reference (source)
		public static System.Numerics.Complex CalculateGainPhase(double fundamentalFreq, LeftRightSeries measuredSeries)
		{
			var measuredTimeSeries = measuredSeries.TimeRslt;
			if (measuredTimeSeries == null)
				return Complex.Zero;

			var m2 = Math.Sqrt(2);
			// Left channel
			// we do manual FFT here because we need the complex values for phase output
			var window = new FftSharp.Windows.FlatTop();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left, true);
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right, true);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				var fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFreq, (uint)nca, (uint)measuredTimeSeries.Left.Length);
				var ratio = spectrum_measured[fundamentalBin] / spectrum_ref[fundamentalBin];
				u = ratio;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			return u;
		}

		// given a left-right time series it finds the voltage at freq for both channels
		public static System.Numerics.Complex CalculateDualGain(double fundamentalFreq, LeftRightSeries measuredSeries)
		{
			var measuredTimeSeries = measuredSeries.TimeRslt;
			if (measuredTimeSeries == null)
				return Complex.Zero;

			var m2 = Math.Sqrt(2);
			// Left channel
			// we do manual FFT here because we may as well and do flattop for precision
			var window = new FftSharp.Windows.FlatTop();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left, true);   // true == normalized by # elements
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right, true);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				var fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFreq, (uint)nca, (uint)measuredTimeSeries.Left.Length);
				double left = spectrum_measured[fundamentalBin].Magnitude * m2;
				double right = spectrum_ref[fundamentalBin].Magnitude * m2;
				u = new Complex(left, right);   // pack it in stupidly
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			return u;
		}
	}
}
