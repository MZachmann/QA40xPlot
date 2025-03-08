

// this aggregates the settings somewhere static, which does mean only one of each

using System.Text.Json;

namespace QA40xPlot.ViewModels
{
	public class ViewSettings
	{
		public static ViewSettings Singleton { get; private set; } = new ViewSettings();
		public SpectrumViewModel SpectrumVm { get; private set; }
		public ImdViewModel ImdVm { get; private set; }
		public ThdFreqViewModel ThdFreq { get; private set; }
		public ThdAmpViewModel ThdAmp { get; private set; }
		public ThdChannelViewModel ChannelLeft { get; private set; }
		public ThdChannelViewModel ChannelRight { get; private set; }
		public ImdChannelViewModel ImdChannelLeft { get; private set; }
		public ImdChannelViewModel ImdChannelRight { get; private set; }
		public FreqRespViewModel FreqRespVm { get; private set; }
		public MainViewModel Main { get; private set; }
		public string SerializeAll()
		{
			string jsonString = JsonSerializer.Serialize(this);
			return jsonString;
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

			//var vout = SerializeAll();
			//Console.WriteLine(vout);
		}
	}
}
