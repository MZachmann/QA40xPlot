using QA40xPlot.BareMetal;
using QA40xPlot.ViewModels;
using System.Net.Sockets;
using System.Windows;

namespace QA40xPlot.Libraries
{
	internal static class QaREST
	{

		/// <summary>
		/// Do the startup of the QA40x, checking the rest interface for existance
		/// </summary>
		/// <param name="sampleRate"></param>
		/// <param name="fftsize"></param>
		/// <param name="Windowing"></param>
		/// <param name="attenuation"></param>
		/// <param name="setdefault">this may take a little time, so do it once?</param>
		/// <returns>success true or false</returns>
		public static async Task<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation, bool setdefault = false)
		{
			try
			{
				// ********************************************************************  
				// Load a settings we want
				// ********************************************************************  
				if (setdefault)
				{
					// Check if REST interface is available and device connected
					if (await QaREST.CheckDeviceConnected() == false)
						return false;

					await Qa40x.SetDefaults();
					// call the QaComm version to save the variable values for other stuff
				}
				// call the QaComm version to save the variable values for other stuff
				await QaComm.SetOutputSource(OutputSources.Off);
				await QaComm.SetSampleRate(sampleRate);
				await QaComm.SetFftSize(fftsize);
				await QaComm.SetWindowing(Windowing);
				await Qa40x.SetRoundFrequencies(true);
				await QaComm.SetInputRange(attenuation);
				return true;
			}
			catch (Exception)
			{
			}
			return false;
		}

		static public async Task<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFrequencySeries = true, bool getTimeSeries = true)
		{
			LeftRightSeries lrfs = new LeftRightSeries();

			await Qa40x.DoAcquisition();
			if (ct.IsCancellationRequested || lrfs == null)
				return lrfs ?? new();
			if (getFrequencySeries)
			{
				lrfs.FreqRslt = await Qa40x.GetInputFrequencySeries();
				if (ct.IsCancellationRequested || lrfs.FreqRslt == null)
					return lrfs;
			}
			if (getTimeSeries)
			{
				lrfs.TimeRslt = await Qa40x.GetInputTimeSeries();
				if (ct.IsCancellationRequested)
					return lrfs;
			}

			if (averages <= 1)
				return lrfs;        // Only one measurement

			if (getFrequencySeries && lrfs.FreqRslt != null)
			{
				for (int i = 1; i < averages; i++)
				{
					await Qa40x.DoAcquisition();
					if (ct.IsCancellationRequested)
						return lrfs;
					LeftRightSeries lrfs2 = new LeftRightSeries();
					lrfs2.FreqRslt = await Qa40x.GetInputFrequencySeries();
					for (int j = 0; j < lrfs.FreqRslt.Left.Length; j++)
					{
						lrfs.FreqRslt.Left[j] += lrfs2.FreqRslt.Left[j];
						lrfs.FreqRslt.Right[j] += lrfs2.FreqRslt.Right[j];
					}
				}

				for (int j = 0; j < lrfs.FreqRslt.Left.Length; j++)
				{
					lrfs.FreqRslt.Left[j] = lrfs.FreqRslt.Left[j] / averages;
					lrfs.FreqRslt.Right[j] = lrfs.FreqRslt.Right[j] / averages;
				}
			}

			return lrfs;
		}

		static public async Task<LeftRightSeries> DoAcquireUser(CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			LeftRightSeries lrfs = new LeftRightSeries();
			await Qa40x.DoUserAcquisition(dataLeft, dataRight);

			if (ct.IsCancellationRequested )
				return lrfs;

			{
				lrfs.TimeRslt = await Qa40x.GetInputTimeSeries();
				var gain = MathUtil.ToDouble(ViewSettings.ExternalGain, 0);
				if (gain != 0.0)
				{
					gain = 1.0 / QaLibrary.ConvertVoltage(gain, Data.E_VoltageUnit.dBV, Data.E_VoltageUnit.Volt);	// linearize
					lrfs.TimeRslt.Left = lrfs.TimeRslt.Left.Select(x => x * gain).ToArray();
					lrfs.TimeRslt.Right = lrfs.TimeRslt.Right.Select(x => x * gain).ToArray();
				}
				if (ct.IsCancellationRequested)
					return lrfs;
			}

			if(getFreq)
			{
				// we do the math here to support more windowing options
				// but more importantly we scaled the time result for an external gain device
				var windowing = QaComm.GetWindowing();
				lrfs.FreqRslt = QaMath.CalculateSpectrum(lrfs.TimeRslt, windowing);
			}

			//if (getFreq)
			//{
			//	lrfs.FreqRslt = await Qa40x.GetInputFrequencySeries();
			//	if (ct.IsCancellationRequested || lrfs.FreqRslt == null)
			//		return lrfs;
			//}

			return lrfs;        // Only one measurement
		}

		/// <summary>
		/// This method checks if the server is running by attempting to connect to it on localhost at port 9402.
		/// </summary>
		/// <returns></returns>
		public static bool IsServerRunning()
		{
			using (Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				var result = socket.BeginConnect("localhost", 9402, null, null);

				// test the connection for 3 seconds
				bool success = result.AsyncWaitHandle.WaitOne(1000, false);

				var resturnVal = socket.Connected;
				if (socket.Connected)
					socket.Disconnect(true);

				return resturnVal;
			}
		}

		static public async Task<bool> CheckDeviceConnected()
		{
			// Check if webserver available
			if (!IsServerRunning())
			{
				MessageBox.Show($"QA40X application is not running.\nPlease start the application first.", "Could not reach webserver", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}

			// Check if device connected
			if (!await Qa40x.IsConnected())
			{
				MessageBox.Show($"QA40X analyser is not connected via USB.\nPlease connect the device first.", "QA40X not connected", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}

			bool busy = await Qa40x.IsBusy();
			if (busy)
			{
				MessageBox.Show($"The QA40x seems to be already runnng. Stop the aqcuisition and generator in the QuantAsylum software manually.", "QA40X busy", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}

			return true;
		}



	}
}
