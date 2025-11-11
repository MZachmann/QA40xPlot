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
			var vm = ViewSettings.Singleton.AmpSweepVm;
			this.DataContext = vm;
			LegendWindow.SetDataContext(vm);
			vm.SetAction(this.WpfPlot1, this.MiniShow.WpfPlot2, this.MiniShow.WpfPlot3, this.TAbout);
		}

		private void GainButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			GainPopup.IsOpen = !GainPopup.IsOpen;
		}

		private void LoadButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			LoadPopup.IsOpen = !LoadPopup.IsOpen;
		}

		private void GainPopup_Closed(object sender, EventArgs e)
		{
			AmpSweepViewModel.UpdateGain();
		}

		private void LoadPopup_Closed(object sender, EventArgs e)
		{
			AmpSweepViewModel.UpdateLoad();
		}

		private void VoltPopup_Closed(object sender, EventArgs e)
		{
			AmpSweepViewModel.UpdateVoltages();
		}
	}
}
