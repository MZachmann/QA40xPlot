using QA40xPlot.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Data
{
	public class OtherSet : FloorViewModel
	{
		public string Name { get; set; } = string.Empty;
		private bool _IsOnL = false;
		public bool IsOnL
		{
			get { return _IsOnL; }
			set { SetProperty(ref _IsOnL, value); }
		}
		private bool _IsOnR = false;
		public bool IsOnR
		{
			get { return _IsOnR; }
			set { SetProperty(ref _IsOnR, value); }
		}

		public string Value { get; set; } = string.Empty;
		public int Id { get; set; } = 0; // the parent data descriptor ID

		public OtherSet() { }
		public OtherSet(string name, int isOn, int id, string value = "")
		{
			Name = name;
			IsOnL = 1 == (isOn & 1);
			IsOnR = 2 == (isOn & 2);
			Value = value;
			Id = id;
		}
	}
}
