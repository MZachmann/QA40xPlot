using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using ScottPlot;
using ScottPlot.AxisLimitManagers;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	public partial class ColorPicker : Window
	{
		public static List<String> PlotColors { get => SettingsViewModel.PlotColors; } // the background colors
		public static Rect ViewWindow { get; set; } = Rect.Empty;
		public double ClrOpacity { get; set; }
		public bool WasApplied { get; set; }

		/// <summary>
		/// convert plot color to an opacity value for the slider
		/// </summary>
		/// <param name="opaq"></param>
		/// <returns></returns>
		private double TxtToOpaque(string current)
		{
			var x = PlotUtil.StrToColor(current);      // as a color
			double normal = 100.0 * x.A / 255.0;             // normalize to 0-100
			return normal;
		}

		public void DoShowOpaque(double alpha)
		{
			MySlider.Value = alpha;                  // set the slider to the current opacity
			ShowOpaque.Content = $"{alpha:0.#}%"; // Show the opacity percentage
		}

		public ColorPicker(string currentColor = "")
		{
			InitializeComponent();
			NowColor = currentColor;
			PopulateColors();
			ClrOpacity = TxtToOpaque(currentColor);             // normalize to 0-100
			DoShowOpaque(ClrOpacity); // Show the opacity percentage
			WasApplied = false;
			if(ViewWindow.Width > 100 && ViewWindow.Height > 100)
			{
				Width = ViewWindow.Width;
				Height = ViewWindow.Height;
				Left = ViewWindow.Left;
				Top = ViewWindow.Top;
			}
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
					Background = new System.Windows.Media.SolidColorBrush(myclr),
					Width = 30,
					Height = 30,
					Margin = new Thickness(5)
				};
				if (currentColor == color)
				{
					colorButton.Height = 32;
					colorButton.Width = 32;
					colorButton.BorderThickness = new Thickness(3);
					colorButton.BorderBrush = System.Windows.Media.Brushes.Red; // Highlight the current color
				}
				colorButton.Click += ColorButton_Click;
				ColorWrapPanel.Children.Add(colorButton);
			}
		}

		private void ColorButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.Background is System.Windows.Media.SolidColorBrush brush)
			{
				var alphaVal = MySlider.Value; // 0...1
				if (alphaVal < 10)   // if nearly transparent reset to solid
				{
					alphaVal = 100; // avoid setting alpha to 0
					MySlider.Value = alphaVal; // reset the slider to 100%
				}
				var color = brush.Color.ToString();  // update that
				var aColor = PlotUtil.StrToColor(color).WithAlpha(alphaVal / 100.0);
				NowColor = PlotUtil.ColorToStr(aColor); // Update the NowColor property
														//MySlider.Value = alphaVal; // set the slider to the current opacity
														//DialogResult = true;
														//Close();
			}
		}

		// Register the dependency property
		public static readonly DependencyProperty ColorProperty =
			DependencyProperty.Register(
				"NowColor",                // Property name
				typeof(string),              // Property type
				typeof(ColorPicker),       // Owner type
				new FrameworkPropertyMetadata(
					"Red", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChange)
			);

		public string AValue = string.Empty;
		// CLR wrapper for the dependency property
		public string NowColor
		{
			get => (string)GetValue(ColorProperty);
			set
			{
				AValue = value; // Store the value for later use
				SetValue(ColorProperty, value);
			}
		}

		private Rect GetWindowSize()
		{
			Rect r = Rect.Empty;
			try
			{
				// Get the width and height of the main window
				if (WindowState == WindowState.Normal)
				{
					double windowWidth = Width;
					double windowHeight = Height;
					double Xoffset = Left;
					double Yoffset = Top;
					r = new Rect(Xoffset, Yoffset, windowWidth, windowHeight);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error: {ex.Message}");
			}
			return r;
		}


		private void DoOk(object sender, RoutedEventArgs e)
		{
			WasApplied = false;
			DialogResult = true;
			// Ensure the current window size is captured
			ViewWindow = GetWindowSize();
			Close();
		}

		private void DoApply(object sender, RoutedEventArgs e)
		{
			WasApplied = true;
			DialogResult = true;
			ViewWindow = GetWindowSize();
			Close();
		}

		private void OnSliderChange(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (sender is Slider slider)
			{
				// Convert the slider value (0-100) to an opacity value (0-255)
				var alphaVal = slider.Value; // 0...1
				if(alphaVal < 99 || NowColor.Contains("#"))
				{
					var currentColor = PlotUtil.StrToColor(NowColor).WithAlpha(alphaVal / 100.0);
					NowColor = PlotUtil.ColorToStr(currentColor); // Update the NowColor property
				}
				ShowOpaque.Content = $"{alphaVal:0.#}%"; // Show the opacity percentage
			}
		}

		private void DoCancel(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		// value property change callback.
		private static void OnValueChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var dx = d as ColorPicker;
			if (dx == null)
				return ; // not a ColorPicker

			var color = e.NewValue as string;  // the new entry
			var ecolor = dx.AValue;			// the last entry from program 
			if (color != ecolor && color != null)
			{
				// we get here if use is actually editing the value manually
				// hence the dependency property has changed without the value being 'set'
				var aColor = PlotUtil.StrToColor(color);
				var alpha = 100.0 * aColor.A / 255.0; // get the alpha value
				dx.DoShowOpaque(alpha); // Show the opacity percentage
			}
		}
	}
}
