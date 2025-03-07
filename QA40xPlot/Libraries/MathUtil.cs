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

	}
}

