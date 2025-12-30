using System.Collections;
using System.Collections.Generic;

namespace QA40xPlot.Extensions
{
	public static class EnumExtend
	{
		/// <summary>
		/// count the number of elements until the predicate becomes true
		/// if the predicate never becomes true, return -1
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="source"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public static int CountUntil<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			ArgumentNullException.ThrowIfNull(source);

			ArgumentNullException.ThrowIfNull(predicate);

			int count = 0;
			foreach (TSource element in source)
			{
				if (!predicate(element))
				{
					count++;
				}
				else
					return count;
			}
			return -1;
		}

		/// <summary>
		/// count the number of elements while the predicate is true
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="source"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public static int CountWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			ArgumentNullException.ThrowIfNull(source);

			ArgumentNullException.ThrowIfNull(predicate);

			int count = 0;
			foreach (TSource element in source)
			{
				if (predicate(element))
				{
					count++;
				}
				else
					return count;
			}
			return count;
		}

	}
}
