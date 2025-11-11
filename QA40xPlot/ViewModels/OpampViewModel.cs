using Newtonsoft.Json;

namespace QA40xPlot.ViewModels
{
	// this add some stuff used by all the opamp tests
	public abstract class OpampViewModel : BaseViewModel
	{
		[JsonIgnore]
		public string SupplyTip { get => "Enter supply values. May use | to delimit +-. Default 15V"; }

		private SelectItemList _Loadsets = [
				new SelectItem(true, "Open"), new SelectItem(true, "2000 Ω"),
				new SelectItem(true, "604 Ω"), new SelectItem(true, "470 Ω")
			];
		[JsonIgnore]
		public SelectItemList Loadsets
		{
			get => _Loadsets;
			set => SetProperty(ref _Loadsets, value);
		}

		private SelectItemList _Supplysets = [new SelectItem(true, "7"),  new SelectItem(false, "9"),
					new SelectItem(false, "12|8"),  new SelectItem(false, "15")];
		[JsonIgnore]
		public SelectItemList Supplysets
		{
			get => _Supplysets;
			set => SetProperty(ref _Supplysets, value);
		}

		private SelectItemList _Voltsets = [new SelectItem(true, "0.1"),  new SelectItem(false, "1"),
					new SelectItem(false, "3"),  new SelectItem(false, "5.5")];
		[JsonIgnore]
		public SelectItemList Voltsets
		{
			get => _Voltsets;
			set => SetProperty(ref _Voltsets, value);
		}

		private SelectItemList _Gainsets = [new SelectItem(true, "1"),  new SelectItem(true, "-1"),
					new SelectItem(true, "10"),  new SelectItem(true, "-10")];
		[JsonIgnore]
		public SelectItemList Gainsets
		{
			get => _Gainsets;
			set => SetProperty(ref _Gainsets, value);
		}

		[JsonIgnore]
		public string SupplyDisplay
		{
			get
			{
				var jn = string.Join(SelectItemList.Delimit, Supplysets.SelectedNames(true));
				return jn;
			}
		}

		// when this is saved it shows the current settings
		// the value is set only when we load a configuration so parse it
		public string SupplySummary
		{
			get
			{
				var jn = Supplysets.ParseableList("15");
				return jn;
			}
			set
			{
				Supplysets = SelectItemList.ParseList(value, 4);
				RaisePropertyChanged("SupplyDisplay");
			}
		}

		[JsonIgnore]
		public string VoltageDisplay
		{
			get
			{
				var jn = string.Join(SelectItemList.Delimit, Voltsets.SelectedNames(true));
				return jn;
			}
		}

		// when this is saved it shows the current settings
		// the value is set only when we load a configuration so parse it
		public string VoltSummary
		{
			get 
			{
				var jn = Voltsets.ParseableList("0.1");
				return jn;
			}
			set
			{
				Voltsets = SelectItemList.ParseList(value, 4);
				RaisePropertyChanged("VoltageDisplay");
			}
		}

		// when this is saved it shows the current settings
		// the value is set only when we load a configuration so parse it
		public string LoadSummary
		{
			get => string.Join(SelectItemList.Delimit, Loadsets.SelectedNames());
			set
			{
				var u = value.Split(SelectItemList.Delimit, StringSplitOptions.RemoveEmptyEntries);
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
			get => string.Join(SelectItemList.Delimit, Gainsets.SelectedNames());
			set
			{
				var u = value.Split(SelectItemList.Delimit, StringSplitOptions.RemoveEmptyEntries);
				foreach (var item in Gainsets)
				{
					item.IsSelected = u.Contains(item.Name);
				}
				RaisePropertyChanged("GainSummary");
			}
		}
	}
}
