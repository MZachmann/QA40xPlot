using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using SkiaSharp;
using System.Diagnostics;
using System.IO;
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

		public MainWindow()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.Main;
			this.DataContext = vm;
			vm.ProgressMessage = "Hello, World!";
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
			vm.DoNewTab(sender, e);
		}

		private void OnPhoto(object sender, RoutedEventArgs e)
		{
			if (DataContext != null && DataContext is MainViewModel)
			{
				((MainViewModel)DataContext).DoPhoto(this);
			}
		}

		static bool bdone = false;
		private void DoContentRendered(object sender, EventArgs e)
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

	}
}