using FftSharp;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Extensions;
using QA40xPlot.ViewModels;
using ScottPlot;
using System;
using System.Diagnostics;
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
		/// calculate the residual after removing the fundamental
		/// </summary>
		/// <param name="lrts">time series data</param>
		/// <param name="fund">fundamental frequency or zero</param>
		/// <param name="numHarmonics">number of harmonics or zero</param>
		/// <returns></returns>
		public static LeftRightTimeSeries CalculateResidual(LeftRightTimeSeries lrts, double fund, double scale, int numHarmonics)
		{
			LeftRightTimeSeries lrOut = new();
			lrOut.dt = lrts.dt;
			if (lrts.Left == null || lrts.Left.Length == 0)
				return lrOut;

			double fundamental = fund;
			int bin = 0;
			if (fund <= 2)
			{
				// if the caller wants us to determine the fundamental
				// get the full fft unwindowed
				var lrfs = QaMath.CalculateSpectrum(lrts, "");
				// find the fundamental/largest value
				var data = ViewSettings.IsTestLeft ? lrfs.Left : lrfs.Right;
				var largest = data.Max();
				bin = data.CountWhile(x => x < largest);
				// got frequency of the largest one
				fundamental = bin * lrfs.Df;
			}

			// 1/6 octave seems enough with Hann and the built-in generator
			var octave = 0.16;
			var srate = (int)(0.1 + 1 / lrts.dt);
			if(numHarmonics == 0)
			{
				//var lrcs = QaMath.CalculateComplexSpectrum(lrts, "");
				//// notch out the fundamental and return the rest
				//var theta = lrcs.Left[bin].Phase;
				//var mag = lrcs.Left[bin].Magnitude;
				//var thetaset = Enumerable.Range(0, lrcs.Left.Length).Select(x => 2 * Math.PI * fundamental * x / srate + theta);
				//lrOut.Left = lrts.Left.Zip(thetaset, (x,y) => mag*Math.Cos(y)).ToArray();
				// remove end gunk
				//var window = QaMath.GetWindowType("Tukey");    // best?
				//double[] lrtLeft = window.Apply(lrts.Left, false);  // the input signal
				//double[] lrtRight = window.Apply(lrts.Right, false);  // the input signal

				lrOut.Left = FftSharp.Filter.BandStop(lrts.Left, srate, fundamental * (1 - octave), fundamental * (1 + octave));
				lrOut.Right = FftSharp.Filter.BandStop(lrts.Right, srate, fundamental * (1 - octave), fundamental * (1 + octave));
			}
			else
			{
				// only keep between 2nd harmonic and 1+nth harmonic inclusive
				lrOut.Left = FftSharp.Filter.BandPass(lrts.Left, srate, fundamental * (2 - octave), fundamental * (numHarmonics + 1 + octave));
				lrOut.Right = FftSharp.Filter.BandPass(lrts.Right, srate, fundamental * (2 - octave), fundamental * (numHarmonics + 1 + octave));
			}
			if (scale != 1.0)
			{
				// rescale at user request
				lrOut.Left = lrOut.Left.Select(x => x * scale).ToArray();
				lrOut.Right = lrOut.Right.Select(x => x * scale).ToArray();
			}

			return lrOut;
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
				case "Blackman-3":
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
				case "Blackman-7":
					window = new BlackmanH7();    // best?
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

		/// <summary>
		/// set windowing to "" for none
		/// </summary>
		/// <param name="lrfs"></param>
		/// <param name="windowing"></param>
		/// <returns></returns>
		public static LeftRightFreqComplexSeries CalculateComplexSpectrum(LeftRightTimeSeries lrfs, string windowing)
		{
			LeftRightFreqComplexSeries lfs = new();
			try
			{
				var timeSeries = lrfs;
				// Left channel
				// only take half of the data since it's symmetric so length of freq data = 1/2 length of time data
				if(windowing.Length > 0)
				{
					var window = GetWindowType(windowing);
					double[] wdwLeft = window.Apply(timeSeries.Left, false);
					lfs.Left = FFT.Forward(wdwLeft);
					double[] wdwRight = window.Apply(timeSeries.Right, false);
					lfs.Right = FFT.Forward(wdwRight);
				}
				else
				{
					lfs.Left = FFT.Forward(timeSeries.Left);
					lfs.Right = FFT.Forward(timeSeries.Right);
				}
				// normalize the values to amplitude
				var leng = lfs.Left.Length;
				lfs.Left = lfs.Left.Select(x => 2 * x / leng).ToArray();
				lfs.Right = lfs.Right.Select(x => 2 * x / leng).ToArray();

				var nca2 = (int)(0.01 + 1 / timeSeries.dt);      // total time in tics = sample rate
				lfs.Df = nca2 / (double)timeSeries.Left.Length; // ???
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return lfs;
		}

		public static LeftRightFrequencySeries CalculateSpectrum(LeftRightTimeSeries lrfs, string windowing)
		{
			LeftRightFrequencySeries lfs = new();
			if (lrfs == null || lrfs.Left.Length == 0)
				return lfs;
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
				var theta = Enumerable.Range(0, (int)samples.SampleSize).Select(x => 2 * Math.PI * f * x / samples.SampleRate);
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
			if(gw.Name == "WaveFile")
			{
				return WaveGenerator.ReadWaveFile(gw.WaveFile, (uint)samples.SampleRate, (uint)samples.SampleSize, gw.Voltage);
			}

			var dvamp = gw.Voltage * Math.Sqrt(2); // rms voltage -> peak voltage
												   // we always put frequencies in the bin so they don't leak into other bins when we sample them
			var freq = QaLibrary.GetNearestBinFrequency(gw.Frequency, (uint)samples.SampleRate, (uint)samples.SampleSize);
			// frequency vector
			var theta = Enumerable.Range(0, (int)samples.SampleSize).Select(x => 2 * Math.PI * freq * x / samples.SampleRate);
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
					var bq = BiquadBuilder.BuildRiaaBiquad((uint)samples.SampleRate, false);
					chirp3 = chirp3.Select(x => bq.Process(x)).ToArray();
					return chirp3;
				default:
					Debug.Assert(false, "Invalid waveform type");
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


		/// <summary>
		/// find time delay between left and right
		/// </summary>
		/// <param name="lrts">the time series</param>
		/// <returns>L to R time delay in seconds, possibly negative</returns>
		public static double CalculateTimeDelay(LeftRightTimeSeries lrts)
		{
			if (lrts == null)
				return 0;

			var left = lrts.Left;
			var right = lrts.Right;
			if (left == null || right == null || left.Length == 0 || right.Length == 0)
				return 0;

			try
			{
				// FFT of both signals - must be power of 2 length
				System.Numerics.Complex[] specL = FFT.Forward(left);
				System.Numerics.Complex[] specR = FFT.Forward(right);
				int n = specL.Length;

				// cross-spectrum = specL * conj(specR)
				Complex[] cross = specL.Zip(specR, (a, b) => a * Complex.Conjugate(b)).ToArray();

				// inverse FFT -> cross-correlation (circular). Real part contains correlation values.
				FFT.Inverse(cross);


				// find index of maximum magnitude (use real part magnitude)
				var crossplus = cross.Select(c => Math.Abs(c.Real)).ToArray();
				double maxVal = double.MinValue;
				int maxIdx = 0;
				for (int i = 0; i < n; i++)
				{
					double mag = crossplus[i];
					if (mag > maxVal)
					{
						maxVal = mag;
						maxIdx = i;
					}
				}

				// convert index to lag (account for wrap-around)
				int lag = maxIdx;
				if (maxIdx > n / 2)
					lag = maxIdx - n;

				// time delay = lag * dt
				double delay = lag * lrts.dt;
				return delay;
			}
			catch (Exception)
			{
				// on failure return 0 (consistent with existing code patterns)
				return 0;
			}
		}
	}
}
