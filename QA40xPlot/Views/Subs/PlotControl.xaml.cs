using QA40xPlot.ViewModels;
using ScottPlot.WPF;
using System.Windows.Controls;
using System.Windows.Input;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for PlotControl.xaml
	/// </summary>
	public partial class PlotControl : UserControl
	{
		public ScottPlot.Plot ThePlot { get; set; }
		private WpfPlot _APlot;
		public WpfPlot APlot { get { return _APlot; } set { _APlot = value; ThePlot = value.Plot; } }
		public BaseViewModel? GrandParent { get; set; }

		private bool _TrackMouse = true;
		public bool TrackMouse
		{
			get { return _TrackMouse; }
			set
			{
				_TrackMouse = value;
				if (value)
				{
					this.MouseMove -= OnMouseMove;
					this.MouseMove += OnMouseMove;
					this.MouseUp -= OnMouseDown;
					this.MouseUp += OnMouseDown;
					this.MouseDown -= OnMouseDown;
					this.MouseDown += OnMouseDown;
				}
				else
				{
					this.MouseUp -= OnMouseDown;
					this.MouseDown -= OnMouseDown;
					this.MouseMove -= OnMouseMove;
				}
			}
		}

		private void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				GrandParent?.RaiseMouseTracked("off");
			}
		}

		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			// Get the mouse position relative to the Canvas
			//Point position = e.GetPosition(this);
			//var coords = this.ThePlot.GetCoordinates(new Pixel(position.X, position.Y));
			//var cx = Math.Pow(10, coords.X);
			//var cy = Math.Pow(10, coords.Y);
			GrandParent?.RaiseMouseTracked("helo");

			// Display the position in a TextBlock
			//MousePositionTextBlock.Text = $"X: {position.X}, Y: {position.Y}";
		}


		public PlotControl()
		{
			InitializeComponent();
			GrandParent = null;
			_APlot = TheWpfPlot;
			ThePlot = TheWpfPlot.Plot;
		}

		public void Refresh()
		{
			APlot.Refresh();
		}

	}
}
