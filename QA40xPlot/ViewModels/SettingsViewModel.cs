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
		private string _ReferenceImpedance = string.Empty;
		public string ReferenceImpedance
		{
			get { return _ReferenceImpedance; }
			set
			{
				SetProperty(ref _ReferenceImpedance, value);
			}
		}
		#endregion


		public SettingsViewModel() 
		{
			this.ReferenceImpedance = "10";
		}
	}
}
