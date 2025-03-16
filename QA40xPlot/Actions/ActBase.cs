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

		// decade and offset for log
		private string FormatLogger(double val)
		{
			if (val < 10000)
				return val.ToString();
			return ((int)val / 1000).ToString() + "K";
		}

		private void SetupFreqTics(ScottPlot.Plot myPlot)
		{
			ScottPlot.TickGenerators.NumericManual tickGenX = new();
			myPlot.Axes.Bottom.TickGenerator = tickGenX;
			// we start at a freq 1 of and end at 100000 I guess
			for(int i=0; i<7; i++)
			{
				for(int j=1; j<=9; j++)
				{
					if(j==1 || j==2 || j==5)
					{
						var val = (int)(0.5 + Math.Pow(10, i + Math.Log10(j)));
						tickGenX.AddMajor(i + Math.Log10(j), FormatLogger(val));
					}
					else
					{
						tickGenX.AddMinor(i + Math.Log10(j));
					}
				}
			}
		}

		private static void SetupAmpTics(ScottPlot.Plot myPlot)
		{
			// create a minor tick generator that places log-distributed minor ticks
			ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenX = new();
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.NumericAutomatic tickGenX = new();

			// create a custom tick formatter to set the label text for each tick
			static string LogTickLabelFormatter(double y) => $"{Math.Pow(10, Math.Round(y, 10)):#0.######}";

			minorTickGenX.Divisions = 10;
			tickGenX.MinorTickGenerator = minorTickGenX;
			tickGenX.TargetTickCount = 25;
			// tell our major tick generator to only show major ticks that are whole integers
			tickGenX.IntegerTicksOnly = true;
			// tell our custom tick generator to use our new label formatter
			tickGenX.LabelFormatter = LogTickLabelFormatter;
			myPlot.Axes.Bottom.TickGenerator = tickGenX;
		}

		private void SetupPhaseTics(ScottPlot.Plot myPlot)
		{
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);

			ScottPlot.TickGenerators.NumericAutomatic tickGenY2 = new();
			tickGenY2.TargetTickCount = 15;
			tickGenY2.MinorTickGenerator = minorTickGen;

			// tell the left axis to use our custom tick generator
			myPlot.Axes.Right.TickGenerator = tickGenY2;
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
		public void SetMagFreqRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 100)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs magnitude
		public void SetPhaseFreqRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Right,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -180, +180)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs percent
		private void AddPctAmpRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(0.0001), Math.Log10(100000), Math.Log10(0.00000001), Math.Log10(100))
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs magnitude
		private void AddMagAmpRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(0.0001), Math.Log10(100000), -200, 100)
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
			myPlot.Axes.Right.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.Axes.Right.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			// Legend
			SetupLegend(myPlot);
		}

		public void InitializeMagFreqPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupMagTics(myPlot);
			SetupFreqTics(myPlot);
			SetMagFreqRule(myPlot);
		}

		public void AddPhasePlot(ScottPlot.Plot myPlot)
		{
			SetupPhaseTics(myPlot);
			SetPhaseFreqRule(myPlot);
		}

		public void InitializePctFreqPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupPercentTics(myPlot);
			SetupFreqTics(myPlot);
			AddPctFreqRule(myPlot);
		}

		public void InitializePctAmpPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupPercentTics(myPlot);
			SetupAmpTics(myPlot);
			AddPctAmpRule(myPlot);
		}

		public void InitializeMagAmpPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupMagTics(myPlot);
			SetupAmpTics(myPlot);
			AddMagAmpRule(myPlot);
		}

	}
}
