using QA40xPlot.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
			SetDataContext(true);
		}

		public void SetDataContext(bool Leftright)
		{
			ThdChannelViewModel vm = Leftright ? ViewSettings.Singleton.ChannelLeft : ViewSettings.Singleton.ChannelRight;
			this.DataContext = vm;
		}
	}
}
