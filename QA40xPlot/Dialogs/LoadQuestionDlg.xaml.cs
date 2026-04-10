using System.Windows;

namespace QA40xPlot.Dialogs
{
    public partial class LoadQuestionDlg : Window
    {
        public bool IsLoadConfig { get; set; }
        public bool IsLoadData { get; set; }

        public LoadQuestionDlg(bool useConfig, bool useData)
        {
           InitializeComponent();
			LoadConfig.IsChecked = useConfig;
			LoadData.IsChecked = useData;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoadConfig = LoadConfig.IsChecked == true;
            IsLoadData = LoadData.IsChecked == true;
            DialogResult = true;
            Close();
        }
    }
}
