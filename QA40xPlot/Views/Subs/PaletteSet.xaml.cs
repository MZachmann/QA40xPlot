using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using QA40xPlot.ViewModels.Subs;
using ScottPlot.NamedColors;
using System.Collections.ObjectModel;
using System.Drawing;
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
			if( hexlist.Length < ThePalette.PaletteSize)
			{
				var j = ThePalette.PaletteSize - hexlist.Length;
				for(int i=0; i<j; i++)
				{
					PaletteColors.Add(new ColorInfo
					{
						ColorName = hexlist[i % hexlist.Length]
					});
				}
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
			ViewSettings.Singleton.MainVm.CurrentView?.UpdatePlotColors(); // force a redraw of the graph
			Close();
		}

		private void DoCancel(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			ViewSettings.Singleton.MainVm.CurrentPaletteRect = MainViewModel.GetWindowSize(this);
			ViewSettings.Singleton.SettingsVm.PaletteColors = OriginalColors; // restore original colors
			ViewSettings.Singleton.MainVm.CurrentView?.UpdatePlotColors(); // force a redraw of the graph
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
				ViewSettings.Singleton.MainVm.CurrentView?.UpdatePlotColors(); // force a redraw of the graph
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
			var viewWind = ViewSettings.Singleton.MainVm.CurrentPaletteRect;
			if (viewWind.Length > 0)
			{
				MainViewModel.SetWindowSize(this, viewWind);
			}
			var wrap = this.PaletteSpot;    // the wrap panel in the XAML
			var colors = _PaletteShow.PaletteColors; // the colors from the PaletteView
			int indx = 0;
			foreach (var color in colors)
			{
				var clrbutton = new ColorBox
				{
					Color = color.ColorName,
					Background = System.Windows.Media.Brushes.Transparent,
					BorderBrush = System.Windows.Media.Brushes.Transparent,
					Width = 60,
					Height = 60
				};
				var button = new Canvas()
				{
					Width = 60,
					Height = 60,
				};
				var txtbutton = new TextBox()
				{
					Text = indx.ToString(),
					BorderThickness = new Thickness(0),
					IsReadOnly = true
				};
				button.Children.Add(clrbutton);
				button.Children.Add(txtbutton);
				indx++;
				clrbutton.PropertyChanged += (sender,e) =>
				{	if (e.PropertyName == nameof(ColorBox.Color))
					{
						ApplySet(); // apply the changes when the color changes
					}
				};

				wrap.Children.Add(button);
				_Buttons.Add(clrbutton);	// so we can do ok easily
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
				var myVm = ViewSettings.Singleton.MainVm.CurrentView;
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
