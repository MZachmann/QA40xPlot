using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class FreqRespPlotPage : UserControl
	{

		//private void OnVoltageChanged(object sender, RoutedEventArgs e)
		//{
		//	var vm = ViewModels.ViewSettings.Singleton.FreqRespVm;
		//	var u = ((TextBox)sender).Text;
		//	vm.actSpec.UpdateGenAmplitude(u);
		//}

		public FreqRespPlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.FreqRespVm;
			this.DataContext = vm;
			LegendWindow.SetDataContext(vm);
			vm.SetAction(this.WpfPlot1, this.MiniShow.WpfPlot2, this.MiniShow.WpfPlot3, this.TAbout);
		}
	}
}
