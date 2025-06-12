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

		private bool OnColorChange(ColorPicker dlg)
		{
			Color = dlg.NowColor;
			return true;
		}

		private void DoColor_Click(object sender, RoutedEventArgs e)
		{
			var colorPickerDialog = new ColorPicker(Color);
			var originalColor = Color;
			colorPickerDialog.CallMe = OnColorChange;
			if (colorPickerDialog.ShowDialog() == true)
			{
				Color = colorPickerDialog.NowColor;
			}
			else
			{
				Color = originalColor;
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