using FftSharp;
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
    public interface QAMath
    {

		public static double MagAtFreq(double[] pts, double df, double dFreq)
		{
			var bin = (int)Math.Floor(dFreq / df);
			if (bin >= pts.Length)
				return 0;
			var ba = pts[bin];
			if (bin > 0)
				ba = Math.Max(ba, pts[bin - 1]);
			if(bin < (pts.Length-1))
				ba = Math.Max(ba, pts[bin + 1]);
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

			var totalV = Math.Sqrt(timeSeries.Select(x => x * x).Sum() / timeSeries.Length); // rms voltage
			var windowBw = 1.5; // hann
			var totalVF = Math.Sqrt(freqSeries.Select(x => x * x / windowBw).Sum()); // rms voltage
			var fundamentalV = freqSeries.Max();	// fundamental magnitude of output
			var totalV3 = freqSeries.Select(x => x * df * x * df).Sum();	// ?
			List<double> thd = new();
			var dfreq = freqSeries.ToList().IndexOf(fundamentalV) * df;
			double thdall = 0;
			for(int i=2; i<12; i++)
			{
				var x = MagAtFreq(freqSeries, df, dfreq * i);
				thd.Add(x/fundamentalV);		// each individual distortion component
				thdall += x * x;				// total sum of squares
			}
			var thd_pct = 100 * Math.Sqrt(thdall) / fundamentalV;   // total thd in percent
			var snr = 20*Math.Log10(totalV / Math.Sqrt(totalV*totalV - fundamentalV*fundamentalV));

			return 0;

		}


		// create a chirp from F0 ... F1 for a total time of chirpTime
		// chirpsize must be the same as fftSize in the device
		//exponential chirp: f(t) = f0 * k^(t/T) where k=f0/f1
		// Y = 10^(Slope*X + Y-intercept)

		public static List<double> CalculateChirp(double f0, double f1, double dVolts, uint chirpSize, uint sampleRate)
		{
			double dt = 1 / (double)sampleRate;	// interval time
			var lout = new List<double>();
			var k = f1/f0;	// number of octaves
			var T = chirpSize * dt; // total time
			for (int i = 0; i < chirpSize; i++)
			{

				double t = i * dt;
				double ft = f0 * Math.Pow(k, (t / T));
				var fmulx = T / Math.Log(k);
				lout.Add((dVolts / Math.Sqrt(2)) * Math.Cos(2 * Math.PI * fmulx * ft )); // * Math.Sqrt(ft / f1));
			}
			return lout;
		}

		// given a left-right time series representing a sine wave it finds the gain/phase at freq
		// using left input as data and right input as reference (source)
		public static System.Numerics.Complex CalculateGainPhase(double fundamentalFreq, LeftRightSeries measuredSeries)
		{
			var measuredTimeSeries = measuredSeries.TimeRslt;
			if(measuredTimeSeries == null)
				return Complex.Zero;

			var m2 = Math.Sqrt(2);
			// Left channel
			var window = new FftSharp.Windows.Hanning();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left, true);
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right, true);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);
			//var old = measuredSeries.FreqRslt;
			if( measuredSeries.FreqRslt == null || measuredSeries.FreqRslt.Left == null)
			{
				measuredSeries.FreqRslt = new();
				measuredSeries.FreqRslt.Left = spectrum_measured.Select(x => x.Magnitude * m2).ToArray();
				measuredSeries.FreqRslt.Right = spectrum_ref.Select(x => x.Magnitude * m2).ToArray();
				var nca2 = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				measuredSeries.FreqRslt.Df = nca2 / (double)spectrum_measured.Length; // ???
			}

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);		// total time in tics = sample rate
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

		// given a left-right time series representing a sine wave it finds the gain/phase at freq
		// using left input as data and right input as reference (source)
		public static System.Numerics.Complex CalculateDualGain(double fundamentalFreq, LeftRightSeries measuredSeries)
		{
			var measuredTimeSeries = measuredSeries.TimeRslt;
			if( measuredTimeSeries == null)
				return Complex.Zero;

			var m2 = Math.Sqrt(2);
			// Left channel
			var window = new FftSharp.Windows.Hanning();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left, true);	// true == normalized by # elements
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right, true);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);
			if (measuredSeries.FreqRslt == null || measuredSeries.FreqRslt.Left == null)
			{
				measuredSeries.FreqRslt = new();
				measuredSeries.FreqRslt.Left = spectrum_measured.Select(x => x.Magnitude * m2).ToArray();
				measuredSeries.FreqRslt.Right = spectrum_ref.Select(x => x.Magnitude * m2).ToArray();
				var nca2 = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				measuredSeries.FreqRslt.Df = nca2 / (double)spectrum_measured.Length; // ???
			}

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				var fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFreq, (uint)nca, (uint)measuredTimeSeries.Left.Length);
				double left = spectrum_measured[fundamentalBin].Magnitude * m2;
				double right = spectrum_ref[fundamentalBin].Magnitude * m2;
				u = new Complex(left, right);	// pack it in stupidly
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			return u;
		}
	}
}
