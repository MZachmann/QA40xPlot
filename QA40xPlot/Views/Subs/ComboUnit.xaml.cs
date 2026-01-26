using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ComboUnit.xaml
	/// </summary>
	public partial class ComboUnit : UserControl
	{
		public ComboBox ValueComboBox { get; }

		// DependencyProperty registration for TheText
		public static readonly DependencyProperty TheTextProperty =
			DependencyProperty.Register(
				nameof(TheText),
				typeof(string),
				typeof(ComboUnit),
				new FrameworkPropertyMetadata(
					default(string),
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		// DependencyProperty registration for TheSource
		public static readonly DependencyProperty TheSourceProperty =
			DependencyProperty.Register(
				nameof(TheSource),
				typeof(List<string>),
				typeof(ComboUnit),
				new FrameworkPropertyMetadata(
					default(List<string>),
					FrameworkPropertyMetadataOptions.None));

		// DependencyProperty registration for IsEditable
		public static readonly DependencyProperty IsEditableProperty =
			DependencyProperty.Register(
				nameof(IsEditable),
				typeof(bool),
				typeof(ComboUnit),
				new FrameworkPropertyMetadata(
					default(bool),
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		// CLR wrapper for TheText
		public string TheText
		{
			get => (string)GetValue(TheTextProperty);
			set => SetValue(TheTextProperty, value);
		}

		// CLR wrapper for TheSource
		public List<string> TheSource
		{
			get => (List<string>)GetValue(TheSourceProperty);
			set => SetValue(TheSourceProperty, value);
		}

		// CLR wrapper for IsEditable
		public bool IsEditable
		{
			get => (bool)GetValue(IsEditableProperty);
			set => SetValue(IsEditableProperty, value);
		}

		public ComboUnit()
		{
			InitializeComponent();
			ValueComboBox = cbCombo;
		}

		private void OnMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			e.Handled = true;
		}
	}
}
