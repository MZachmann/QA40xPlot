using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for CursorExpander.xaml
	/// </summary>
	public partial class CursorExpander : UserControl
	{
		public CursorExpander()
		{
			InitializeComponent();
		}

		// Register the dependency property
		public static readonly DependencyProperty HeadingProperty =
			DependencyProperty.Register(
				"Head",                // Property name
				typeof(string),              // Property type
				typeof(CursorExpander),       // Owner type
				new PropertyMetadata("Frequency")
			);

		// CLR wrapper for the dependency property
		public string Head
		{
			get => (string)GetValue(HeadingProperty);
			set
			{
				// this is called when it gets set by palette editor
				//var oldc = Head;
				SetValue(HeadingProperty, value);
				//if (oldc != Head)
				//	RaisePropertyChanged("Head");
			}
		}


	}
}
