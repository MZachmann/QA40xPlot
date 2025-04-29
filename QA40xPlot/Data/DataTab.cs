using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using Newtonsoft.Json;
using Windows.Gaming.Input;

namespace QA40xPlot.Data
{
	public class DataDescript : FloorViewModel
	{
		private string _Name = string.Empty;
		public string Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}
		private string _Description = string.Empty;
		public string Description
		{
			get => _Description;
			set => SetProperty(ref _Description, value);
		}
		public string CreateDate { get; set; }  // Measurement date time
		public bool Saved { get; set; }
		public double GeneratorVoltage { get; set; } // the generator voltage, if any
		public DataDescript()
		{
			Name = string.Empty;
			Description = string.Empty;
			CreateDate = string.Empty;
			Saved = false;
			GeneratorVoltage = 1e-10;
		}
	}
	// our way of storing results (pages) in our Document. Each datatab has
	// a viewmodel, a time series and a dictionary of other properties
	public class DataTab<T>
	{
		public static List<string> Glossary = new List<string>() { "FFT", "Left", "Right" };

		// ------------------------------------------------------------------
		// we only serialize these things
		public DataDescript Definition { get; set; } = new DataDescript();
		public T ViewModel { get; private set; }
		public LeftRightTimeSeries TimeRslt { get; set; }
		public LeftRightPair? NoiseFloor { get; set; }

		// ------------------------------------------------------------------
		// all other properties are calculated but may be cached in PropertySet
		[JsonIgnore]
		public bool Show { get; set; }                                                  // Show in graph
		[JsonIgnore]
		public LeftRightFrequencySeries? FreqRslt { get { return GetProperty<LeftRightFrequencySeries>("FFT"); } }

		[JsonIgnore]
		public Dictionary<string, object> PropertySet { get; private set; }

		public void SetProperty(string key, object? value)
		{
			if (value != null) 
				PropertySet[key] = value; 
			else 
				PropertySet.Remove(key);
		}

		/// <summary>
		/// get the property from the dictionary, if it exists
		/// </summary>
		/// <param name="key"></param>
		/// <returns>the property or null</returns>
		public object? GetProperty(string key)
		{
			return PropertySet.ContainsKey(key) ? PropertySet[key] : null;
		}

		/// <summary>
		/// get the property from the dictionary, if it exists, as a specific type
		/// </summary>
		/// <typeparam name="T">datatype of the object</typeparam>
		/// <param name="key"></param>
		/// <returns>the property as a T or null</returns>
		public K? GetProperty<K>(string key)
		{
			if (PropertySet.ContainsKey(key))
			{
				try
				{
					var u = PropertySet[key];
					if(u == null)
						return default;
					return (K)u;
				}
				catch (InvalidCastException)
				{
					return default;
				}
			}
			return default;
		}

		/// <summary>
		/// the datatab contains the viewmodel for the tab, the time series acquired and an optional dictionary of other stuff
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="series"></param>
		/// <param name="dct"></param>
		public DataTab(T viewModel, LeftRightTimeSeries series, Dictionary<string,object>? dct = null)
		{
			if (viewModel != null)
			{
				ViewModel = (T)((ICloneable)viewModel).Clone();   // get a blank vm
			}
			else
			{
				ViewModel = default!;
			}
				var nama = ViewModel?.GetType();
			TimeRslt = series;
			Definition.Name = string.Empty;
			Definition.Description = string.Empty;
			Definition.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			Definition.Saved = false;
			if (dct != null)
				PropertySet = dct;
			else
				PropertySet = new Dictionary<string, object>();
		}
	}
}
