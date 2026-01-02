using QA40xPlot.ViewModels;
using System.Windows.Controls;
using Windows.UI.ViewManagement;

namespace QA40xPlot.Views
{
	public class ColorUtil
	{
		// From https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes?WT.mc_id=DT-MVP-5003978#know-when-dark-mode-is-enabled
		private bool CheckColorTheme()
		{
			var settings = new UISettings();
			var clr = settings.GetColorValue(UIColorType.Background);
			var isLight = IsColorLight(clr);
			return isLight;
		}

		private static bool IsColorLight(Windows.UI.Color clr)
		{
			return ((5 * clr.G) + (2 * clr.R) + clr.B) > (8 * 128);
		}

	}

	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class ThdFreqPlotPage : UserControl
	{
		public ThdFreqPlotPage()
		{
			InitializeComponent();
			//var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			//DataContext = vm;
			//LegendWindow.SetDataContext(vm);
			//vm.SetAction(WpfPlot1, MiniShow.WpfPlot2, MiniShow.WpfPlot3, TAbout);
			//AmpLoad.DataContext = ViewSettings.Singleton.SettingsVm;
		}

	}
}
