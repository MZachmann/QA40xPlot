﻿using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using FftSharp.Windows;
using static FreqRespViewModel;
using System.DirectoryServices.ActiveDirectory;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace QA40xPlot.ViewModels
{
	public class ThdAmpViewModel : BaseViewModel
	{
		public static List<String> VoltItems { get => new List<string> { "mV", "V", "dbV" }; }
		public static List<String> MeasureTypes { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }
		public static List<String> StartVoltages { get => new List<string> { "0.0001", "0.0002", "0.0005", "0.001", "0.002", "0.005", "0.01", "0.02", "0.05", "0.1", "0.2", "0.5" }; }
		public static List<String> EndVoltages { get => new List<string> { "1", "2", "5", "10", "20", "50", "100", "200" }; }

		private ActThdAmplitude actThd { get; set; }
		private PlotControl actPlot {  get; set; }

		[JsonIgnore]
		public RelayCommand DoStart { get => new RelayCommand(StartIt); }
		[JsonIgnore]
		public RelayCommand DoStop { get => new RelayCommand(StopIt); }

		#region Setters and Getters
		private string _StartVoltage = string.Empty;         // type of alert
		public string StartVoltage
		{
			get => _StartVoltage; set => SetProperty(ref _StartVoltage, value);
		}

		private string _EndVoltage = string.Empty;         // type of alert
		public string EndVoltage
		{
			get => _EndVoltage; set => SetProperty(ref _EndVoltage, value);
		}

		private string _StartPower = string.Empty;         // type of alert
		public string StartPower
		{
			get => _StartPower; set => SetProperty(ref _StartPower, value);
		}

		private string _EndPower = string.Empty;         // type of alert
		public string EndPower
		{
			get => _EndPower; set => SetProperty(ref _EndPower, value);
		}

		private double _AmpLoad;         // type of alert
		public double AmpLoad
		{
			get => _AmpLoad; set => SetProperty(ref _AmpLoad, value);
		}

		private string _TestFreq = string.Empty;         // type of alert
		public string TestFreq
		{
			get => _TestFreq;
			set => SetProperty(ref _TestFreq, value);
		}

		private string _GraphStartVolts = string.Empty;         // type of alert
		public string GraphStartVolts
		{
			get => _GraphStartVolts;
			set => SetProperty(ref _GraphStartVolts, value);
		}

		private string _GraphEndVolts = string.Empty;         // type of alert
		public string GraphEndVolts
		{
			get => _GraphEndVolts;
			set =>
				SetProperty(ref _GraphEndVolts, value);
		}

		private uint _StepsOctave;         // type of alert
		public uint StepsOctave
		{
			get => _StepsOctave; set => SetProperty(ref _StepsOctave, value);
		}

		private uint _Averages;         // type of alert
		public uint Averages
		{
			get => _Averages; set => SetProperty(ref _Averages, value);
		}

		private int _MeasureType;
		public int MeasureType
		{
			get => _MeasureType;
			set => SetProperty(ref _MeasureType, value);
		}

		private bool _ReadVoltage;
		[JsonIgnore]
		public bool ReadVoltage
		{
			get => _ReadVoltage;
			set => SetProperty(ref _ReadVoltage, value);
		}

		private bool _ReadPower;
		[JsonIgnore]
		public bool ReadPower
		{
			get => _ReadPower;
			set => SetProperty(ref _ReadPower, value);
		}

		private string _rangeTop = string.Empty;
		public string RangeTop
		{
			get { return _rangeTop; }
			set => SetProperty(ref _rangeTop, value);
		}

		private string _rangeBottom = string.Empty;
		public string RangeBottom
		{
			get { return _rangeBottom; }
			set => SetProperty(ref _rangeBottom, value);
		}
		private string _rangeTopdB = string.Empty;
		public string RangeTopdB
		{
			get { return _rangeTopdB; }
			set => SetProperty(ref _rangeTopdB, value);
		}

		private string _rangeBottomdB = string.Empty;
		public string RangeBottomdB
		{
			get { return _rangeBottomdB; }
			set => SetProperty(ref _rangeBottomdB, value);
		}

		private bool _ShowThickLines;
		public bool ShowThickLines
		{
			get => _ShowThickLines;
			set => SetProperty(ref _ShowThickLines, value);
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

		private bool _ShowLeft;
		public bool ShowLeft
		{
			get => _ShowLeft;
			set => SetProperty(ref _ShowLeft, value);
		}

		private bool _ShowRight;
		public bool ShowRight
		{
			get => _ShowRight;
			set => SetProperty(ref _ShowRight, value);
		}
		private bool _ShowTHD;
		public bool ShowTHD
		{
			get => _ShowTHD;
			set => SetProperty(ref _ShowTHD, value);
		}
		private bool _ShowMagnitude;
		public bool ShowMagnitude
		{
			get => _ShowMagnitude;
			set => SetProperty(ref _ShowMagnitude, value);
		}
		private bool _ShowD2;
		public bool ShowD2
		{
			get => _ShowD2;
			set => SetProperty(ref _ShowD2, value);
		}
		private bool _ShowD3;
		public bool ShowD3
		{
			get => _ShowD3;
			set => SetProperty(ref _ShowD3, value);
		}
		private bool _ShowD4;
		public bool ShowD4
		{
			get => _ShowD4;
			set => SetProperty(ref _ShowD4, value);
		}
		private bool _ShowD5;
		public bool ShowD5
		{
			get => _ShowD5;
			set => SetProperty(ref _ShowD5, value);
		}
		private bool _ShowD6;
		public bool ShowD6
		{
			get => _ShowD6;
			set => SetProperty(ref _ShowD6, value);
		}
		private bool _ShowNoiseFloor;
		public bool ShowNoiseFloor
		{
			get => _ShowNoiseFloor;
			set => SetProperty(ref _ShowNoiseFloor, value);
		}
		private uint _SampleRate;
		public uint SampleRate
		{
			get => _SampleRate;
			set => SetProperty(ref _SampleRate, value);
		}
		private uint _FftSize;
		public uint FftSize
		{
			get => _FftSize;
			set => SetProperty(ref _FftSize, value);
		}
		private string _Windowing = string.Empty;
		public string WindowingMethod
		{
			get => _Windowing;
			set => SetProperty(ref _Windowing, value);
		}
		private int _XAxisType;
		public int XAxisType
		{
			get => _XAxisType;
			set => SetProperty(ref _XAxisType, value);
		}
		private Visibility _ToShowRange;
		[JsonIgnore]
		public Visibility ToShowRange
		{
			get => _ToShowRange;
			set => SetProperty(ref _ToShowRange, value);
		}
		private Visibility _ToShowdB;
		[JsonIgnore]
		public Visibility ToShowdB
		{
			get => _ToShowdB;
			set => SetProperty(ref _ToShowdB, value);
		}
		#endregion

		/// <summary>
		/// Generator type changed
		/// </summary>
		private void UpdateGeneratorParameters()
		{
			switch (MeasureType)
			{
				case 0: // Input voltage
				case 1: // Output voltage
					ReadVoltage = true;
					ReadPower = false;
					break;
				case 2: // Output power
					ReadVoltage = false;
					ReadPower = true;
					break;
			}
		}

		// the property change is used to trigger repaints of the graph
		private void CheckPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "MeasureType":
					UpdateGeneratorParameters(); 
					break;
				case "StartVoltage":
					//actThd?.UpdateStartVoltageDisplay();
					break;
				case "EndVoltage":
					//actThd?.UpdateEndVoltageDisplay();
					break;
				case "ShowPercent":
					ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
					ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;
					actThd?.UpdateGraph(true);
					break;
				case "XAxisType":
				case "GraphStartVolts":
				case "GraphEndVolts":
				case "RangeBottomdB":
				case "RangeBottom":
				case "RangeTopdB":
				case "RangeTop":
				case "ShowRight":
				case "ShowLeft":
				case "ShowTHD":
				case "ShowMagnitude":
				case "ShowD2":
				case "ShowD3":
				case "ShowD4":
				case "ShowD5":
				case "ShowD6":
				case "ShowNoiseFloor":
				case "ShowPoints":
				case "ShowThickLines":
					actThd?.UpdateGraph(true);
					break;
				default:
					break;
			}
		}

		private static void StartIt()
		{
			// Implement the logic to start the measurement process
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			vm.actThd.StartMeasurement();
		}

		private static void StopIt()
		{
			var vm = ViewModels.ViewSettings.Singleton.ThdAmp;
			vm.actThd.DoCancel();
		}

		public void SetAction(PlotControl plot, PlotControl plot1, PlotControl plot2)
		{
			ThdAmplitudeData data = new ThdAmplitudeData();
			actThd = new ActThdAmplitude(ref data, plot, plot1, plot2);
			SetupMainPlot(plot);
			actPlot = plot;
		}

		// when the mouse moves in the plotcontrol window it sends a mouseevent to the parent view model (this)
		// here's the tracker event handler
		private static void DoMouseTracked(object sender, MouseEventArgs e)
		{
			var thdAmpVm = ViewSettings.Singleton.ThdAmp;
			thdAmpVm.DoMouse(sender, e);
		}

		private string FormatValue(double value)
		{
			if( ! ShowPercent)
				return MathUtil.FormatLogger(value) + " dB";
			return MathUtil.FormatPercent(Math.Pow(10,value/20 + 2)) + " %";
		}

		private string FormatColumn( ThdColumn column )
		{
			var vm = ViewSettings.Singleton.ThdAmp;
			string sout = "Mag: ";
			if (ShowPercent)
			{
				var MagValue = Math.Pow(10, column.Mag / 20);
				sout += MathUtil.FormatVoltage(MagValue);
			}
			else
			{
				sout += MathUtil.FormatLogger(column.Mag) + " dBV";
			}
			sout += Environment.NewLine;

			if( vm.ShowTHD)
				sout += "THD: " + FormatValue(column.THD) + Environment.NewLine;
			if (vm.ShowNoiseFloor)
				sout += "Noise: " + FormatValue(column.Noise) + Environment.NewLine;
			if (vm.ShowD2)
				sout += "D2: " + FormatValue(column.D2) + Environment.NewLine;
			if (vm.ShowD3)
				sout += "D3: " + FormatValue(column.D3) + Environment.NewLine;
			if (vm.ShowD4)
				sout += "D4: " + FormatValue(column.D4) + Environment.NewLine;
			if (vm.ShowD5)
				sout += "D5: " + FormatValue(column.D5) + Environment.NewLine;
			if (vm.ShowD6)
				sout += "D6+: " + FormatValue(column.D6P) + Environment.NewLine;
			return sout;
		}

		private void DoMouse(object sender, MouseEventArgs e)
		{

			if (e.LeftButton == MouseButtonState.Pressed && !IsMouseDown)
			{
				IsTracking = !IsTracking;
				IsMouseDown = true;
			}
			else
			if (e.LeftButton == MouseButtonState.Released && IsMouseDown)
			{
				IsMouseDown = false;
			}
			if (IsTracking)
			{
				var p = e.GetPosition(actPlot);
				var cord = ConvertScottCoords(actPlot, p.X, p.Y);
				FreqValue = Math.Pow(10, cord.Item1); // amplitude actually
			}
			Tuple<ThdColumn?, ThdColumn?> zv = actThd.LookupX(FreqValue);
			ZValue = string.Empty;
			if( zv.Item1 != null )
			{
				FreqShow = Math.Pow(10, zv.Item1.Amplitude/20).ToString("0.### V");
				if (zv.Item2 != null)
					ZValue += "Left: " + Environment.NewLine;
				ZValue += FormatColumn( zv.Item1 );
			}
			if (zv.Item2 != null)
			{
				if (zv.Item1 == null)
					FreqShow = Math.Pow(10, zv.Item2.Amplitude / 20).ToString("0.### V");
				else
					ZValue += "Right: " + Environment.NewLine;
				ZValue += FormatColumn(zv.Item2);
			}
		}

		~ThdAmpViewModel()
		{
			PropertyChanged -= CheckPropertyChanged;
			MouseTracked -= DoMouseTracked;
		}

		public ThdAmpViewModel()
		{
			PropertyChanged += CheckPropertyChanged;
			MouseTracked += DoMouseTracked;

			actPlot = default!;
			actThd = default!;

			AmpLoad = 8;
			TestFreq = "1000";
			GraphStartVolts = "0.002";
			GraphEndVolts = "10";
			StepsOctave = 1;
			Averages = 3;

			MeasureType = 0;
			ReadVoltage = true;
			ReadPower = false;

			RangeTop = "1";             // when graphing percents distortion this is logarithmic 0.01....
			RangeBottom = "0.001";

			ShowThickLines = true;
			ShowPoints = false;
			ShowPercent = false;
			ShowLeft = true;
			ShowRight = false;
			ShowTHD = true;
			ShowMagnitude = true;
			ShowD2 = true;
			ShowD3 = true;
			ShowD4 = true;
			ShowD5 = true;
			ShowD6 = true;
			ShowNoiseFloor = true;

			SampleRate = 96000;
			FftSize = 65536*2;
			WindowingMethod = "Hann";
			RangeTopdB = "20";
			RangeBottomdB = "-180";

			XAxisType = 0;
			StartVoltage = "0.1";
			EndVoltage = "1";
			StartPower = "0.5";
			EndPower = "5";

			ToShowdB = ShowPercent ? Visibility.Collapsed : Visibility.Visible;
			ToShowRange = ShowPercent ? Visibility.Visible : Visibility.Collapsed;          
			// make a few things happen to synch the gui
			Task.Delay(1000).ContinueWith(t => { actThd?.UpdateGraph(true); });
		}
	}
}

