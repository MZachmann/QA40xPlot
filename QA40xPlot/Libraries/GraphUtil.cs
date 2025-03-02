using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QA40xPlot.Libraries
{
	public enum PixelSizes
	{
		TITLE_SIZE = 14,
		SUBTITLE_SIZE = 12,
		AXIS_SIZE = 9,
		LABEL_SIZE = 9,
		LEGEND_SIZE = 11,
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
	}
}
