using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Actions
{
	public class ActBase
	{
		public void SetupLegend(ScottPlot.Plot myPlot)
		{
			// Set up the legend
			myPlot.Legend.IsVisible = true;
			myPlot.Legend.Alignment = ScottPlot.Alignment.UpperRight;
			myPlot.Legend.Orientation = ScottPlot.Orientation.Vertical;
			myPlot.Legend.FontSize = GraphUtil.PtToPixels(PixelSizes.LEGEND_SIZE);
			myPlot.ShowLegend();
		}

		private void SetupMagTics(ScottPlot.Plot myPlot)
		{
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);

			ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
			tickGenY.TargetTickCount = 15;
			tickGenY.MinorTickGenerator = minorTickGen;

			// tell the left axis to use our custom tick generator
			myPlot.Axes.Left.TickGenerator = tickGenY;
		}

		void SetupPercentTics(ScottPlot.Plot myPlot)
		{
			ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenY = new();
			minorTickGenY.Divisions = 10;

			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
			tickGenY.MinorTickGenerator = minorTickGenY;

			// create a custom tick formatter to set the label text for each tick
			static string LogTickLabelFormatter(double y) => $"{Math.Pow(10, Math.Round(y, 10)):#0.######}";

			// tell our major tick generator to only show major ticks that are whole integers
			tickGenY.IntegerTicksOnly = true;

			// tell our custom tick generator to use our new label formatter
			tickGenY.LabelFormatter = LogTickLabelFormatter;

			// tell the left axis to use our custom tick generator
			myPlot.Axes.Left.TickGenerator = tickGenY;

			// ******* y-ticks ****
			// create a minor tick generator that places log-distributed minor ticks
			ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();
			minorTickGen.Divisions = 10;
		}

		private void SetupFreqTics(ScottPlot.Plot myPlot)
		{
			// create a minor tick generator that places log-distributed minor ticks
			ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenX = new();

			// create a numeric tick generator that uses our custom minor tick generator
			//ScottPlot.TickGenerators.NumericAutomatic tickGenX = new();
			//tickGenX.MinorTickGenerator = minorTickGenX;

			// create a manual tick generator and add ticks
			ScottPlot.TickGenerators.NumericManual tickGenX = new();

			// add major ticks with their labels
			tickGenX.AddMajor(Math.Log10(1), "1");
			tickGenX.AddMajor(Math.Log10(2), "2");
			tickGenX.AddMajor(Math.Log10(5), "5");
			tickGenX.AddMajor(Math.Log10(10), "10");
			tickGenX.AddMajor(Math.Log10(20), "20");
			tickGenX.AddMajor(Math.Log10(50), "50");
			tickGenX.AddMajor(Math.Log10(100), "100");
			tickGenX.AddMajor(Math.Log10(200), "200");
			tickGenX.AddMajor(Math.Log10(500), "500");
			tickGenX.AddMajor(Math.Log10(1000), "1k");
			tickGenX.AddMajor(Math.Log10(2000), "2k");
			tickGenX.AddMajor(Math.Log10(5000), "5k");
			tickGenX.AddMajor(Math.Log10(10000), "10k");
			tickGenX.AddMajor(Math.Log10(20000), "20k");
			tickGenX.AddMajor(Math.Log10(50000), "50k");
			tickGenX.AddMajor(Math.Log10(100000), "100k");

			myPlot.Axes.Bottom.TickGenerator = tickGenX;
		}

		// this sets the axes bounds for freq vs percent
		private void AddPctFreqRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), Math.Log10(0.00000001), Math.Log10(100))
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs magnitude
		private void AddMagFreqRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// generic initialization of a plot basics
		private void InitializeAPlot(ScottPlot.Plot myPlot)
		{
			myPlot.Clear();

			// show grid lines for major ticks
			myPlot.Grid.MajorLineColor = Colors.Black.WithOpacity(.35);
			myPlot.Grid.MajorLineWidth = 1;
			myPlot.Grid.MinorLineColor = Colors.Black.WithOpacity(.15);
			myPlot.Grid.MinorLineWidth = 1;

			myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.TITLE_SIZE);

			myPlot.Axes.Bottom.Label.Alignment = Alignment.MiddleCenter;
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			// Legend
			SetupLegend(myPlot);
		}

		public void InitializeMagFreqPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupMagTics(myPlot);
			SetupFreqTics(myPlot);
			AddMagFreqRule(myPlot);
		}

		public void InitializePctFreqPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupPercentTics(myPlot);
			SetupFreqTics(myPlot);
			AddPctFreqRule(myPlot);
		}
	}
}
