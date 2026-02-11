using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Windows.Media;

namespace QA40xPlot.Libraries
{
	public interface MathUtil
	{
		/// <summary>
		/// Given an input voltage value scale it for the unit to return a double
		/// in units of volt or watt for math
		/// </summary>
		/// <param name="value">the data value</param>
		/// <param name="genFormat">the entry format</param>
		/// <returns>a converted double that is ready to become text</returns>
		public static double MergeUnitValue(double value, string genFormat)
		{
			switch (genFormat)
			{
				// power formats
				case "mW":    // the generator has 18dBV output, the input has 32dBV maximum
					return value / 1000;
				case "μW":
					return value / 1000000;
				case "W":
					return value;
				case "dBW":
					return Math.Pow(10, value / 10);
				case "dBm":
					return Math.Pow(10, value / 10) / 1000;
				// voltage formats
				case "mV":
					return value / 1000;
				case "μV":
					return value / 1000000;
				case "dBV":
					return Math.Pow(10, value / 20);
				case "dBmV":
					return Math.Pow(10, value / 20) / 1000;
				case "dBu":
					return Math.Pow(10, value / 20) * 0.775;
				case "dBFS":
					return Math.Pow(10, (value + 18) / 20);
			}
			return value; // default to volts
		}

		/// <summary>
		/// Parse text to double. Return fallback double if failed
		/// </summary>
		/// <param name="text">Text to parse</param>
		/// <param name="fallback">Fallback text</param>
		/// <returns></returns>
		public static double ToDouble(string? text, double fallback = 1e-6)
		{
			CultureInfo usCulture = new CultureInfo("en-US");
			if (text == null)
				return fallback; // return fallback if text is null

			string txtInt = text.Replace(',', '.');     // convert commas to decimal points for US culture
			txtInt = txtInt.Replace("'", ".");            // convert these to periods also, for US culture
			if (double.TryParse(txtInt, NumberStyles.Any, usCulture, out double value))
				return value;     // return parsed value

			return fallback;
		}

		/// <summary>
		/// Parse text to double. Return fallback texts if failed
		/// </summary>
		/// <param name="text">Text to parse</param>
		/// <param name="fallback">Fallback text</param>
		/// <returns></returns>
		public static int ToInt(string? text, int fallback)
		{

			if (int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out int value))
				return value;     // return parsed value

			return fallback;
		}

		/// <summary>
		/// Parse text to double. Return fallback texts if failed
		/// </summary>
		/// <param name="text">Text to parse</param>
		/// <param name="fallback">Fallback text</param>
		/// <returns></returns>
		public static uint ToUint(string? text, uint fallback = 0)
		{

			if (uint.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out uint value))
				return value;     // return parsed value

			return fallback;
		}

		/// <summary>
		/// format floatingpoint data, val is absolute or dbm...
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with no suffix</returns>
		public static string FormatLogger(double val)
		{
			if (val <= 1e-5)
			{
				return val.ToString("G3");
			}
			if (val < 1.0)
			{
				return val.ToString("0.#####");
			}
			else if (val < 10.0)
			{
				return val.ToString("0.##");
			}
			else if (val < 10000)
				return Math.Round(val).ToString();
			return (Math.Round(val) / 1000).ToString() + "K";
		}

		/// <summary>
		/// format percent data val = in percent
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with no suffix</returns>
		public static string FormatPercent(double val)
		{
			if (val <= 1e-5)
			{
				return val.ToString("G3");
			}
			if (val < 1.0)
			{
				return val.ToString("0.######");
			}
			else if (val < 10.0)
			{
				return val.ToString("0.##");
			}

			return Math.Round(val).ToString();
		}

		/// <summary>
		/// pretty format voltage only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatUnits(double val, string units, bool addSpace = false)
		{
			var sign = Math.Sign(val);
			val = Math.Abs(val);
			string rslt = string.Empty;
			string spacer = addSpace ? " " : string.Empty;
			if (val >= 1000000)
			{
				rslt = (val / 1000000).ToString("0.###") + spacer + "M" + units;
			}
			else if (val >= 1000)
			{
				rslt = (val / 1000).ToString("0.###") + spacer + "K" + units;
			}
			else if (val >= 1)
			{
				rslt = val.ToString("0.###") + spacer + units;
			}
			else if (val >= 1e-3)
			{
				rslt = (1000 * val).ToString("G3") + spacer + "m" + units;
			}
			else if (val >= 1e-6)
			{
				rslt = (1000000 * val).ToString("G3") + spacer + "μ" + units;
			}
			else if (val > 0)
			{
				rslt = (1e9 * val).ToString("G3") + spacer + "n" + units;
			}
			else
			{
				rslt = "0 " + spacer + units;
			}
			if (sign < 0)
				rslt = "-" + rslt;
			return rslt.TrimStart();
		}

		/// <summary>
		/// pretty format voltage only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatVoltage(double val, bool addSpacer = false)
		{
			return FormatUnits(val, "V", addSpacer);
		}

		/// <summary>
		/// pretty format voltage only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatPower(double val, bool addSpacer = false)
		{
			return FormatUnits(val, "W", addSpacer);
		}

		/// <summary>
		/// pretty format voltage only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatFrequency(double val)
		{
			return FormatUnits(val, "Hz");
		}

		/// <summary>
		/// pretty format voltage only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatCurrent(double val)
		{
			return FormatUnits(val, "A");
		}

		/// <summary>
		/// pretty format resistance only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatResistance(double val)
		{
			return FormatUnits(val, "Ω");
		}

		/// <summary>
		/// pretty format inductance only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatInductance(double val)
		{
			return FormatUnits(val, "H");
		}

		/// <summary>
		/// pretty format capacitance only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatCapacitance(double val)
		{
			return FormatUnits(val, "F");
		}

		/// <summary>
		/// pretty format capacitance only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatPhase(double val)
		{
			val = (int)(Math.Floor(val));
			return FormatUnits(val, "°");
		}

		public static double MeasureString(System.Windows.Controls.TextBox textBlock, string? candidate)
		{
			if (candidate == null)
				return 0;

			var formattedText = new FormattedText(
				candidate,
				CultureInfo.CurrentCulture,
				System.Windows.FlowDirection.LeftToRight,
				new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
				textBlock.FontSize,
				System.Windows.Media.Brushes.Black,
				new NumberSubstitution(),
				VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);
			return formattedText.Width;
		}

		public static double[] ToImpedanceMag(double[] re, double[] im)
		{
			var xout = new double[re.Length];
			for (int i = 0; i < im.Length; i++)
			{
				xout[i] = ToImpedanceMag(re[i], im[i]);
			}
			return xout;
		}

		public static double[] ToImpedancePhase(double[] re, double[] im)
		{
			var xout = new double[re.Length];
			for (int i = 0; i < im.Length; i++)
			{
				xout[i] = ToImpedancePhase(re[i], im[i]);
			}
			return xout;
		}

		private static bool SameDouble(double a, double b)
		{
			return Math.Abs(a - b) < (Math.Abs(a) / 1e10);
		}

		// return mag of Z/(1-Z)
		public static double ToImpedanceMag(double re, double im)
		{
			var maga = re * re + im * im;
			var magb = (1 - re) * (1 - re) + im * im;
			var tmage = Math.Sqrt(maga / magb);
#if DEBUG
			var tcplx = (new Complex(re, im)) / (new Complex(1 - re, -im));
			var tmag = tcplx.Magnitude;
			Debug.Assert(SameDouble(tmag, tmage), "disagree on magnitude");
#endif
			return tmage;
		}

		// return phase of Z/(1-Z)
		public static double ToImpedancePhase(double re, double im)
		{
			var denomre = (1 - re) * (1 - re) + im * im;
			var numre = (re * (1 - re) - im * im);
			var numim = (re * im + im * (1 - re));
			var phasea = Math.Atan2(numim / denomre, numre / denomre);
#if DEBUG
			var denomCplx = (new Complex(1 - re, -im)) * (new Complex(1 - re, im));
			var numCplx = (new Complex(re, im)) * (new Complex(1 - re, im));
			var tcplx = (new Complex(re, im)) / (new Complex(1 - re, -im));
			var tphase = tcplx.Phase;
			Debug.Assert(SameDouble(tphase, phasea), "phases differ");
#endif
			return phasea;
		}

		public static double[] ToCplxMag(double[] re, double[] im)
		{
			var xout = new double[re.Length];
			for (int i = 0; i < im.Length; i++)
			{
				xout[i] = ToCplxMag(re[i], im[i]);
			}
			return xout;
		}

		public static double[] ToCplxPhase(double[] re, double[] im)
		{
			var xout = new double[re.Length];
			for (int i = 0; i < im.Length; i++)
			{
				xout[i] = ToCplxPhase(re[i], im[i]);
			}
			return xout;
		}

		// return mag of Z/(1-Z)
		public static double ToCplxMag(double re, double im)
		{
			var magout = Math.Sqrt(re * re + im * im);
			return magout;
			//var xtest = z / ((new Complex(1, 0)) - z);  // do the math
			//return xtest;
		}

		// return phase of Z/(1-Z)
		public static double ToCplxPhase(double re, double im)
		{
			var phaseout = Math.Atan2(im, re);
			return phaseout;
		}

		/// <summary>
		/// Forward smoothing using a running total
		/// Increases the windowSize as F increases logarithmically
		/// the window size 'goal' is a percent of an octave
		/// </summary>
		/// <param name="data">the double[] data to smooth</param>
		/// <param name="windowSize">the size of the window</param>
		/// <param name="windowDelta">every N points increase the windowSize by 1</param>
		/// <returns></returns>
		public static double[] SmoothAverage(double[] data, double perOctave)
		{
			double[] smooth = new double[data.Length];
			double runningSum = 0;
			int pointsInSum = 0;
			int windowSize = 1;
#if DEBUG
			for (int i = 0; i < smooth.Length; i++)
				smooth[i] = -100; // initialize to invalid value
#endif
			for (int i = 0; i < smooth.Length; i++)
			{
				runningSum += data[i];
				if (pointsInSum < windowSize)
				{
					pointsInSum++;
					if (pointsInSum == windowSize)
					{
						smooth[i - (int)(windowSize / 2)] = runningSum / windowSize;
					}
					continue;
				}
				runningSum -= data[i - windowSize];
				smooth[i - (int)(windowSize / 2)] = runningSum / windowSize;
				if (((i * perOctave) > windowSize) || (windowSize == 1))
				{
					windowSize += 2;
				}
			}
			for (int i = 0; windowSize > 2; i++)
			{
				smooth[data.Length - (int)(windowSize / 2)] = runningSum / windowSize;
				runningSum -= data[data.Length - windowSize--];
				runningSum -= data[data.Length - windowSize--];
			}
			// self - test that we didn't stupidly miss any points
#if DEBUG
			for (int i = 0; i < smooth.Length; i++)
			{
				if ((smooth[i] == -100))
				{
					Debug.Assert(false, "did not fill smoothing array");
				}
			}
#endif
			return smooth;
		}

		/// <summary>
		/// Forward smoothing using a running total
		/// Increases the windowSize as F increases logarithmically
		/// the window size 'goal' is a percent of an octave
		/// </summary>
		/// <param name="data">the double[] data to smooth</param>
		/// <param name="windowSize">the size of the window</param>
		/// <param name="windowDelta">every N points increase the windowSize by 1</param>
		/// <returns></returns>
		public static double[]
			SmoothForward(double[] data, double perOctave)
		{
			double[] smooth = new double[data.Length];
			double runningSum = 0;
			int pointsInSum = 0;
			int windowSize = 1;
			for (int i = 0; i < smooth.Length; i++)
			{
				runningSum += data[i];
				if (pointsInSum < windowSize)
				{
					pointsInSum++;
					smooth[i] = runningSum / pointsInSum;
					continue;
				}
				runningSum -= data[i - windowSize];
				smooth[i] = runningSum / windowSize;
				if ((i * perOctave) > windowSize)
				{
					windowSize++;
				}
			}
			return smooth;
		}

	}
}

