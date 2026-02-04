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

		public bool AllowCopy
		{
			get;
			set;
		} = true;

		public TabAbout()
		{
			InitializeComponent();
		}

		public TabAbout(bool Allow)
		{
			AllowCopy = Allow;
			InitializeComponent();
		}

		public void SetDataContext(DataDescript dataDef)
		{
			this.DataContext = dataDef;
		}

		private void OnWindMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (AllowCopy)
				_MovableWnd.OnWindMouseDown(sender, e);
		}

		private void OnWindMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (AllowCopy)
				_MovableWnd.OnWindMouseUp(sender, e);
		}

		private void OnWindMouseMove(object sender, MouseEventArgs e)
		{
			if (AllowCopy)
				_MovableWnd.OnWindMouseMove(sender, e);
		}

	}
}
