using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	internal class MovableWnd
	{
		private double _MouseX = 0;
		private double _MouseY = 0;
		public MovableWnd()
		{
		}

		public void OnWindMouseDown(object sender, MouseButtonEventArgs e)
		{
			// check
			var u = sender as UIElement;
			u?.CaptureMouse();
			var pos = e.GetPosition(u);
			_MouseX = pos.X;
			_MouseY = pos.Y;
		}

		public void OnWindMouseUp(object sender, MouseButtonEventArgs e)
		{
			var u = sender as UIElement;
			if (u != null && u.IsMouseCaptured)
				u?.ReleaseMouseCapture();
		}

		public void OnWindMouseMove(object sender, MouseEventArgs e)
		{
			var u = sender as UserControl;
			if (u != null && u.IsMouseCaptured)
			{
				var pos = e.GetPosition(u);
				// Do something with pos if needed
				var halign = u.HorizontalAlignment;
				var valign = u.VerticalAlignment;
				var mgn = u.Margin;
				if (halign == HorizontalAlignment.Left)
					mgn.Left += pos.X - _MouseX;
				else
					mgn.Right -= pos.X - _MouseX;
				if (valign == VerticalAlignment.Top)
					mgn.Top += pos.Y - _MouseY;
				else
					mgn.Bottom -= pos.Y - _MouseY;
				// once we start moving it, reset right and bottom to 0
				u.Margin = mgn;
			}
		}
	}
}
