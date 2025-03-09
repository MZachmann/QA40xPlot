using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;


namespace QA40xPlot
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		[DllImport("User32.dll")]
		public static extern uint GetDpiForWindow([In] IntPtr hmonitor);

		[DllImport("User32.dll")]
		public static extern uint GetDpiForSystem();

		public MainWindow()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.Main;
			this.DataContext = vm;
			vm.ProgressMessage = "Hello, World!";
			vm.ScreenDpi = TestGetDpi();
		}

		private string FileAddon()
		{
			DateTime now = DateTime.Now;
			string formattedDate = $"{now:yyyy-MM-dd_HH-mm-ss}";
			return formattedDate;

		}

		public uint TestGetDpi()
		{
			var systemDpi = GetDpiForSystem();
			var currentWindowDpi = GetCurrentWindowDpi();
			return systemDpi;
		}
		private uint GetCurrentWindowDpi()
		{
			var window = GetWindow(this);
			var wih = new WindowInteropHelper(window ?? throw new InvalidOperationException());
			var hWnd = wih.EnsureHandle();
			return GetDpiForWindow(hWnd);
		}

		public void SaveToPng(string filename)
		{
			var pngData = GraphUtil.CopyAsBitmap(this);
			if (pngData != null)
			{
				var png2 = GraphUtil.EncodeBitmap(pngData, new PngBitmapEncoder());
				if (png2 != null)
				{
					File.WriteAllBytes(filename, png2);
				}
			}
		}

		public void SaveToSettings(string filename)
		{
			var pngData = ViewSettings.Singleton;
			// Serialize the object to a JSON string
			string jsonString = JsonConvert.SerializeObject(pngData, Formatting.Indented);

			// Write the JSON string to a file
			File.WriteAllText(filename, jsonString);
		}

		public void LoadFromSettings(string filename)
		{
			try
			{
				var pngData = ViewSettings.Singleton;
				// Read the JSON file into a string
				string jsonContent = File.ReadAllText(filename);
				// Deserialize the JSON string into an object
				var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsonContent);
				ViewSettings.Singleton.GetSettingsFrom(jsonObject);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}

		}

		public void SaveToFrd(string filename)
		{
			var vma = ViewSettings.Singleton.Main.CurrentView;
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
			}

			if (vmf != null && vmf.LeftData.Count > 0)
			{
				string sout = string.Empty;
				for (int i = 0; i < vmf.LeftData.Count; i++)
				{
					var va = vmf.FreqData[i];
					var vb = vmf.LeftData[i];
					//sout += string.Format("{0:F0},{1:F4},{2:F4}\r\n", vmf.FreqData[i], 20 * Math.Log10(vmf.LeftData[i]), 20 * Math.Log10(vmf.RightData[i]));
					sout += string.Format("{0:F0},{1:F4}\r\n", vmf.FreqData[i], 20 * Math.Log10(vmf.LeftData[i]));
				}
				File.WriteAllBytes(filename, System.Text.Encoding.UTF8.GetBytes(sout));
			}
			else
			{
				ViewSettings.Singleton.Main.CurrentView = vma;
			}
		}

		private async void OnPhoto(object sender, RoutedEventArgs e)
		{
			var vm = ViewModels.ViewSettings.Singleton.Main;
			DateTime now = DateTime.Now;
			string formattedDate = $"{now:yyyy-MM-dd HH:mm:ss}";
			await vm.SetProgressMessage("QA40xPlot Screen Capture at " + formattedDate); 
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QAImg{0}", FileAddon()), // Default file name
				DefaultExt = ".png", // Default file extension
				Filter = "PNG files (.png)|*.png|All files (*.*)|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = saveFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = saveFileDialog.FileName;
				SaveToPng(filename);
			}
			await vm.SetProgressMessage(string.Empty);
		}

		private void OnExport(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaData{0}", FileAddon()), // Default file name
				DefaultExt = ".frd", // Default file extension
				Filter = "FRD files (.frd)|*.frd|All files (*.*)|*.*" // Filter files by extension
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

		private void OnSave(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaSettings{0}", FileAddon()), // Default file name
				DefaultExt = ".cfg", // Default file extension
				Filter = "Settings files (.cfg)|*.cfg|All files (*.*)|*.*" // Filter files by extension
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

		private void OnLoad(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				FileName = String.Format("QaSettings{0}", FileAddon()), // Default file name
				DefaultExt = ".cfg", // Default file extension
				Filter = "Settings files (.cfg)|*.cfg|All files (*.*)|*.*" // Filter files by extension
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

		private void OnNewTab(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var vm = ViewSettings.Singleton.Main;
			var x = e.AddedItems[0] as TabItem;
			string u = x?.Header.ToString();
			switch(u)
			{
				case "Spectrum":
					vm.CurrentView = ViewSettings.Singleton.SpectrumVm;
					break;
				case "Intermodulation":
					vm.CurrentView = ViewSettings.Singleton.ImdVm;
					break;
				case "Frequency Response":
					vm.CurrentView = ViewSettings.Singleton.FreqRespVm;
					break;
				case "THD vs Frequency":
					vm.CurrentView = ViewSettings.Singleton.ThdFreq;
					break;
				case "THD vs Amplitude":
					vm.CurrentView = ViewSettings.Singleton.ThdAmp;
					break;
			}
		}

	}
}