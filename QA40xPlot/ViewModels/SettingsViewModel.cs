using QA40xPlot.BareMetal;
using ScottPlot;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace QA40xPlot.ViewModels
{
	public class SettingsViewModel : BaseViewModel
	{
		public static List<String> UsbBufferSizes { get => new List<string>() { "2048", "4096", "8192", "16384", "32768", "65536"}; }
		public static List<String> RelayUsageList { get => new List<string>() { "Never", "OnFinish", "OnExit" }; }
		public static List<String> BackColors { get => new List<string>() { "Transparent",
			"#dce4e4", "#f8f8f8", "#20ffffff",
			"White",
			"MintCream", "LightGray", 
			"DarkGray", "LightGreen", "Lavender" }; }
		public static List<string> ThemeList { get => new List<string> { "None", "Light", "Dark" }; }

		#region setters and getters
		private bool _UseREST;
		public bool UseREST
		{
			get { return _UseREST; }
			set
			{
				SetProperty(ref _UseREST, value);
				// change the entire interface around
				QaComm.SetIODevice(value ? "REST" : "USB");
			}
		}

		private string _BackgroundClr = BackColors[0];	// slightly darker mintcream
		public string BackgroundClr
		{
			get { return _BackgroundClr; }
			set
			{
				SetProperty(ref _BackgroundClr, value);
				try
				{
					// set the background color of the main window
					var clr = (SolidColorBrush?)new BrushConverter().ConvertFrom(value);
					if (clr != null) 
						ViewSettings.Singleton.Main.Background = clr;
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error converting color: {ex.Message}");
				}
			}
		}

		private string _GraphForeground = BackColors[0];  // slightly darker mintcream
		public string GraphForeground
		{
			get { return _GraphForeground; }
			set
			{
				SetProperty(ref _GraphForeground, value);
			}
		}

		private string _ThemeSet = "None";  // slightly darker mintcream
		public string ThemeSet
		{
			get { return _ThemeSet; }
			set
			{
				if(value != _ThemeSet)
				{
					SetProperty(ref _ThemeSet, value);
					// -- set theme
#pragma warning disable WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
					switch (value)
					{
						case "None":
							Application.Current.ThemeMode = ThemeMode.None;
							if( BackgroundClr ==  BackColors[0] )
							{
								BackgroundClr = BackColors[1];	// transparent doesn't work well
							}
							break;
						case "Light":
							Application.Current.ThemeMode = ThemeMode.Light;
							break;
						case "Dark":
							Application.Current.ThemeMode = ThemeMode.Dark;
							BackgroundClr = BackColors[0];
							break;
						case "System":
							Application.Current.ThemeMode = ThemeMode.System;
							BackgroundClr = BackColors[0];
							break;
						default:
							break;
					}
#pragma warning restore WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
					ViewSettings.Singleton.Main.ThemeBkgd = BackgroundClr;
				}
			}
		}

		private string _GraphBackClr = BackColors[0];  // slightly darker mintcream
		public string GraphBackClr
		{
			get { return _GraphBackClr; }
			set
			{
				SetProperty(ref _GraphBackClr, value);
				try
				{
					var clr = (SolidColorBrush?)new BrushConverter().ConvertFrom(value);
					if (clr != null) 
						ViewSettings.Singleton.Main.GraphBackground = clr;
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error converting color: {ex.Message}");
				}
			}
		}



		private string _TestChannel = "Left";
		public string TestChannel
		{
			get { return _TestChannel; }
			set
			{
				SetProperty(ref _TestChannel, value);
			}
		}
		private string _AmplifierLoad = string.Empty;
		public string AmplifierLoad
		{
			get { return _AmplifierLoad; }
			set
			{
				SetProperty(ref _AmplifierLoad, value);
			}
		}
		// how to use the relay... OnExit / OnFinish / Never
		private string _RelayUsage = "OnExit";
		public string RelayUsage
		{
			get { return _RelayUsage; }
			set
			{
				SetProperty(ref _RelayUsage, value);
			}
		}

		private string _SaveOnExit;
		public string SaveOnExit
		{
			get { return _SaveOnExit; }
			set
			{
				SetProperty(ref _SaveOnExit, value);
			}
		}

		private string _UsbBufferSize;
		public string UsbBufferSize
		{
			get { return _UsbBufferSize; }
			set
			{
				SetProperty(ref _UsbBufferSize, value);
			}
		}

		private string _PowerFrequency;
		public string PowerFrequency
		{
			get { return _PowerFrequency; }
			set
			{
				SetProperty(ref _PowerFrequency, value);
			}
		}
		#endregion


		public SettingsViewModel() 
		{
			Name = "Settings";
			_AmplifierLoad = "10";
			_UsbBufferSize = "16384";
			_SaveOnExit = "False";
			_PowerFrequency = "60";
		}
	}
}
