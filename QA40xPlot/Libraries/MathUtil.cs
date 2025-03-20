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
		/// Parse text to double. Return fallback texts if failed
		/// </summary>
		/// <param name="text">Text to parse</param>
		/// <param name="fallback">Fallback text</param>
		/// <returns></returns>
		public static double ParseTextToDouble(string? text, double fallback)
		{

			if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out double value))
				return value;     // return parsed value

			return fallback;
		}

		public static double ToDouble(string? text)
		{
			return ParseTextToDouble(text, 1e-6);
		}

		/// <summary>
		/// Parse text to double. Return fallback texts if failed
		/// </summary>
		/// <param name="text">Text to parse</param>
		/// <param name="fallback">Fallback text</param>
		/// <returns></returns>
		public static int ParseTextToInt(string? text, int fallback)
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
		public static uint ParseTextToUint(string? text, uint fallback)
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
		public static string FormatVoltage(double val)
		{
			string rslt = string.Empty;
			if (val >= .01)
			{
				rslt = val.ToString("0.###") + " V";
			}
			else if (val >= 1e-5)
			{
				rslt = (1000 * val).ToString("G3") + " mV";
			}
			else if (val >= 1e-8)
			{
				rslt = (1000000 * val).ToString("G3") + " uV";
			}
			else
			{
				rslt = (1e9 * val).ToString("G3") + " nV";
			}
			return rslt;
		}

	}
}

