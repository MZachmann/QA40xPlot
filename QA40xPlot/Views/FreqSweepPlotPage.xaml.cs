using QA40xPlot.ViewModels;
using System.Windows.Controls;
using Windows.UI.ViewManagement;

namespace QA40xPlot.Views
{
	//public class ColorUtil
	//{
	//	// From https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes?WT.mc_id=DT-MVP-5003978#know-when-dark-mode-is-enabled
	//	private bool CheckColorTheme()
	//	{
	//		var settings = new UISettings();
	//		var clr = settings.GetColorValue(UIColorType.Background);
	//		var isLight = IsColorLight(clr);
	//		return isLight;
	//	}

	//	private static bool IsColorLight(Windows.UI.Color clr)
	//	{
	//		return ((5 * clr.G) + (2 * clr.R) + clr.B) > (8 * 128);
	//	}

	//}

	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class FreqSweepPlotPage : UserControl
	{
		public FreqSweepPlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.FreqVm;
			this.DataContext = vm;
			vm.SetAction(this.WpfPlot1, this.MiniShow.WpfPlot2, this.MiniShow.WpfPlot3, this.TAbout);
			this.AmpLoad.DataContext = ViewSettings.Singleton.SettingsVm;
		}

		private void GainButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			GainPopup.IsOpen = !GainPopup.IsOpen;
		}

		private void LoadButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			LoadPopup.IsOpen = !LoadPopup.IsOpen;

		}

		private void GainPopup_Closed(object sender, EventArgs e)
		{
			FreqSweepViewModel.UpdateGain();
		}

		private void LoadPopup_Closed(object sender, EventArgs e)
		{
			FreqSweepViewModel.UpdateLoad();
		}
	}
}
