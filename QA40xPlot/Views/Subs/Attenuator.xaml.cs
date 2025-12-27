using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for Attenuator.xaml
	/// </summary>
	public partial class Attenuator : UserControl, INotifyPropertyChanged
	{
		public Attenuator()
		{
			InitializeComponent();
		}

		// Register the dependency property
		public static readonly DependencyProperty AtStartProperty =
			DependencyProperty.Register(
				"AttnAtStart",                // Property name
				typeof(bool),              // Property type
				typeof(Attenuator),       // Owner type
				new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
			);

		// CLR wrapper for the dependency property
		public bool AttnAtStart
		{
			get => (bool)GetValue(AtStartProperty);
			set { SetValue(AtStartProperty, value); RaisePropertyChanged("AttnAtStart"); }
		}

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = "")
		{
			var changed = PropertyChanged;
			if (changed == null)
				return;
			changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// RaisePropertyChanged
		/// Tell the window a property has changed
		/// </summary>
		/// <param name="propertyName"></param>
		public void RaisePropertyChanged(string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}
		#endregion

	}
}
