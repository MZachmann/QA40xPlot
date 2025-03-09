using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QA40xPlot.Views
{
	public class EnumBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value.ToString().Equals(parameter.ToString());
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
			return (bool)value ? parameter : Binding.DoNothing;
		}
	}
}
