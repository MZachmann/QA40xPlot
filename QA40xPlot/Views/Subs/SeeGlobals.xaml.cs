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
			this.EGain.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.EGain2.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.EGain3.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.GGain.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.GGain2.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.GGain3.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.Noises.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.Noises2.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.Noiseband.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.Noiseband2.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.Noiseband3.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.SoundDevice.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
			this.SoundDevice2.DataContext = ViewSettings.Singleton.SettingsVm; // synch just this control
		}
	}
}
