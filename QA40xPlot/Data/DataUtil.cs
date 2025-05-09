using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Data
{
	internal class DataUtil
	{
		/// <summary>
		/// Reflect the shown status from the OtherSet to the DataTab list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tabs"></param>
		/// <param name="os"></param>
		public static void ReflectOtherSet<T>( List<DataTab<T>> tabs, List<OtherSet> os )
		{
			foreach(var o in os)
			{
				var tab = tabs.FirstOrDefault(t => t.Id == o.Id);
				if (tab != null)
				{
					tab.Show = (o.IsOnL ? 1 : 0) + (o.IsOnR ? 2 : 0);
				}
			}
		}

		/// <summary>
		/// find the shown Left,Right properties in the datatab list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="tabs"></param>
		/// <returns></returns>
		public static List<U> FindShownInfo<T, U>(List<DataTab<T>> tabs)
		{
			List<U> result = new();
			foreach (var t in tabs)
			{
				if (1 == (t.Show & 1))
				{
					var l = t.GetProperty("Left");
					if (l != null)
					{
						result.Add((U)l);
					}
				}
				if (2 == (t.Show & 2))
				{
					var r = t.GetProperty("Right");
					if (r != null)
					{
						result.Add((U)r);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// find the shown Left,Right properties in the datatab list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="tabs"></param>
		/// <returns></returns>
		public static List<double[]> FindShownFreqs<T>(List<DataTab<T>> tabs)
		{
			List<double[]> result = new();
			foreach (var t in tabs)
			{
				var f = t.FreqRslt;
				if (f == null)
					continue;
				if (1 == (t.Show & 1))
				{
					result.Add(f.Left);
				}
				if (2 == (t.Show & 2))
				{
					result.Add(f.Right);
				}
			}
			return result;
		}

			/// <summary>
		/// find the shown Left,Right properties in the datatab list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="tabs"></param>
		/// <returns></returns>
		public static List<double[]> FindShownTimes<T>(List<DataTab<T>> tabs)
		{
			List<double[]> result = new();
			foreach (var t in tabs)
			{
				var f = t.TimeRslt;
				if (f == null)
					continue;
				if (1 == (t.Show & 1))
				{
					result.Add(f.Left);
				}
				if (2 == (t.Show & 2))
				{
					result.Add(f.Right);
				}
			}
			return result;
		}
	}
}
