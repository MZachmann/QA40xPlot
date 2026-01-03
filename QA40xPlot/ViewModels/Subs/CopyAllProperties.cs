using System.Reflection;

namespace QA40xPlot.ViewModels
{
	public static class ObjectExtensions
	{
		public static bool HasProperty(this object obj, string propertyName)
		{
			return obj.GetType().GetProperty(propertyName) != null;
		}

		public static void CopyOnlyPropertiesTo<T>(this T source, object destination, List<string>? includeList = null)
		{
			if (source == null || destination == null)
				throw new ArgumentNullException("Source or/and Destination Objects are null");

			Type type = typeof(T);
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			// since stuff is being loaded while we do this, and setting a property can cause side effects, so
			// make a clone incase it changes so we don't crash...
			var props = (PropertyInfo[])properties.Clone();
			foreach (PropertyInfo property in properties)
			{
				if (includeList != null && includeList.Contains(property.Name))
				{
					if (property.CanRead && property.CanWrite)
					{
						object? value = property.GetValue(source);
						property.SetValue(destination, value);
					}
				}
			}
		}

		public static void CopyPropertiesTo<T>(this T source, object destination, List<string>? excludeList = null)
		{
			if (source == null || destination == null)
				throw new ArgumentNullException("Source or/and Destination Objects are null");

			Type type = typeof(T);
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			// since stuff is being loaded while we do this, and setting a property can cause side effects, so
			// make a clone incase it changes so we don't crash...
			var props = (PropertyInfo[])properties.Clone();
			foreach (PropertyInfo property in properties)
			{
				if(excludeList != null && excludeList.Contains(property.Name))
					continue;
				if (property.CanRead && property.CanWrite)
				{
					object? value = property.GetValue(source);
					property.SetValue(destination, value);
				}
			}
		}
	}
}
