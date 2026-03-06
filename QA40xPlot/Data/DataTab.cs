using Newtonsoft.Json;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using System.Numerics;

namespace QA40xPlot.Data
{

	// our way of storing results (pages) in our Document. Each datatab has
	// a viewmodel = settings from the run, a time series and a dictionary of other properties
	// in addition the Definition is descriptive stuff about the data
	public class DataTab 
	{

		private static int _CurrentId = 1;      // unique id for the datatab

		// ------------------------------------------------------------------
		// we only serialize these things
		public DataDescript Definition { get; set; } = new DataDescript();
		public BaseViewModel ViewModel
		{
			get;
			 set;
		}
		public LeftRightPair NoiseFloor { get; set; } = new();      // if we have the noise floor measurement we just need to retain the scalars
		public LeftRightPair NoiseFloorA { get; set; } = new();      // if we have the noise floor measurement we just need to retain the scalars
		public LeftRightPair NoiseFloorC { get; set; } = new();      // if we have the noise floor measurement we just need to retain the scalars
																	 // for sweeps
		public SweepData Sweep { get; set; } = new();               // X values, freq or time or amplitude...
		public SweepStepList SweepSteps { get; set; } = new();      // qa430 configuration list
																	// this is just a load/save object that converts TimeRslt doubles into longs for saving exactly
		public LeftRightTimeSaver? TimeSaver { get; set; } = null; // the time series, if any
																   // this is just a load/save object that converts FreqRslt doubles into longs for saving exactly
		public LeftRightFreqSaver? FreqSaver { get; set; } = null; // the time series, if any

		// ------------------------------------------------------------------
		// all other properties are calculated but may be cached in PropertySet
		[JsonIgnore]
		public LeftRightTimeSeries TimeRslt { get; set; }   // if we acquired data
		[JsonIgnore]
		public int Show { get => (Definition.IsOnL ? 1 : 0) + (Definition.IsOnR ? 2 : 0); }        // Show in graph 0 = none, 1 = left, 2 = right, 3 = both
		[JsonIgnore]
		public int Id { get; set; } // the generator, if any

		[JsonIgnore]
		public LeftRightFrequencySeries? FreqRslt
		{
			get { return GetProperty<LeftRightFrequencySeries?>("FFT"); }
			set { SetProperty("FFT", value); }
		}
		[JsonIgnore]
		public LeftRightFrequencySeries? NoiseRslt
		{
			get { return GetProperty<LeftRightFrequencySeries?>("Noise"); }
			set { SetProperty("Noise", value); }
		}
		/// <summary>
		/// the delay values associated with frequency vector
		/// </summary>
		[JsonIgnore]
		public double[]? DelayRslt
		{
			get { return GetProperty<double[]?>("Delay"); }
			set { SetProperty("Delay", value); }
		}
		[JsonIgnore]
		public double[] GainLeft { get => Sweep.RawLeft ?? []; }
		[JsonIgnore]
		public double[] GainRight { get => Sweep.RawRight ?? []; }
		[JsonIgnore]
		public double[] GainReal { get => Sweep.RawLeft ?? []; }
		[JsonIgnore]
		public double[] GainImag { get => Sweep.RawRight ?? []; }
		[JsonIgnore]
		public Complex[] GainCplx { get => GainReal.Zip(GainImag, (x, y) => new Complex(x, y)).ToArray(); }
		[JsonIgnore]
		public (double[], double[]) GainData
		{
			get
			{
				if (Sweep.RawLeft == null || Sweep.RawLeft.Length == 0)
					return ([], []);
				return (Sweep.RawLeft, Sweep.RawRight);
			}
			set
			{
				Sweep.RawLeft = value.Item1 ?? [];
				Sweep.RawRight = value.Item2 ?? [];
			}
		}

		[JsonIgnore]
		public double[] GainFrequencies
		{
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
					if (u == null)
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
		public DataTab(BaseViewModel viewModel, LeftRightTimeSeries series, Dictionary<string, object>? dct = null)
		{
			Definition.Id = _CurrentId;
			Id = _CurrentId++;  // unique id for the data descriptor
			if (viewModel != null)
			{
				ViewModel = (BaseViewModel)((ICloneable)viewModel).Clone();   // get a copy of the VM here
			}
			else
			{
				ViewModel = default!;
			}

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
