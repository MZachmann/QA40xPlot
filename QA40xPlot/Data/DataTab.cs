using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using System.ComponentModel;
using System.Numerics;

namespace QA40xPlot.Data
{
	// this contains info about the test as well as intermediate results
	// it contains a set of properties that do not persist
	public class DataDescript : FloorViewModel
	{
		[JsonIgnore]
		public RelayCommand DoCopyToAll { get => new RelayCommand(DoCopyAll);}

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
		private string _LeftColor = "Transparent"; // left color for the graph

		[JsonIgnore]
		public string LeftColor
		{
			get => _LeftColor;
			set
			{
				SetProperty(ref _LeftColor, value);
				RaisePropertyChanged("Repaint");
			}  // left color for the graph
		}
		private string _RightColor = "Transparent"; // left color for the graph
		[JsonIgnore]
		public string RightColor
		{
			get => _RightColor;
			set
			{
				SetProperty(ref _RightColor, value);  // left color for the graph
				RaisePropertyChanged("Repaint");    // this will notify the main graph via special property
			}  // left color for the graph
		}

		private string _OffsetLeft = "0";
		[JsonIgnore]
		public string OffsetLeft
		{
			get { return _OffsetLeft; }
			set { SetProperty(ref _OffsetLeft, value); }
		}
		[JsonIgnore]
		public double OffsetLeftValue
		{
			get => MathUtil.ToDouble(_OffsetLeft, 0);
		}

		private string _OffsetRight = "0";
		[JsonIgnore]
		public string OffsetRight
		{
			get { return _OffsetRight; }
			set { SetProperty(ref _OffsetRight, value); }
		}
		[JsonIgnore]
		public double OffsetRightValue
		{
			get => MathUtil.ToDouble(_OffsetRight, 0);
		}

		private bool _IsOnL = false;
		[JsonIgnore]
		public bool IsOnL
		{
			get { return _IsOnL; }
			set { SetProperty(ref _IsOnL, value); }
		}
		private bool _IsOnR = false;
		[JsonIgnore]
		public bool IsOnR
		{
			get { return _IsOnR; }
			set { SetProperty(ref _IsOnR, value); }
		}

		private string _FileName = ""; // our unique FileName
		[JsonIgnore]
		public string FileName
		{
			get { return _FileName; }
			set { SetProperty(ref _FileName, value); }
		}

		private int _Id = 0; // our unique id
		[JsonIgnore]
		public int Id
		{
			get { return _Id; }
			set { SetProperty(ref _Id, value); }
		}

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

		private void DoCopyAll()
		{
			var vset = ViewSettings.Singleton;
			if (vset != null)
			{
				// pick a random view model
				ViewSettings.Singleton.CopyAboutToAll(this); ;
			}
		}

		// when we change the definition in the tab, notify the main viewmodel
		// and tell it what exactly changed with a Ds prefix for uniqueness
		private void ChangeDefinition(object? sender, PropertyChangedEventArgs e)
		{
			List<string> used = new() { "Name", "Heading", "Repaint" };
			var mainVm = ViewSettings.Singleton.MainVm.CurrentView; // ????
			if (mainVm != null && used.Contains(e.PropertyName ?? string.Empty))
			{
				mainVm.RaisePropertyChanged("Ds" + e.PropertyName);
			}
		}
	}

	public class SweepData
	{
		public double[] X { get; set; } = [];       // X values, freq or time or amplitude...
		public double[] RawLeft { get; set; } = [];   // columnar data for distortion sweeps
		public double[] RawRight { get; set; } = [];  // columnar data for sweeps
	}

	public class SweepStepList
	{
		public AcquireStep[] Steps { get; set; } = [];
	}

	// our way of storing results (pages) in our Document. Each datatab has
	// a viewmodel, a time series and a dictionary of other properties
	public class DataTab<T>
	{
		private static int _CurrentId = 1;      // unique id for the datatab
												// ------------------------------------------------------------------
												// we only serialize these things
		public DataDescript Definition { get; set; } = new DataDescript();
		public T ViewModel { get; private set; }
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
			get { return GetProperty<LeftRightFrequencySeries>("FFT"); }
			set { SetProperty("FFT", value); }
		}
		[JsonIgnore]
		public LeftRightFrequencySeries? NoiseRslt
		{
			get { return GetProperty<LeftRightFrequencySeries>("Noise"); }
			set { SetProperty("Noise", value); }
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
					return ([],[]);
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
		public DataTab(T viewModel, LeftRightTimeSeries series, Dictionary<string, object>? dct = null)
		{
			Definition.Id = _CurrentId;
			Id = _CurrentId++;  // unique id for the data descriptor
			if (viewModel != null)
			{
				ViewModel = (T)((ICloneable)viewModel).Clone();   // get a copy of the VM here
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
