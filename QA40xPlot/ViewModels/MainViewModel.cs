
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
		private int _ProgressAmount = 0;         // type of alert
		public int ProgressAmount
		{
			get => _ProgressAmount; set => SetProperty(ref _ProgressAmount, value);
		}
		private int _ProgressMax = 0;
		public int ProgressMax
		{
			get => _ProgressMax; set => SetProperty(ref _ProgressMax, value);
		}
		#endregion

		public async Task SetProgressMessage(String message, int delay = 0)
		{
			ProgressMessage = message;
			if (delay > 0)
				await Task.Delay(delay);
		}

		public void SetupProgressBar(int most)
		{
			ProgressMax = most;
		}

		public async Task SetProgressBar(int progress, int delay = 0)
		{
			ProgressAmount = progress;
			if (delay > 0)
				await Task.Delay(delay);
		}
	}
}
