using QA40xPlot.QA430;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for QA430ShowConfigs.xaml
	/// </summary>
	public partial class QA430ShowConfigs : Window
	{
		public QA430ShowConfigs()
		{
			InitializeComponent();
			this.DataContext = Qa430Usb.Singleton.QAModel;
			PopulateImages();
		}

		public string ConfigName {get;set;} = "";

		public void PopulateImages()
		{
			// put a button with an image inside for each config option
			// when clicked it does an ok and sets ConfigName to the selection
			foreach (var who in QA430Model.ConfigOptions)
			{
				var btn = new Button
				{
					Margin = new Thickness(5),
					Padding = new Thickness(5),
					Tag = who
				};
				btn.Click += (s, e) =>
				{
					DialogResult = true;
					ConfigName = (string)((Button)s).Tag;
					Close();
				};
				var uri = @"/QA40xPlot;component/Images/QA430Configs/" + who + ".png";
				var logo = new BitmapImage();
				logo.BeginInit();
				logo.UriSource = new Uri(uri, UriKind.Relative);
				logo.EndInit();
				var uu = new Image
				{
					Source = logo,
					Stretch = Stretch.Uniform,
					Height = 160
				};
				btn.Content = uu;
				ConfigWrapPanel.Children.Add(btn);
			}
		}
	}
}
