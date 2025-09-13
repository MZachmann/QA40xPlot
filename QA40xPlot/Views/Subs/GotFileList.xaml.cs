using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class GotFileList : UserControl
	{
		public GotFileList()
		{
			InitializeComponent();
		}

		private void OnClick(object sender, RoutedEventArgs e)
		{
			var btu = sender as Button;
			if (btu != null)
			{
				var ids = btu.CommandParameter.ToString() ?? "";
				var myVm = ViewSettings.Singleton.MainVm.CurrentView;
				myVm?.DoDeleteIt(ids);
			}
		}

		private void ForceRepaint(object sender, RoutedEventArgs e)
		{
			var u = this.DataContext as BaseViewModel;
			var btu = sender as CheckBox;
			if (btu != null && u != null)
			{
				//var ids = btu.CommandParameter.ToString() ?? "";
				u.ForceGraphDraw();
			}
		}
	}
}
