﻿using Newtonsoft.Json;

namespace QA40xPlot.ViewModels
{
	public class MainViewModel : BaseViewModel
	{
#region Setters and Getters
		private String _ProgressMessage = String.Empty;         // type of alert
		[JsonIgnore]
		public String ProgressMessage
		{
			get => _ProgressMessage; set => SetProperty(ref _ProgressMessage, value);
		}
		private int _ProgressAmount = 0;         // type of alert
		[JsonIgnore]
		public int ProgressAmount
		{
			get => _ProgressAmount; set => SetProperty(ref _ProgressAmount, value);
		}
		private int _ProgressMax = 0;
		[JsonIgnore]
		public int ProgressMax
		{
			get => _ProgressMax; set => SetProperty(ref _ProgressMax, value);
		}
		private double _ScreenDpi = 0;
		[JsonIgnore]
		public double ScreenDpi
		{
			get => _ScreenDpi; set => SetProperty(ref _ScreenDpi, value);
		}
		private BaseViewModel? _CurrentView = null;
		[JsonIgnore]
		public BaseViewModel? CurrentView
		{
			get { return _CurrentView; }
			set { 
				SetProperty(ref _CurrentView, value);
				OnPropertyChanged("HasExport");	// always update this
				}
			}

		#endregion

		public async Task SetProgressMessage(String message, int delay = 0)
		{
			ProgressMessage = message;
			if (delay > 0)
				await Task.Delay(delay);
		}

		public void SetupProgressBar(int most)
		{
			ProgressMax = most;
		}

		public async Task SetProgressBar(int progress, int delay = 0)
		{
			ProgressAmount = progress;
			if (delay > 0)
				await Task.Delay(delay);
		}
	}
}
