using QA40xPlot.Libraries;
using System.Collections.ObjectModel;

namespace QA40xPlot.ViewModels
{
	public class SelectItemList : ObservableCollection<SelectItem>
	{
		static public readonly char Delimit = ';';
		static public readonly char NoUse = '~';

		/// <summary>
		/// convert a list of selectable items to a single string
		/// that can be saved
		/// </summary>
		/// <param name="ifnone"></param>
		/// <returns></returns>
		public string ParseableList(string ifnone, bool isEditable = true)
		{
			if (!this.Any())
			{
				this.Add(new SelectItem(true, ifnone));
			}
			var alist = this.Where(x => x.Name.Length > 0);
			if (!isEditable)
			{
				alist = alist.Where(x => x.IsSelected);
			}
			var jn = alist.Select(x => (x.IsSelected ? "" : NoUse) + x.Name);
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
						if (SelectItemList.NoUse == u[i][0])
							vout.Add(new SelectItem(false, u[i].Substring(1)));
						else
							vout.Add(new SelectItem(true, u[i]));
					}
				}
				for (; i < minNumItems; i++)
				{
					vout.Add(new SelectItem(true, string.Empty));
				}
			}
			return vout;
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
