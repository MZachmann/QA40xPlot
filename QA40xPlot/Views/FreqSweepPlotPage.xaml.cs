using QA40xPlot.ViewModels;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for opamp test : frequency sweep page
	/// </summary>
	public partial class FreqSweepPlotPage : UserControl
	{
		public FreqSweepPlotPage()
		{
			InitializeComponent();
			var vm = ViewSettings.Singleton.FreqVm;
			DataContext = vm;
			LegendWindow.SetDataContext(vm);
			vm.SetAction(this.WpfPlot1, this.MiniShow.WpfPlot2, this.MiniShow.WpfPlot3, this.TAbout);
		}

		private void GainPopup_Closed(object sender, EventArgs e)
		{
			FreqSweepViewModel.UpdateGain();
		}

		private void LoadPopup_Closed(object sender, EventArgs e)
		{
			FreqSweepViewModel.UpdateLoad();
		}

		private void VoltPopup_Closed(object sender, EventArgs e)
		{
			FreqSweepViewModel.UpdateVoltages();
		}

		private void SupplyPopup_Closed(object sender, EventArgs e)
		{
			FreqSweepViewModel.UpdateSupplies();
		}

    }
}
