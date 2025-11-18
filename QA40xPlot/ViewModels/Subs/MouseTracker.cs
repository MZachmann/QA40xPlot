using System.Windows.Input;

namespace QA40xPlot.ViewModels
{
	public interface IMouseTracker
	{
		//
		// Summary:
		//     For tracking a mouse event in a plot
		event MouseEventHandler? MouseTracked;
	}
}
