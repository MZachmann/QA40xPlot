using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Windows;
using System.Windows.Data;

namespace QA40xPlot.Converters
{
	public class EnumBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value.ToString()?.Equals(parameter.ToString()) == true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class EnumNotBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value.ToString()?.Equals(parameter.ToString()) == false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class EnumToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var tobl = value.ToString()?.Equals(parameter.ToString()) == true;
			if (tobl is bool boolValue)
			{
				return boolValue ? Visibility.Visible : Visibility.Collapsed;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class EnumNotVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var tobl = value.ToString()?.Equals(parameter.ToString()) == true;
			if (tobl is bool boolValue)
			{
				return boolValue ? Visibility.Collapsed : Visibility.Visible;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	// so things can be enabled with the inverse condition
	public class InverseBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool boolValue)
			{
				return boolValue ? Visibility.Visible : Visibility.Collapsed;
			}
			return Visibility.Collapsed;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class BoolToNotVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool boolValue)
			{
				return !boolValue ? Visibility.Visible : Visibility.Collapsed;
			}
			return Visibility.Collapsed;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	// collapse if the string matches the parameter
	public class StringToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is string stringValue)
			{
				return (stringValue == parameter.ToString()) ? Visibility.Collapsed : Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	/// <summary>
	/// internationalize max # of decimal points in a double value as in 0.###
	/// ConverterParameter is the number of digits to round to, e.g. 3 for 0.###, -3 for 0.000
	/// because ContentFormat {0.###} does not internationalize
	/// </summary>
	public class DoubleIntlConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (parameter != null && parameter is string)
			{
				var idigits = MathUtil.ToInt((string)parameter, 3); // get the number of digits to round to
				if (idigits >= 0)
				{
					if (value is double dblValue)
					{
						dblValue = Math.Round(dblValue, idigits); // round to the specified number of digits
						return dblValue.ToString(); // format to 3 significant digits
					}
				}
				else
				{
					// fixed format
					if (value is double dblValue)
					{
						var suffix = Math.Abs(idigits).ToString(); // get the suffix for the number of digits
						return dblValue.ToString("F" + suffix); // format to n significant digits
					}
				}
			}
			return string.Empty;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class VtoPctConverter : IMultiValueConverter
	{
		public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return string.Empty;
			foreach (object foo in value)
				if (foo == null || foo == DependencyProperty.UnsetValue)
					return string.Empty;
			// three arguments are:
			// voltage, reference voltage, usepercent
			string rslt = string.Empty;
			if ((bool)value[2])
			{
				double dv = (double)value[0];
				rslt = dv.ToString("G3") + "%";
			}
			else
			{
				double dv = (double)value[1];
				if (dv >= 1)
				{
					rslt = dv.ToString("0.###") + " V";
				}
				else if (dv >= 1e-3)
				{
					rslt = (1000 * dv).ToString("G3") + " mV";
				}
				else if (dv >= 1e-6)
				{
					rslt = (1000000 * dv).ToString("G3") + " μV";
				}
				else
				{
					rslt = (1e9 * dv).ToString("G3") + " nV";
				}
			}
			return rslt;
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return [new object()];
		}
	}

	public class VoltFormatter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || value == DependencyProperty.UnsetValue)
				return string.Empty;
			// three arguments are:
			// voltage, reference voltage, usepercent
			string rslt = string.Empty;
			{
				double dv = (double)value;
				var adv = Math.Abs(dv);
				if (adv >= 100)
				{
					rslt = dv.ToString("0.#") + " V";
				}
				else if (adv >= .099)
				{
					rslt = dv.ToString("0.###") + " V";
				}
				else if (adv >= 1e-4)
				{
					rslt = (1000 * dv).ToString("G3") + " mV";
				}
				else if (adv >= 1e-7)
				{
					rslt = (1000000 * dv).ToString("G3") + " μV";
				}
				else
				{
					rslt = (1e9 * dv).ToString("G3") + " nV";
				}
			}
			return rslt;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class WattFormatter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || value == DependencyProperty.UnsetValue)
				return string.Empty;
			// three arguments are:
			// voltage, reference voltage, usepercent
			string rslt = string.Empty;
			{
				double dv = (double)value;
				var adv = Math.Abs(dv);
				if (adv >= 100)
				{
					rslt = dv.ToString("0.#") + " W";
				}
				else if (adv >= .099)
				{
					rslt = dv.ToString("0.###") + " W";
				}
				else if (adv >= 1e-4)
				{
					rslt = (1000 * dv).ToString("G3") + " mW";
				}
				else if (adv >= 1e-7)
				{
					rslt = (1000000 * dv).ToString("G3") + " μW";
				}
				else
				{
					rslt = (1e9 * dv).ToString("G3") + " nW";
				}
			}
			return rslt;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	public class TimeFormatter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
			{
				if (value == null || value == DependencyProperty.UnsetValue)
					return string.Empty;
				// three arguments are:
				// voltage, reference voltage, usepercent
				string rslt = string.Empty;
				{
					double dv = (double)value;
					var adv = Math.Abs(dv);
					if (adv >= 100)
					{
						rslt = dv.ToString("0.#") + " S";
					}
					else if (adv >= .099)
					{
						rslt = dv.ToString("0.###") + " S";
					}
					else if (adv >= 1e-4)
					{
						rslt = (1000 * dv).ToString("G3") + " mS";
					}
					else if (adv >= 1e-7)
					{
						rslt = (1000000 * dv).ToString("G3") + " μS";
					}
					else
					{
						rslt = (1e9 * dv).ToString("G3") + " nS";
					}
				}
				return rslt;
			}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	/// <summary>
	/// convert a voltage to dbv format
	/// </summary>
	public class VoltDbvFormatter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null || value == DependencyProperty.UnsetValue)
				return string.Empty;
			// three arguments are:
			// voltage, reference voltage, usepercent
			string rslt = string.Empty;
			{
				double dv = (double)value;
				if (dv <= 0)
				{
					return "N/A dBV"; // no negative voltages
				}
				dv = QaLibrary.ConvertVoltage(dv, E_VoltageUnit.Volt, E_VoltageUnit.dBV); // convert to dBV
				var adv = Math.Abs(dv);
				if (adv >= 10)
				{
					rslt = dv.ToString("0.#");
				}
				else if (adv >= 1)
				{
					rslt = dv.ToString("0.##");
				}
				else
				{
					rslt = dv.ToString("G3");
				}
			}
			return rslt + " dBV";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
