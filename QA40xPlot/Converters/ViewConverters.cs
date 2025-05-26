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

	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool boolValue)
			{
				return boolValue ? Visibility.Visible : Visibility.Hidden;
			}
			return Visibility.Hidden;
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
				if (dv >= .01)
				{
					rslt = dv.ToString("0.###") + " V";
				}
				else if (dv >= 1e-5)
				{
					rslt = (1000 * dv).ToString("G3") + " mV";
				}
				else if (dv >= 1e-8)
				{
					rslt = (1000000 * dv).ToString("G3") + " uV";
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
				if (dv >= .01)
				{
					rslt = dv.ToString("0.###") + " V";
				}
				else if (dv >= 1e-5)
				{
					rslt = (1000 * dv).ToString("G3") + " mV";
				}
				else if (dv >= 1e-8)
				{
					rslt = (1000000 * dv).ToString("G3") + " uV";
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
}
