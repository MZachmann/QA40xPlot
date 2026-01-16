using QA40xPlot.Extensions;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Diagnostics;
using System.Numerics;
using System.Windows;

// this is currently not used but left behind just in case we want to show residuals in their own window

namespace QA40xPlot.ViewModels
{
	public class ResidualViewModel : FloorViewModel
	{
		protected static double ToD(string sval, double defval = 0.0)
		{
			return MathUtil.ToDouble(sval, defval);
		}

		private static Window? _MyWindow = null;
#region properties
		private int _NumHarmonics = 4;
		public int NumHarmonics
		{
			get { return _NumHarmonics; }
			set { SetProperty(ref _NumHarmonics, value); }
		}

		private LeftRightTimeSeries _TimeSeries = new();
		public LeftRightTimeSeries TimeSeries
		{
			get { return _TimeSeries; }
			set { _TimeSeries = value; }
		}

		// new string property backing field and property
		private string _MaxVolts = "1.0";
		public string MaxVolts
		{
			get => _MaxVolts;
			set => SetProperty(ref _MaxVolts, value);
		}
		private string _MinVolts = "-1.0";
		public string MinVolts
		{
			get => _MinVolts;
			set => SetProperty (ref _MinVolts, value);
		}
		private string _StartTime = "0";
		public string StartTime
		{
			get => _StartTime;
			set => SetProperty (ref _StartTime, value);
		}
		private string _EndTime = "10";
		public string EndTime
		{
			get => _EndTime;
			set => SetProperty(ref _EndTime, value);
		}
		private bool _ShowLeft = true;
		public bool ShowLeft
		{
			get => _ShowLeft;
			set => SetProperty(ref _ShowLeft, value);
		}
		private bool _ShowRight = false;
		public bool ShowRight
		{
			get => _ShowRight;
			set => SetProperty(ref _ShowRight, value);
		}

		private WpfPlot _ThePlot = new();
		public WpfPlot ThePlot
		{
			get { return _ThePlot; }
			set { _ThePlot = value; }
		}

		private string _PlotFormat = "V";
		public string PlotFormat
		{
			get => _PlotFormat;
			set
			{
				SetProperty(ref _PlotFormat, value);
			}
		}

		private static string _WindowLocation = "0";
		public static string WindowLocation
		{
			get => _WindowLocation;
			set => _WindowLocation = value;
		}

		public static bool HasResidualPlot()
		{
			return (_MyWindow != null);
		}
#endregion

		/// <summary>
		/// show the residuals window if it's not already showing
		/// </summary>
		public static Window ShowResidualPlot()
		{
			if (_MyWindow == null)
			{
				// viewmodel setup
				var vm = new ResidualViewModel();
				// window proc
				_MyWindow = new ResidualsWnd(vm);
				_MyWindow.Closing += OnClosingResiduals; // cleanup on close
				if (ResidualViewModel.WindowLocation.Length > 7)
				{
					MainViewModel.SetWindowSize(_MyWindow, ResidualViewModel.WindowLocation);
				}
				_MyWindow.Show();
			}
			else
			{
				Debug.WriteLine("Attempt to re-show residuals");
			}
			return _MyWindow;
		}

		/// <summary>
		/// show the residuals window if it's not already showing
		/// </summary>
		public static void UpdateResidualPlot(LeftRightTimeSeries lrfs, bool setChanged)
		{
			if (_MyWindow != null)
			{
				// viewmodel setup
				var vm = (ResidualViewModel)_MyWindow.DataContext;
				vm.TimeSeries = lrfs;
				// window proc
				vm.UpdatePlot(setChanged);
			}
			else
			{
				Debug.WriteLine("Attempt to re-show residuals");
			}
		}
		/// <summary>
		/// the Window says it is closing, get the size
		/// </summary>
		/// <param name="_"></param>
		/// <param name="__"></param>
		public static void OnClosingResiduals(object? _, EventArgs? __)
		{
			if (_MyWindow != null)
			{
				ResidualViewModel.WindowLocation =  MainViewModel.GetWindowSize(_MyWindow);
				_MyWindow = null;
			}
		}

		// Gui has asked to close the plot
		public static void CloseResidualPlot()
		{
			if (_MyWindow != null)
			{
				_MyWindow.Close();
			}
		}

		/// <summary>
		/// Initialize the magnitude plot
		/// </summary>
		void InitializeMagnitudePlot()
		{
			ScottPlot.Plot myPlot = ThePlot.Plot;
			PlotUtil.InitializeMagTimePlot(myPlot);
			myPlot.Axes.SetLimits(ToD(StartTime), ToD(EndTime), ToD(MinVolts), ToD(MaxVolts));

			//UpdatePlotTitle();
			myPlot.XLabel("Time (mS)");
			myPlot.YLabel("Voltage");

			ThePlot.Refresh();
		}


		private void UpdatePlot(bool settingsChanged)
		{
			if (ThePlot != null)
			{
				//ThePlot.Plot.Remove<Marker>();             // Remove all current markers
				if (settingsChanged)
				{
					InitializeMagnitudePlot();
				}
				DrawPlotLines(0); // draw the lines 
			}
		}

		public int DrawPlotLines(int resultNr)
		{

			ThePlot.Plot.Remove<SignalXY>();             // Remove all current lines
			PlotValues();
			ThePlot.Refresh();
			return ++resultNr;
		}

		/// <summary>
		/// Plot the THD % graph
		/// </summary>
		/// <param name="data"></param>
		void PlotValues()
		{
			ScottPlot.Plot myPlot = ThePlot.Plot;
			bool useLeft;   // dynamically update these
			bool useRight;
			useLeft = ShowLeft; // dynamically update these
			useRight = ShowRight;

			var td = QaMath.CalculateResidual(TimeSeries);

			var timeData = td;
			if (timeData == null || timeData.Left.Length == 0)
				return;

			double maxleft = timeData.Left.Max();
			double maxright = timeData.Right.Max();

			var timeX = Enumerable.Range(0, timeData.Left.Length).Select(x => x * 1000 * timeData.dt).ToArray(); // in ms
			//var showThick = MyVModel.ShowThickLines;    // so it dynamically updates
			//var markerSize = scopeVm.ShowPoints ? (showThick ? _Thickness : 1) + 3 : 1;
			if (useLeft)
			{
				var pLeft = myPlot.Add.SignalXY(timeX, timeData.Left);
				pLeft.LineWidth = 3;
				pLeft.Color = GraphUtil.GetPaletteColor("Transparent", 0);
				//pLeft.MarkerSize = markerSize;
				//pLeft.LegendText = isMain ? "Left" : ClipName(page.Definition.Name) + ".L";
			}

			if (useRight)
			{
				var pRight = myPlot.Add.SignalXY(timeX, timeData.Right);
				pRight.LineWidth = 3;
				pRight.Color = GraphUtil.GetPaletteColor("Transparent", 1);
				//pRight.MarkerSize = markerSize;
				//pRight.LegendText = isMain ? "Right" : ClipName(page.Definition.Name) + ".R";
			}

			ThePlot.Refresh();
		}



	}
}
