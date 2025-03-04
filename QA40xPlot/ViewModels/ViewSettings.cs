

// this aggregates the settings somewhere static, which does mean only one of each

namespace QA40xPlot.ViewModels
{
	public class ViewSettings
	{
		public static ViewSettings Singleton { get; private set; } = new ViewSettings();
		public SpectrumViewModel SpectrumVm { get; private set; }
		public ThdFreqViewModel ThdFreq { get; private set; }
		public ThdAmpViewModel ThdAmp { get; private set; }
		public ChannelViewModel ChannelLeft { get; private set; }
		public ChannelViewModel ChannelRight { get; private set; }
		public FreqRespViewModel FreqRespVm { get; private set; }
		public MainViewModel Main { get; private set; }

		public ViewSettings() 
		{
			SpectrumVm = new SpectrumViewModel();
			ThdAmp = new ThdAmpViewModel();
			ThdFreq = new ThdFreqViewModel();
			ChannelLeft = new ChannelViewModel();
			ChannelRight = new ChannelViewModel();
			FreqRespVm = new FreqRespViewModel();
			Main = new MainViewModel();
		}
	}
}
