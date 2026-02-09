using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for Intro430.xaml
	/// </summary>
	public partial class Intro430 : UserControl
	{
		// DependencyProperty registration for TheText
		public static readonly DependencyProperty TheSGainProperty =
			DependencyProperty.Register(
				nameof(ShowGain),
				typeof(bool),
				typeof(Intro430),
				new FrameworkPropertyMetadata(default(bool)));

		// CLR wrapper for TheText
		public bool ShowGain
		{
			get => (bool)GetValue(TheSGainProperty);
			set => SetValue(TheSGainProperty, value);
		}

		public Intro430()
		{
			InitializeComponent();
		}
	}
}
