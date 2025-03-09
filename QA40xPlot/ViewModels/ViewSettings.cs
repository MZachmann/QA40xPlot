

// this aggregates the settings somewhere static, which does mean only one of each

using System.Reflection;
using Newtonsoft.Json;

namespace QA40xPlot.ViewModels
{
	public class ViewSettings
	{
		private Dictionary<string, string> _ProductTitle = new Dictionary<string, string>() { { "Name", "QA40xPlot" }, { "Version", "0.04"} };
		public Dictionary<string,string> Product { get { return _ProductTitle; } private set { _ProductTitle = value; } }


		public static ViewSettings Singleton { get; private set; } = new ViewSettings();
		public SpectrumViewModel SpectrumVm { get; private set; }
		public ImdViewModel ImdVm { get; private set; }
		public ThdFreqViewModel ThdFreq { get; private set; }
		public ThdAmpViewModel ThdAmp { get; private set; }
		public FreqRespViewModel FreqRespVm { get; private set; }
		public MainViewModel Main { get; private set; }
		// these are output only and don't need serializing
		[JsonIgnore]
		public ThdChannelViewModel ChannelLeft { get; private set; }
		[JsonIgnore]
		public ThdChannelViewModel ChannelRight { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelLeft { get; private set; }
		[JsonIgnore]
		public ImdChannelViewModel ImdChannelRight { get; private set; }

		public static void GetPropertiesFrom(Dictionary<string, object> vws, object dest)
		{
			if (vws == null || dest == null)
				return;

			Type type = dest.GetType();
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

			foreach (PropertyInfo property in properties)
			{
				if (property.CanRead && property.CanWrite)
				{
					if (vws.ContainsKey(property.Name))
					{
						object value = vws[property.Name];
						try
						{
							property.SetValue(dest, Convert.ChangeType(value, property.PropertyType));
						}
						catch (Exception ex) { }	// for now ignore this
					}
				}
			}
		}


		public void GetSettingsFrom( Dictionary<string, Dictionary<string,object>> vws)
		{
			GetPropertiesFrom(vws["Main"],Main);
			GetPropertiesFrom(vws["SpectrumVm"],SpectrumVm);
			GetPropertiesFrom(vws["ImdVm"],ImdVm);
			GetPropertiesFrom(vws["ThdAmp"],ThdAmp);
			GetPropertiesFrom(vws["ThdFreq"],ThdFreq);
			GetPropertiesFrom(vws["FreqRespVm"],FreqRespVm);
		}

		public ViewSettings() 
		{
			Main = new MainViewModel();
			SpectrumVm = new SpectrumViewModel();
			ImdVm = new ImdViewModel();
			ThdAmp = new ThdAmpViewModel();
			ThdFreq = new ThdFreqViewModel();
			ChannelLeft = new ThdChannelViewModel();
			ChannelRight = new ThdChannelViewModel();
			ImdChannelLeft = new ImdChannelViewModel();
			ImdChannelRight = new ImdChannelViewModel();
			FreqRespVm = new FreqRespViewModel();
		}
	}
}
