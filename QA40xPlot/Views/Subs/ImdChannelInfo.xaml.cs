using QA40xPlot.ViewModels;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ImdChannelInfo.xaml
	/// </summary>
	public partial class ImdChannelInfo : UserControl
	{
		public ImdChannelInfo()
		{
			InitializeComponent();
			SetDataContext(true);
		}

		public void SetDataContext(bool Leftright)
		{
			var vm = Leftright ? ViewSettings.Singleton.ImdChannelLeft : ViewSettings.Singleton.ImdChannelRight;
			this.DataContext = vm;
		}
	}
}
