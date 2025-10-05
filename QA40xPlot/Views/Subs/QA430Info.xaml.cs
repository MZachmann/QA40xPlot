using QA40xPlot.QA430;
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
	/// Interaction logic for QA430Info.xaml
	/// </summary>
	public partial class QA430Info : Window
	{
		internal bool AllowClose { get; set; } = false;

		public QA430Info()
		{
			InitializeComponent();
			SetDataContext();
		}

		internal void SetDataContext()
		{
			this.DataContext = Qa430Usb.Singleton.QAModel;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);
			if (!AllowClose)//NOT a user close request? ... then hide
			{
				e.Cancel = true;
				Console.Beep();
				//this.Hide();
			}
		}
	}
}
