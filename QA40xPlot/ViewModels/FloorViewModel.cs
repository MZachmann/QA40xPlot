using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Views;
using ScottPlot;

namespace QA40xPlot.ViewModels
{
	public abstract class FloorViewModel : INotifyPropertyChanged
	{
		protected bool SetProperty<T>(ref T backingStore, T value,
			[CallerMemberName] string propertyName = "",
			Action? onChanged = null)
		{
			if (EqualityComparer<T>.Default.Equals(backingStore, value))
				return false;

			backingStore = value;
			onChanged?.Invoke();
			OnPropertyChanged(propertyName);
			return true;
		}

		public FloorViewModel()
		{
		}

		private string _ThemeBkgd = string.Empty;
		[JsonIgnore]
		public string ThemeBkgd
		{
			get => _ThemeBkgd;
			set { SetProperty(ref _ThemeBkgd, value); RaisePropertyChanged("ThemeBackground"); }
		}

		public System.Windows.Media.Brush ThemeBackground
		{
			get {
				try
				{
					var x = ViewSettings.Singleton.SettingsVm.ThemeSet;
					var y = ViewSettings.Singleton.SettingsVm.GraphBackClr;
					var color = (x == "Dark") ? System.Windows.Media.Colors.Black : System.Windows.Media.Colors.White;
					var bclr = System.Windows.Media.Color.FromArgb(150, color.R, color.G, color.B);
					return new System.Windows.Media.SolidColorBrush(bclr);
				}
				catch (Exception )
				{
				}
				return System.Windows.Media.Brushes.Red;
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
