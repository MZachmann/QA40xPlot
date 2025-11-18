using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	public partial class UpDownControl : UserControl
	{
		public UpDownControl()
		{
			InitializeComponent();
		}

		private void UpButton_Click(object sender, RoutedEventArgs e)
		{
			var val = Value; // MathUtil.ToInt(Value, 0);
			if (val < Maximum)
			{
				val++;
			}
			else
			{
				val = Maximum;
			}
			Value = val;// .ToString();
		}

		private void DownButton_Click(object sender, RoutedEventArgs e)
		{
			var val = Value; // MathUtil.ToInt(Value, 0);
			if (val > Minimum)
			{
				val--;
			}
			else
			{
				val = Minimum;
			}
			Value = val; //.ToString();
		}
		// Register the dependency property
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				"Value",                // Property name
				typeof(int),              // Property type
				typeof(UpDownControl),       // Owner type
								new FrameworkPropertyMetadata(
					0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
			);

		public static readonly DependencyProperty MinimumProperty =
			DependencyProperty.Register(
				"Minimum",                // Property name
				typeof(int),              // Property type
				typeof(UpDownControl),       // Owner type
				new PropertyMetadata(        // Default value and callback
					0)
			);
		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register(
				"Maximum",                // Property name
				typeof(int),              // Property type
				typeof(UpDownControl),       // Owner type
				new PropertyMetadata(        // Default value and callback
					100)
			);

		// CLR wrapper for the dependency property
		public int Value
		{
			get => (int)GetValue(ValueProperty);
			set
			{
				value = Math.Max(Minimum, Math.Min(Maximum, value));
				SetValue(ValueProperty, value);
			}
		}

		public int Minimum
		{
			get => (int)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, Minimum);
		}

		public int Maximum
		{
			get => (int)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, Maximum);
		}

	}
}