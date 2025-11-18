using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	public partial class ColorPicker : Window
	{
		public static List<String> PlotColors { get => SettingsViewModel.PlotColors; } // the background colors
		public double ClrOpacity { get; set; }
		public Func<ColorPicker, bool>? CallMe { get; set; }

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
			var viewWind = ViewSettings.Singleton.MainVm.CurrentColorRect;
			if (viewWind.Length > 0)
			{
				MainViewModel.SetWindowSize(this, viewWind);
			}
		}

		private static double ToBrightness(System.Windows.Media.Color clr)
		{
			// BT.601 Y = 0.299 R + 0.587 G + 0.114 B
			return clr.R * 0.299 + clr.G * 0.587 + clr.B * 0.114;
		}

		private void PopulateColors()
		{
			var currentColor = PlotUtil.StrToColor(NowColor);
			List<System.Windows.Media.Color> allcolors = new();
			var mycolor = PlotUtil.ScottToMedia(currentColor);
			int[] bytelist = [0, 45, 128, 185, 225, 255];
			int[] greenbytes = [0, 45, 90, 128, 150, 185, 225, 255];
			foreach (var r in bytelist)
			{
				foreach (var g in greenbytes)
				{
					foreach (var b in bytelist)
					{
						allcolors.Add(System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b));
						//Debug.WriteLine($"{r}:{g}:{b}");
					}
				}
			}
			// organize the colors
			var redDom = allcolors.Where(x => x.R > x.G && x.R > x.B).ToList();
			var greenDom = allcolors.Where(x => x.G > x.R && x.G > x.B).ToList();
			var blueDom = allcolors.Where(x => x.B > x.R && x.B > x.G).ToList();
			var grayDom = allcolors.Where(x => x.R == x.G && x.R == x.B).ToList();
			// enhance the gray set
			grayDom = Enumerable.Range(0, 16).Select(x => (byte)(17 * x)).Select(x =>
				System.Windows.Media.Color.FromRgb(x, x, x)).ToList();
			var otherDom = allcolors.Where(x => !redDom.Contains(x) && !greenDom.Contains(x) && !blueDom.Contains(x) && !grayDom.Contains(x)).ToList();

			redDom.Sort((x, y) => ToBrightness(y).CompareTo(ToBrightness(x)));
			blueDom.Sort((x, y) => ToBrightness(y).CompareTo(ToBrightness(x)));
			greenDom.Sort((x, y) => ToBrightness(y).CompareTo(ToBrightness(x)));
			grayDom.Sort((x, y) => ToBrightness(y).CompareTo(ToBrightness(x)));
			otherDom.Sort((x, y) => ToBrightness(y).CompareTo(ToBrightness(x)));

			allcolors.Clear();
			allcolors.AddRange(grayDom);
			allcolors.AddRange(redDom);
			allcolors.AddRange(greenDom);
			allcolors.AddRange(blueDom);
			allcolors.AddRange(otherDom);

			// Add more palettes as needed
			// Use a specific palette, e.g., Tsitsulin
			foreach (var color in allcolors)
			{
				// Convert ScottPlot Color to System.Windows.Media.Color
				var colorButton = new Button
				{
					Background = new System.Windows.Media.SolidColorBrush(color),
					Width = 30,
					Height = 30,
					Margin = new Thickness(5),
					Style = (Style)FindResource("NoHoverButtonStyle"),
				};
				if (mycolor == color)
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
				if (CallMe != null)
					CallMe(this);
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

		private void DoOk(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			// Ensure the current window size is captured
			ViewSettings.Singleton.MainVm.CurrentColorRect = MainViewModel.GetWindowSize(this);
			Close();
		}

		private void DoApply(object sender, RoutedEventArgs e)
		{
			ViewSettings.Singleton.MainVm.CurrentColorRect = MainViewModel.GetWindowSize(this);
			if (CallMe != null)
				CallMe(this);
		}

		private void OnSliderChange(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (sender is Slider slider)
			{
				// Convert the slider value (0-100) to an opacity value (0-255)
				var alphaVal = slider.Value; // 0...1
				if (alphaVal < 99 || NowColor.Contains("#"))
				{
					var currentColor = PlotUtil.StrToColor(NowColor).WithAlpha(alphaVal / 100.0);
					NowColor = PlotUtil.ColorToStr(currentColor); // Update the NowColor property
				}
				ShowOpaque.Content = $"{alphaVal:0.#}%"; // Show the opacity percentage
				if (CallMe != null)
					CallMe(this);
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
				return; // not a ColorPicker

			var color = e.NewValue as string;  // the new entry
			var ecolor = dx.AValue;         // the last entry from program 
			if (color != ecolor && color != null)
			{
				// we get here if use is actually editing the value manually
				// hence the dependency property has changed without the value being 'set'
				var aColor = PlotUtil.StrToColor(color);
				var alpha = 100.0 * aColor.A / 255.0; // get the alpha value
				dx.DoShowOpaque(alpha); // Show the opacity percentage
				if (dx.CallMe != null)
					dx.CallMe(dx);
			}
		}
	}
}
