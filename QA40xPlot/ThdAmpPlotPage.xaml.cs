using QA40xPlot.Actions;
using QA40xPlot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.ViewManagement;
using Xceed.Wpf.Toolkit;

namespace QA40xPlot
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class ThdAmpPlotPage : UserControl
	{

		public void DoStart(object sender, RoutedEventArgs e)
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			vm.actThd.StartMeasurement();
		}

		public void DoStop(object sender, RoutedEventArgs e)
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.ThdFreq;
			//vm.actThd.Start();
		}

		public ThdAmpPlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			this.DataContext = vm;
			vm.SetAction(this.WpfPlot1, this.WpfPlot2, this.WpfPlot3);
		}

		private void OnStartVoltageChanged(object sender, RoutedEventArgs e)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			var u = ((TextBox)sender).Text;
			vm.actThd.UpdateStartAmplitude(u);
		}

		private void OnEndVoltageChanged(object sender, RoutedEventArgs e)
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			var u = ((TextBox)sender).Text;
			vm.actThd.UpdateEndAmplitude(u);
		}

	}
}
