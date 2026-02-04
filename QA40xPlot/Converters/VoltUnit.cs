using QA40xPlot.Libraries;

namespace QA40xPlot.Converters
{
	/// <summary>
	/// this is not an IValueConverter any more
	/// </summary>
	public class VoltUnitConverter
	{
		/// <summary>
		/// Given an input voltage format, get a display format converter
		/// </summary>
		/// <param name="value">the data value</param>
		/// <param name="genFormat">the entry format</param>
		/// <returns>a converted double that is ready to become text</returns>
		private static double RemoveUnitVal(double value, string genFormat)
		{
			switch (genFormat)
			{
				// power formats
				case "mW":    // the generator has 18dBV output, the input has 32dBV maximum
					return value * 1000;
				case "μW":
					return value * 1000000;
				case "W":
					return value;
				case "dBW":
					return 10 * Math.Log10(value);
				case "dBm":
					return 10 * Math.Log10(value / 1000);
				// voltage formats
				case "mV":
					return value * 1000;
				case "μV":
					return value * 1000000;
				case "dBV":
					return 20 * Math.Log10(value);
				case "dBmV":
					return 20 * Math.Log10(value * 1000);
				case "dBu":
					return 20 * Math.Log10(value / 0.775);
				case "dBFS":
					return 20 * Math.Log10(value) - 18;
			}
			return value; // default to volts
		}

		/// <summary>
		/// convert an amount + unit into a double value
		/// </summary>
		/// <param name="value">the entry string</param>
		/// <param name="uom">unit of measure</param>
		/// <param name="defValue">result value if no parse</param>
		/// <returns></returns>
		public static double MergeUnit(string value, string uom, double defValue = 0.0)
		{
			var ampnew = defValue;
			try
			{
				// get the input variables here
				var ampvalue = MathUtil.ToDouble((string)value, double.MaxValue);     // actual voltage or power
				if (ampvalue != double.MaxValue)
				{
					ampnew = MathUtil.MergeUnitValue(ampvalue, uom); // convert to volts or watts
				}
			}
			catch (Exception ex)
			{
				// log the error
				System.Diagnostics.Debug.WriteLine($"Error in VoltUnitConverter: {ex.Message}");
			}
			return ampnew;
		}

		// converted united amount back to volts as it were

		/// <summary>
		/// converted united amount back to an entry that would 
		/// produce that with the given uom
		/// so ".050", "mV" -> 05
		/// </summary>
		/// <param name="value">the scaled entry</param>
		/// <param name="uom">the unit of measure</param>
		/// <param name="defValue">result default value if no parse</param>
		/// <returns></returns>
		public static double RemoveUnit(string value, string uom, double defValue)
		{
			var ampnew = defValue;
			try
			{
				// just return the input value, we leave it alone otherwise
				var ampvalue = MathUtil.ToDouble((string)value, double.MaxValue);     // actual voltage or power
				if (ampvalue != double.MaxValue)
				{
					ampnew = RemoveUnitVal(ampvalue, uom); // convert to volts or watts
				}
			}
			catch (Exception ex)
			{
				// log the error
				System.Diagnostics.Debug.WriteLine($"Error in VoltUnitConvertBack: {ex.Message}");
			}
			return ampnew;
		}
	}
}