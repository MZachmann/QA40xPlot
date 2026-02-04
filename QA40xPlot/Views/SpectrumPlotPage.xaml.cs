using System.Windows.Controls;

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
			LegendWindow.SetDataContext(vm);
			vm.SetAction(this, this.WpfPlot1, this.Info1, this.Info2, this.Info3, this.Info4, this.TAbout);
		}
	}
}
