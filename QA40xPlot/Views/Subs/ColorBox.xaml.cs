using Newtonsoft.Json.Linq;
using ScottPlot;
using System.ComponentModel;
using System.Diagnostics;
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
			XColor = (Color.ToString() == "Transparent") ? "∅" : string.Empty;
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
				typeof(string),         // Property type
				typeof(ColorBox),       // Owner type
				new FrameworkPropertyMetadata(
					"Red", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColorChanged)
			);

		private static void ColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var box = d as ColorBox;
			Debug.Assert(box != null, "Not a colorbox");
			if(box != null)
			{
				var u = box.XColor;
				box.XColor = (e.NewValue.ToString() == "Transparent") ? "∅" : string.Empty;
				if(e.NewValue != e.OldValue)
					box.RaisePropertyChanged("Color");
			}
		}

		private string _XColor = "x";
		public string XColor
		{
			get => _XColor;
			set { _XColor = value; RaisePropertyChanged("XColor"); }
		}
		// CLR wrapper for the dependency property
		public string Color
		{
			get => (string)GetValue(ColorProperty);
			set
			{
				// this is called when it gets set by palette editor
				var oldc = Color;
				SetValue(ColorProperty, value);
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