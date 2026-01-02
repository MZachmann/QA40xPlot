using QA40xPlot.ViewModels;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class ThdAmpPlotPage : UserControl
	{

		public ThdAmpPlotPage()
		{
			InitializeComponent();
			//var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			//this.DataContext = vm;
			//LegendWindow.SetDataContext(vm);
			//vm.SetAction(this.WpfPlot1, this.MiniShow.WpfPlot2, this.MiniShow.WpfPlot3, this.TAbout);
			//this.AmpLoad.DataContext = ViewSettings.Singleton.SettingsVm;

		}
	}
}
