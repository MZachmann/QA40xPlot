
namespace QA40xPlot.ViewModels
{
	public class MainViewModel : BaseViewModel
	{
#region Setters and Getters
		private String _ProgressMessage = String.Empty;         // type of alert
		public String ProgressMessage
		{
			get => _ProgressMessage; set => SetProperty(ref _ProgressMessage, value);
		}
		private String _ProgressBar = String.Empty;         // type of alert
		public String ProgressBar
		{
			get => _ProgressBar; set => SetProperty(ref _ProgressBar, value);
		}
#endregion

		public async Task ShowProgressMessage(String message, int delay = 0)
		{
			ProgressMessage = message;
			if (delay > 0)
				await Task.Delay(delay);
		}

		public async Task ShowProgressBar(String message, int delay = 0)
		{
			ProgressBar = message;
			if (delay > 0)
				await Task.Delay(delay);
		}
	}
}
