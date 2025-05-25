using System.Windows.Controls;
using System.Windows.Media;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class SpectrumPlotPage : UserControl
	{
		public SpectrumPlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.SpectrumVm;
			this.DataContext = vm;
			vm.SetAction(this, this.WpfPlot1, this.Info1, this.Info2, this.TAbout);
		}
	}
}
