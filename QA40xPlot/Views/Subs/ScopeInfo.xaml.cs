using Newtonsoft.Json;
using QA40xPlot.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.Views
{

	/// <summary>
	/// Interaction logic for ScopeInfo.xaml
	/// </summary>
	public partial class ScopeInfo : UserControl
	{
		[JsonIgnore]
		private MovableWnd _MovableWnd = new MovableWnd();

		public ScopeInfo()
		{
			InitializeComponent();
		}

		public void SetDataContext(ScopeInfoViewModel mdl)
		{
			this.DataContext = mdl;
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
