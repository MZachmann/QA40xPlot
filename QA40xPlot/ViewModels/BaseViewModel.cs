using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using QA40xPlot.Views;
using ScottPlot;

namespace QA40xPlot.ViewModels
{
	public abstract class BaseViewModel : INotifyPropertyChanged, IMouseTracker
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

		#endregion

		#region Setters and Getters
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
		#endregion

		private string _XPos;
		[JsonIgnore]
		public string XPos
		{
			get => _XPos;
			set => SetProperty(ref _XPos, value);
		}

		private string _ZValue;
		[JsonIgnore]
		public string ZValue
		{
			get => _ZValue;
			set => SetProperty(ref _ZValue, value);
		}

		private double _FreqValue;
		[JsonIgnore]
		public double FreqValue
		{
			get => _FreqValue;
			set => SetProperty(ref _FreqValue, value);
		}

		private string _FreqShow;
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

		protected bool SetProperty<T>(ref T backingStore, T value,
			[CallerMemberName] string propertyName = "",
			Action? onChanged = null)
		{
			if (EqualityComparer<T>.Default.Equals(backingStore, value))
				return false;

			backingStore = value;
			onChanged?.Invoke();
			OnPropertyChanged(propertyName);
			return true;
		}

		public 	BaseViewModel()
		{
			HasExport = false;
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

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged;
		public event MouseEventHandler? MouseTracked;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = "")
		{
			var changed = PropertyChanged;
			if (changed == null)
				return;
			changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

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

		/// <summary>
		/// RaisePropertyChanged
		/// Tell the window a property has changed
		/// </summary>
		/// <param name="propertyName"></param>
		protected void RaisePropertyChanged(string? propertyName = null)
		{
			OnPropertyChanged(propertyName);
		}
		#endregion
	}
}
