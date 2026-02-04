using QA40xPlot.ViewModels;
using System.Windows.Controls;
using System.Windows.Media;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for VoltButton.xaml
	/// </summary>
	public partial class VoltButton : UserControl
	{
		public Brush BrdrColor
		{
			get
			{
				var x = ViewSettings.Singleton.SettingsVm.ThemeSet;

				Color bclr = Color.FromArgb(40, 255, 255, 255);     // slight brighten
				switch (x)
				{
					case "Dark":
						bclr = Color.FromArgb(120, 255, 255, 255);     // slight brighten
						break;
					case "None":
					case "Light":
					default:
						bclr = Color.FromArgb(50, 0, 0, 0);     // slight darken
						break;
				}
				return new SolidColorBrush(bclr);
			}
		}

		public Brush BkgColor
		{
			get
			{
				var x = ViewSettings.Singleton.SettingsVm.ThemeSet;

				Color bclr = Color.FromArgb(16, 255, 255, 255);     // slight brighten
				switch (x)
				{
					case "Dark":
						bclr = Color.FromArgb(16, 255, 255, 255);     // slight brighten
						break;
					case "None":
					case "Light":
					default:
						bclr = Color.FromArgb(10, 0, 0, 0);     // slight brighten
						break;
				}
				return new SolidColorBrush(bclr);
			}
		}

		public VoltButton()
		{
			InitializeComponent();
		}
	}
}
