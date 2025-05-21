using QA40xPlot.BareMetal;
using ScottPlot;
using System.Windows;
using System.Windows.Media;

namespace QA40xPlot.ViewModels
{
	public class SettingsViewModel : BaseViewModel
	{
		public static List<String> UsbBufferSizes { get => new List<string>() { "2048", "4096", "8192", "16384", "32768", "65536"}; }
		public static List<String> RelayUsageList { get => new List<string>() { "Never", "OnFinish", "OnExit" }; }
		public static List<String> BackColors { get => new List<string>() { "#dce4e4", "#f8f8f8", "#20ffffff",
			"White",
			"MintCream", "LightGray", 
			"LightBlue", "LightGreen", "Lavender", 
			"LightGoldenrodYellow", "LightCoral" }; }

		#region setters and getters
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

		private string _BackgroundClr = BackColors[0];	// slightly darker mintcream
		public string BackgroundClr
		{
			get { return _BackgroundClr; }
			set
			{
				SetProperty(ref _BackgroundClr, value);
				var clr = (SolidColorBrush?)new BrushConverter().ConvertFrom(value);
				if (clr != null) ViewSettings.Singleton.Main.Background = clr;
			}
		}


		private string _GraphBackClr = BackColors[0];  // slightly darker mintcream
		public string GraphBackClr
		{
			get { return _GraphBackClr; }
			set
			{
				SetProperty(ref _GraphBackClr, value);
				var clr = (SolidColorBrush?)new BrushConverter().ConvertFrom(value);
				if (clr != null) ViewSettings.Singleton.Main.GraphBackground = clr;
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
	}
}
