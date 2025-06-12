﻿using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using QA40xPlot.Views;
using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Input;


public class FreqRespViewModel : BaseViewModel
{
	public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
	public static List<String> Smoothings { get => new List<string> { "None", "1/24", "1/6" }; }
	public static List<String> TestTypes { get => new List<string> { "Response", "Impedance", "Gain" }; }

	private ActFrequencyResponse MyAction { get => actFreq; }
	private PlotControl actPlot { get; set; }
	private ActFrequencyResponse actFreq { get;  set; }
	[JsonIgnore]
	public AsyncRelayCommand DoStart { get => new AsyncRelayCommand(StartIt); }
	[JsonIgnore]
	public RelayCommand DoStop { get => new RelayCommand(StopIt); }
	[JsonIgnore]
	public RelayCommand ToggleGenerator { get => new RelayCommand(StopIt); }
	[JsonIgnore]
	public RelayCommand<object> DoFitToData { get => new RelayCommand<object>(OnFitToData); }
	[JsonIgnore]
	public RelayCommand<object> DoViewToFit { get => new RelayCommand<object>(OnViewToFit); }
	[JsonIgnore]
	public AsyncRelayCommand DoLoadTab { get => new AsyncRelayCommand(LoadItTab); }
	[JsonIgnore]
	public AsyncRelayCommand DoGetTab { get => new AsyncRelayCommand(GetItTab); }
	[JsonIgnore]
	public RelayCommand DoSaveTab { get => new RelayCommand(SaveItTab); }

	[JsonIgnore]
	private static FreqRespViewModel MyVModel { get => ViewSettings.Singleton.FreqRespVm; }
	[JsonIgnore]
	public bool IsNotChirp { get => !_IsChirp; }

	#region Setters and Getters
	private bool _UseMUseMicCorrection = false;
	public bool UseMicCorrection
	{
		get { return _UseMUseMicCorrection; }
		set { SetProperty(ref _UseMUseMicCorrection, value); }
	}

	private bool _IsChirp;
	public bool IsChirp
	{
		get { return _IsChirp; }
		set { SetProperty(ref _IsChirp, value); RaisePropertyChanged("IsNotChirp"); }
	}

	private string _ZReference = string.Empty;
	public string ZReference
	{
		get => _ZReference;
		set => SetProperty(ref _ZReference, value);
	}

	private string _TestType = TestTypes[0];
	public string TestType
	{
		get => _TestType;
		set { SetProperty(ref _TestType, value); } // this is used to set the tab header
	}

	private string _StartFreq = string.Empty;
	public string StartFreq
	{
		get => _StartFreq;
		set => SetProperty(ref _StartFreq, value);
	}

	private string _EndFreq = string.Empty;
	public string EndFreq
	{
		get => _EndFreq;
		set => SetProperty(ref _EndFreq, value);
	}

	private uint _StepsOctave;         // type of alert
	public uint StepsOctave
	{
		get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
	}

	private string _Smoothing = string.Empty;         // type of alert
	public string Smoothing
	{
		get => _Smoothing;
		set => SetProperty(ref _Smoothing, value);
	}

	private string _Gen1Voltage = string.Empty;         // type of alert
	public string Gen1Voltage
	{
		get => _Gen1Voltage;
		set => SetProperty(ref _Gen1Voltage, value);
	}

	private bool _RightChannel;         // type of alert
	public bool RightChannel
	{
		get => _RightChannel; set => SetProperty(ref _RightChannel, value);
	}

	private bool _LeftChannel;         // type of alert
	public bool LeftChannel
	{
		get => _LeftChannel; set => SetProperty(ref _LeftChannel, value);
	}

	private bool _ShowPoints;
	public bool ShowPoints
	{
		get => _ShowPoints;
		set => SetProperty(ref _ShowPoints, value);
	}

	private bool _ShowPercent;
	public bool ShowPercent
	{
		get => _ShowPercent;
		set => SetProperty(ref _ShowPercent, value);
	}

	private bool _Show3dBBandwidth_L;
	public bool Show3dBBandwidth_L
	{
		get => _Show3dBBandwidth_L;
		set => SetProperty(ref _Show3dBBandwidth_L, value);
	}
	private bool _Show3dBBandwidth_R;
	public bool Show3dBBandwidth_R
	{
		get => _Show3dBBandwidth_R;
		set => SetProperty(ref _Show3dBBandwidth_R, value);
	}

	private bool _Show1dBBandwidth_L;
	public bool Show1dBBandwidth_L
	{
		get => _Show1dBBandwidth_L;
		set => SetProperty(ref _Show1dBBandwidth_L, value);
	}
	private bool _Show1dBBandwidth_R;
	public bool Show1dBBandwidth_R
	{
		get => _Show1dBBandwidth_R;
		set => SetProperty(ref _Show1dBBandwidth_R, value);
	}
	#endregion

	public TestingType GetTestingType(string type)
	{
		var vm = MyVModel;
		return (TestingType)TestTypes.IndexOf(type);
	}

	// the property change is used to trigger repaints of the graph
	private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case "UpdateGraph":
				MyAction?.UpdateGraph(true);
				break;
			case "DsHeading":
			case "DsRepaint":
				MyAction?.UpdateGraph(true);
				break;
			case "DsName":
				MyAction?.UpdatePlotTitle();
				break;
			case "ShowTabInfo":
				if (actAbout != null)
					actAbout.Visibility = ShowTabInfo ? Visibility.Visible : Visibility.Hidden;
				break;
			case "Voltage":
			case "AmpLoad":
			case "OutPower":
			case "GenDirection":
				//MyAction?.UpdateGeneratorParameters();
				break;
			case "TestType":
				// set the tab header as we change type
				if( ViewSettings.Singleton != null && ViewSettings.Singleton.Main != null && ViewSettings.Singleton.Main.FreqRespHdr != null)
				{
					ViewSettings.Singleton.Main.FreqRespHdr = TestType;
				}
				MyAction?.UpdateGraph(true);
				break;
			case "GraphStartX":
			case "GraphEndX":
			case "RangeBottomdB":
			case "RangeBottom":
			case "RangeTopdB":
			case "RangeTop":
				MyAction?.UpdateGraph(true);
				break;
			case "ShowRight":
			case "ShowLeft":
			case "ShowThickLines":
			case "StepsOctave":
				MyAction?.UpdateGraph(false);
				break;
			case "SampleRate":
				if(IsChirp)
				{
					if( SampleRate == "96000")
					{
						this.FftSize = "64K";
					}
					if (SampleRate == "192000")
					{
						this.FftSize = "128K";
					}
					else if (SampleRate == "48000")
					{
						this.FftSize = "32K";
					}
				}
				break;
			//case "IsChirp":
			//	if(IsChirp)
			//	{
			//		this.SampleRate = "96000";
			//		this.FftSize = "64K";
			//	}
			//	break;
			default:
				break;
		}
	}

	public void SetAction(PlotControl plot, PlotControl plot2, PlotControl plot3, TabAbout TAbout)
	{
		actFreq = new ActFrequencyResponse(plot, plot2, plot3);
		actAbout = TAbout;
		SetupMainPlot(plot);
		actPlot = plot;
		MyVModel.LinkAbout(actFreq.PageData.Definition);
	}

	// here param is the id of the tab to remove from the othertab list
	public override void DoDeleteIt(string param)
	{
		var id = MathUtil.ToInt(param, -1);
		var fat = OtherSetList.FirstOrDefault(x => x.Id == id);
		if (fat != null)
		{
			OtherSetList.Remove(fat);
			MyAction.DeleteTab(id);
		}
	}

	private static async Task StartIt()
	{
		// Implement the logic to start the measurement process
		var vm = MyVModel;
		await vm.actFreq.DoMeasurement();
	}

	private static void StopIt()
	{
		var vm = MyVModel;
		vm.actFreq.DoCancel();
	}

	public DataBlob? GetFftData()
	{
		var vm = MyVModel;
		return vm.actFreq.CreateExportData();
	}

	private static async Task DoGetLoad(bool isLoad)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			FileName = string.Empty, // Default file name
			DefaultExt = ".zip", // Default file extension
			Filter = PlotFileFilter // Filter files by extension
		};

		// Show save file dialog box
		bool? result = openFileDialog.ShowDialog();

		// Process save file dialog box results
		if (result == true)
		{
			// open document
			string filename = openFileDialog.FileName;
			var vm = MyVModel;
			await vm.actFreq.LoadFromFile(filename, isLoad);
		}
	}

	private static async Task LoadItTab()
	{
		await DoGetLoad(true);
	}

	private static async Task GetItTab()
	{
		await DoGetLoad(false);
	}


	private static string FileAddon()
	{
		DateTime now = DateTime.Now;
		string formattedDate = $"{now:yyyy-MM-dd_HH-mm-ss}";
		return formattedDate;
	}

	private static void SaveItTab()
	{
		string prefix = "QaFile";
		var ttype = MyVModel.GetTestingType(MyVModel.TestType);
		if (ttype == TestingType.Response)
			prefix = "QaResponse";
		else if (ttype == TestingType.Impedance)
			prefix = "QaImpedance";
		else if (ttype == TestingType.Gain)
			prefix = "QaGain";
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			FileName = prefix + FileAddon(),
			DefaultExt = ".plt", // Default file extension
			Filter = PlotFileFilter // Filter files by extension
		};

		// Show save file dialog box
		bool? result = saveFileDialog.ShowDialog();

		// Process save file dialog box results
		if (result == true)
		{
			// Save document
			string filename = saveFileDialog.FileName;
			if (filename.Count() > 1)
			{
				var vm = MyVModel;
				vm.actFreq.SaveToFile(filename);
			}
		}
	}

	private void OnViewToFit(object? parameter)
	{
		var pram = parameter as string;
		if (pram == null) 
			return;
		MyAction.PinGraphRange(pram);
	}

	private void OnFitToData(object? parameter)
	{
		var bounds = MyAction.GetDataBounds();
		switch (parameter)
		{
			case "XF":  // X frequency
				this.GraphStartX = bounds.Left.ToString("0");
				this.GraphEndX = bounds.Right.ToString("0");
				break;
			case "YP":  // Y percent
				var xp = bounds.Y + bounds.Height;  // max Y value
				var bot = ((100 * bounds.Y) / xp);  // bottom value in percent
				bot = Math.Pow(10, Math.Max(-7, Math.Floor(Math.Log10(bot))));  // nearest power of 10
				this.RangeTop = "100";  // always 100%
				this.RangeBottom = bot.ToString("0.##########");
				break;
			case "YM":  // Y magnitude
				var ttype = GetTestingType(TestType);
				if (ttype == TestingType.Impedance)
				{
					this.RangeBottomdB = bounds.Y.ToString("0");
					this.RangeTopdB = (bounds.Height + bounds.Y).ToString("0");
				}
				else
				{
					this.RangeBottomdB = (20 * Math.Log10(Math.Max(1e-14, bounds.Y))).ToString("0");
					this.RangeTopdB = Math.Ceiling((20 * Math.Log10(Math.Max(1e-14, bounds.Height + bounds.Y)))).ToString("0");
				}
				break;
			default:
				break;
		}
		MyAction?.UpdateGraph(true);
	}

	// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
	// here's the tracker event handler
	private static void DoMouseTracked(object sender, MouseEventArgs e)
	{
		var freqVm = MyVModel;
		freqVm.DoMouse(sender, e);
	}

	private void DoMouse(object sender, MouseEventArgs e)
	{
		SetMouseTrack(e);
		if (IsTracking)
		{
			var p = e.GetPosition(actPlot);
			var cord = ConvertScottCoords(actPlot, p.X, p.Y);
			FreqValue = Math.Pow(10, cord.Item1); // frequency
		}
		var zv = MyAction.LookupX(FreqValue);
		var ttype = GetTestingType(TestType);
		FreqShow = zv.Item1.ToString("0.# Hz");
		switch ( ttype)
		{
			case TestingType.Response:
				ZValue = "Left: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dBV") + Environment.NewLine + "Right: " + (20 * Math.Log10(zv.Item3)).ToString("0.## dBV");
				break;
			case TestingType.Impedance:
				{

					ZValue = "Z: " + MathUtil.FormatUnits(zv.Item2, "|Z|") + ", " + MathUtil.FormatPhase(zv.Item3);
					var x = Complex.FromPolarCoordinates(zv.Item2, Math.PI * zv.Item3 / 180);
					ZValue += Environment.NewLine + "X: " + MathUtil.FormatUnits(x.Real, "R") + ", " + MathUtil.FormatUnits(x.Imaginary, "I");
					var res = MathUtil.FormatResistance(x.Real);
					string sadd = string.Empty;
					if(x.Imaginary >= 0)
					{
						var ind = MathUtil.FormatInductance(x.Imaginary / (2 * Math.PI * zv.Item1));       // imaginary over 2piF
						sadd = res + " + " + ind;
					}
					else
					{
						var cap = MathUtil.FormatCapacitance(-1 / (2 * Math.PI * zv.Item1 * x.Imaginary));       // 2piF / imaginary
						sadd = res + " + " + cap;
					}
					ZValue += Environment.NewLine + sadd;
				}
				break;
			case TestingType.Gain:
				ZValue = "G: " + (20 * Math.Log10(zv.Item2)).ToString("0.## dB") + Environment.NewLine + "  " + zv.Item3.ToString("0.## Deg");
				break;
		}
	}

	~FreqRespViewModel()
	{
		PropertyChanged -= CheckPropertyChanged;
		MouseTracked -= DoMouseTracked;

	}

	public FreqRespViewModel()
	{
		Name = "Response";
		PropertyChanged += CheckPropertyChanged;
		MouseTracked += DoMouseTracked;

		actPlot = default!;
		actFreq = default!;

		LeftWidth = 95;
		RightWidth = 50;

		GraphStartX = "20";
		GraphEndX = "20000";
		StepsOctave = 1;
		LeftChannel = true;
		RightChannel = false;
		RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
		RangeBottom = "0.001";

		ShowThickLines = true;
		ShowPercent = false;

		RangeTopdB = "20";
		RangeBottomdB = "-180";

		Gen1Voltage = "0.1";
		Smoothing = "None";
		Show3dBBandwidth_L = true;
		Show3dBBandwidth_R = false;
		Show1dBBandwidth_L = false;
		Show1dBBandwidth_R = false;
		ShowPoints = true;
		TestType = TestTypes[0];	// this messes up if we start at impedance and set to impedance later so ??

		PlotFormat = "dBV";

		StartFreq = "20";
		EndFreq = "20000";
		ZReference = "10";

		// make a few things happen to synch the gui
		Task.Delay(1000).ContinueWith(t => { MyAction?.UpdateGraph(true); });
	}
}
