using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FftSharp;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.Views;
using ScottPlot;
using Xceed.Wpf.Toolkit;

namespace QA40xPlot.ViewModels
{
	public abstract class BaseViewModel : FloorViewModel, IMouseTracker
	{
		// public INavigation ViewNavigator { get; set; }
		#region Shared Properties
		public static List<String> EndFrequencies { get => new List<string> { "1000", "2000", "5000", "10000", "20000", "50000", "100000" }; }
		public static List<String> StartFrequencies { get => new List<string> { "5", "10", "20", "50", "100", "200", "500", "1000", "5000", "10000" }; }
		public static List<String> TopDbs { get => new List<string> { "100", "50", "20", "0", "-50", "-80"  }; }
		public static List<String> BottomDbs { get => new List<string> { "0", "-50", "-100", "-120", "-140", "-160", "-180", "-200" }; }
		public static List<String> StartPercents { get => new List<string> { "100", "10", "1", "0.1", "0.01" }; }
		public static List<String> EndPercents { get => new List<string> { "0.1", "0.01", "0.001", "0.0001", "0.00001", "0.000001" }; }
		public static List<String> SampleRates { get => new List<string> { "48000", "96000", "192000", "384000" }; }
		public static List<String>	FftSizes { get => new List<string> { "64K", "128K", "256K", "512K", "1024K" }; }
		public static List<uint>	FftActualSizes { get => new List<uint> { 65536, 131072, 262144, 524288, 1048576 }; }
		public static List<String> GenVoltages { get => new List<string> { "0.05", "0.1", "0.25", "0.5", "0.75", "1", "2", "5" }; }
		public static List<String> GenPowers { get => new List<string> { "0.05", "0.1", "0.25", "0.5", "0.75", "1", "2", "5", "10", "25" }; }
		public static List<String> MeasureVolts { get => new List<string> { "Input Voltage", "Output Voltage" }; }
		public static List<String> MeasureVoltsFull { get => new List<string> { "Input Voltage", "Output Voltage", "Output Power" }; }
		public static List<String> Impedances { get => new List<string> { "5", "8", "10", "20", "100", "500", "1000" }; }
		public static string DutInfo { get => "DUT = Device Under Test"; }
		public static string DutDescript { get => "Input Voltage = DUT Input(Generator Output), Output Voltage = DUT Output(QA40x Input)"; }
		public static string AutoRangeDescript { get => "When the test is started a safe Attenuation value is calculated based on a test at 42."; }
		#endregion

		#region Setters and Getters
		// the power display variables, all readonly
		[JsonIgnore]
		public bool IsGenPower { get => (GenDirection == MeasureVoltsFull[2]); }
		[JsonIgnore]
		public string GenAmpDescript { get => (IsGenPower ? "Power" : "Voltage"); }
		[JsonIgnore]
		public string GenAmpUnits { get => (IsGenPower ? "W" : "V"); }

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

		private string _GenDirection = string.Empty;
		public string GenDirection
		{
			get => _GenDirection;
			set { 
				SetProperty(ref _GenDirection, value);
				OnPropertyChanged("GenAmpDescript"); 
				OnPropertyChanged("GenAmpUnits"); 
			}
		}
		private double _Attenuation;
		public double Attenuation
		{
			get => _Attenuation;
			set => SetProperty(ref _Attenuation, value);
		}
		#endregion

		#region Output Setters and Getters
		private bool _IsRunning = false;         // type of alert
		[JsonIgnore]
		public bool IsRunning
		{
			get { return _IsRunning; }
			set { SetProperty(ref _IsRunning, value); IsNotRunning = !value; }
		}
		private bool _IsNotRunning = true;         // type of alert
		[JsonIgnore]
		public bool IsNotRunning
		{
			get { return _IsNotRunning; }
			private set { SetProperty(ref _IsNotRunning, value); }
		}

		private string _XPos = string.Empty;
		[JsonIgnore]
		public string XPos
		{
			get => _XPos;
			set => SetProperty(ref _XPos, value);
		}

		private string _ZValue = string.Empty;
		[JsonIgnore]
		public string ZValue
		{
			get => _ZValue;
			set => SetProperty(ref _ZValue, value);
		}

		private double _FreqValue = 0.0;
		[JsonIgnore]
		public double FreqValue
		{
			get => _FreqValue;
			set => SetProperty(ref _FreqValue, value);
		}

		private string _FreqShow = string.Empty;
		[JsonIgnore]
		public string FreqShow
		{
			get => _FreqShow;
			set => SetProperty(ref _FreqShow, value);
		}

		bool isBusy = false;
		[JsonIgnore]
		public bool IsBusy
		{
			get { return isBusy; }
			set { SetProperty(ref isBusy, value); }
		}

		bool _isTracking = true;
		[JsonIgnore]
		public bool IsTracking
		{
			get { return _isTracking; }
			set { SetProperty(ref _isTracking, value); }
		}

		bool _isMouseDown = false;
		[JsonIgnore]
		public bool IsMouseDown
		{
			get { return _isMouseDown; }
			set { SetProperty(ref _isMouseDown, value); }
		}

		private bool _HasExport = false;
		[JsonIgnore]
		public bool HasExport
		{
			get { return _HasExport; }
			set { SetProperty(ref _HasExport, value); }
		}
		#endregion

		/// <summary>
		/// Convert direction string to a direction type
		/// this works for 2-value and 3-value answers
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public E_GeneratorDirection ToDirection(string  direction)
		{
			var u = MeasureVoltsFull.IndexOf(direction);
			if (u == -1)
				u = 0;
			return (E_GeneratorDirection)u;
		}

		private double GainForVolts(double frequency, LeftRightFrequencySeries? lrGains)
		{
			if (null == lrGains)
				return 1.0;
			if( frequency == 0 || lrGains.Left.Length == 1)
			{
				return Math.Max(lrGains.Left.Max(), lrGains.Right.Max());
			}
			// frequency non-zero search and we have a vector so find it
			var bin = (int)Math.Floor(frequency / lrGains.Df);
			// take +-2 bins also
			var skips = Math.Max(0,bin - 2);
			var takes = Math.Min(5, lrGains.Left.Length);
			var mxl = lrGains.Left.Skip(skips).Take(takes).Max();
			var mxr = lrGains.Right.Skip(skips).Take(takes).Max();
			if( ShowLeft && !ShowRight)
			{
				return mxl;
			}
			return Math.Max(mxl, mxr);
		}

		/// <summary>
		/// convert a string value in the gui to an input or output voltage
		/// based on the current generator direction
		/// </summary>
		/// <param name="amplitude">string amplitude with type depending on generator direction</param>
		/// <param name="frequency">frequency of interested or 0 for all</param>
		/// <param name="isInput">dut input or dut output voltage</param>
		/// <param name="lrGains">the gain calculations</param>
		/// <returns></returns>
		public double ToGenVoltage(string amplitude, double frequency, bool isInput, LeftRightFrequencySeries? lrGains)
		{
			var genType = ToDirection(GenDirection);
			var vtest = MathUtil.ToDouble(amplitude, 4321);
			if (vtest == 4321)
				return 1e-5;
			switch (genType)
			{
				case E_GeneratorDirection.INPUT_VOLTAGE:
					if (lrGains != null && !isInput)
					{
						vtest *= Math.Max(lrGains.Left.Max(), lrGains.Right.Max()); // max expected DUT output voltage
					}
					break;
				case E_GeneratorDirection.OUTPUT_VOLTAGE:
					if (lrGains != null && isInput)
						vtest /= Math.Max(lrGains.Left.Max(), lrGains.Right.Max()); // expected QA40x generator voltage
					break;
				case E_GeneratorDirection.OUTPUT_POWER:
					// now vtest is actually power setting... so convert to voltage
					vtest = Math.Sqrt(vtest * ViewSettings.AmplifierLoad);  // so sqrt(power * load) = output volts
					if (lrGains != null && isInput)
						vtest /= Math.Max(lrGains.Left.Max(), lrGains.Right.Max()); // expected QA40x generator voltage
					break;
			}
			return vtest;
		}

		public 	BaseViewModel()
		{
			HasExport = false;
			GenDirection = MeasureVolts[0];
			ShowLeft = true;
			ShowRight = true;
		}

		public void SetupMainPlot(PlotControl plot)
		{
			plot.TrackMouse = true;
			plot.GrandParent = this;
		}

		// convert mouse coordinates to scottplot coordinates
		// from ScottPlot github comment https://github.com/ScottPlot/ScottPlot/issues/3514
		public Tuple<double,double> ConvertScottCoords(PlotControl plt, double x, double y)
		{
			PresentationSource source = PresentationSource.FromVisual(plt);
			double dpiX = 1;
			double dpiY = 1;
			if (source != null)
			{
				dpiX = source.CompositionTarget.TransformToDevice.M11;
				dpiY = source.CompositionTarget.TransformToDevice.M22;
			}

			Pixel mousePixel = new(x * dpiX, y * dpiY);
			var ep = plt.ThePlot.GetCoordinates(mousePixel);
			double XPos = ep.X;
			double YPos = ep.Y;
			return Tuple.Create(XPos, YPos);
		}

		#region IMouseHandler
		public event MouseEventHandler? MouseTracked;

		// when the mouse moves we send it up to the actual view model
		protected void OnMouseTracked([CallerMemberName] string? propertyName = "")
		{
			var changed = MouseTracked;
			if (changed == null)
				return;
			var md = InputManager.Current.PrimaryMouseDevice;
			changed.Invoke(this, new MouseEventArgs(md, Environment.TickCount));
		}

		/// <summary>
		/// RaisePropertyChanged
		/// Tell the window a mouse event has happened
		/// </summary>
		/// <param name="propertyName"></param>
		public void RaiseMouseTracked(string? propertyName = null)
		{
			OnMouseTracked(propertyName);
		}

		#endregion
	}
}
