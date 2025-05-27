using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QA40xPlot.Views
{
	public partial class ColorPicker : Window
	{
		public static List<String> PlotColors { get => SettingsViewModel.PlotColors; } // the background colors
		public ColorPicker(string currentColor = "")
		{
			InitializeComponent();
			NowColor = currentColor;
			PopulateColors();
		}

		private void PopulateColors()
		{
			List<ScottPlot.Color> allcolors = new();
			List<IPalette> palettes = new List<IPalette>();
			palettes.Add(new ScottPlot.Palettes.Tsitsulin());
			palettes.Add(new ScottPlot.Palettes.Category10());
			palettes.Add(new ScottPlot.Palettes.Category20());
			palettes.Add(new ScottPlot.Palettes.Amber());
			palettes.Add(new ScottPlot.Palettes.Aurora());
			palettes.Add(new ScottPlot.Palettes.Building());
			palettes.Add(new ScottPlot.Palettes.ColorblindFriendly());
			palettes.Add(new ScottPlot.Palettes.Dark());
			palettes.Add(new ScottPlot.Palettes.LightSpectrum());
			palettes.Add(new ScottPlot.Palettes.Nord());
			palettes.Add(new ScottPlot.Palettes.Normal());
			palettes.Add(new ScottPlot.Palettes.OneHalf());
			palettes.Add(new ScottPlot.Palettes.OneHalfDark());
			palettes.Add(new ScottPlot.Palettes.LightOcean()); // Add more palettes as needed
			palettes.Add(new ScottPlot.Palettes.Penumbra());
			palettes.Add(new ScottPlot.Palettes.PolarNight());
			palettes.Add(new ScottPlot.Palettes.DarkPastel());
			palettes.Add(new ScottPlot.Palettes.Redness());
			// get unique colors from all palettes
			foreach (var palet in palettes)
				foreach (var c in palet.Colors)
					if (!allcolors.Contains(c)) // Avoid duplicates
						allcolors.Add(c);

			var currentColor = PlotUtil.StrToColor(NowColor);
			// Add more palettes as needed
			// Use a specific palette, e.g., Tsitsulin
			foreach (var color in allcolors)
			{
				// Convert ScottPlot Color to System.Windows.Media.Color
				System.Windows.Media.Color myclr = PlotUtil.ScottToMedia(color);
				var colorButton = new Button
				{
					Background = new SolidColorBrush(myclr),
					Width = 30,
					Height = 30,
					Margin = new Thickness(5)
				};
				if (currentColor == color)
				{
					colorButton.Height = 32;
					colorButton.Width = 32;
					colorButton.BorderThickness = new Thickness(3);
					colorButton.BorderBrush = Brushes.Red; // Highlight the current color
				}
				colorButton.Click += ColorButton_Click;
				ColorWrapPanel.Children.Add(colorButton);
			}
		}

		private void ColorButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Background is SolidColorBrush brush)
			{
				NowColor = brush.Color.ToString();
				DialogResult = true;
				Close();
			}
		}

		// Register the dependency property
		public static readonly DependencyProperty ColorProperty =
			DependencyProperty.Register(
				"NowColor",                // Property name
				typeof(string),              // Property type
				typeof(ColorPicker),       // Owner type
				new FrameworkPropertyMetadata(
					"Red", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
			);

		// CLR wrapper for the dependency property
		public string NowColor
		{
			get => (string)GetValue(ColorProperty);
			set
			{
				SetValue(ColorProperty, value);
			}
		}

		private void DoOk(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}
