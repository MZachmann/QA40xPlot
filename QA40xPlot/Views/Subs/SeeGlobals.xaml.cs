using QA40xPlot.ViewModels;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ShowGlobals.xaml
	/// </summary>
	public partial class SeeGlobals : UserControl
	{
		public SeeGlobals()
		{
			InitializeComponent();
			this.EGain.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.AmpLoad.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
		}
	}
}
