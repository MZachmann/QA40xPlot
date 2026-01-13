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
		/// let us turn the Start button on or off
		/// </summary>
		public static readonly DependencyProperty ShowStartProperty =
			DependencyProperty.Register(
				nameof(ShowStart),
				typeof(bool),
				typeof(StartStop),
				new PropertyMetadata(false));

		public bool ShowStart
		{
			get => (bool)GetValue(ShowStartProperty);
			set => SetValue(ShowStartProperty, value);
		}

		/// <summary>
		/// let us turn the Run button on or off
		/// </summary>
		public static readonly DependencyProperty ShowRunProperty =
			DependencyProperty.Register(
				nameof(ShowRun),
				typeof(bool),
				typeof(StartStop),
				new PropertyMetadata(true));

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
