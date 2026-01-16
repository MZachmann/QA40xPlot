using QA40xPlot.ViewModels;
using System.Windows;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ResidualsWnd.xaml
	/// </summary>
	public partial class ResidualsWnd : Window
	{
		public ResidualsWnd(ResidualViewModel vm)
		{
			InitializeComponent();
			DataContext = vm;
			vm.ThePlot = this.ResidualPlot;	// pass along the plot reference
		}
	}
}
