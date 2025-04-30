using FftSharp;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using System.Diagnostics;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40x_BareMetal
{
    /// <summary>
    /// A simple class to help wrap async transfers
    /// </summary>
    class AsyncResult
    {
        /// <summary>
        /// This is specific to the underlying library. LIBUSBDOTNET uses UsbTransfer. 
        /// WinUsbDotNet uses IAsyncResult.
        /// </summary>
        UsbTransfer UsbXfer;

        /// <summary>
        /// Buffer of data to be received.
        /// </summary>
        public byte[] ReadBuffer;

        /// <summary>
        /// This will change depending on lib used.
        /// </summary>
        /// <param name="usb"></param>
        public AsyncResult(UsbTransfer usb, byte[] readBuffer)
        {
			UsbXfer = usb;
			ReadBuffer = readBuffer;
        }

        /// <summary>
        /// Waits until the data associated with this USB object has been read from 
        /// or written to, or timed out
        /// </summary>
        /// <returns></returns>
        public int Wait()
        {
            UsbXfer.Wait(out int transferred);
            return transferred;
        }
    }

	/// <summary>
	/// This class is solely used to communicate with the USB device. It handles the
    /// higher level calls and basic read/writes. It also has replacment calls
    /// for the common QaLibrary methods such as DoAcquisition and InitializeDevice.
	/// </summary>
	static class QaUsb
    {
		// singleton QaAnalyzer object - works but...
		private static QaAnalyzer? _qAnalyzer;
		public static QaAnalyzer? QAnalyzer => _qAnalyzer;

        static object ReadRegLock = new object();

        static List<AsyncResult> WriteQueue = new List<AsyncResult>();
        static List<AsyncResult> ReadQueue = new List<AsyncResult>();

        static readonly int RegReadWriteTimeout = 20;
        static readonly int MainI2SReadWriteTimeout = 2000;

        public static async Task<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFreq = true)
        {
			var datapt = new double[QAnalyzer?.Params?.FFTSize ?? 0];
            if( QAnalyzer?.Params?.OutputSource == OutputSources.Sine)
            {
				var gp1 = WaveGenerator.Singleton.GenParams;
				var gp2 = WaveGenerator.Singleton.Gen2Params;
				double dt = 1.0 / (QAnalyzer?.Params.SampleRate ?? 1);
				if (gp1?.Enabled == true)
				{
					datapt = datapt.Select((x, index) => x + (gp1.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp1.Frequency * dt * index)).ToArray();
				}
				if (gp2?.Enabled == true)
				{
					datapt = datapt.Select((x, index) => x + (gp2.Voltage * Math.Sqrt(2)) * Math.Sin(2 * Math.PI * gp2.Frequency * dt * index)).ToArray();
				}
			}
			var lrfs = await DoAcquireUser(averages, ct, datapt, datapt, getFreq);
			return lrfs;
		}

        public static void SetInputRange(int range)
		{
			if (QAnalyzer == null )
				return;
            if(QAnalyzer.Params == null)
            {
                Debug.WriteLine("*** Write to null params");
                return;
			}
			QAnalyzer.SetInput(range);
		}

		public static int GetInputRange()
		{
            return QAnalyzer?.Params?.MaxInputLevel ?? 42;
		}

		public static void SetOutputRange(int range)
		{
			if (QAnalyzer == null || QAnalyzer.Params == null)
				return;
			QAnalyzer.SetOutput(range);
		}

        public static LeftRightFrequencySeries? CalculateFreq(LeftRightTimeSeries? lrts)
        {
            if( lrts == null || QAnalyzer?.Params == null)
				return null;

			// calculate the frequency spectrum
			return QaMath.CalculateSpectrum(lrts, QAnalyzer.Params.WindowType);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="datapt"></param>
		/// <param name="getFreq"></param>
		/// <returns></returns>
		public static async Task<LeftRightSeries> DoAcquireUser(uint averages, CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			LeftRightSeries lrfs = new LeftRightSeries();
            var dpt = new double[dataLeft.Length];
			List<AcqResult> runList = new List<AcqResult>();
            // set the output amplitude to support the data
            var maxOut = Math.Max(dataLeft.Max(), dataRight.Max());
			var minOut = Math.Min(dataLeft.Min(), dataRight.Min());
            maxOut = Math.Max(Math.Abs(maxOut), Math.Abs(minOut));  // maximum output voltage
            // don't bother setting output amplitude if we have no output
			var mlevel = Control.DetermineOutput(1.1 * ((maxOut > 0) ? maxOut : 1e-8) ); // the setting for our voltage + 10%
			SetOutputRange(mlevel); // set the output voltage

			for (int rrun = 0; rrun < averages; rrun++)
                {
                    try
                    {
                        var newData = await Acquisition.DoStreamingAsync(ct, dataLeft, dataRight);
                        if (ct.IsCancellationRequested || lrfs == null || newData.Valid == false)
                            return lrfs ?? new();
                        runList.Add(newData);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error: {ex.Message}");
                        return lrfs ?? new();
                    }
                }

			{
                lrfs.TimeRslt = new();
                if (runList.Count == 1)
                {
                    lrfs.TimeRslt.Left = runList.First().Left;
                    lrfs.TimeRslt.Right = runList.First().Right;
                }
                else
                {
					var left = runList.First().Left;
					var right = runList.First().Right;
                    for(int i=1; i<runList.Count; i++)
					{
						left = left.Zip(runList[i].Left, (x, y) => x + y).ToArray();
						right = right.Zip(runList[i].Right, (x, y) => x + y).ToArray();
					}
                    lrfs.TimeRslt.Left = left.Select(x => x / runList.Count).ToArray();
                    lrfs.TimeRslt.Right = right.Select(x => x / runList.Count).ToArray();
				}
				lrfs.TimeRslt.dt = 1.0 / (_qAnalyzer?.Params?.SampleRate ?? 1);
			}
			if (ct.IsCancellationRequested)
				return lrfs;

            if (getFreq)
            {
				lrfs.FreqRslt = CalculateFreq(lrfs.TimeRslt);
            }
			return lrfs;        // Only one measurement
		}

		/// <summary>
		/// Do the startup of the QA40x, checking the rest interface for existance
		/// </summary>
		/// <param name="sampleRate"></param>
		/// <param name="fftsize"></param>
		/// <param name="Windowing"></param>
		/// <param name="attenuation"></param>
		/// <param name="setdefault">this may take a little time, so do it once?</param>
		/// <returns>success true or false</returns>
		public static bool InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			try
			{
                // ********************************************************************  
                // Load a settings we want
                // ********************************************************************  
                if (_qAnalyzer == null )
                {
					Open();
                }
				var qan = _qAnalyzer;
				//if (setdefault && qan != null)
    //            {
    //                qan?.DataReader?.Reset();
    //            }
				if (qan == null || qan.Params == null)
					return false;

				qan.SetSampleRate((int)sampleRate);
				qan.SetInput(attenuation);
                //qan.SetOutput(18); // this is set when we do an acquisition based on the voltage output data
                qan.Params.SetWindowing(Windowing);
				qan.Params.FFTSize = (int)fftsize;
                WaveGenerator.Singleton.GenParams.Enabled = false;
				WaveGenerator.Singleton.Gen2Params.Enabled = false;
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error: {ex.Message}");
			}
			return false;
		}

        /// <summary>
        /// SetOutputSource
        /// </summary>
        /// <param name="src"></param>
        /// <returns>true if it worked</returns>
        public static bool SetOutputSource(OutputSources src)
		{
			var qan = _qAnalyzer;
			if (qan == null || qan.Params == null)
				return false;
            qan.Params.OutputSource = src;
			return true;
		}

		/// <summary>
		/// Attempts to open the USB connection to a device
		/// </summary>
		/// <returns></returns>
		public static bool Open()
        {
            Random r = new Random();
            try
            {
                _qAnalyzer = new QaAnalyzer();
                _qAnalyzer?.Init();

                try
                {
                    if (VerifyConnection())
                    {
                        return true;
                    }

                }
                catch (Exception )
                {
                   
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Closes a USB connection (if it's open)
        /// </summary>
        /// <returns></returns>
        static public bool Close(bool OnExit)
        {
            try
            {
                if (_qAnalyzer != null)
                {
					// Stop streaming. This also extinguishes the RUN led
					WriteRegister(8, 0);
                    //
					_qAnalyzer.Close(OnExit);
					_qAnalyzer = null;
                }

                // Needed for linux, harmless for win
                UsbDevice.Exit();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
			}

            return false;
        }

        /// <summary>
        /// Generates a random number, writes that to register 0, and attempts to read that same value back.
        /// </summary>
        /// <returns></returns>
        static public bool VerifyConnection()
        {
            uint val;

            unchecked
            {
                val = Convert.ToUInt32(new Random().Next());
            }

            QaUsb.WriteRegister(0, val);

            if (QaUsb.ReadRegister(0) == val)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Performs a read on a USB register
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        static public UInt32 ReadRegister(byte reg)
        {
            byte[] data = new byte[4];
            UInt32 val;

            // Lock so reads (two step USB operation) can't be broken up by writes (single step USB operation). We need to 
            // consider there can be USB writes from the main (UI) thread, and also from the aquisition thread. So, it's 
            // important to ensure a read doesn't get broken up
            lock (ReadRegLock)
            {
                try
                {
                    byte[] txBuf = WriteRegisterPrep((byte)(0x80 + reg), 0);
                    WriteRegisterRaw(txBuf);
                    int len = 0;
                    QAnalyzer?.RegisterReader?.Read(data, RegReadWriteTimeout, out len);

                    if (len == 0)
                        throw new Exception($"Usb.ReadRegister failed to read data. Register: {reg}");

                    val = (UInt32)((data[0] << 24) + (data[1] << 16) + (data[2] << 8) + data[3]);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
					throw;
                }
            }

            return val;
        }

        /// <summary>
        /// Writes to a USB register
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="val"></param>
        static public void WriteRegister(byte reg, uint val)
        {
            // Values greater than or equal to 0x80 signify a read. Not allowed here
            if (reg >= 0x80)
            {
                throw new Exception("Usb.WriteRegister(): Invalid register");
            }

            byte[] buf = WriteRegisterPrep(reg, val);

            lock (ReadRegLock)
            {
                WriteRegisterRaw(buf);
            }
        }

        static void WriteRegisterRaw(byte[] data)
        {
            int len = data.Length;
            try
            {
				QAnalyzer?.RegisterWriter?.Write(data, RegReadWriteTimeout, out len);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
				throw;
            }
        }

        static byte[] WriteRegisterPrep(byte reg, uint val)
        {
            byte[] array = BitConverter.GetBytes(val).Reverse().ToArray();
			byte[] r = new byte[5];

            r[0] = reg;
            Array.Copy(array, 0, r, 1, 4);
            return r;
        }

        //
        // METHODS BELOW MUST BE CALLED FROM A SINGLE THREAD. The code below is simply a wrapper for overlapped IO.
        // Since the data acquistion is exclusively performed its own thread this isn't an issue. The root issue is
        // that the c# List<AsyncResult> type isn't thread safe. Thus the restriction
        //

        static public void InitOverlapped()
        {
            WriteQueue.Clear();
            ReadQueue.Clear();
        }

        /// <summary>
        /// Submits a buffer to be written and returns immediately. The submitted buffer is 
        /// copied to a local buffer before returning.
        /// </summary>
        /// <param name="data"></param>
        static public void WriteDataBegin(byte[] data, int offset, int len)
        {
            ErrorCode ec;

            if (len == 0)
                return;

            byte[] localBuf = new byte[len];
            Array.Copy(data, offset, localBuf, 0, len);
			UsbTransfer? ar = null;
			ec = QAnalyzer?.DataWriter?.SubmitAsyncTransfer(localBuf, 0, localBuf.Length, MainI2SReadWriteTimeout, out ar) ?? ErrorCode.UnknownError;
            if (ec != ErrorCode.None)
            {
                //Log.WriteLine(LogType.Error, "Error code in Usb.WriteDataBegin: ");
                throw new Exception("Bad result in WriteDataBegin in Usb.cs");
            }
            if(ar != null)
                WriteQueue.Add(new AsyncResult(ar, localBuf));
        }

        /// <summary>
        /// Waits until the oldest submitted buffers has been written successfully OR timed out.
        /// The number of bytes written is returned.
        /// </summary>
        /// <returns></returns>
        static public int WriteDataEnd()
        {
            if (WriteQueue.Count == 0)
                throw new Exception("No buffers in Usb WriteDataEnd()");

            AsyncResult ar = WriteQueue[0];
            WriteQueue.RemoveAt(0);
            return ar.Wait();
        }

        /// <summary>
        /// Creates and submits a buffer to be read asynchronously. Returns immediately.
        /// </summary>
        /// <param name="data"></param>
        static public void ReadDataBegin(int bufSize)
        {
            byte[] readBuffer = new byte[bufSize];
			UsbTransfer? ar = null;
			QAnalyzer?.DataReader?.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, MainI2SReadWriteTimeout, out ar);
            if(ar != null)
                ReadQueue.Add(new AsyncResult(ar, readBuffer));
        }

        /// <summary>
        /// Waits until the oldest submitted buffer has been read successfully OR timed out
        /// </summary>
        /// <returns></returns>
        static public byte[] ReadDataEnd()
        {
            AsyncResult ar = ReadQueue[0];
            ReadQueue.RemoveAt(0);
            if (ar.Wait() == 0)
            {
                return [];
            }
            else
            {
                return ar.ReadBuffer;
            }
        }

	}
}
