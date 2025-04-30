using Microsoft.Win32;
using Newtonsoft.Json;
using QA40x_BareMetal;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using SkiaSharp;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

		// Modify the GetVersionInfo method
		static string GetVersionInfo()
		{
			// Get the current assembly
			Assembly assembly = Assembly.GetExecutingAssembly();
			string productVersion = string.Empty;

			// Get the product version
			object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
			if (attributes.Length > 0)
			{
				AssemblyFileVersionAttribute versionAttribute = (AssemblyFileVersionAttribute)attributes[0];
				productVersion = versionAttribute.Version;
				Debug.WriteLine("Product Version: " + productVersion);
			}
			else
			{
				Debug.WriteLine("Product Version not found.");
			}
			return new string(productVersion.TakeWhile(x => x != '+').ToArray());
		}

		public MainWindow()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.Main;
			this.DataContext = vm;
			vm.ProgressMessage = "Welcome to QA40xPlot v" + GetVersionInfo();
			vm.ScreenDpi = TestGetDpi();
			this.ContentRendered += DoContentRendered;
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

		private void OnNewTab(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var vm = ViewSettings.Singleton.Main;
			if (e.AddedItems.Count > 0)
			{
				var x = e.AddedItems[0];
				if (x is TabItem)
					vm.DoNewTab(sender, e);
			}
		}

		private void OnPhoto(object sender, RoutedEventArgs e)
		{
			if (DataContext != null && DataContext is MainViewModel)
			{
				((MainViewModel)DataContext).DoPhoto(this);
			}
		}

		private void OnHelp(object sender, RoutedEventArgs e)
		{
			try
			{
				var filename = "/Help/HelpSummary.html";
				var dir = System.AppDomain.CurrentDomain.BaseDirectory;
				var uri = new Uri(dir + filename);
				Process.Start(new ProcessStartInfo(dir + filename) { UseShellExecute = true });
				e.Handled = true;
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		static bool bdone = false;
		private void DoContentRendered(object? sender, EventArgs e)
		{
			if (!bdone)
			{
				bdone = true;
				// look for a default config file
				var fpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				fpath += @"\QADefault.cfg";
				if (File.Exists(fpath))
				{
					ViewSettings.Singleton.Main.LoadFromSettings(fpath);
				}
			}
		}

		public static void ForceRepaint()
		{
			// Force the entire window to repaint
			Application.Current.MainWindow.InvalidateVisual();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (QaLowUsb.IsDeviceConnected() == true)
			{
				try
				{
					if( !ViewSettings.IsUseREST)
					{
						// set max attenuation for safety, turns on ATTEN led
						QaComm.SetInputRange(QaLibrary.DEVICE_MAX_ATTENUATION);
						// now close down
						QaComm.Close(true);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			// do my stuff before closing
			if ( ViewSettings.IsSaveOnExit)
			{
				try
				{
					// look for a default config file
					var fpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
					fpath += @"\QADefault.cfg";
					ViewSettings.Singleton.Main.SaveToSettings(fpath);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			base.OnClosing(e);
		}

	}
}