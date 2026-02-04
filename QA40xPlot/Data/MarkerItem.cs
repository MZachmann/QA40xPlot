using System.Collections.ObjectModel;

namespace QA40xPlot.Data
{
	// to support the internal LegendWnd legend window
	// we have a list of these MarkerItem objects
	internal class MarkerList : ObservableCollection<MarkerItem>
	{
		internal MarkerList() : base()
		{
		}
	}

	internal class MarkerItem
	{
		internal ScottPlot.LinePattern ThePattern { get; set; }
		internal ScottPlot.Color TheColor { get; set; }
		internal string Label { get; set; }
		internal int ColorIdx { get; set; }
		internal bool IsShown { get; set; }
		internal ScottPlot.Plottables.SignalXY? Signal { get; set; }
		internal Views.PlotControl? ThePlot { get; set; }

		internal MarkerItem()
		{
			ThePattern = ScottPlot.LinePattern.Solid;
			TheColor = ScottPlot.Colors.Black;
			Label = string.Empty;
			ColorIdx = 0;
			IsShown = true;
			Signal = null;
			ThePlot = null;
		}

		internal MarkerItem(ScottPlot.LinePattern pattern, ScottPlot.Color color, string label, int idx, ScottPlot.Plottables.SignalXY? signal = null, Views.PlotControl? thePlot = null, bool doShown = true)
		{
			ThePattern = pattern;
			TheColor = color;
			Label = label;
			ColorIdx = idx;
			IsShown = doShown;
			Signal = signal;
			ThePlot = thePlot;
		}
	}
}
