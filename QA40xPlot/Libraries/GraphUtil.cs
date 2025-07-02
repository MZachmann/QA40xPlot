using QA40xPlot.ViewModels;
using QA40xPlot.ViewModels.Subs;
using ScottPlot;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QA40xPlot.Libraries
{
	public enum PixelSizes
	{
		TITLE_SIZE = 14,
		SUBTITLE_SIZE = 12,
		AXIS_SIZE = 9,
		LABEL_SIZE = 9,
		LEGEND_SIZE = 9,
	}

	public static class GraphUtil
	{
		private static ThePalette _PlotPalette = new ThePalette();
			//new ScottPlot.Palettes.Tsitsulin();

		public static ThePalette PlotPalette { get => _PlotPalette; }

		public static int PtToPixels(PixelSizes fontsize)
		{
			var vm = ViewModels.ViewSettings.Singleton.Main;
			return (int)((int)fontsize * vm.ScreenDpi / 72);
		}
		public static int PtToPixels(int fontsize)
		{
			var vm = ViewModels.ViewSettings.Singleton.Main;
			return (int)(fontsize * vm.ScreenDpi / 72);
		}

		public static ScottPlot.Color GetPaletteColor(string colorName, int iIndex)
		{
			if("Transparent" != colorName)
			{
				return PlotUtil.StrToColor(colorName);
			}
			var c = PlotPalette.GetColor(iIndex);	// these may be translucent
			return c;
		}

		/// <summary>
		/// is this plot format log or linear where
		/// linear formats are %,V,W
		/// </summary>
		/// <param name="plotFormat"></param>
		/// <returns>true if log</returns>
		public static bool IsPlotFormatLog(string plotFormat)
		{
			switch (plotFormat)
			{
				case "SPL":
				case "dBFS":
				case "dBr":
				case "dBu":
				case "dBV":
				case "dBW":
				case "dB":
					return true;
			}
			return false;
		}

		/// <summary>
		/// for volt and watt, pretty print
		/// </summary>
		/// <param name="dv"></param>
		/// <param name="plotFormat"></param>
		/// <returns></returns>
		public static string PrettyPrint(double dv, string plotFormat)
		{
			var sfx = GetFormatSuffix(plotFormat);
			string rslt = string.Empty;
			var adv = Math.Abs(dv);
			if (plotFormat == "V" || plotFormat == "W")
			{
				{
					if (adv >= .01)
					{
						rslt = dv.ToString("0.###") + " " + sfx;
					}
					else if (adv >= 1e-5)
					{
						rslt = (1000 * dv).ToString("G3") + " m" + sfx;
					}
					else if (adv >= 1e-8)
					{
						rslt = (1000000 * dv).ToString("G3") + " u" + sfx;
					}
					else
					{
						rslt = (1e9 * dv).ToString("G3") + " n" + sfx;
					}
				}
			}
			else if(plotFormat == "%")
			{
				if (adv <= 1e-5)
				{
					rslt = dv.ToString("G3");
				}
				if (adv < 1.0)
				{
					rslt =  dv.ToString("0.######");
				}
				else if (adv < 10.0)
				{
					rslt =  dv.ToString("0.##");
				}
				else
				{
					rslt = Math.Round(dv).ToString();
				}
				rslt += " " + sfx;
			}
			else
			{
				// the rest of these are logarithmic and take no fancy formatting...
				if (adv < 1.0)
				{
					rslt = dv.ToString("0.###");
				}
				if (adv < 10.0)
				{
					rslt = dv.ToString("0.##");
				}
				else if (adv < 100.0)
				{
					rslt = dv.ToString("0.#");
				}
				else
				{
					rslt = dv.ToString("0.#");
				}
				rslt += " " + sfx;
			}
			return rslt;
		}

		/// <summary>
		/// get a pretty string value for this voltage
		/// </summary>
		/// <param name="plotFormat"></param>
		/// <param name="volts"></param>
		/// <param name="dRef"></param>
		/// <returns></returns>
		public static string DoValueFormat(string plotFormat, double volts, double dRef = 1.0)
		{
			var vfi = GetValueFormatter(plotFormat, dRef);
			return PrettyPrint(vfi(volts), plotFormat);
		}

		/// <summary>
		/// get a plot display format converter
		/// the linear formats (%,V,W) are converted to log10 for plotting
		/// </summary>
		/// <param name="plotFormat">the format</param>
		/// <param name="refX">the ref (max) value for scaling</param>
		/// <returns>a function to get plottable value</returns>
		public static Func<double, double> GetLogFormatter(string plotFormat, double refX = 1.0)
		{
			if( IsPlotFormatLog(plotFormat))
			{
				return GetValueFormatter(plotFormat, refX);
			}

			switch (plotFormat)
			{
				case "V":
					return (x => Math.Log10(x));
				case "%":
					return (x => Math.Log10(100 * x / refX));
				case "W":
					return (x => Math.Log10(x * x / ViewSettings.AmplifierLoad));
			}
			return (x => x); // default to volts
		}

		/// <summary>
		/// Given an input voltage format, get a display format converter
		/// </summary>
		/// <param name="plotFormat">the format</param>
		/// <param name="refX">the ref (max) value for percent and dBr</param>
		/// <returns>a Function(double) that does the display conversion</returns>
		public static Func<double,double> GetValueFormatter(string plotFormat, double refX = 1.0)
		{
			switch (plotFormat)
			{
				case "SPL":
					return (x => 20 * Math.Log10(x) );
				case "dBFS":    // the generator has 18dBV output, the input has 32dBV maximum
					return (x => 20 * Math.Log10(x) - 32);
				case "dBr":
					return (x => 20 * Math.Log10(x / refX));
				case "dBu":
					return (x => 20 * Math.Log10(x / 0.775));
				case "dBV":
					return (x => 20 * Math.Log10(x ));
				case "dBW":
					return (x => 10 * Math.Log10(x * x / ViewSettings.AmplifierLoad));
				case "V":
					return (x => x);
				case "%":
					return (x => 100 * x / refX);
				case "W":
					return (x => x * x / ViewSettings.AmplifierLoad);
			}
			return (x => x); // default to volts
		}

		/// <summary>
		/// Given an input voltage, convert to the desired data format for display
		/// </summary>
		/// <param name="plotFormat">the data format</param>
		/// <param name="volts"></param>
		/// <param name="dRef">reference value for percent and dbr</param>
		/// <returns>the converted double</returns>
		public static double ReformatValue(string plotFormat, double volts, double dRef = 1.0)
		{
			var vfi = GetValueFormatter(plotFormat, dRef);
			return vfi(volts);
		}

		/// <summary>
		/// Given an input voltage, convert to the desired data format for plotting
		/// </summary>
		/// <param name="plotFormat">the data format</param>
		/// <param name="volts"></param>
		/// <param name="dRef">reference value for percent and dbr</param>
		/// <returns>the converted double with logs of linear (%,V,W) formats</returns>
		public static double ReformatLogValue(string plotFormat, double volts, double dRef = 1.0)
		{
			var vfi = GetLogFormatter(plotFormat, dRef);
			return vfi(volts);
		}

		/// <summary>
		/// Given an input voltage format, get the display suffix
		/// </summary>
		/// <param name="format"></param>
		/// <returns>the format suffix</returns>
		public static string GetFormatSuffix(string plotFormat)
		{
			switch (plotFormat)
			{
				case "SPL":
					return "dB";
				case "dBFS":
				case "dBr":
				case "dBu":
				case "dBV":
				case "dBW":
				case "V":
				case "W":
					return plotFormat;
				case "%":
					return "%";
				case "Ohms":		// not available for user...
					return "Ohms";
			}
			return string.Empty; // default to none
		}

		/// <summary>
		/// Given an input voltage format, get the plot title 
		/// </summary>
		/// <param name="format"></param>
		/// <returns>the format suffix</returns>
		public static string GetFormatTitle(string plotFormat)
		{
			switch (plotFormat)
			{
				case "SPL":
				case "dBFS":
				case "dBr":
				case "dBu":
				case "dBV":
				case "dBW":
					return plotFormat;
				case "V":
					return "Volts";
				case "W":
					return "Watts";
				case "%":
					return "Percent";
			}
			return string.Empty; // default to none
		}

		public static RenderTargetBitmap? GetImage(Window view)
		{
			Size size = new Size(view.ActualWidth, view.ActualHeight);
			if (size.IsEmpty)
				return null;

			RenderTargetBitmap result = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);

			DrawingVisual drawingvisual = new DrawingVisual();
			using (DrawingContext context = drawingvisual.RenderOpen())
			{
				context.DrawRectangle(new VisualBrush(view), null, new Rect(new Point(), size));
				context.Close();
			}

			result.Render(drawingvisual);
			return result;
		}

		public static RenderTargetBitmap? CopyAsBitmap(FrameworkElement frameworkElement)
		{

			var targetWidth = (int)frameworkElement.ActualWidth;
			var targetHeight = (int)frameworkElement.ActualHeight;

			// Exit if there's no 'area' to render
			if (targetWidth == 0 || targetHeight == 0)
				return null;

			// Prepare the rendering target
			var result = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);

			// Render the framework element into the target
			result.Render(frameworkElement);

			return result;
		}

		public static byte[] EncodeBitmap(BitmapSource bitmapSource, BitmapEncoder bitmapEncoder)
		{

			// Create a 'frame' for the BitmapSource, then add it to the encoder
			var bitmapFrame = BitmapFrame.Create(bitmapSource);
			bitmapEncoder.Frames.Add(bitmapFrame);

			// Prepare a memory stream to receive the encoded data, then 'save' into it
			var memoryStream = new MemoryStream();
			bitmapEncoder.Save(memoryStream);

			// Return the results of the stream as a byte array
			return memoryStream.ToArray();
		}

		public static void SaveAsPng(RenderTargetBitmap src, Stream outputStream)
		{
			PngBitmapEncoder encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(src));

			encoder.Save(outputStream);
		}
	}
}
