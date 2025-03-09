using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

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

	public interface GraphUtil
	{
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
