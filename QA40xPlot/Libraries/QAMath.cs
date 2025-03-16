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
		public static System.Numerics.Complex CalculateGainPhase(double fundamentalFreq, LeftRightTimeSeries measuredTimeSeries)
		{
			// Left channel
			var window = new FftSharp.Windows.Hanning();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left);
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured);

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref);

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);		// total time in tics = sample rate
				var fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFreq, (uint)nca, (uint)measuredTimeSeries.Left.Length);
				var phase = (spectrum_measured[fundamentalBin].Phase - spectrum_ref[fundamentalBin].Phase);
				if (phase< -Math.PI)
					phase = phase + 2*Math.PI;
				else if (phase > Math.PI)
					phase = phase - 2*Math.PI;
				double gain = 1.0;
				gain = spectrum_measured[fundamentalBin].Magnitude / spectrum_ref[fundamentalBin].Magnitude;
				u = Complex.FromPolarCoordinates(gain, phase);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			return u;
		}

		// given a left-right time series representing a sine wave it finds the gain/phase at freq
		// using left input as data and right input as reference (source)
		public static System.Numerics.Complex CalculateDualGain(double fundamentalFreq, LeftRightTimeSeries measuredTimeSeries)
		{
			// Left channel
			var window = new FftSharp.Windows.Hanning();
			double[] windowed_measured = window.Apply(measuredTimeSeries.Left);
			System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured).ToList().Select(x => x * measuredTimeSeries.dt).ToArray();

			double[] windowed_ref = window.Apply(measuredTimeSeries.Right);
			System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref).ToList().Select(x => x * measuredTimeSeries.dt).ToArray(); ;

			System.Numerics.Complex u = new();
			try
			{
				var nca = (int)(0.01 + 1 / measuredTimeSeries.dt);      // total time in tics = sample rate
				var fundamentalBin = QaLibrary.GetBinOfFrequency(fundamentalFreq, (uint)nca, (uint)measuredTimeSeries.Left.Length);
				var phase = (spectrum_measured[fundamentalBin].Phase - spectrum_ref[fundamentalBin].Phase);
				if (phase < -Math.PI)
					phase = phase + 2 * Math.PI;
				else if (phase > Math.PI)
					phase = phase - 2 * Math.PI;
				double gain = 1.0;
				gain = spectrum_measured[fundamentalBin].Magnitude;
				double gain2 = 1.0;
				gain2 = spectrum_ref[fundamentalBin].Magnitude;
				u = new Complex(gain, gain2);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

			return u;
		}
	}
}
