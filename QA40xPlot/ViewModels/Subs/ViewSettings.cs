

// this aggregates the settings somewhere static, which does mean only one of each

using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using Newtonsoft.Json;
using QA40xPlot.Libraries;
using QA40xPlot.Views;

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
		public ThdChannelViewModel ChannelLeft { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel ChannelRight { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelLeft { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelRight { get; private set; }

		public static void GetPropertiesFrom(Dictionary<string, Dictionary<string, object>> vwsIn, string name, object dest)
		{
			if (vwsIn == null || dest == null)
				return;
			if (!vwsIn.ContainsKey(name))
				return;
			Dictionary<string, object> vws = (Dictionary<string, object>)vwsIn[name];

			Type type = dest.GetType();
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			try
			{
				foreach (PropertyInfo property in properties)
				{
					if (property.CanRead && property.CanWrite)
					{
						if (vws.ContainsKey(property.Name))
						{
							object value = vws[property.Name];
							try
							{
								//Debug.WriteLine("Property " + property.Name);
								property.SetValue(dest, Convert.ChangeType(value, property.PropertyType));
							}
							catch (Exception ) { }    // for now ignore this
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information); 
			}

		}

		[JsonIgnore]
		public static double AmplifierLoad { get => MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.AmplifierLoad, 0); }
		/// <summary>
		/// returns if left channel is our voltage output math
		/// </summary>
		[JsonIgnore]
		public static bool IsTestLeft { get => ViewSettings.Singleton.SettingsVm.TestChannel != "Right"; }

		[JsonIgnore]
		public static bool IsSaveOnExit { get => ViewSettings.Singleton.SettingsVm?.SaveOnExit == "True"; }

		public void GetSettingsFrom( Dictionary<string, Dictionary<string,object>> vws)
		{
			GetPropertiesFrom(vws,"Main",Main);
			GetPropertiesFrom(vws,"SpectrumVm",SpectrumVm);
			GetPropertiesFrom(vws,"ImdVm",ImdVm);
			GetPropertiesFrom(vws,"ThdAmp",ThdAmp);
			GetPropertiesFrom(vws,"ThdFreq",ThdFreq);
			GetPropertiesFrom(vws,"FreqRespVm",FreqRespVm);
			GetPropertiesFrom(vws,"SettingsVm", SettingsVm);
			GetPropertiesFrom(vws,"ScopeVm", ScopeVm);
		}

		public ViewSettings() 
		{
			SettingsVm = new SettingsViewModel();
			Main = new MainViewModel();
			ChannelLeft = new ThdChannelViewModel();
			ChannelRight = new ThdChannelViewModel();
			ImdChannelLeft = new ImdChannelViewModel();
			ImdChannelRight = new ImdChannelViewModel();
			SpectrumVm = new SpectrumViewModel();
			ImdVm = new ImdViewModel();
			ThdAmp = new ThdAmpViewModel();
			ThdFreq = new ThdFreqViewModel();
			FreqRespVm = new FreqRespViewModel();
			ScopeVm = new ScopeViewModel();
		}
	}
}
