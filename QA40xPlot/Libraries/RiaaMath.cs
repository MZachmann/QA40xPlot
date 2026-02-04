using System.Diagnostics;
using System.Numerics;

// we have timepoints T of 75us, 318us, and 3180us ==
// giving frequencies of 2122, 500.5, and 50.05
// so  freq = (1/(T*2*PI)) or
// vout = vin/(1+j(f/fc)) where fc==cutoff

namespace QA40xPlot.Libraries
{
	// Biquad filter processor for time domain filtering
	// given top and bottom polynomial coefficients
	// the Process method does n=1...t
	// note this is definitely not memoryless, hence the Restart method
	public class BiquadFilter
	{
		private double a0, a1, a2, b0, b1, b2;
		private double z1, z2;

		public BiquadFilter(double ip0, double ip1, double ip2, double iz0, double iz1, double iz2)
		{
			// Normalize coefficients so that a0 = 1
			b0 = ip0;   // poles = denomizator
			b1 = ip1;
			b2 = ip2;
			a0 = iz0;
			a1 = iz1;   // zeros = numerator
			a2 = iz2;
			z1 = z2 = 0.0;
			var gain = (b0 + b1 + b2) / (a0 + a1 + a2);
			var u = gain;

		}

		public void Restart()
		{
			z1 = z2 = 0.0;  // reset these since they're tracking values
		}

		public void PrintString()
		{
			Debug.WriteLine($"a0:{a0} a1:{a1} a2:{a2}  b0:{b0} b1:{b1} b2:{b2}");
		}

		public double Process(double xin)
		{
			double xout = b0 * xin + a0 * z1;
			z1 = b1 * xin - a1 * xout + z2;
			z2 = b2 * xin - a2 * xout;
			return xout;
		}
	}

	// perform the frequency-based Riaa filter
	// not very performant
	internal static class RiaaTransform
	{
		// values for the filter. Use a 50KHz pole also to avoid overloads
		static double x1 = 0.00318 * 2 * Math.PI;   // 50.05 Hz
		static double x2 = 0.000318 * 2 * Math.PI;  // 500.5 Hz
		static double x3 = 0.000075 * 2 * Math.PI;  // 2122Hz
		static double x4 = 0.000003180 * 2 * Math.PI;  // 50.05 KHz

		// apply the transform to a frequency via complex math
		// this is the 3-term RIAA transform (no high freq wall)
		public static double Fvalue3(double freq)
		{
			var f1 = new Complex(1, x1 * freq);
			var f2 = new Complex(1, x2 * freq);
			var f3 = new Complex(1, x3 * freq);
			var f4 = new Complex(1, 0); // no high frequency wall
			var udiv = ((f1 * f3) / (f2 * f4)).Magnitude;
			return udiv;
		}

		// apply the transform to a frequency via complex math
		// this is the 4-term RIAA transform (with high freq wall)
		public static double Fvalue(double freq)
		{
			var f1 = new Complex(1, x1 * freq);
			var f2 = new Complex(1, x2 * freq);
			var f3 = new Complex(1, x3 * freq);
			var f4 = new Complex(1, x4 * freq); // high frequency wall
			var udiv = ((f1 * f3) / (f2 * f4)).Magnitude;
			return udiv;
		}
	}   // end class RiaaTransform

	// frequency based riaa filtering
	// mainly used to build a compensation file/curve
	public class RiaaFilter
	{

		private double _Df = 0.0;
		private uint _SampleSize = 0;
		private double[] _RiaaFilter = [];

		// just do a table lookup on a created filter
		public double DoFilter(double freq)
		{
			if (_Df <= 0 || freq <= 0)
				return 0.0;

			var idx = (int)(freq / _Df);
			if (idx >= _RiaaFilter.Length)
				idx = _RiaaFilter.Length - 1;

			return _RiaaFilter[idx];
		}

		// set up the lookup table and locals
		public void BuildFilter(uint sampleSize, double df)
		{
			_Df = df;
			_SampleSize = sampleSize;
			_RiaaFilter = Enumerable.Range(0, (int)sampleSize / 2).Select((x, i) => RiaaTransform.Fvalue3(i * df)).ToArray();
			var at1000 = _RiaaFilter.Max(); // maximum gain of 1
			_RiaaFilter = _RiaaFilter.Select(x => x / at1000).ToArray();
		}

		/// <summary>
		/// create the table of Riaa multiplier values
		/// normalized to 0dB gain at 1KHz
		/// </summary>
		/// <param name="sampleSize">usually samplerate/2</param>
		/// <param name="Df">delta frequency for sweep</param>
		/// <returns>the containing object, with a transform method</returns>
		public static RiaaFilter CreateFilter(uint sampleSize, double df)
		{
			RiaaFilter rfx = new();
			rfx.BuildFilter(sampleSize, df);
			return rfx;
		}

		// create an SPL export file for mic compensation
		public static void WriteFilter(string filename)
		{
			var df = QaLibrary.CalcBinSize(96000, 65536);
			var filter = CreateFilter(65536, df);
			var strout = "db x";
			var vals = Enumerable.Range(1, 65536 / 2 - 1).Select(x => filter.DoFilter(x * df)).ToArray();
			var valsstr = vals.Select((x, i) => string.Format($"{i * df:F2} {-20 * Math.Log10(x)}")).ToList();
			valsstr.Insert(0, strout);
			System.IO.File.WriteAllLines(filename, valsstr);
		}
	};

	// this converts Laplace to Z domain for a biquad filter
	class BiquadBuilder
	{
		// the Time Poles & Zeros
		static double[] RiaaPoles = [3180e-6, 75e-6];   // poles
		static double[] RiaaZeros = [318e-6, 3.18e-6];  // optional second zero

		/// <summary>
		/// Calculates the pole-zero mapping coefficient for a given time constant and sampling frequency.
		/// </summary>
		/// <remarks>This method is commonly used in digital filter design to convert continuous-time system
		/// parameters to their discrete-time equivalents. Both parameters must be positive.</remarks>
		/// <param name="T">The time constant of the system, in seconds. </param>
		/// <param name="fs">The sampling frequency, in hertz. </param>
		/// <returns>The pole-zero mapping coefficient</returns>
		static double PoleZeroMap(double T, double fs)
		{
			return Math.Exp(-1.0 / (fs * T));
		}
		/// <summary>
		/// Compute Z-Domain Biquad coefficients based on time zeros and poles
		/// </summary>
		/// <param name="Poles"></param>
		/// <param name="Zeros"></param>
		/// <param name="fs"></param>
		/// <param name="extrazero"></param>
		/// <returns>a biquadfilter object with Process</returns>
		static BiquadFilter BuildBiquad(double[] Poles, double[] Zeros, double fs, bool extrazero)
		{
			var p1 = PoleZeroMap(Poles[0], fs);
			var p2 = PoleZeroMap(Poles[1], fs);
			var z1 = PoleZeroMap(Zeros[0], fs);
			var z2 = PoleZeroMap(Zeros[1], fs);

			var d0 = 1.0;
			var d1 = -p1 - p2;
			var d2 = p1 * p2;

			var n0 = 1.0;
			var n1 = -z1;
			var n2 = 0.0;

			if (extrazero)
			{
				n0 = 1.0;
				n1 = -z1 - z2;
				n2 = z1 * z2;
			}
			return new BiquadFilter(d0, d1, d2, n0, n1, n2);
		}

		public static BiquadFilter BuildRiaaBiquad(uint sampleRate, bool useExtraZero)
		{
			double[] zeros = (double[])RiaaZeros.Clone();

			// this redoes the second zero based on fs
			// based on Jan Didden stuff
			double[] t4s = [20053, 30478, 41656];   // with added zero
			double[] t4sno = [21833, 38433, 75313]; // (without)
			List<uint> sr = [48000, 96000, 192000];
			var idx = sr.IndexOf(sampleRate);
			zeros[1] = 1 / (2 * Math.PI * (useExtraZero ? t4s[idx] : t4sno[idx]));
			// always high end filter somewhat with that extra zero
			var bq = BuildBiquad(RiaaPoles, zeros, sampleRate, true);
			bq.PrintString();
			return bq;
		}

		// diagnostics
		public static void GetQuad()
		{
			BuildRiaaBiquad(48000, false);
			BuildRiaaBiquad(96000, false);
			BuildRiaaBiquad(192000, false);
			BuildRiaaBiquad(48000, true);
			BuildRiaaBiquad(96000, true);
			BuildRiaaBiquad(192000, true);
		}

	};
}
