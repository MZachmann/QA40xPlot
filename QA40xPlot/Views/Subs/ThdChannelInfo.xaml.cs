using QA40xPlot.ViewModels;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ThdChannelInfo.xaml
	/// </summary>
	public partial class ThdChannelInfo : UserControl
	{
		public ThdChannelInfo()
		{
			InitializeComponent();
		}

		public void SetDataContext(ThdChannelViewModel vm)
		{
			this.DataContext = vm;
		}

	}
}
