using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.ViewModels
{
	public class SettingsViewModel : BaseViewModel
	{
		#region setters and getters
		private string _AmplifierLoad = string.Empty;
		public string AmplifierLoad
		{
			get { return _AmplifierLoad; }
			set
			{
				SetProperty(ref _AmplifierLoad, value);
			}
		}
		#endregion


		public SettingsViewModel() 
		{
			this.AmplifierLoad = "10";
		}
	}
}
