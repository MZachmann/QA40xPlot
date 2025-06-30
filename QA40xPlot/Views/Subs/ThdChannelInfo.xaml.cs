using QA40xPlot.ViewModels;
using QA40xPlot.ViewModels.Subs;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ThdChannelInfo.xaml
	/// </summary>
	public partial class ThdChannelInfo : UserControl
	{
		private MovableWnd _MovableWnd = new MovableWnd();

		public ThdChannelInfo()
		{
			InitializeComponent();
		}

		public void SetDataContext(ThdChannelViewModel vm)
		{
			this.DataContext = vm;
		}

		private void OnWindMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			_MovableWnd.OnWindMouseDown(sender, e);
		}

		private void OnWindMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			_MovableWnd.OnWindMouseUp(sender, e);
		}

		private void OnWindMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			_MovableWnd.OnWindMouseMove(sender, e);
		}
	}
}
