using QA40xPlot.ViewModels;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ScopeInfo.xaml
	/// </summary>
	public partial class ScopeInfo : UserControl
	{
		public ScopeInfo()
		{
			InitializeComponent();
		}

		public void SetDataContext(ScopeInfoViewModel mdl)
		{
			this.DataContext = mdl;
		}
	}
}
