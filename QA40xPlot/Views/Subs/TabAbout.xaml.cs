using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for TabAbout.xaml
	/// </summary>
	public partial class TabAbout : UserControl
	{
		[JsonIgnore]
		private MovableWnd _MovableWnd = new MovableWnd();

		public TabAbout()
		{
			InitializeComponent();
		}

		public void SetDataContext(DataDescript dataDef)
		{
			this.DataContext = dataDef;
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
