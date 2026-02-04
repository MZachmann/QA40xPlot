using FftSharp;

namespace QA40xPlot.Libraries
{
	public class FftUtil
	{
		// FftSharp related utility methods can be added here if needed
		/// <summary>
		/// Scale all values in the window equally (in-place) so their total is 1
		/// </summary>
		internal static void NormalizeInPlace(double[] values)
		{
			double sum = 0;
			for (int i = 0; i < values.Length; i++)
				sum += values[i];

			for (int i = 0; i < values.Length; i++)
				values[i] /= sum;
		}
	}

	// blackman harris 7-term window

	public class BlackmanH7 : Window, IWindow
	{
		// see https://www.mathworks.com/matlabcentral/mlc-downloads/downloads/submissions/46092/versions/3/previews/coswin.m/index.html?access_key=
		// note that these coefficients are slightly different from those above
		private readonly double[] Coefficients = { 2.712203605850388e-001, 4.334446123274422e-001, 2.180041228929303e-001,
			6.578534329560609e-002, 1.076186730534183e-002, 7.700127105808265e-004, 1.368088305992921e-005 };

		public override string Name => "Blackman-Harris-7";
		public override string Description =>
			"The Blackman-Harris-7 window is similar to Hamming and Hanning windows. " +
			"The resulting spectrum has a wide peak, but good side lobe compression.";

		public override bool IsSymmetric => true;

		public BlackmanH7()
		{
		}

		public override double[] Create(int size, bool normalize = false)
		{
			double[] window = new double[size];

			for (int i = 0; i < size; i++)
			{
				double frac = (double)i / (size - 1);
				double value = 0.0;

				for (int j = 0; j < Coefficients.Length; j++)
				{
					double sign = (j % 2 == 0) ? 1.0 : -1.0;
					value += sign * Coefficients[j] * Math.Cos(2 * Math.PI * j * frac);
				}
				window[i] = value;
			}

			if (normalize)
				FftUtil.NormalizeInPlace(window);

			return window;
		}
	}
}
