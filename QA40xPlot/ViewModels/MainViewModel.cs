using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

		private bool _ShowQA430 = false;
		[JsonIgnore]
		public bool ShowQA430
		{
			get => _ShowQA430;
			set => SetProperty(ref _ShowQA430, value);
		}

		// the maximum width of a tab
		private double _MaxTab = 400;
		[JsonIgnore]
		public double MaxTab
		{
			get => _MaxTab;
			set => SetProperty(ref _MaxTab, value);
		}

		private bool _HasQA430 = false;
		[JsonIgnore]
		public bool HasQA430
		{
			get => _HasQA430;
			set => SetProperty(ref _HasQA430, value);
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
		private double _ScreenDpi = 72;
		[JsonIgnore]
		public double ScreenDpi
		{
			get => _ScreenDpi; 
			set => SetProperty(ref _ScreenDpi, value);
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

		// the sound object if we're using an external device also
		private SoundUtil? _ExternalSound = null;
		[JsonIgnore]
		public SoundUtil? ExternalSound
		{
			get => _ExternalSound;
			set => SetProperty(ref _ExternalSound, value);
		}

		private string _CurrentPaletteRect = string.Empty;
		[JsonIgnore]
		public string CurrentPaletteRect
		{
			get => _CurrentPaletteRect;
			set => SetProperty(ref _CurrentPaletteRect, value);
		}

		private string _CurrentWindowRect = string.Empty;
		public string CurrentWindowRect
		{
			get => _CurrentWindowRect;
			set => SetProperty(ref _CurrentWindowRect, value);
		}

		private string _CurrentWindowState = string.Empty;
		public string CurrentWindowState
		{
			get => _CurrentWindowState;
			set => SetProperty(ref _CurrentWindowState, value);
		}

		private string _CurrentColorRect = string.Empty;
		public string CurrentColorRect
		{
			get => _CurrentColorRect;
			set => SetProperty(ref _CurrentColorRect, value);
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

		public static string GetWindowSize(Window wndw)
		{
			string rs = string.Empty;
			try
			{
				// Get the width and height of the main window
				if (wndw?.WindowState == WindowState.Normal)
				{
					double windowWidth = wndw.Width;
					double windowHeight = wndw.Height;
					double Xoffset = wndw.Left;
					double Yoffset = wndw.Top;
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
		public static void SetWindowSize(Window wndw, string sr)
		{
			if(sr.Length == 0)
				return;

			try
			{
				var u = sr.Split(',').Select(x => MathUtil.ToDouble(x)).ToArray();
				if (u.Length < 4)
					return;

				if (wndw != null && u[2] > 0 && u[3] > 0)
				{
					double screenWidth = SystemParameters.PrimaryScreenWidth;
					double screenHeight = SystemParameters.PrimaryScreenHeight;
					if ((u[0] + u[2]) <= screenWidth && (u[1] + u[3]) <= screenHeight)
					{
						wndw.Left = u[0];
						wndw.Top = u[1];
						wndw.Width = u[2];
						wndw.Height = u[3];
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
			var windsize = GetWindowSize(Application.Current.MainWindow);
			if(windsize.Length > 0)
				ViewSettings.Singleton.MainVm.CurrentWindowRect = windsize;
			ViewSettings.Singleton.MainVm.CurrentWindowState = Application.Current.MainWindow.WindowState.ToString();

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
				var winRect = ViewSettings.Singleton.MainVm.CurrentWindowRect;
				if(winRect.Length > 0)
				{
					SetWindowSize(Application.Current.MainWindow, winRect);
				}
				var winState = ViewSettings.Singleton.MainVm.CurrentWindowState;
				if(winState == "Maximized")
				{
					this.CurrentWindowState = "Maximized";
				}
				// paint the windows
				if (ViewSettings.Singleton.MainVm.CurrentView != null)
					ViewSettings.Singleton.MainVm.CurrentView.RaisePropertyChanged("DsRepaint");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}

		}

		public static void SaveToFrd(string filename)
		{
			var vma = ViewSettings.Singleton.MainVm.CurrentView;
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
				ViewSettings.Singleton.MainVm.CurrentView = vma;
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
				case "freqsweep":	// qa430 opamp tab
					CurrentView = ViewSettings.Singleton.FreqVm;
					CurrentView.RaisePropertyChanged("CheckQA430");  // on new window refresh the list
					break;
				case "settings":
					CurrentView = ViewSettings.Singleton.SettingsVm;
					CurrentView.RaisePropertyChanged("EchoNames");	// on new window refresh the list
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
