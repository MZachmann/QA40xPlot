using QA40xPlot.ViewModels;
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
		//	var vm = ViewModels.ViewSettings.Singleton.SpectrumVm;
		//	var u = ((TextBox)sender).Text;
		//	vm.actSpec.UpdateGenAmplitude(u);
		//}

		public FreqRespPlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.FreqRespVm;
			this.DataContext = vm;
			vm.SetAction(this.WpfPlot1, this.WpfPlot2, this.WpfPlot3);
			this.ZReference.DataContext = ViewSettings.Singleton.SettingsVm;
		}
	}
}
