using QA40xPlot.Actions;
using QA40xPlot.ViewModels;
using ScottPlot;

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

		public static ScottPlot.TickGenerators.NumericAutomatic BuildMagTics(ScottPlot.Plot myPlot)
		{
			// create a numeric tick generator that uses our custom minor tick generator
			ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(2);

			ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
			tickGenY.TargetTickCount = 15;
			tickGenY.MinorTickGenerator = minorTickGen;

			// tell the left axis to use our custom tick generator
			return tickGenY;
		}

		private static void SetupMagTics(ScottPlot.Plot myPlot)
		{
			// tell the left axis to use our custom tick generator
			myPlot.Axes.Left.TickGenerator = BuildMagTics(myPlot);
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

		public static string AxisParameter(object? parameter)
		{
			string pname = parameter as string ?? string.Empty;
			if (pname.Length == 0)
				return string.Empty;

			switch (pname)
			{
				case "XF":
					return "GraphStartX";
				case "XT":
					return "GraphStartX";
				case "YP":
					return "RangeTop";
				case "YM":
					return "RangeTopdB";
				case "PH":
					return "PhaseTop";
				case "Y2":
					return "Range2Top";
				default:
					return "GraphStartX";
			}
		}

		// setup the on-screen menu
		public static void SetupMenus<T>(ScottPlot.Plot myPlot, ActBase<T> myAct, BaseViewModel bvm) where T : BaseViewModel
		{
			myPlot.PlotControl?.Menu?.Clear();
			myPlot.PlotControl?.Menu?.Add("Pin All", x => myAct.PinAll(myPlot, bvm));
			myPlot.PlotControl?.Menu?.Add("Fit All", x => myAct.FitAll(myPlot, bvm));//, LeftRightFrequencySeries lfrs));
			myPlot.PlotControl?.Menu?.Add("Snapshot", x => myAct.AddSnapshotPlot());//, LeftRightFrequencySeries lfrs));

			//myPlot.PlotControl?.Menu?.Add("Add Marker", x => this.AddCustomMarker());
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

		public static void SetStockAxis(ScottPlot.Plot myPlot, IAxis axis)
		{
			axis.RemoveTickGenerator();
			axis.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			var foreColor = PlotUtil.StrToColor(ViewSettings.Singleton.SettingsVm.GraphForeground);
			var light = ToBrightness(foreColor);
			var clr = (light < 128) ? ScottPlot.Colors.White : ScottPlot.Colors.Black;
			axis.Label.ForeColor = clr;         // use dark text color if needed
			axis.TickLabelStyle.ForeColor = clr;
			axis.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			var tickgen = PlotUtil.BuildMagTics(myPlot);
			axis.TickGenerator = tickgen;
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
			if (myPlot.Axes.Right != null)
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
				limits: new AxisLimits(Math.Log10(1), Math.Log10(200000), -200, 10000)
				);
			myPlot.Axes.Rules.Add(rule);
		}

		public static ScottPlot.AxisPanels.LeftAxis AddSecondY(Plot myPlot, BaseViewModel bvm)
		{
			// create a second axis and add it to the plot
			var yAxis2 = myPlot.Axes.AddLeftAxis();
			bvm.SecondYAxis = yAxis2;
			return yAxis2;
		}

		public static void SetHeadingColor(System.Windows.Controls.Label myLabel)
		{
			var backColor = StrToColor(ViewSettings.Singleton.SettingsVm.GraphForeground);
			var light = ToBrightness(backColor);
			var brush = new System.Windows.Media.BrushConverter().ConvertFromString((light < 128) ? "White" : "Black");
			if (brush != null)
				myLabel.Foreground = (System.Windows.Media.SolidColorBrush)brush;
		}

		public static IYAxis AddSecondYR(Plot myPlot, BaseViewModel bvm)
		{
			// create a second axis and add it to the plot
			var yAxis2 = myPlot.Axes.Right;
			bvm.SecondYAxis = yAxis2;
			return yAxis2;
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

			myPlot.Axes.Title.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.TITLE_SIZE);
			myPlot.Axes.Bottom.Label.Alignment = Alignment.MiddleCenter;
			myPlot.Axes.Bottom.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.Axes.Bottom.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

			myPlot.Axes.Left.Label.FontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			myPlot.Axes.Left.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);

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
			// SetupLegend(myPlot);
			myPlot.HideLegend();
			UpdateAPlot(myPlot);
		}

		public static void AddPhasePlot(ScottPlot.Plot myPlot)
		{
			SetStockAxis(myPlot, myPlot.Axes.Right);
			SetupPhaseTics(myPlot);
		}

		public static void AddGroupDelayPlot(ScottPlot.Plot myPlot)
		{
			BuildMagTics(myPlot);
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

		public static System.Windows.Media.DoubleCollection ScottToMedia(ScottPlot.LinePattern lp)
		{
			switch (lp.Name)
			{
				case "Dashed":
					return new System.Windows.Media.DoubleCollection() { 5, 5, 5, 5, 5, 3 };
				case "Dotted":
					return new System.Windows.Media.DoubleCollection() { 2, 5, 2, 5, 2, 5, 2, 5 };
				case "DenselyDashed":
					return new System.Windows.Media.DoubleCollection() { 5, 2, 5, 2, 5, 2, 5, 2 };
				default:
					return new System.Windows.Media.DoubleCollection() { 28 };
			}
		}

		public static System.Windows.Media.Color ScottToMedia(ScottPlot.Color clr)
		{
			// Convert the ScottPlot Color object to a System.Windows.Media.Color object
			return new System.Windows.Media.Color
			{
				A = clr.A,
				R = clr.R,
				G = clr.G,
				B = clr.B
			};
		}

		public static ScottPlot.Color MediaToScott(System.Windows.Media.Color clr)
		{
			// Convert the ScottPlot Color object to a System.Windows.Media.Color object
			var c = new ScottPlot.Color(clr.A, clr.R, clr.G, clr.B);
			return c;
		}

		public static string ColorToStr(Color color)
		{   // Convert the Color object to a hexadecimal string
			// note that ScottPlot parses hex colors wrong with the alpha channel last not first
			// so use WithAlpha instead of inserting into the hex
			string hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
			return hex;
		}

		public static System.Windows.Media.Color StrToMediaColor(string fromSetting)
		{
			var color = System.Windows.Media.Colors.Black; // default color in case of error
			try
			{
				color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(fromSetting);
			}
			catch (Exception ex)
			{
				// Handle other exceptions if necessary
				System.Diagnostics.Debug.WriteLine($"Error converting string to color: {ex.Message}");
			}
			return color;
		}

		public static ScottPlot.Color StrToColor(string fromSetting)
		{
			try
			{
				var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(fromSetting);
				// Convert the Color object to a hexadecimal string
				// note that ScottPlot parses hex colors wrong with the alpha channel last not first
				// so use WithAlpha instead of inserting into the hex
				string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
				return new Color(hex).WithAlpha(color.A);
			}
			catch (FormatException)
			{
				// If the string is not a valid color, return a default color
				return Colors.Black; // or any other default color you prefer
			}
			catch (Exception ex)
			{
				// Handle other exceptions if necessary
				Console.WriteLine($"Error converting string to color: {ex.Message}");
				return Colors.Black; // or any other default color you prefer
			}
		}

		private static double ToBrightness(Color clr)
		{
			// BT.601 Y = 0.299 R + 0.587 G + 0.114 B
			return clr.R * 0.299 + clr.G * 0.587 + clr.B * 0.114;
		}

		/// <summary>
		/// Set the plot coloring based on global settings
		/// </summary>
		/// <param name="myPlot"></param>
		public static void UpdateAPlot(ScottPlot.Plot myPlot)
		{
			// change figure colors Dark mode
			var backColor = StrToColor(ViewSettings.Singleton.SettingsVm.GraphBackClr);
			myPlot.DataBackground.Color = backColor;

			var light = ToBrightness(backColor);
			var clr = (light < 128) ? ScottPlot.Colors.White : ScottPlot.Colors.Black;
			myPlot.Grid.MajorLineColor = clr.WithOpacity(.35);
			myPlot.Grid.MinorLineColor = clr.WithOpacity(.15);

			var foreColor = StrToColor(ViewSettings.Singleton.SettingsVm.GraphForeground);
			myPlot.FigureBackground.Color = foreColor;

			// show grid lines for major ticks
			myPlot.Grid.MajorLineWidth = 1;
			myPlot.Grid.MinorLineWidth = 1;

			// if foreground color is dark, swap text color
			light = ToBrightness(foreColor);
			clr = (light < 128) ? ScottPlot.Colors.White : ScottPlot.Colors.Black;
			// use dark text color
			myPlot.Axes.Title.Label.ForeColor = clr;
			myPlot.Axes.Bottom.Label.ForeColor = clr;
			myPlot.Axes.Bottom.TickLabelStyle.ForeColor = clr;

			myPlot.Axes.Left.Label.ForeColor = clr;
			myPlot.Axes.Left.TickLabelStyle.ForeColor = clr;

			if (myPlot.Axes.Right != null)
			{
				myPlot.Axes.Right.Label.ForeColor = clr;
				myPlot.Axes.Right.TickLabelStyle.ForeColor = clr;
			}

		}
		public static void AddGroupDelay(BaseViewModel frqrsVm, Plot myPlot)
		{
			var axis = AddSecondY(myPlot, frqrsVm);
			axis.RemoveTickGenerator();
			var y2axis = frqrsVm.SecondYAxis;
			if (y2axis != null)
			{
				myPlot.Axes.SetLimitsY(MathUtil.ToDouble(frqrsVm.Range2Bottom, -10), 
					MathUtil.ToDouble(frqrsVm.Range2Top, 10), y2axis);
			}
			frqrsVm.Y2AxisUnit = "ms";
			axis.LabelText = "Group Delay (ms)";
			var tickgen = BuildMagTics(myPlot);
			var foreColor = StrToColor(ViewSettings.Singleton.SettingsVm.GraphForeground);
			var light = ToBrightness(foreColor);
			var clr = (light < 128) ? ScottPlot.Colors.White : ScottPlot.Colors.Black;

			axis.TickGenerator = tickgen;
			axis.LabelFontSize = GraphUtil.PtToPixels(PixelSizes.LABEL_SIZE);
			axis.TickLabelStyle.FontSize = GraphUtil.PtToPixels(PixelSizes.AXIS_SIZE);
			// use dark text color
			axis.LabelFontColor = clr;
			axis.TickLabelStyle.ForeColor = clr;
		}

	}
}
