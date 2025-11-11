using QA40xPlot.Libraries;
using System.Collections.ObjectModel;

namespace QA40xPlot.ViewModels
{
	public class SelectItemList : ObservableCollection<SelectItem>
	{
		static public readonly char Delimit = ';';

		/// <summary>
		/// convert a list of selectable items to a single string
		/// that can be saved
		/// </summary>
		/// <param name="ifnone"></param>
		/// <returns></returns>
		public string ParseableList(string ifnone)
		{
			if(! this.Any())
			{
				this.Add(new SelectItem(true, ifnone));
			}
			var jn = this.Select(x => (x.IsSelected ? "+" : "") + x.Name);
			return string.Join(Delimit, jn);
		}

		/// <summary>
		/// deserialize a SelectItemList
		/// </summary>
		/// <param name="pList"></param>
		/// <returns></returns>
		static public SelectItemList ParseList(string pList, int minNumItems)
		{
			var vout = new SelectItemList();
			var u = pList.Split(SelectItemList.Delimit, StringSplitOptions.RemoveEmptyEntries);
			if (u.Length > 0)
			{
				var tl = u.Length;      // # of entries to parse
				int i = 0;
				for (; i < tl; i++)
				{
					if (u[i].Length > 0)
					{
						if ('+' == u[i][0])
							vout.Add(new SelectItem(true, u[i].Substring(1)));
						else
							vout.Add(new SelectItem(false, u[i]));
					}
				}
				for (; i < minNumItems; i++)
				{
					vout.Add(new SelectItem(false, i.ToString()));
				}
			}
			return vout;
		}

		/// <summary>
		/// find all selected items in the list
		/// if required==true then autoselect the first if necessary
		/// </summary>
		/// <param name="required"></param>
		/// <returns>list of selected names</returns>
		public List<String> SelectedNames(bool required = false)
		{
			var outs = this.Where(x => x.IsSelected).Select(x => x.Name).ToList();
			if (required && !outs.Any())
			{
				outs = new List<string> { this.First().Name };
			}
			return outs;
		}

		/// <summary>
		/// find all selected items in the list
		/// if required==true then autoselect the first if necessary
		/// </summary>
		/// <param name="required"></param>
		/// <returns>list of selected values</returns>
		public List<double> SelectedValues(bool required = false)
		{
			return this.SelectedNames(required).Select(x => MathUtil.ToDouble(x, 1e-6)).ToList();
		}
	}

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
