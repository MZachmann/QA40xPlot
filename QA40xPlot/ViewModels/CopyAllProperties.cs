using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.ViewModels
{
	public static class ObjectExtensions
	{
		public static bool HasProperty(this object obj, string propertyName)
		{
			return obj.GetType().GetProperty(propertyName) != null;
		}

		public static void CopyPropertiesTo<T>(this T source, object destination)
		{
			if (source == null || destination == null)
				throw new ArgumentNullException("Source or/and Destination Objects are null");

			Type type = typeof(T);
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

			foreach (PropertyInfo property in properties)
			{
				if (property.CanRead && property.CanWrite)
				{
					object value = property.GetValue(source);
					property.SetValue(destination, value);
				}
			}
		}
	}
}
