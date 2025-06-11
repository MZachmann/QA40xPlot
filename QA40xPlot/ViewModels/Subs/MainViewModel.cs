using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QA40xPlot.ViewModels
{
	public class MainViewModel : FloorViewModel
	{
		[JsonIgnore]
		public RelayCommand DoExport { get => new RelayCommand(OnExport); }
		[JsonIgnore]
		public RelayCommand DoSaveCfg { get => new RelayCommand(OnSaveCfg); }
		[JsonIgnore]
		public RelayCommand DoLoadCfg { get => new RelayCommand(OnLoadCfg); }
		[JsonIgnore]
		public AsyncRelayCommand DoPhoto { get => new AsyncRelayCommand(OnPhoto); }


		#region Setters and Getters
		private System.Windows.Media.SolidColorBrush _Background = 
			(new BrushConverter().ConvertFrom("#dce4e4") as SolidColorBrush) ?? System.Windows.Media.Brushes.MintCream;
		private System.Windows.Media.SolidColorBrush _GraphBackground =
			(new BrushConverter().ConvertFrom("#f8f8f8") as SolidColorBrush) ?? System.Windows.Media.Brushes.MintCream;

		[JsonIgnore]
		public System.Windows.Media.SolidColorBrush Background
		{
			get => _Background; 
			set => SetProperty(ref _Background, value);
		}

		[JsonIgnore]
		public System.Windows.Media.SolidColorBrush GraphBackground
		{
			get => _GraphBackground;
			set => SetProperty(ref _GraphBackground, value);
		}

		private string _ProgressMessage = string.Empty;         // type of alert
		[JsonIgnore]
		public string ProgressMessage
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

		private string _FreqRespHdr = "Response";
		[JsonIgnore]
		public string FreqRespHdr
		{
			get => _FreqRespHdr;
			set => SetProperty(ref _FreqRespHdr, value);
		}

		private BaseViewModel? _CurrentView = null;
		[JsonIgnore]
		public BaseViewModel? CurrentView
		{
			get { return _CurrentView; }
			set { 
				SetProperty(ref _CurrentView, value);
				RaisePropertyChanged("HasExport");	// always update this
				}
		}

		private string _CurrentWindowRect = string.Empty;
		public string CurrentWindowRect
		{
			get => _CurrentWindowRect;
			set => SetProperty(ref _CurrentWindowRect, value);
		}

		#endregion

		private static void StopIt()
		{
		}

		private static string FileAddon()
		{
			DateTime now = DateTime.Now;
			string formattedDate = $"{now:yyyy-MM-dd_HH-mm-ss}";
			return formattedDate;
		}

		public static void SaveToPng(Window wnd, string filename)
		{
			var pngData = GraphUtil.CopyAsBitmap(wnd);
			if (pngData != null)
			{
				var png2 = GraphUtil.EncodeBitmap(pngData, new PngBitmapEncoder());
				if (png2 != null)
				{
					File.WriteAllBytes(filename, png2);
				}
			}
		}

		private static string GetWindowSize()
		{
			string rs = string.Empty;
			try
			{
				// Get the width and height of the main window
				if (Application.Current.MainWindow?.WindowState == WindowState.Normal)
				{
					double windowWidth = Application.Current.MainWindow.Width;
					double windowHeight = Application.Current.MainWindow.Height;
					double Xoffset = Application.Current.MainWindow.Left;
					double Yoffset = Application.Current.MainWindow.Top;
					var r = new Rect(Xoffset, Yoffset, windowWidth, windowHeight);
					rs = r.ToString();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error: {ex.Message}");
			}
			return rs;
		}

		// when we load a configuration, we get a string rect for the app window size
		// parse it and set the window size
		public static void SetWindowSize(string sr)
		{
			if(sr.Length == 0)
				return;

			try
			{
				var u = sr.Split(new char[] { ',' }).Select(x => MathUtil.ToDouble(x)).ToArray();
				if (u.Length < 4)
					return;

				if (Application.Current.MainWindow != null && u[2] > 0 && u[3] > 0)
				{
					double screenWidth = SystemParameters.PrimaryScreenWidth;
					double screenHeight = SystemParameters.PrimaryScreenHeight;
					if ((u[0] + u[2]) <= screenWidth && (u[1] + u[3]) <= screenHeight)
					{
						Application.Current.MainWindow.Left = u[0];
						Application.Current.MainWindow.Top = u[1];
						Application.Current.MainWindow.Width = u[2];
						Application.Current.MainWindow.Height = u[3];
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error: {ex.Message}");
			}
		}	

		public void SaveToSettings(string filename)
		{
			var cfgData = ViewSettings.Singleton;
			// Ensure the current window size is captured
			var windsize = GetWindowSize();
			if(windsize.Length > 0)
				ViewSettings.Singleton.Main.CurrentWindowRect = windsize;

			// Serialize the object to a JSON string
			string jsonString = JsonConvert.SerializeObject(cfgData, Formatting.Indented);

			// Write the JSON string to a file
			File.WriteAllText(filename, jsonString);
		}

		public void LoadFromSettings(string filename)
		{
			try
			{
				var cfgData = ViewSettings.Singleton;
				// Read the JSON file into a string
				string jsonContent = File.ReadAllText(filename);
				// Deserialize the JSON string into an object
				var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsonContent);
				if( jsonObject != null)
					ViewSettings.Singleton.GetSettingsFrom(jsonObject);
				var winRect = ViewSettings.Singleton.Main.CurrentWindowRect;
				SetWindowSize(winRect);
				// paint the windows
				if (ViewSettings.Singleton.Main.CurrentView != null)
					ViewSettings.Singleton.Main.CurrentView.RaisePropertyChanged("DsRepaint");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}

		}

		public static void SaveToFrd(string filename)
		{
			var vma = ViewSettings.Singleton.Main.CurrentView;
			bool isImpedance = false;
			DataBlob? vmf = null;
			if (vma is SpectrumViewModel)
			{
				vmf = ((SpectrumViewModel)vma).GetFftData();
			}
			else if (vma is ImdViewModel)
			{
				vmf = ((ImdViewModel)vma).GetFftData();
			}
			else if (vma is FreqRespViewModel)
			{
				vmf = ((FreqRespViewModel)vma).GetFftData();
				isImpedance = ((FreqRespViewModel)vma).TestType == "Impedance";
			}

			if (vmf != null && vmf.LeftData.Count > 0)
			{
				string sout = string.Empty;
				for (int i = 0; i < vmf.LeftData.Count; i++)
				{
					var va = vmf.FreqData[i];
					var vb = vmf.LeftData[i];
					if( isImpedance)
					{
						sout += string.Format("{0:F0}, {1:F4}, {2:F4}\r\n", vmf.FreqData[i], vmf.LeftData[i], 180 * vmf.PhaseData[i] / Math.PI);
					}
					else if ( vmf.PhaseData.Count > 0 )
					{
						sout += string.Format("{0:F0}, {1:F4}, {2:F4}\r\n", vmf.FreqData[i], 20 * Math.Log10(vmf.LeftData[i]), 180 * vmf.PhaseData[i] / Math.PI);
					}
					else
					{
						sout += string.Format("{0:F0}, {1:F4}\r\n", vmf.FreqData[i], 20 * Math.Log10(vmf.LeftData[i]));
					}
				}
				File.WriteAllBytes(filename, System.Text.Encoding.UTF8.GetBytes(sout));
			}
			else
			{
				ViewSettings.Singleton.Main.CurrentView = vma;
			}
		}

		public async Task OnPhoto()        
		{
			DateTime now = DateTime.Now;
			string formattedDate = $"{now:yyyy-MM-dd HH:mm:ss}";
			await SetProgressMessage("QA40xPlot Screen Capture at " + formattedDate);
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QAImg{0}", FileAddon()), // Default file name
				DefaultExt = ".png", // Default file extension
				Filter = "PNG files|*.png|All files|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = saveFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = saveFileDialog.FileName;
				var mainwnd = Application.Current.MainWindow;
				SaveToPng(mainwnd, filename);
			}
			await SetProgressMessage(string.Empty);
		}

		private static void OnExport()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaData{0}", FileAddon()), // Default file name
				DefaultExt = ".frd", // Default file extension
				Filter = "FRD files|*.frd|ZMA files|*.zma|All files|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = saveFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = saveFileDialog.FileName;
				SaveToFrd(filename);
			}
		}

		private void OnSaveCfg()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaSettings{0}", FileAddon()), // Default file name
				DefaultExt = ".cfg", // Default file extension
				Filter = "Settings files|*.cfg|All files|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = saveFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = saveFileDialog.FileName;
				SaveToSettings(filename);
			}

		}

		private void OnLoadCfg()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				FileName = string.Empty, // Default file name
				DefaultExt = ".cfg", // Default file extension
				Filter = "Settings files|*.cfg|All files|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = openFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = openFileDialog.FileName;
				LoadFromSettings(filename);
			}
		}

		// this is called when the user switches to a different tab item
		// the tabs are named for consistent lookup
		public void DoNewTab(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var x = e.AddedItems[0] as TabItem;
			string? u = x?.Name;
			switch (u)
			{
				case "spectrum":
					CurrentView = ViewSettings.Singleton.SpectrumVm;
					break;
				case "intermod":
					CurrentView = ViewSettings.Singleton.ImdVm;
					break;
				case "scope":
					CurrentView = ViewSettings.Singleton.ScopeVm;
					break;
				case "freqresp":
					CurrentView = ViewSettings.Singleton.FreqRespVm;
					break;
				case "tvf":
					CurrentView = ViewSettings.Singleton.ThdFreq;
					break;
				case "tva":
					CurrentView = ViewSettings.Singleton.ThdAmp;
					break;
				case "settings":
					CurrentView = ViewSettings.Singleton.SettingsVm;
					break;
			}
			if(CurrentView != null)
			{
				CurrentView.ForceGraphUpdate();
			}
		}

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
