using Newtonsoft.Json;
using QA40xPlot.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ImdChannelInfo.xaml
	/// </summary>
	public partial class ImdChannelInfo : UserControl
	{
		[JsonIgnore]
		private MovableWnd _MovableWnd = new MovableWnd();

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
