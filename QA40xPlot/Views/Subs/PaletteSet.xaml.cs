using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot.NamedColors;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	public class ColorInfo
	{
		public string ColorName { get; set; } = string.Empty; // Name of the color
	}

	public class PaletteView : FloorViewModel
	{
		private ObservableCollection<ColorInfo> _PaletteColors = new ObservableCollection<ColorInfo>();
		public ObservableCollection<ColorInfo> PaletteColors
		{
			get => _PaletteColors;
			set => SetProperty(ref _PaletteColors, value);
		}

		public PaletteView()
		{
			var clrs = ViewSettings.Singleton.SettingsVm.PaletteColors; // the colors as a text list
			var hexlist = clrs.Split(',').Select(x => x.TrimStart()).ToArray();
			foreach (var color in hexlist)
			{
				PaletteColors.Add(new ColorInfo
				{
					ColorName = color
				});
			}
		}
	}

	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class PaletteSet : Window
	{
		private PaletteView _PaletteShow = new PaletteView();
		private List<ColorBox> _Buttons = new List<ColorBox>();
		string OriginalColors = string.Empty;

		private void DoOk(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			DoApply(sender, e); // apply the changes
			ViewSettings.Singleton.Main.CurrentView?.UpdatePlotColors(); // force a redraw of the graph
			Close();
		}

		private void DoCancel(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			ViewSettings.Singleton.Main.CurrentPaletteRect = MainViewModel.GetWindowSize(this);
			ViewSettings.Singleton.SettingsVm.PaletteColors = OriginalColors; // restore original colors
			ViewSettings.Singleton.Main.CurrentView?.UpdatePlotColors(); // force a redraw of the graph
			Close();
		}

		private void ApplySet()
		{
			// nothing
			try
			{
				var colors = _Buttons.Select(box => box.Color).ToList(); // get the color names from the boxes
				var result = String.Join(", ", colors); // join them into a single string
				ViewSettings.Singleton.SettingsVm.PaletteColors = result; // save the colors to the settings
				ViewSettings.Singleton.Main.CurrentView?.UpdatePlotColors(); // force a redraw of the graph
			}
			catch
			{

			}
		}

		private void DoApply(object sender, RoutedEventArgs e)
		{
			ApplySet();
		}

		public PaletteSet()
		{
			SetDataContext(_PaletteShow);
			OriginalColors = ViewSettings.Singleton.SettingsVm.PaletteColors;
			InitializeComponent();
			var viewWind = ViewSettings.Singleton.Main.CurrentPaletteRect;
			if (viewWind.Length > 0)
			{
				MainViewModel.SetWindowSize(this, viewWind);
			}
			var wrap = this.PaletteSpot;    // the wrap panel in the XAML
			var colors = _PaletteShow.PaletteColors; // the colors from the PaletteView
			foreach (var color in colors)
			{

				var button = new ColorBox
				{
					Color = color.ColorName,
					Background = System.Windows.Media.Brushes.Transparent,
					BorderBrush = System.Windows.Media.Brushes.Transparent,
					Width = 50,
					Height = 50,
				};

				button.PropertyChanged += (sender,e) =>
				{	if (e.PropertyName == nameof(ColorBox.Color))
					{
						ApplySet(); // apply the changes when the color changes
					}
				};

				wrap.Children.Add(button);
				_Buttons.Add(button);	// so we can do ok easily
			}
		}

		public void SetDataContext(PaletteView dataDef)
		{
			this.DataContext = dataDef;
		}

		private void OnClick(object sender, RoutedEventArgs e)
		{
			var btu = sender as Button;
			if (btu != null)
			{
				var ids = btu.CommandParameter.ToString() ?? "";
				var myVm = ViewSettings.Singleton.Main.CurrentView;
				myVm?.DoDeleteIt(ids);
			}
		}

		private void ForceRepaint(object sender, RoutedEventArgs e)
		{
			var u = this.DataContext as BaseViewModel;
			var btu = sender as CheckBox;
			if (btu != null && u != null)
			{
				//var ids = btu.CommandParameter.ToString() ?? "";
				u.ForceGraphDraw();
			}
		}
	}
}
