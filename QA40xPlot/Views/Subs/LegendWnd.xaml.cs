using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using QA40xPlot.ViewModels.Subs;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for LegendWnd.xaml
	/// this is a control designed to contain Legend info
	/// in the form of (Label, linePattern, Color)
	/// </summary>
	public partial class LegendWnd : UserControl
	{
		private MovableWnd _MovableWnd = new MovableWnd();

		public LegendWnd()
		{
			InitializeComponent();
		}

		~LegendWnd()
		{
			var vm = this.DataContext as BaseViewModel;
			if(vm != null && vm.LegendInfo != null)
			{
				vm.LegendInfo.CollectionChanged -= Vm_PropertyChanged;
			}
		}

		public void SetDataContext(BaseViewModel vm)
		{
			this.DataContext = vm;
			vm.LegendInfo.CollectionChanged += Vm_PropertyChanged;
		}

		bool checkReset = false;
		// trap when we add data to the legend list
		private void Vm_PropertyChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if(sender != null)
			{
				if(e.Action == NotifyCollectionChangedAction.Add)
				{
					PopulateLegends();
					checkReset = true;
				}
				else if (checkReset && e.Action == NotifyCollectionChangedAction.Reset)
				{
					PopulateLegends();
				}
			}
		}

		private void OnWindMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if(sender is LegendWnd)
			{
				TheBorder.BorderBrush = System.Windows.Media.Brushes.Green;
				_MovableWnd.OnWindMouseDown(sender, e);
			}
		}

		private void OnWindMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender is LegendWnd)
			{
				TheBorder.BorderBrush = System.Windows.Media.Brushes.DarkGray;
				_MovableWnd.OnWindMouseUp(sender, e);
			}
		}

		private void OnWindMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (sender is LegendWnd)
				_MovableWnd.OnWindMouseMove(sender, e);
		}

		private double MeasureString(TextBox textBlock, string candidate)
		{
			var formattedText = new FormattedText(
				candidate,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
				textBlock.FontSize,
				System.Windows.Media.Brushes.Black,
				new NumberSubstitution(),
				VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);
			return formattedText.Width;
		}

		private void PopulateLegends()
		{
			var baseview = DataContext as BaseViewModel;
			if (baseview == null )
				return;
			// empty the wrap panel

			LegendWrapPanel.Children.Clear();
			var info = baseview.LegendInfo; // short name of the list of markers
			// convert marker info into a UIElement stackpanel
			double maxSize = 0;
			foreach(var marker in info)
			{
				var kid = new StackPanel()
				{
					Orientation = Orientation.Horizontal
				};
				var clr = PlotUtil.ScottToMedia(marker.TheColor);
				var stkArray = PlotUtil.ScottToMedia(marker.ThePattern);
				var lne = new System.Windows.Shapes.Line()
				{
					Stroke = new System.Windows.Media.SolidColorBrush(clr),
					X1 = 0,
					X2 = 28,
					Y1 = 0,
					Y2 = 0,
					VerticalAlignment = VerticalAlignment.Center,
					StrokeThickness = 2,
					StrokeDashArray = stkArray,
				};
				kid.Children.Add(lne);
				var tbox = new TextBox()
				{
					Text = marker.Label,
					IsReadOnly = true,
					Margin = new Thickness(5, 0, 0, 0),
					BorderBrush = Brushes.Transparent
				};
				kid.Children.Add(tbox);
				maxSize = Math.Max(maxSize, MeasureString(tbox, marker.Label));
				LegendWrapPanel.Children.Add(kid);
			}
			// now autosize
			maxSize += 10;      // slop
			// all but the none theme add more white space
			var isSmall = (ViewSettings.Singleton.SettingsVm.ThemeSet == "None");
			if (!isSmall)
				maxSize += 20;
			foreach (var kid in LegendWrapPanel.Children)
			{
				var st = kid as StackPanel;
				// bump each textbox to be same width
				if(st != null)
					((TextBox)st.Children[1]).Width = maxSize;
			}
			// now make the panel at most 10xn
			//maxSize += 50;  // add line length
			//LegendWrapPanel.MaxWidth = maxSize + (maxSize * (int)(info.Count / 10));
			LegendWrapPanel.MaxHeight = isSmall ? 165 : 250; // gives us 9 per column
		}
	}
}
