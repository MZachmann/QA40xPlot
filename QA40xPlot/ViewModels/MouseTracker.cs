using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
