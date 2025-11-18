using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for SettingsPage.xaml
	/// </summary>
	public partial class SettingsPage : UserControl
	{
		public SettingsPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.SettingsVm;
			this.DataContext = vm;
		}
	}
}
