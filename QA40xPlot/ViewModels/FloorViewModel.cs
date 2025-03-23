using System.ComponentModel;
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
		protected void RaisePropertyChanged(string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}
		#endregion
	}
}
