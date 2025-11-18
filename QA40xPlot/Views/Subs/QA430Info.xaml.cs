using QA40xPlot.QA430;
using System.Windows;

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
				//Console.Beep();
				this.Hide();
			}
		}

		private void OnSelChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			// The event was raised by the user
			QA430Model qam = (QA430Model)DataContext;
			qam.OpampConfigOption = (short)QA430Model.OpampConfigOptions.Custom;
		}
	}
}
