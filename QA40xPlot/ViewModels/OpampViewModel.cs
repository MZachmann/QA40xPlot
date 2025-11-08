using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace QA40xPlot.ViewModels
{
	// this add some stuff used by all the opamp tests
	public abstract class OpampViewModel : BaseViewModel
	{
		[JsonIgnore]
		public string SupplyTip { get => "Delimit tests with space or semicolon. Use | or _ to delimit +-"; }

		private ObservableCollection<SelectItem> _Loadsets = [new SelectItem(true, "Open"), new SelectItem(true, "2000 Ω"),
					new SelectItem(true, "604 Ω"), new SelectItem(true, "470 Ω")];
		[JsonIgnore]
		public ObservableCollection<SelectItem> Loadsets
		{
			get => _Loadsets;
			set => SetProperty(ref _Loadsets, value);
		}

		private ObservableCollection<SelectItem> _Gainsets = [new SelectItem(true, "1"),  new SelectItem(true, "-1"),
					new SelectItem(true, "10"),  new SelectItem(true, "-10")];
		[JsonIgnore]
		public ObservableCollection<SelectItem> Gainsets
		{
			get => _Gainsets;
			set => SetProperty(ref _Gainsets, value);
		}

		private string _SupplyList = "6|5.5;7;12;15";
		public string SupplyList
		{
			get => _SupplyList;
			set => SetProperty(ref _SupplyList, value);
		}

		// when this is saved it shows the current settings
		// the value is set only when we load a configuration so parse it
		public string LoadSummary
		{
			get => string.Join(',', Loadsets.Where(x => x.IsSelected).Select(x => x.Name));
			set
			{
				var u = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
				foreach (var item in Loadsets)
				{
					item.IsSelected = u.Contains(item.Name);
				}
				RaisePropertyChanged("LoadSummary");
			}
		}

		// when this is saved it shows the current settings
		// the value is set only when we load a configuration so parse it
		public string GainSummary
		{
			get => string.Join(',', Gainsets.Where(x => x.IsSelected).Select(x => x.Name));
			set
			{
				var u = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
				foreach (var item in Gainsets)
				{
					item.IsSelected = u.Contains(item.Name);
				}
				RaisePropertyChanged("GainSummary");
			}
		}
	}
}
