using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	public partial class ColorBox : UserControl
	{
		public ColorBox()
		{
			InitializeComponent();
		}

		private void DoColor_Click(object sender, RoutedEventArgs e)
		{
			var wasapplied = true;
			while (wasapplied)
			{
				var colorPickerDialog = new ColorPicker(Color);
				if (colorPickerDialog.ShowDialog() == true)
				{
					Color = colorPickerDialog.NowColor;
					wasapplied = colorPickerDialog.WasApplied;
				}
				else
					wasapplied = false;
			}
		}

		// Register the dependency property
		public static readonly DependencyProperty ColorProperty =
			DependencyProperty.Register(
				"Color",                // Property name
				typeof(string),              // Property type
				typeof(ColorBox),       // Owner type
				new FrameworkPropertyMetadata(
					"Red", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
			);

		// CLR wrapper for the dependency property
		public string Color
		{
			get => (string)GetValue(ColorProperty);
			set
			{
				SetValue(ColorProperty, value);
			}
		}

	}
}