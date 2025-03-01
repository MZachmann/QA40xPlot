using System.ComponentModel;
using System.Windows.Controls;
using ScottPlot.WPF;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for PlotControl.xaml
	/// </summary>
	public partial class PlotControl : UserControl
	{
		public PlotControl()
		{
			InitializeComponent();
			ThePlot = this.TheWpfPlot.Plot;
			_plot = this.TheWpfPlot;
		}

		public void Refresh()
		{ 
			Plot.Refresh();
		}


		public ScottPlot.Plot ThePlot { get; set; }
		private WpfPlot _plot;
		public WpfPlot Plot { get { return _plot; } set { _plot = value; ThePlot = value.Plot; } }
	}
}
