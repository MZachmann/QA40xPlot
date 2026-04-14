using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace QA40xPlot.Data
{
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

	// this contains info about the test as well as intermediate results
	// it contains a set of properties that do not persist
	public class DataDescript : FloorViewModel
	{
		[JsonIgnore]
		public RelayCommand DoCopyToAll { get => new RelayCommand(DoCopyAll); }
		[JsonIgnore]
		public RelayCommand DoUpdateSource { get => new RelayCommand(UpdateSource); }

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
				if (SetProperty(ref _LeftColor, value))
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
				if (SetProperty(ref _RightColor, value))  // left color for the graph
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

		// if we have edited part of the header info, save it to the file
		private void UpdateSource()
		{
			string didok = "";
			var bex = File.Exists(FileName);
			if (bex)
			{
				try
				{
					// read the existing file
					var ftext = Util.LoadFileText(FileName);
					if (ftext.Length > 0)
					{
						// convert it into a big dictionary
						var dict = Util.Deserialize(ftext);
						// find the DataDefinition
						if (dict?.ContainsKey("Definition") ?? false)
						{
							// update the DataDefinition fields we edits
							var defn = dict["Definition"];
							defn["Name"] = this.Name;
							defn["Heading"] = this.Heading;
							defn["Description"] = this.Description;
							// resave it back to the source file
							string jsonString = Util.ConvertToJson(dict);
							Util.CompressTextToFile(jsonString, FileName); // zip it
						}
						else
						{
							didok = $"Invalid file format {FileName}";
						}
					}
					else
						didok = $"Unable to read file {FileName}";
					if (didok.Length > 0)
						Debug.WriteLine(didok);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
			}
			else
			{

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
}
