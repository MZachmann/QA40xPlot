

// this aggregates the settings somewhere static, which does mean only one of each

namespace QA40xPlot.ViewModels
{
	public class ViewSettings
	{
		public static ViewSettings Singleton { get; private set; } = new ViewSettings();
		public ThdFreqViewModel ThdFreq { get; private set; }
		public ThdAmpViewModel ThdAmp { get; private set; }
		public MainViewModel Main { get; private set; }

		public ViewSettings() 
		{
			ThdAmp = new ThdAmpViewModel();
			ThdFreq = new ThdFreqViewModel();
			Main = new MainViewModel();
		}
	}
}
