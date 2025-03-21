using FftSharp;
using ScottPlot.Colormaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QA40xPlot.Libraries
{
    public interface QAMath
    {
		// given a left-right time series representing a sine wave it finds the gain/phase at freq
		// using left input as data and right input as reference (source)
		public static System.Numerics.Complex CalculateGainPhase(double fundamentalFreq, LeftRightSeries measuredSeries)
		{
			var measuredTimeSeries = measuredSeries.TimeRslt;
			if(measuredTimeSeries == null)
				return Complex.Zero;

			var m2 = Math.Sqrt(2);
			// Left channel
			var window = new FftSharp.Windows.Hanning();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left, true);
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right, true);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);
			//var old = measuredSeries.FreqRslt;
			if( measuredSeries.FreqRslt == null || measuredSeries.FreqRslt.Left == null)
			{
				measuredSeries.FreqRslt = new();
				measuredSeries.FreqRslt.Left = spectrum_measured.Select(x => x.Magnitude * m2).ToArray();
				measuredSeries.FreqRslt.Right = spectrum_ref.Select(x => x.Magnitude * m2).ToArray();
				var nca2 = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				measuredSeries.FreqRslt.Df = nca2 / (double)spectrum_measured.Length; // ???
			}

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);		// total time in tics = sample rate
				var fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFreq, (uint)nca, (uint)measuredTimeSeries.Left.Length);
				var ratio = spectrum_measured[fundamentalBin] / spectrum_ref[fundamentalBin];
				u = ratio;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			return u;
		}

		// given a left-right time series representing a sine wave it finds the gain/phase at freq
		// using left input as data and right input as reference (source)
		public static System.Numerics.Complex CalculateDualGain(double fundamentalFreq, LeftRightSeries measuredSeries)
		{
			var measuredTimeSeries = measuredSeries.TimeRslt;
			if( measuredTimeSeries == null)
				return Complex.Zero;

			var m2 = Math.Sqrt(2);
			// Left channel
			var window = new FftSharp.Windows.Hanning();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left, true);	// true == normalized by # elements
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right, true);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);
			if (measuredSeries.FreqRslt == null || measuredSeries.FreqRslt.Left == null)
			{
				measuredSeries.FreqRslt = new();
				measuredSeries.FreqRslt.Left = spectrum_measured.Select(x => x.Magnitude * m2).ToArray();
				measuredSeries.FreqRslt.Right = spectrum_ref.Select(x => x.Magnitude * m2).ToArray();
				var nca2 = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				measuredSeries.FreqRslt.Df = nca2 / (double)spectrum_measured.Length; // ???
			}

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				var fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFreq, (uint)nca, (uint)measuredTimeSeries.Left.Length);
				double left = spectrum_measured[fundamentalBin].Magnitude * m2;
				double right = spectrum_ref[fundamentalBin].Magnitude * m2;
				u = new Complex(left, right);	// pack it in stupidly
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			return u;
		}
	}
}
