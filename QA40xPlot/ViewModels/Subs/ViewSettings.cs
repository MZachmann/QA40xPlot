

// this aggregates the settings somewhere static, which does mean only one of each

using System.Reflection;
using System.Windows;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using ScottPlot;

namespace QA40xPlot.ViewModels
{
	public class ViewSettings
	{
		private readonly Dictionary<string, string> _ProductTitle = new Dictionary<string, string>() { { "Name", "QA40xPlot" }, { "Version", "0.12"} };
		public Dictionary<string, string> Product { get { return _ProductTitle; } private set {; } }
		public static ViewSettings Singleton { get; private set; } = new ViewSettings();
		public SpectrumViewModel SpectrumVm { get; private set; }
		public ImdViewModel ImdVm { get; private set; }
		public ThdFreqViewModel ThdFreq { get; private set; }
		public ThdAmpViewModel ThdAmp { get; private set; }
		public FreqRespViewModel FreqRespVm { get; private set; }
		public ScopeViewModel ScopeVm { get; private set; }
		public MainViewModel Main { get; private set; }
		public SettingsViewModel SettingsVm { get; private set; }
		// these are output only and don't need serializing
		[JsonIgnore]
		public List<object> MyTabLibrary { get; private set; } = new List<object>();
		[JsonIgnore]
		public ScopeInfoViewModel ScopeInfoLeft { get; private set; }
		[JsonIgnore]
		public ScopeInfoViewModel ScopeInfoRight { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel ChannelLeft { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel ChannelRight { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelLeft { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelRight { get; private set; }
		[JsonIgnore]
		public DataDescript TabDefs { get; set; }

		[JsonIgnore]
		public static double AmplifierLoad { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.AmplifierLoad, 0); }
		[JsonIgnore]
		public static string ExternalGain { get => ViewSettings.Singleton.SettingsVm.ExternalGain; }

		/// <summary>
		/// returns if left channel is our voltage output math
		/// </summary>
		[JsonIgnore]
		public static bool IsTestLeft { get => ViewSettings.Singleton.SettingsVm.TestChannel != "Right"; }

		[JsonIgnore]
		public static bool IsSaveOnExit { get => ViewSettings.Singleton.SettingsVm?.SaveOnExit == "True"; }
		[JsonIgnore]
		public static bool IsUseREST { get => ViewSettings.Singleton.SettingsVm?.UseREST == true; }

		public void GetSettingsFrom( Dictionary<string, Dictionary<string,object>> vws)
		{
			Util.GetPropertiesFrom(vws,"Main",Main);
			Util.GetPropertiesFrom(vws,"SpectrumVm",SpectrumVm);
			Util.GetPropertiesFrom(vws,"ImdVm",ImdVm);
			Util.GetPropertiesFrom(vws,"ThdAmp",ThdAmp);
			Util.GetPropertiesFrom(vws,"ThdFreq",ThdFreq);
			Util.GetPropertiesFrom(vws,"FreqRespVm",FreqRespVm);
			Util.GetPropertiesFrom(vws,"SettingsVm", SettingsVm);
			Util.GetPropertiesFrom(vws,"ScopeVm", ScopeVm);
		}

		public ViewSettings() 
		{
			SettingsVm = new SettingsViewModel();
			Main = new MainViewModel();
			ChannelLeft = new ThdChannelViewModel();
			ChannelRight = new ThdChannelViewModel();
			ScopeInfoLeft = new ScopeInfoViewModel();
			ScopeInfoRight = new ScopeInfoViewModel();
			ImdChannelLeft = new ImdChannelViewModel();
			ImdChannelRight = new ImdChannelViewModel();
			SpectrumVm = new SpectrumViewModel();
			ImdVm = new ImdViewModel();
			ThdAmp = new ThdAmpViewModel();
			ThdFreq = new ThdFreqViewModel();
			FreqRespVm = new FreqRespViewModel();
			ScopeVm = new ScopeViewModel();
			TabDefs = new DataDescript();
		}
	}
}
