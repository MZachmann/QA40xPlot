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

		internal MarkerItem()
		{
			ThePattern = ScottPlot.LinePattern.Solid;
			TheColor = ScottPlot.Colors.Black;
			Label = string.Empty;
			ColorIdx = 0;
		}

		internal MarkerItem(ScottPlot.LinePattern pattern, ScottPlot.Color color, string label, int idx)
		{
			ThePattern = pattern;
			TheColor = color;
			Label = label;
			ColorIdx = idx;
		}
	}
}
