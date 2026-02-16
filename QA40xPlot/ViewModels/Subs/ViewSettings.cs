using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Diagnostics;

// this aggregates the settings somewhere static, which does mean only one of each
namespace QA40xPlot.ViewModels
{
	public class ViewSettings
	{
		public static ViewSettings Singleton { get; private set; } = new ViewSettings();
		private readonly List<BaseViewModel> ViewModelList = new();
		private readonly Dictionary<string, string> _ProductTitle = new Dictionary<string, string>() { { "Name", "QA40xPlot" }, { "Version", "0.30" } };
		public Dictionary<string, string> Product { get { return _ProductTitle; } private set {; } }
		public SpectrumViewModel SpectrumVm { get; private set; }
		public ImdViewModel ImdVm { get; private set; }
		//public ThdFreqViewModel ThdFreq { get; private set; }
		//public ThdAmpViewModel ThdAmp { get; private set; }
		public FreqRespViewModel FreqRespVm { get; private set; }
		public FrQa430ViewModel FrQa430Vm { get; private set; }
		public FreqSweepViewModel FreqVm { get; private set; }
		public AmpSweepViewModel AmpVm { get; private set; }
		public ScopeViewModel ScopeVm { get; private set; }
		public MainViewModel MainVm { get; private set; }
		public SettingsViewModel SettingsVm { get; private set; }
		// these are output only and don't need serializing
		[JsonIgnore]
		public ScopeInfoViewModel ScopeInfoLeft { get; private set; }
		[JsonIgnore]
		public ScopeInfoViewModel ScopeInfoRight { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel ChannelLeft { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel ChannelRight { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel Channel2Left { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel Channel2Right { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelLeft { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelRight { get; private set; }
		[JsonIgnore]
		public DataDescript TabDefs { get; set; }

		[JsonIgnore]
		public static double AmplifierLoad { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.AmplifierLoad, 8); }
		[JsonIgnore]
		public static double AddonDistortion { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.AddDistortion, 0); }
		[JsonIgnore]
		public static string ExternalGain { get => ViewSettings.Singleton.SettingsVm.ExternalGain; }
		[JsonIgnore]
		public static double SafetyMargin { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.SafetyMargin, 5); }
		[JsonIgnore]
		public static string NoiseWeight { get => ViewSettings.Singleton.SettingsVm.NoiseWeight; }
		[JsonIgnore]
		public static int WaveEchoes { get => ViewSettings.Singleton.SettingsVm.EchoDevices; }
		[JsonIgnore]
		public static float Thickness { get => (float)MathUtil.ToDouble(ViewSettings.Singleton?.SettingsVm?.LineThickness, 0); }

		/// <summary>
		/// returns if left channel is our voltage output math
		/// </summary>
		[JsonIgnore]
		public static bool IsTestLeft { get => ViewSettings.Singleton.SettingsVm.TestChannel != "Right"; }

		[JsonIgnore]
		public static bool IsSaveOnExit { get => ViewSettings.Singleton.SettingsVm?.SaveOnExit == "True"; }
		[JsonIgnore]
		public static bool IsUseREST { get => ViewSettings.Singleton.SettingsVm?.UseREST == true; }
		[JsonIgnore]
		public static double NoiseBandwidth { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.NoiseBandwidthStr, 20000); }
		[JsonIgnore]
		public static double MinNoiseFrequency { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.MinNoiseFreq, 20); }
		[JsonIgnore]
		public static double NoiseRefresh { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.NoiseRefreshStr, 200); }

		private bool IsValidVersion(Dictionary<string, Dictionary<string, object>> vws)
		{
			string[] validSet = { "0.20", "0.30" };
			bool isValid = false;
			try
			{
				var vers = vws["Product"]["Version"];
				// for now require this specific product version
				if (vers != null && validSet.Contains(vers.ToString()))
				{
					isValid = true;
				}
			}
			catch
			{
				Debug.WriteLine("Config version not found");
			}
			return isValid;
		}

		public int GetSettingsFrom(Dictionary<string, Dictionary<string, object>> vws)
		{
			int rslt = 0;
			var useThis = IsValidVersion(vws);
			if (!useThis)
			{
				rslt = 1;   // config mismatch
				Debug.WriteLine("Config version mismatch, skipping");
				return rslt;
			}
			// here the object name must be the same as the string used to store it
			Util.GetPropertiesFrom(vws, "SpectrumVm", SpectrumVm);
			Util.GetPropertiesFrom(vws, "ImdVm", ImdVm);
			//Util.GetPropertiesFrom(vws, "ThdAmp", ThdAmp);
			//Util.GetPropertiesFrom(vws, "ThdFreq", ThdFreq);
			Util.GetPropertiesFrom(vws, "FreqRespVm", FreqRespVm);
			Util.GetPropertiesFrom(vws, "FrQa430Vm", FrQa430Vm);
			Util.GetPropertiesFrom(vws, "ScopeVm", ScopeVm);
			Util.GetPropertiesFrom(vws, "FreqVm", FreqVm);
			Util.GetPropertiesFrom(vws, "AmpVm", AmpVm);
			Util.GetPropertiesFrom(vws, "MainVm", MainVm);
			Util.GetPropertiesFrom(vws, "SettingsVm", SettingsVm);  // this will update global settings last which makes sense
			return rslt;
		}

		public ViewSettings()
		{
			SettingsVm = new SettingsViewModel();
			MainVm = new MainViewModel();
			ChannelLeft = new ThdChannelViewModel();
			ChannelRight = new ThdChannelViewModel();
			Channel2Left = new ThdChannelViewModel();
			Channel2Right = new ThdChannelViewModel();
			ScopeInfoLeft = new ScopeInfoViewModel();
			ScopeInfoRight = new ScopeInfoViewModel();
			ImdChannelLeft = new ImdChannelViewModel();
			ImdChannelRight = new ImdChannelViewModel();
			SpectrumVm = new SpectrumViewModel();
			ImdVm = new ImdViewModel();
			//ThdAmp = new ThdAmpViewModel();
			//ThdFreq = new ThdFreqViewModel();
			FreqRespVm = new FreqRespViewModel();
			FrQa430Vm = new FrQa430ViewModel();
			ScopeVm = new ScopeViewModel();
			FreqVm = new FreqSweepViewModel();
			AmpVm = new AmpSweepViewModel();
			TabDefs = new DataDescript();

			// enumerate the Pages other than viewsettings
			ViewModelList.Add(SpectrumVm);
			ViewModelList.Add(ImdVm);
			ViewModelList.Add(ScopeVm);
			ViewModelList.Add(FreqVm);
			ViewModelList.Add(AmpVm);
			ViewModelList.Add(FreqRespVm);
			ViewModelList.Add(FrQa430Vm);
		}

		public void CopyAboutToAll(DataDescript desc)
		{
			foreach (var vm in ViewModelList)
			{
				// copy the datadescript stuff into the viewmodel base
				vm.CopyDescript(vm, desc);
			}
		}
	}
}
