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
			if (e.AddedItems == null || e.AddedItems.Count == 0)
				return;

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

		private void OnPhoto(object sender, RoutedEventArgs e)
		{
			if( DataContext != null && DataContext is MainViewModel)
			{
				((MainViewModel)DataContext).DoPhoto(this);
			}
		}
	}
}