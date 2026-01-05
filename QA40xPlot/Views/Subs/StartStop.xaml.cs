using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for StartStop.xaml
	/// </summary>
	public partial class StartStop : UserControl
	{
		/// <summary>
		/// let us turn the Run button on or off
		/// </summary>
		public static readonly DependencyProperty ShowRunProperty =
			DependencyProperty.Register(
				nameof(ShowRun),
				typeof(bool),
				typeof(StartStop),
				new PropertyMetadata(false));

		public bool ShowRun
		{
			get => (bool)GetValue(ShowRunProperty);
			set => SetValue(ShowRunProperty, value);
		}

		public StartStop()
		{
			InitializeComponent();
		}
	}
}
