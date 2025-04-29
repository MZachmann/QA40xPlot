using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QA40xPlot.ViewModels
{
	public class SettingsViewModel : BaseViewModel
	{
		#region setters and getters
		private Rect _WindowPlace;
		public Rect WindowPlace
		{
			get { return _WindowPlace; }
			set
			{
				SetProperty(ref _WindowPlace, value);
			}
		}
		private string _TestChannel = "Left";
		public string TestChannel
		{
			get { return _TestChannel; }
			set
			{
				SetProperty(ref _TestChannel, value);
			}
		}
		private string _AmplifierLoad = string.Empty;
		public string AmplifierLoad
		{
			get { return _AmplifierLoad; }
			set
			{
				SetProperty(ref _AmplifierLoad, value);
			}
		}

		private string _SaveOnExit = "False";
		public string SaveOnExit
		{
			get { return _SaveOnExit; }
			set
			{
				SetProperty(ref _SaveOnExit, value);
			}
		}
		#endregion


		public SettingsViewModel() 
		{
			Name = "Settings";
			this.AmplifierLoad = "10";
		}
	}
}
