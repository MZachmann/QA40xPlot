using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for XAxisSub.xaml
	/// </summary>
	public partial class XAxisHz : UserControl
	{
		/// </summary>
		public static readonly DependencyProperty ShowLinearProperty =
			DependencyProperty.Register(
				nameof(ShowLinear),
				typeof(bool),
				typeof(XAxisHz),
				new PropertyMetadata(false));

		public bool ShowLinear
		{
			get => (bool)GetValue(ShowLinearProperty);
			set => SetValue(ShowLinearProperty, value);
		}

		public XAxisHz()
		{
			InitializeComponent();
		}
	}
}
