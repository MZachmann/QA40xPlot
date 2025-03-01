using QA40xPlot.Actions;
using QA40xPlot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.ViewManagement;
using Xceed.Wpf.Toolkit;

namespace QA40xPlot
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
	public partial class PlotPage : UserControl
	{
		public PlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			this.DataContext = vm;
			vm.SetAction(this.WpfPlot1, this.WpfPlot1, this.WpfPlot1);
		}

		private void OnVoltageChanged(object sender, RoutedEventArgs e)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			var u = ((TextBox)sender).Text;
			vm.actThd.UpdateGenAmplitude(u);
		}

		private void OnAmpVoltageChanged(object sender, RoutedEventArgs e)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			var u = ((TextBox)sender).Text;
			vm.actThd.UpdateAmpAmplitude(u);
		}
	}
}
