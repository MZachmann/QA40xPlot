using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;


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

        public MainWindow()
        {
            InitializeComponent();
            var vm = ViewModels.ViewSettings.Singleton.Main;
            this.DataContext = vm;
            vm.ProgressMessage = "Hello, World!";
			vm.ScreenDpi = TestGetDpi();
		}
    }
}