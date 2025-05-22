using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Libraries
{
	public interface MathUtil
	{

		/// <summary>
		/// Parse text to double. Return fallback double if failed
		/// </summary>
		/// <param name="text">Text to parse</param>
		/// <param name="fallback">Fallback text</param>
		/// <returns></returns>
		public static double ToDouble(string? text, double fallback = 1e-6)
		{
			if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out double value))
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
		public static string FormatUnits(double val, string units)
		{
			var sign = Math.Sign(val);
			val = Math.Abs(val);
			string rslt = string.Empty;
			if (val >= 1000000)
			{
				rslt = (val / 1000000).ToString("0.###") + "M" + units;
			}
			else if ( val >= 1000)
			{
				rslt = (val/1000).ToString("0.###") + "K" + units;
			}
			else if (val >= 1)
			{
				rslt = val.ToString("0.###") + units;
			}
			else if (val >= 1e-3)
			{
				rslt = (1000 * val).ToString("G3") + "m" + units;
			}
			else if (val >= 1e-6)
			{
				rslt = (1000000 * val).ToString("G3") + "u" + units;
			}
			else
			{
				rslt = (1e9 * val).ToString("G3") + "n" + units;
			}
			if (sign < 0)
				rslt = "-" + rslt;
			return rslt;
		}

		/// <summary>
		/// pretty format voltage only
		/// <summary>
		/// <param name="val">value to format</param>
		/// <returns>string with best suffix</returns>
		public static string FormatVoltage(double val)
		{
			return FormatUnits(val, "V");
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

	}
}

