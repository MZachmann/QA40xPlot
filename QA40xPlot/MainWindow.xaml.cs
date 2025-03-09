using Microsoft.Win32;
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

		private uint _ImageCount = 0;
		private uint ImageCount { get { return _ImageCount; } set { _ImageCount = value; } }

		private uint _ExportCount = 0;
		private uint ExportCount { get { return _ExportCount; } set { _ExportCount = value; } }

		public MainWindow()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.Main;
			this.DataContext = vm;
			vm.ProgressMessage = "Hello, World!";
			vm.ScreenDpi = TestGetDpi();
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
					ImageCount++;
				}
			}
		}

		public void SaveToFrd(string filename)
		{
			var vma = ViewSettings.Singleton.SpectrumVm;
			var vmf = vma.GetFftData();
			if (vmf != null && vmf.LeftData.Count > 0)
			{
				string sout = string.Empty;
				for (int i = 0; i < vmf.LeftData.Count; i++)
				{
					var va = vmf.FreqData[i];
					var vb = vmf.LeftData[i];
					sout += string.Format("{0:F0},{1:F4},{2:F4}\r\n", vmf.FreqData[i], 20 * Math.Log10(vmf.LeftData[i]), 20 * Math.Log10(vmf.RightData[i]));
				}
				File.WriteAllBytes(filename, System.Text.Encoding.UTF8.GetBytes(sout));
				ExportCount++;
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
				FileName = String.Format("QAImg{0}", ImageCount), // Default file name
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
		}

		private void OnExport(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaData", ExportCount), // Default file name
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

		private void OnNewTab(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var vm = ViewSettings.Singleton.Main;
			var x = e.AddedItems[0] as TabItem;
			string u = x?.Header.ToString();
			if(u == "Spectrum")
			{
				vm.CurrentView = ViewSettings.Singleton.SpectrumVm;
			}
			else if(u == "Intermodulation")
			{
				vm.CurrentView = ViewSettings.Singleton.ImdVm;
			}
			else
			{
				vm.CurrentView = ViewSettings.Singleton.FreqRespVm;
			}

		}
	}
}