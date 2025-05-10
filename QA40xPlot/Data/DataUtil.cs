using HarfBuzzSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QA40xPlot.Data
{
	internal class DataUtil
	{
		/// <summary>
		/// Reflect the shown status from the OtherSet to the DataTab list. 
		/// it deletes stuff marked deleted
		/// and copies the show values
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tabs"></param>
		/// <param name="os"></param>
		public static void ReflectOtherSet<T>( List<DataTab<T>> tabs, ObservableCollection<OtherSet> os )
		{
			// see about deleting some
			List<OtherSet> osremove = [];
			foreach(var o in os)
			{
				if (o.IsDeleted)
				{
					for(int i=0; i<tabs.Count; i++)
					{
						if (tabs[i].Id == o.Id)
						{
							tabs.RemoveAt(i);
							break;
						}
					}
					osremove.Add(o);
				}
				else
				{
					var tab = tabs.FirstOrDefault(t => t.Id == o.Id);
					if (tab != null)
					{
						tab.Show = (o.IsOnL ? 1 : 0) + (o.IsOnR ? 2 : 0);
					}
				}
			}
			foreach (var o in osremove)
			{
				os.Remove(o);
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
