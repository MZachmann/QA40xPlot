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

		public void SetDataContext(List<OtherSet> dataDef)
		{
			this.DataContext = dataDef;
		}

		private void OnClick(object sender, RoutedEventArgs e)
		{
			var u = this.DataContext;
			if( u != null)
			{
				var btu = sender as Button;
				if(btu != null)
				{
					var ids = btu.CommandParameter.ToString() ?? "";
					((BaseViewModel)u).DoDeleteIt(ids);
				}
			}
        }

		private void ForceRepaint(object sender, RoutedEventArgs e)
		{
			var u = this.DataContext;
			if (u != null)
			{
				((BaseViewModel)u).ForceGraphUpdate();
			}
		}
	}
}
