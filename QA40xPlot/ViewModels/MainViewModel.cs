using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;

namespace QA40xPlot.ViewModels
{
	public class MainViewModel : BaseViewModel
	{
		[JsonIgnore]
		public RelayCommand DoExport { get => new RelayCommand(OnExport); }
		[JsonIgnore]
		public RelayCommand DoSave { get => new RelayCommand(OnSave); }
		[JsonIgnore]
		public RelayCommand DoLoad { get => new RelayCommand(OnLoad); }

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

		private bool _ShowDataPercent = false;
		[JsonIgnore]
		public bool ShowDataPercent
		{
			get => _ShowDataPercent;
			set => SetProperty(ref _ShowDataPercent, value);
		}

		private string _FreqRespHdr = "Impedance";
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
				OnPropertyChanged("HasExport");	// always update this
				}
			}

		#endregion

		private static void StopIt(object parameter)
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

		public static void SaveToFrd(string filename)
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

		public async void DoPhoto(Window parent)        
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
				SaveToPng(parent, filename);
			}
			await SetProgressMessage(string.Empty);
		}

		private static void OnExport(object parameter)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaData{0}", FileAddon()), // Default file name
				DefaultExt = ".frd", // Default file extension
				Filter = "FRD files|*.frd|All files|*.*" // Filter files by extension
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

		private void OnSave(object parameter)
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

		private void OnLoad(object sender)
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

		private void OnNewTab(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var vm = ViewSettings.Singleton.Main;
			var x = e.AddedItems[0] as TabItem;
			string u = x?.Header.ToString();
			switch (u)
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
