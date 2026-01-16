
using System.Numerics;

/// Historical LeftRight pair classes and conversion utilities
namespace QA40xPlot.Libraries
{
	public static class ConvertUtil
	{
		// convert a double to an 8 byte string value
		public static ulong CvtFromDouble(double d)
		{
			ulong bits = BitConverter.DoubleToUInt64Bits(d);
			return bits; // Convert.ToString(bits);
		}

		/// <summary>
		/// convert an 8 byte string value to a double. This is the reverse of CvtFromDouble
		/// </summary>
		/// <param name="sd"></param>
		/// <returns></returns>
		public static double CvtToDouble(ulong sd)
		{
			//UInt64 bits = UInt64.Parse(sd);
			double db = BitConverter.UInt64BitsToDouble(sd);
			return db;
		}

		/// <summary>
		/// convert a double array to a base64 string. This is used to save the Left and Right arrays
		/// </summary>
		/// <param name="arr"></param>
		/// <returns></returns>
		public static string CvtFromArray(double[] arr)
		{
			if (arr == null || arr.Length == 0)
				return string.Empty;
			byte[] byteArray = new byte[arr.Length * sizeof(double)];
			Buffer.BlockCopy(arr, 0, byteArray, 0, byteArray.Length);
			return Convert.ToBase64String(byteArray, Base64FormattingOptions.None);
		}

		/// <summary>
		/// convert a base64 string to a double array. This is used to restore the Left and Right arrays
		/// </summary>
		/// <param name="bda">the base64 representation of the double array</param>
		/// <returns></returns>
		public static double[] CvtToArray(string bda)
		{
			if (string.IsNullOrEmpty(bda))
				return new double[0];
			byte[] byteArray = Convert.FromBase64String(bda);
			double[] doubleArray = new double[byteArray.Length / sizeof(double)];
			Buffer.BlockCopy(byteArray, 0, doubleArray, 0, byteArray.Length);
			return doubleArray;
		}
	}

	public class LeftRightPair
	{
		public double Left { get; set; }
		public double Right { get; set; }
		public LeftRightPair(double left, double right)
		{
			Left = left;
			Right = right;
		}
		public LeftRightPair() { }
		public void Divby(double dX)
		{
			Left /= dX;
			Right /= dX;
		}
	}

	public class LeftRightFreqSaver
	{
		/// <summary>
		/// dt is the time between samples. 1/dt is the sample rate
		/// </summary>
		public ulong Df { get; set; }// = string.Empty;
		public string Left { get; set; } = string.Empty;
		public string Right { get; set; } = string.Empty;

		// to avoid warnings. Note this never doesn't get set during real 'new'
		public LeftRightFreqSaver()
		{
		}

		public void FromSeries(LeftRightFrequencySeries lrft)
		{
			Df = ConvertUtil.CvtFromDouble(lrft.Df);
			Left = ConvertUtil.CvtFromArray(lrft.Left); // lrft.Left.Select(CvtFromDouble).ToArray();
			Right = ConvertUtil.CvtFromArray(lrft.Right); // lrft.Right.Select(CvtFromDouble).ToArray();
		}

		public LeftRightFrequencySeries ToSeries()
		{
			LeftRightFrequencySeries lrft = new LeftRightFrequencySeries();
			lrft.Df = ConvertUtil.CvtToDouble(Df);
			lrft.Left = ConvertUtil.CvtToArray(Left); // Left.Select(CvtToDouble).ToArray();
			lrft.Right = ConvertUtil.CvtToArray(Right); // Right.Select(CvtToDouble).ToArray();
			return lrft;
		}

	}

	public class LeftRightTimeSaver
	{
		/// <summary>
		/// dt is the time between samples. 1/dt is the sample rate
		/// </summary>
		public ulong dt { get; set; }// = string.Empty;
		public string Left { get; set; } = string.Empty;
		public string Right { get; set; } = string.Empty;

		// to avoid warnings. Note this never doesn't get set during real 'new'
		public LeftRightTimeSaver()
		{
		}

		public void FromSeries(LeftRightTimeSeries lrft)
		{
			dt = ConvertUtil.CvtFromDouble(lrft.dt);
			Left = ConvertUtil.CvtFromArray(lrft.Left); // lrft.Left.Select(CvtFromDouble).ToArray();
			Right = ConvertUtil.CvtFromArray(lrft.Right); // lrft.Right.Select(CvtFromDouble).ToArray();
		}

		public LeftRightTimeSeries ToSeries()
		{
			LeftRightTimeSeries lrft = new LeftRightTimeSeries();
			lrft.dt = ConvertUtil.CvtToDouble(dt);
			lrft.Left = ConvertUtil.CvtToArray(Left); // Left.Select(CvtToDouble).ToArray();
			lrft.Right = ConvertUtil.CvtToArray(Right); // Right.Select(CvtToDouble).ToArray();
			return lrft;
		}
	}

	public class LeftRightTimeSeries
	{
		/// <summary>
		/// dt is the time between samples. 1/dt is the sample rate
		/// </summary>
		public double dt { get; set; }
		public double[] Left { get; set; }
		public double[] Right { get; set; }

		// to avoid warnings. Note this never doesn't get set during real 'new'
		public LeftRightTimeSeries()
		{
			dt = 0.0;
			Left = new double[0];
			Right = new double[0];
		}
	}

	public class LeftRightFreqComplexSeries
	{
		/// <summary>
		/// df is the frequency spacing of FFT bins
		/// </summary>
		public double Df { get; set; }
		public Complex[] Left { get; set; }
		public Complex[] Right { get; set; }

		// this is only invoked with real data during an acquisition, so ignore the init
		public LeftRightFreqComplexSeries()
		{
			Df = 1.0;
			Left = new Complex[0];
			Right = new Complex[0];
		}

		public int ToBinNumber(double dFreq)
		{
			return QaLibrary.GetBinOfFrequency(dFreq, Df);
		}
	}

	public class LeftRightFrequencySeries
	{
		/// <summary>
		/// df is the frequency spacing of FFT bins
		/// </summary>
		public double Df { get; set; }
		public double[] Left { get; set; }
		public double[] Right { get; set; }

		// this is only invoked with real data during an acquisition, so ignore the init
		public LeftRightFrequencySeries()
		{
			Df = 1.0;
			Left = new double[0];
			Right = new double[0];
		}

		public int ToBinNumber(double dFreq)
		{
			return QaLibrary.GetBinOfFrequency(dFreq, Df);
		}
	}

	public class LeftRightSeries
	{

		public LeftRightFrequencySeries? FreqRslt;
		public LeftRightTimeSeries? TimeRslt;
	}

}
