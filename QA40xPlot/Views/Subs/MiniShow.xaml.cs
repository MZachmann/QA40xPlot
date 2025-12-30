using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for TabAbout.xaml
	/// </summary>
	public partial class MiniShow : UserControl
	{
		private MovableWnd _MovableWnd = new MovableWnd();

		public MiniShow()
		{
			InitializeComponent();
		}

		public void SetDataContext(DataDescript dataDef)
		{
			this.DataContext = dataDef;
			// turn off subheads
		}

		private void Minishow_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			this.WpfPlot2.MyLabel.Visibility = System.Windows.Visibility.Collapsed;
			this.WpfPlot3.MyLabel.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void OnWindMouseDown(object sender, MouseButtonEventArgs e)
		{
			_MovableWnd.OnWindMouseDown(sender, e);
		}

		private void OnWindMouseUp(object sender, MouseButtonEventArgs e)
		{
			_MovableWnd.OnWindMouseUp(sender, e);
		}

		private void OnWindMouseMove(object sender, MouseEventArgs e)
		{
			_MovableWnd.OnWindMouseMove(sender, e);
		}


	}
}
