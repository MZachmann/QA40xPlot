using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Numerics;

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
		private string _Heading = string.Empty;
		public string Heading
		{
			get => _Heading;
			set => SetProperty(ref _Heading, value);
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
		public string LeftColor { get; set; } = "Default"; // left color for the graph
		public string RightColor { get; set; } = "Default"; // right color for the graph
		[JsonIgnore]
		public BaseViewModel? MainVm { get; set; } = null; // the generator, if any
		[JsonIgnore]
		public static List<String> PlotColors { get => SettingsViewModel.PlotColors; } // the background colors

		public DataDescript()
		{
			Name = string.Empty;
			Description = string.Empty;
			Heading = string.Empty;
			CreateDate = string.Empty;
			Saved = false;
			GeneratorVoltage = 1e-10;
			PropertyChanged += ChangeDefinition;
		}

		~DataDescript()
		{
			PropertyChanged -= ChangeDefinition;
		}

		// when we change the definition in the tab, notify the main viewmodel
		// and tell it what exactly changed with a Ds prefix for uniqueness
		private void ChangeDefinition(object? sender, PropertyChangedEventArgs e)
		{
			if(MainVm != null && (e.PropertyName?.Length ?? 0) > 0)
			{
				MainVm.RaisePropertyChanged("Ds" + e.PropertyName);
			}
		}
	}

	public class SweepData
	{
		public double[] X { get; set; } = [];       // X values, freq or time or amplitude...
		public double[] RawLeft { get; set; } = [];   // columnar data for distortion sweeps
		public double[] RawRight { get; set; } = [];  // columnar data for sweeps
	}

	// our way of storing results (pages) in our Document. Each datatab has
	// a viewmodel, a time series and a dictionary of other properties
	public class DataTab<T>
	{
		private static int _CurrentId = 0;      // unique id for the datatab
												// ------------------------------------------------------------------
												// we only serialize these things
		public DataDescript Definition { get; set; } = new DataDescript();
		public T ViewModel { get; private set; }
		public LeftRightTimeSeries TimeRslt { get; set; }   // if we acquired data
		public LeftRightPair NoiseFloor { get; set; } = new();		// if we have the noise floor measurement
		// for sweeps
		public SweepData Sweep { get; set; } = new();           // X values, freq or time or amplitude...

		// ------------------------------------------------------------------
		// all other properties are calculated but may be cached in PropertySet
		[JsonIgnore]
		public int Show { get; set; }        // Show in graph 0 = none, 1 = left, 2 = right, 3 = both
		[JsonIgnore]
		public string FileName { get; set; }  // file name if loaded
		[JsonIgnore]
		public int Id { get; set; } // the generator, if any
		[JsonIgnore]
		public LeftRightFrequencySeries? FreqRslt { 
			get { return GetProperty<LeftRightFrequencySeries>("FFT"); }
			set { SetProperty("FFT", value); }
		}
		[JsonIgnore]
		public Complex[] GainData { 
			get {
				if (Sweep.RawLeft.Length == 0)
					return [];
				try
				{
					return Sweep.RawLeft.Zip(Sweep.RawRight, (x, y) => new Complex(x, y)).ToArray();
				}
				catch (Exception)
				{
				}
				return [];
			}
			set { 
				if(value == null || value.Length == 0)
					Sweep.RawLeft = [];
				else
					Sweep.RawLeft = value.Select(x => x.Real).ToArray();
				if (value == null || value.Length == 0)
					Sweep.RawRight = [];
				else
					Sweep.RawRight = value.Select(x => x.Imaginary).ToArray();
			}
		}
		[JsonIgnore]
		public double[] GainFrequencies	{
			get { return Sweep.X; }
			set { Sweep.X = value; }
		}

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
			Id = _CurrentId++;  // unique id for the data descriptor
			FileName = string.Empty;
			if (viewModel != null)
			{
				ViewModel = (T)((ICloneable)viewModel).Clone();   // get a blank vm
			}
			else
			{
				ViewModel = default!;
			}
			//var nama = ViewModel?.GetType();	// debug view
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
