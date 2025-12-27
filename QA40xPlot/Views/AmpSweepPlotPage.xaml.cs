using QA40xPlot.ViewModels;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for Opamp Test : amplitude sweep page
	/// </summary>
	public partial class AmpSweepPlotPage : UserControl
	{

		public AmpSweepPlotPage()
		{
			InitializeComponent();
			var vm = ViewSettings.Singleton.AmpVm;
			this.DataContext = vm;
			LegendWindow.SetDataContext(vm);
			vm.SetAction(this.WpfPlot1, this.MiniShow.WpfPlot2, this.MiniShow.WpfPlot3, this.TAbout);
		}
	}
}
