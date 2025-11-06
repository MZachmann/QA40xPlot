using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.ViewModels
{
	// this is a bool/name pair that's usable in gui
	public class SelectItem : FloorViewModel
	{
		private bool _IsSelected = false;
		public bool IsSelected
		{
			get => _IsSelected;
			set
			{
				SetProperty(ref _IsSelected, value);
			}
		}

		private string _Name = string.Empty;
		public string Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}
		internal SelectItem(bool isSel, string name)
		{
			IsSelected = isSel;
			Name = name;
		}
	}
}
