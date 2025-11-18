using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	public partial class ColorBox : UserControl, INotifyPropertyChanged
	{
		public ColorBox()
		{
			InitializeComponent();
		}

		// called by the picker when a value changes
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
			colorPickerDialog.DataContext = this.DataContext;   // so background color is right
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
				// this is called when it gets set by palette editor
				var oldc = Color;
				SetValue(ColorProperty, value);
				if (oldc != Color)
					RaisePropertyChanged("Color");
			}
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