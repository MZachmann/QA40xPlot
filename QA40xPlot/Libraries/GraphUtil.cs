using QA40xPlot.ViewModels;
using QA40xPlot.ViewModels.Subs;
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

	// use the last four palette colors
	// leaving space for a second channel at 17,19
	public enum StockColors
	{
		HARMONICS = 16,
		POWER = 18
	}

	public static class GraphUtil
	{
		private static ThePalette _PlotPalette = new ThePalette();
		//new ScottPlot.Palettes.Tsitsulin();

		public static ThePalette PlotPalette { get => _PlotPalette; }

		public static int PtToPixels(PixelSizes fontsize)
		{
			var vm = ViewModels.ViewSettings.Singleton.MainVm;
			return (int)((int)fontsize * vm.ScreenDpi / 72);
		}
		public static int PtToPixels(int fontsize)
		{
			var vm = ViewModels.ViewSettings.Singleton.MainVm;
			return (int)(fontsize * vm.ScreenDpi / 72);
		}

		public static ScottPlot.Color GetPaletteColor(string? colorName, int iIndex)
		{
			if (colorName != null && "Transparent" != colorName && colorName.Length > 0)
			{
				return PlotUtil.StrToColor(colorName);
			}
			var c = PlotPalette.GetColor(iIndex);   // these may be translucent
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
					if (adv >= .09)
					{
						rslt = dv.ToString("0.###") + " " + sfx;
					}
					else if (adv >= 9e-5)
					{
						rslt = (1000 * dv).ToString("G3") + " m" + sfx;
					}
					else if (adv >= 9e-8)
					{
						rslt = (1000000 * dv).ToString("G3") + " μ" + sfx;
					}
					else if (adv > 0)
					{
						rslt = (1e9 * dv).ToString("G3") + " n" + sfx;
					}
					else
					{
						rslt = "0";
					}
				}
			}
			else if (plotFormat == "%")
			{
				if (adv <= 1e-5)
				{
					rslt = dv.ToString("G3");
				}
				if (adv < 1.0)
				{
					rslt = dv.ToString("0.######");
				}
				else if (adv < 10.0)
				{
					rslt = dv.ToString("0.##");
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
				if (adv < 100.0)
				{
					rslt = dv.ToString("0.##");
				}
				//else if (adv < 100.0)
				//{
				//	rslt = dv.ToString("0.#");
				//}
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
		public static string PrettyPlotValue(string PlotFormat, double volts, double dRef = 1.0)
		{
			var vfi = ValueToPlotFn(PlotFormat, dRef);
			return PrettyPrint(vfi(volts), PlotFormat);
		}

		/// <summary>
		/// get a pretty string value for this voltage
		/// </summary>
		/// <param name="plotFormat"></param>
		/// <param name="volts"></param>
		/// <param name="dRef"></param>
		/// <returns></returns>
		public static string PrettyPlotValue(BaseViewModel bvm, double volts, double[] dRef)
		{
			var vfi = ValueToPlotFn(bvm, dRef);
			return PrettyPrint(vfi(volts), bvm.PlotFormat);
		}

		/// <summary>
		/// get a plot display format converter
		/// the linear formats (%,V,W) are converted to log10 for plotting
		/// </summary>
		/// <param name="plotFormat">the format</param>
		/// <param name="refX">the ref (max) value for scaling</param>
		/// <returns>a function to get plottable value</returns>
		public static Func<double, double> ValueToLogPlotFn(BaseViewModel bvm, double[] refX, double dF1 = 0.0)
		{
			var vfi = ValueToPlotFn(bvm, refX, dF1);
			if (IsPlotFormatLog(bvm.PlotFormat))
			{
				return vfi;
			}
			return (x => Math.Log10(vfi(x)));
		}

		/// <summary>
		/// get a plot display format converter
		/// the linear formats (%,V,W) are converted to log10 for plotting
		/// </summary>
		/// <param name="plotFormat">the format</param>
		/// <param name="refX">the ref (max) value for scaling</param>
		/// <returns>a function to get plottable value</returns>
		public static Func<double, double> ValueToLogPlotFn(BaseViewModel bvm, double refX)
		{
			var vfi = ValueToPlotFn(bvm.PlotFormat, refX);
			if (IsPlotFormatLog(bvm.PlotFormat))
			{
				return vfi;
			}
			return (x => Math.Log10(vfi(x)));
		}

		// this just avoids taking a max of the data constantly if not in dbr
		public static Func<double, double> ValueToPlotFn(BaseViewModel bvm, double[] refX, double[] refFreq)
		{
			var plotFormat = bvm.PlotFormat;
			if (refX == null || refX.Length == 0 || (plotFormat != "dBr" && plotFormat != "%"))
			{
				return ValueToPlotFn(plotFormat, 1.0);
			}
			var ax = GetDbrReference(bvm, refX, refFreq);
			return ValueToPlotFn(plotFormat, ax);
		}

		// this just avoids taking a max of the data constantly if not in dbr
		public static Func<double, double> ValueToPlotFn(BaseViewModel bvm, double[] refX, double dF1 = 0.0)
		{
			var plotFormat = bvm.PlotFormat;
			if (refX == null || refX.Length == 0 || (plotFormat != "dBr" && plotFormat != "%"))
			{
				return ValueToPlotFn(plotFormat, 1.0);
			}
			var ax = GetDbrReference(bvm, refX, dF1);
			return ValueToPlotFn(plotFormat, ax);
		}

		public static double GetDbrReference(BaseViewModel bvm, double[] refX, double[] refFreq)
		{
			if (refX == null || refX.Length == 0 || (bvm.PlotFormat != "dBr" && bvm.PlotFormat != "%"))
			{
				return 1.0;
			}
			var toskip = (refX.Length > 1) ? 1 : 0;
			if (bvm.PlotFormat != "dBr")
				return refX.Skip(toskip).Max();
			var idx = BaseViewModel.DbrTypes.IndexOf(bvm.DbrType);
			switch (idx)
			{
				case 0:     // max
					return refX.Skip(toskip).Max();
				case 1:     // @ frequency
					{
						// find the nearest frequency bin
						var dfrq = MathUtil.ToDouble(bvm.DbrValue, 1000.0);
						var frqIndex = 0;
						while (frqIndex < (refFreq.Length - 1) && refFreq[frqIndex] < dfrq)
						{
							frqIndex++;
						}
						if ((frqIndex > 0) && ((dfrq - refFreq[frqIndex - 1]) < (refFreq[frqIndex] - dfrq)))
						{
							frqIndex -= 1;  // closer to the prior bin
						}
						var mga = refX[frqIndex];
						return mga;
					}
				case 2:     // value - use negative since we divide by it
					return QaLibrary.ConvertVoltage(-MathUtil.ToDouble(bvm.DbrValue, 0), Data.E_VoltageUnit.dBV, Data.E_VoltageUnit.Volt);
			}
			return 1.0;
		}

		public static double GetDbrReference(BaseViewModel bvm, double[] refX, double dF1 = 0.0)
		{
			if (refX == null || refX.Length == 0 || (bvm.PlotFormat != "dBr" && bvm.PlotFormat != "%"))
			{
				return 1.0;
			}
			var toskip = (refX.Length > 1) ? 1 : 0;
			if (bvm.PlotFormat != "dBr")
				return refX.Skip(toskip).Max();
			var idx = BaseViewModel.DbrTypes.IndexOf(bvm.DbrType);
			switch (idx)
			{
				case 0:     // max
					return refX.Skip(toskip).Max();
				case 1:     // frequency
					{
						var df = QaLibrary.CalcBinSize(bvm.SampleRateVal, bvm.FftSizeVal);
						var dfrq = MathUtil.ToDouble(bvm.DbrValue, 1000.0) - df * dF1;  // adjust for bin offset
						dfrq = Math.Max(0.0, dfrq);
						var mga = QaMath.MagAtFreq(refX, df, dfrq);
						return mga;
					}
				case 2:     // value - use negative since we divide by it
					return QaLibrary.ConvertVoltage(-MathUtil.ToDouble(bvm.DbrValue, 0), Data.E_VoltageUnit.dBV, Data.E_VoltageUnit.Volt);
			}
			return 1.0;
		}

		/// <summary>
		/// Given an input voltage format and reference value
		/// Return a function to convert data values to ones suitable for the Y axis
		/// </summary>
		/// <param name="plotFormat">the Y axis format</param>
		/// <param name="refX">the ref value for percent (=max) and dBr(variable)</param>
		/// <returns>a Function(double) that does the conversion</returns>
		public static Func<double, double> ValueToPlotFn(string plotFormat, double refX = 1.0)
		{
			switch (plotFormat)
			{
				case "SPL":
					return (x => 20 * Math.Log10(x));
				case "dBFS":    // the generator has 18dBV output, the input has 32dBV maximum
					return (x => 20 * Math.Log10(x) - 32);
				case "dBr":
					return (x => 20 * Math.Log10(x / refX));
				case "dBu":
					return (x => 20 * Math.Log10(x / 0.775));
				case "dBV":
					return (x => 20 * Math.Log10(x));
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
		/// <param name="plotFormat">the Y axis format</param>
		/// <param name="volts">input value</param>
		/// <param name="dRef">reference value for percent and dbr</param>
		/// <returns>the converted double</returns>
		public static double ValueToPlot(string plotFormat, double volts, double dRef = 1.0)
		{
			var vfi = ValueToPlotFn(plotFormat, dRef);
			return vfi(volts);
		}

		/// <summary>
		/// Given an input voltage, convert to the desired data format for display
		/// </summary>
		/// <param name="plotFormat">the data format</param>
		/// <param name="volts"></param>
		/// <param name="dRef">reference value for percent and dbr</param>
		/// <returns>the converted double</returns>
		public static double ValueToPlot(BaseViewModel bvm, double volts, double[] refX)
		{
			var vfi = ValueToPlotFn(bvm, refX);
			return vfi(volts);
		}

		/// <summary>
		/// Given an input voltage, convert to the desired data format for plotting
		/// </summary>
		/// <param name="plotFormat">the data format</param>
		/// <param name="volts"></param>
		/// <param name="dRef">reference value for percent and dbr</param>
		/// <returns>the converted double with logs of linear (%,V,W) formats</returns>
		public static double ValueToLogPlot(BaseViewModel bvm, double volts, double dRef = 1.0)
		{
			var vfi = ValueToLogPlotFn(bvm, dRef);
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
				case "dB":
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
				case "Ohms":        // not available for user...
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
