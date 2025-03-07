using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QA40xPlot.ViewModels
{
	public abstract class BaseViewModel : INotifyPropertyChanged
	{
		// public INavigation ViewNavigator { get; set; }
	#region Shared Properties
		public List<String> EndFrequencies { get => new List<string> { "1000", "2000", "5000", "10000", "20000", "50000", "100000" }; }
		public List<String> StartFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500" }; }
		public List<String> TopDbs { get => new List<string> { "-80", "-50", "0", "20", "50", "100" }; }
		public List<String> BottomDbs { get => new List<string> { "-200", "-180", "-160", "-140", "-120", "-100", "-50", "0" }; }
		public List<String> StartPercents { get => new List<string> { "100", "10", "1", "0.1", "0.01" }; }
		public List<String> EndPercents { get => new List<string> { "0.1", "0.01", "0.001", "0.0001", "0.00001", "0.000001" }; }
		public List<String> SampleRates { get => new List<string> { "48000", "96000", "192000", "384000" }; }
		public List<String>	FftSizes { get => new List<string> { "64K", "128K", "256K", "512K", "1024K" }; }
		public List<uint>	FftActualSizes { get => new List<uint> { 65536, 131072, 262144, 524288, 1048576 }; }
		#endregion

		bool isBusy = false;
		public bool IsBusy
		{
			get { return isBusy; }
			set { SetProperty(ref isBusy, value); }
		}

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
		/// Tell the window this has changed
		/// </summary>
		/// <param name="propertyName"></param>
		protected void RaisePropertyChanged(string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}
		#endregion
	}
}
