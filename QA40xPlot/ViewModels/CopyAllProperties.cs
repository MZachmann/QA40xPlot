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
			// since stuff is being loaded while we do this, and setting a property can cause side effects, so
			// make a clone incase it changes so we don't crash...
			var props = (PropertyInfo[])properties.Clone();
			foreach (PropertyInfo property in props)
			{
				if (property.CanRead && property.CanWrite)
				{
					object? value = property.GetValue(source);
					property.SetValue(destination, value);
				}
			}
		}
	}
}
