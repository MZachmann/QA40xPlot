using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QA40xPlot.Libraries
{
	public static class Util
	{
		public static void GetPropertiesFrom(Dictionary<string, Dictionary<string, object>> vwsIn, string name, object dest)
		{
			if (vwsIn == null || dest == null)
				return;
			if (!vwsIn.ContainsKey(name))
				return;
			Dictionary<string, object> vws = (Dictionary<string, object>)vwsIn[name];

			Type type = dest.GetType();
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			try
			{
				foreach (PropertyInfo property in properties)
				{
					if (property.CanRead && property.CanWrite)
					{
						if (vws.ContainsKey(property.Name))
						{
							object value = vws[property.Name];
							try
							{
								//Debug.WriteLine("Property " + property.Name);
								property.SetValue(dest, Convert.ChangeType(value, property.PropertyType));
							}
							catch (Exception) { }    // for now ignore this
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

		}

	}
}
