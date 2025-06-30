using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.ViewModels.Subs
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
				var mgn = u.Margin;
				mgn.Left += pos.X - _MouseX;
				mgn.Top += pos.Y - _MouseY;
				u.Margin = mgn;
			}
		}
	}
}
