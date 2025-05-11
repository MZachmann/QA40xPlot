using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for GroupHeader.xaml
	/// </summary>
	public partial class GroupHeader : UserControl
	{
		private string _Header = string.Empty;
		public string Header { 
			get => _Header;
			set { this.Headline.Content = value; _Header = value; } 
		}

		public GroupHeader()
		{
			InitializeComponent();
		}
	}
}
