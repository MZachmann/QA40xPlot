using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Libraries
{
	public interface PlotUtil
	{
		public static void SetupLegend(ScottPlot.Plot myPlot)
		{
			// Set up the legend
			myPlot.Legend.IsVisible = true;
			myPlot.Legend.Alignment = ScottPlot.Alignment.UpperRight;
			myPlot.Legend.Orientation = ScottPlot.Orientation.Vertical;
			myPlot.Legend.FontSize = GraphUtil.PtToPixels(PixelSizes.LEGEND_SIZE);
			myPlot.ShowLegend();
		}

		private static void SetupMagTics(ScottPlot.Plot myPlot)
		{
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);

			ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
			tickGenY.TargetTickCount = 15;
			tickGenY.MinorTickGenerator = minorTickGen;

			// tell the left axis to use our custom tick generator
			myPlot.Axes.Left.TickGenerator = tickGenY;
		}

		static void SetupPercentTics(ScottPlot.Plot myPlot)
		{
			ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenY = new();
			minorTickGenY.Divisions = 10;

			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
			tickGenY.MinorTickGenerator = minorTickGenY;

			// create a custom tick formatter to set the label text for each tick
			static string LogTickLabelFormatter(double y) => MathUtil.FormatLogger(Math.Pow(10, y));

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

		private static void SetupFreqTics(ScottPlot.Plot myPlot)
		{
			ScottPlot.TickGenerators.NumericManual tickGenX = new();
			// we start at a freq 1 of and end at 100000 I guess
			for (int i = 0; i < 7; i++)
			{
				for (int j = 1; j <= 9; j++)
				{
					if (j == 1 || j == 2 || j == 5)
					{
						var val = (int)(0.5 + Math.Pow(10, i + Math.Log10(j)));
						tickGenX.AddMajor(i + Math.Log10(j), MathUtil.FormatLogger(val));
					}
					else
					{
						tickGenX.AddMinor(i + Math.Log10(j));
					}
				}
			}
			myPlot.Axes.Bottom.TickGenerator = tickGenX;
		}

		private static void SetupAmpTics(ScottPlot.Plot myPlot)
		{
			ScottPlot.TickGenerators.NumericManual tickGenX = new();
			// we start at an amp of 1mV of and end at 1000 I guess
			for (int i = -3; i < 4; i++)
			{
				for (int j = 1; j <= 9; j++)
				{
					if (j == 1 || j == 2 || j == 5)
					{
						var val = Math.Pow(10, i + Math.Log10(j));
						tickGenX.AddMajor(i + Math.Log10(j), MathUtil.FormatLogger(val));
					}
					else
					{
						tickGenX.AddMinor(i + Math.Log10(j));
					}
				}
			}
			myPlot.Axes.Bottom.TickGenerator = tickGenX;
		}

		private static void SetupTimeTics(ScottPlot.Plot myPlot)
		{
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);

			ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
			tickGenY.TargetTickCount = 15;
			tickGenY.MinorTickGenerator = minorTickGen;

			// tell the left axis to use our custom tick generator
			myPlot.Axes.Left.TickGenerator = tickGenY;
		}

		private static void SetupPhaseTics(ScottPlot.Plot myPlot)
		{
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(5);

			ScottPlot.TickGenerators.NumericAutomatic tickGenY2 = new();
			tickGenY2.TargetTickCount = 12;
			tickGenY2.MinorTickGenerator = minorTickGen;

			// tell the left axis to use our custom tick generator
			myPlot.Axes.Right.TickGenerator = tickGenY2;
		}

		// this sets the axes bounds for freq vs percent
		private static void SetPctFreqRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(200000), Math.Log10(0.000000001), Math.Log10(10000))
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs magnitude while zooming in and out
		public static void SetMagFreqRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(200000), -250, 150)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs magnitude while zooming in and out
		public static void SetOhmFreqRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -200, 10000)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for phase while zooming. bottom is as above...
		public static void AddPhaseFreqRule(ScottPlot.Plot myPlot)
		{
			// myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Right,
				limits: new AxisLimits(Math.Log10(1), Math.Log10(100000), -360, 360)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs percent
		private static void SetPctAmpRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(0.0001), Math.Log10(1000), Math.Log10(0.00000001), Math.Log10(100))
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs magnitude
		private static void SetMagAmpRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(Math.Log10(0.0001), Math.Log10(1000), -200, 100)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// this sets the axes bounds for freq vs magnitude
		private static void SetMagTimeRule(ScottPlot.Plot myPlot)
		{
			myPlot.Axes.Rules.Clear();
			ScottPlot.AxisRules.MaximumBoundary rule = new(
				xAxis: myPlot.Axes.Bottom,
				yAxis: myPlot.Axes.Left,
				limits: new AxisLimits(0, 100000, -100, 100)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		// generic initialization of a plot basics
		private static void InitializeAPlot(ScottPlot.Plot myPlot)
		{
			myPlot.Clear();

			// sometimes we don't show phase so remove it
			if (myPlot.Axes.Right != null)
			{
				myPlot.Axes.Right.RemoveTickGenerator();
				myPlot.Axes.Right.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
				myPlot.Axes.Right.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			}

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

			// change figure colors Dark mode
			myPlot.FigureBackground.Color = new Color(0, 0, 0, 00);
			myPlot.DataBackground.Color = new Color(0, 0, 0, 10);
			//myPlot.FigureBackground.Color = Color.FromHex("#181818");
			//myPlot.DataBackground.Color = Color.FromHex("#1f1f1f");

			//// change axis and grid colors
			//myPlot.Axes.Color(Color.FromHex("#d7d7d7"));
			//myPlot.Grid.MajorLineColor = Color.FromHex("#404040");

			//// change legend colors
			//myPlot.Legend.BackgroundColor = Color.FromHex("#404040");
			//myPlot.Legend.FontColor = Color.FromHex("#d7d7d7");
			//myPlot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

			// Legend
			SetupLegend(myPlot);
		}

		public static void AddPhasePlot(ScottPlot.Plot myPlot)
		{
			SetupPhaseTics(myPlot);
			AddPhaseFreqRule(myPlot);
		}

		public static void InitializeMagFreqPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupMagTics(myPlot);
			SetupFreqTics(myPlot);
			SetMagFreqRule(myPlot);
		}

		public static void InitializePctFreqPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupPercentTics(myPlot);
			SetupFreqTics(myPlot);
			SetPctFreqRule(myPlot);
		}

		public static void InitializePctAmpPlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupPercentTics(myPlot);
			SetupAmpTics(myPlot);
			SetPctAmpRule(myPlot);
		}

		public static void InitializeMagAmpPlot(ScottPlot.Plot myPlot, string plotFormat)
		{
			InitializeAPlot(myPlot);
			SetupAmpTics(myPlot);
			if (GraphUtil.IsPlotFormatLog(plotFormat))
			{
				SetupMagTics(myPlot);
				SetMagAmpRule(myPlot);
			}
			else
			{
				SetupPercentTics(myPlot);
				SetPctAmpRule(myPlot);
			}
		}

		public static void InitializeMagTimePlot(ScottPlot.Plot myPlot)
		{
			InitializeAPlot(myPlot);
			SetupMagTics(myPlot);
			SetupTimeTics(myPlot);
			SetMagTimeRule(myPlot);
		}

		public static void InitializeLogFreqPlot(ScottPlot.Plot myPlot, string plotFormat)
		{
			InitializeAPlot(myPlot);
			SetupFreqTics(myPlot);
			if (GraphUtil.IsPlotFormatLog(plotFormat))
			{
				SetupMagTics(myPlot);
				SetMagFreqRule(myPlot);
			}
			else
			{
				SetupPercentTics(myPlot);
				SetPctFreqRule(myPlot);
			}

		}

	}
}
