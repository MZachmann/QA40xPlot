using System.Configuration;
using System.Data;
using System.Windows;

// Written by MZachmann 4-24-2025
// some of this code came from the original Qa40x application by Joost Breed
// see https://github.com/breedj/qa40x-audio-analyser


namespace QA40xPlot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		public string DefaultCfg { get; set; } = "";
		public string StockDefaultCfg { get; set; } = "QADefault.cfg";
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var callFile = e.Args.FirstOrDefault();
			if (callFile != null && callFile.Length > 0)
			{
				if(!callFile.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
				{
					DefaultCfg = callFile + ".cfg";
				}
				else
				{
					DefaultCfg = callFile;
				}
			}
			else
			{
				DefaultCfg = StockDefaultCfg;
			}
		}
	}

}
