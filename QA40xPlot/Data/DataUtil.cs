using QA40xPlot.Libraries;

namespace QA40xPlot.Data
{
	internal class DataUtil
	{
		/// <summary>
		/// find the shown Left,Right properties in the datatab list
		/// </summary>
		/// <typeparam name="T">type of datatab</typeparam>
		/// <typeparam name="U">result type/viewmodel type</typeparam>
		/// <param name="tabs">list of other tabs</param>
		/// <returns>tabs that are enabled</returns>
		public static List<DataTab<T>> FindShown<T>(List<DataTab<T>> tabs)
		{
			List<DataTab<T>> result = new();
			foreach (var t in tabs)
			{
				if (t.Definition.IsOnL)
				{
					result.Add(t);
				}
				else if (t.Definition.IsOnR)
				{
					result.Add(t);
				}
			}
			return result;
		}

		/// <summary>
		/// find the shown Left,Right properties in the datatab list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="tabs">a list of data tabs</param>
		/// <returns></returns>
		public static List<U> FindShownInfo<T, U>(List<DataTab<T>> tabs)
		{
			List<U> result = new();
			foreach (var t in tabs)
			{
				if (t.Definition.IsOnL)
				{
					var l = t.GetProperty("Left");
					if (l != null)
					{
						result.Add((U)l);
					}
				}
				if (t.Definition.IsOnR)
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
		public static List<LeftRightResult> FindShownGains<T>(List<DataTab<T>> tabs)
		{
			List<LeftRightResult> result = new();
			foreach (var t in tabs)
			{
				if(t.Definition.IsOnL && t.GainLeft != null)
				{
					var u = new LeftRightResult
					{
						Left = t.GainLeft,
						Right = t.GainRight,
						XValues = t.GainFrequencies
					};
					result.Append(u);
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
