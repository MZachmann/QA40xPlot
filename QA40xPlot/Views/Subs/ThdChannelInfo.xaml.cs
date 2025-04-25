using QA40xPlot.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Xml.Linq;

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
