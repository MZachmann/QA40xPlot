using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.BareMetal;
using QA40xPlot.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace QA40xPlot.ViewModels
{
	public class SettingsViewModel : BaseViewModel
	{
		public static List<String> UsbBufferSizes { get => new List<string>() { "2048", "4096", "8192", "16384", "32768", "65536"}; }
		public static List<String> RelayUsageList { get => new List<string>() { "Never", "OnFinish", "OnExit" }; }
		public static List<String> NoiseWeightList { get => new List<string>() { "Z", "A", "C" }; }
		public static List<String> EchoTypes { get => new List<string>() { "QA40x", "WinDevice", "Both" }; }
		public static List<String> BackColors { get => new List<string>() { "Transparent", "#dce4e4", "#60ffffff",
			"#f8f8f8", "White",
			"MintCream", "LightGray", 
			"DarkGray", "LightGreen", "Lavender" }; }
		public static List<String> PlotColors
		{
			get => new List<string>() { "Transparent",
			"Red", "Orange", "Yellow", "Blue", "Green", "Indigo", "Violet",
			"Black", "White", "Gray", "Teal", "Cyan", "Magenta", "Pink",
			"DarkRed", "DarkOrange", "DarkGreen", "DarkBlue", "DarkViolet"
			};
		}
		public static List<string> ThemeList { get => new List<string> { "None", "Light", "Dark", "System" }; }

		[JsonIgnore]
		public RelayCommand DoMicCompensate { get => new RelayCommand(FindMicCompensate); }
		[JsonIgnore]
		public RelayCommand DoEditPlotColors { get => new RelayCommand(EditPlotColors); }
		[JsonIgnore]
		public RelayCommand ClearMicCompensate { get => new RelayCommand(DelMicCompensate); }


		#region temporary setters

		// always start this at blank (none)
		private string _AddDistortion = string.Empty;
		[JsonIgnore]
		public string AddDistortion
		{
			get { return _AddDistortion; }
			set
			{
				SetProperty(ref _AddDistortion, value);
			}
		}
		#endregion

		#region setters and getters
		private string _PaletteColors = " blue, red, orange, green, violet," +
			"yellow, pink, teal, cyan, #00c6f8," +
			"#878500, #00a76c, #f6da9c, #ff5caa, #8accff, #4bff4b, #6efff4, #edc1f5";

		public string PaletteColors 
		{
			get => _PaletteColors; 
			set => SetProperty(ref _PaletteColors, value);
		}

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

		private int _EchoWaves = (int)SoundUtil.EchoNone;
		public int EchoWaves
		{
			get { return _EchoWaves; }
			set
			{
				SetProperty(ref _EchoWaves, value);
			}
		}

		private string _MicCompFile = string.Empty;
		public string MicCompFile {
			get => _MicCompFile;
			set => SetProperty(ref _MicCompFile, value);
		}

		private string _BackgroundClr = BackColors[1];	// mintcream
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

		private string _GraphForeground = BackColors[2];  // brighten more
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

		private string _GraphBackClr = BackColors[2];  // brighten
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

		// if the user has an external gain/attenuator, this is the gain value in power reduction or increase
		// a negative value means attenuation, a positive value means gain
		private string _ExternalGain = "0"; // 0, 10, 20, 30, 40, 50 dB
		public string ExternalGain
		{
			get { return _ExternalGain; }
			set { SetProperty(ref _ExternalGain, value); }	// copy to the globally visible value
		}
		// weighting method for noise analysis
		private string _NoiseWeight = "Z"; // "Z","A", "C"
		public string NoiseWeight
		{
			get { return _NoiseWeight; }
			set { SetProperty(ref _NoiseWeight, value); }  // copy to the globally visible value
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

		public void EditPlotColors()
		{
			//var vm = ViewSettings.Singleton.SettingsVm;
			var paletteDialog = new PaletteSet();
			if (paletteDialog.ShowDialog() == true)
			{
				//Color = colorPickerDialog.NowColor;
			}
			else
			{
				//Color = originalColor;
			}
		}

		public void FindMicCompensate()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				FileName = System.IO.Path.GetFileName( MicCompFile), // Default file name
				DefaultExt = ".txt", // Default file extension
				Filter = "Mic files|*.txt|All files|*.*" // Filter files by extension
			};
			// Show save file dialog box
			bool? result = openFileDialog.ShowDialog();
			if(result == true)
			{
				// Get the file name and display in the TextBox
				MicCompFile = openFileDialog.FileName;
				Debug.WriteLine($"MicCompFile set to: {MicCompFile}");
			}
		}

		public void DelMicCompensate()
		{
			MicCompFile = string.Empty;
		}
	}
}
