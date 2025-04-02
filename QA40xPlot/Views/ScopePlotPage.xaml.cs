using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class ScopePlotPage : UserControl
	{
		public ScopePlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.ScopeVm;
			this.DataContext = vm;
			vm.SetAction(this.WpfPlot1, this.Info1);
		}
	}
}
