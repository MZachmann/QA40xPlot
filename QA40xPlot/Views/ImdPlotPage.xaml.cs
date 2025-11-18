using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class ImdPlotPage : UserControl
	{
		public ImdPlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.ImdVm;
			this.DataContext = vm;
			LegendWindow.SetDataContext(vm);
			vm.SetAction(this.WpfPlot1, this.Info1, this.Info2, this.TAbout);
		}

		public void OnChangedIntermod(object sender, EventArgs e)
		{
			// update the values
			var vm = ViewModels.ViewSettings.Singleton.ImdVm;
			vm.SetImType();
		}
	}
}
