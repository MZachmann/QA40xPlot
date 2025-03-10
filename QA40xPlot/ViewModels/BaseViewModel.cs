﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace QA40xPlot.ViewModels
{
	public abstract class BaseViewModel : INotifyPropertyChanged
	{
		// public INavigation ViewNavigator { get; set; }
		#region Shared Properties
		public static List<String> EndFrequencies { get => new List<string> { "1000", "2000", "5000", "10000", "20000", "50000", "100000" }; }
		public static List<String> StartFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "5000", "10000" }; }
		public static List<String> TopDbs { get => new List<string> { "-80", "-50", "0", "20", "50", "100" }; }
		public static List<String> BottomDbs { get => new List<string> { "-200", "-180", "-160", "-140", "-120", "-100", "-50", "0" }; }
		public static List<String> StartPercents { get => new List<string> { "100", "10", "1", "0.1", "0.01" }; }
		public static List<String> EndPercents { get => new List<string> { "0.1", "0.01", "0.001", "0.0001", "0.00001", "0.000001" }; }
		public static List<String> SampleRates { get => new List<string> { "48000", "96000", "192000", "384000" }; }
		public static List<String>	FftSizes { get => new List<string> { "64K", "128K", "256K", "512K", "1024K" }; }
		public static List<uint>	FftActualSizes { get => new List<uint> { 65536, 131072, 262144, 524288, 1048576 }; }
		#endregion
		#region Setters and Getters
		private bool _IsRunning = false;         // type of alert
		[JsonIgnore]
		public bool IsRunning
		{
			get { return _IsRunning; }
			set { SetProperty(ref _IsRunning, value); IsNotRunning = !value; }
		}
		private bool _IsNotRunning = true;         // type of alert
		[JsonIgnore]
		public bool IsNotRunning
		{
			get { return _IsNotRunning; }
			private set { SetProperty(ref _IsNotRunning, value); }
		}
		#endregion

		bool isBusy = false;
		[JsonIgnore]
		public bool IsBusy
		{
			get { return isBusy; }
			set { SetProperty(ref isBusy, value); }
		}

		private bool _HasExport = false;
		[JsonIgnore]
		public bool HasExport
		{
			get { return _HasExport; }
			set { SetProperty(ref _HasExport, value); }
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

		public 	BaseViewModel()
		{
			HasExport = false;
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
