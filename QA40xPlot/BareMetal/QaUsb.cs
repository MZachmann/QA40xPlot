using FftSharp;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using QA40xPlot.BareMetal;
using QA40xPlot.Libraries;
using System.Diagnostics;

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

    class QaUsb
    {
        private static QaAnalyzer? _qAnalyzer;
		public static QaAnalyzer? QAnalyzer => _qAnalyzer;

        static object ReadRegLock = new object();

        static List<AsyncResult> WriteQueue = new List<AsyncResult>();
        static List<AsyncResult> ReadQueue = new List<AsyncResult>();

        static readonly int RegReadWriteTimeout = 20;
        static readonly int MainI2SReadWriteTimeout = 1000;

        public static async Task<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct)
        {
			var datapt = new double[QAnalyzer?.Params?.FFTSize ?? 0];
            if( QAnalyzer?.GenParams.IsOn == true && QAnalyzer?.Params?.OutputSource == OutputSources.Sine)
            {
				double dt = 1.0 / (_qAnalyzer?.Params?.SampleRate ?? 1);
				datapt = datapt.Select((x,index) => (QAnalyzer.GenParams.Voltage/Math.Sqrt(2)) * Math.Sin(2 * Math.PI * QAnalyzer.GenParams.Frequency * dt * index)).ToArray();
			}
			var lrfs = await DoAcquireUser(ct, datapt, datapt, true);
			return lrfs;
		}

        public static bool CheckDeviceConnected()
        {
            return true;
        }

        public static void SetGen1(double freq, double volts, bool ison)
        {
            QAnalyzer?.SetGenParams(freq, volts, ison);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="datapt"></param>
        /// <param name="getFreq"></param>
        /// <returns></returns>
		public static async Task<LeftRightSeries> DoAcquireUser(CancellationToken ct, double[] dataLeft, double[] dataRight, bool getFreq)
		{
			LeftRightSeries lrfs = new LeftRightSeries();
            var dpt = new double[dataLeft.Length];
			var newData = await Acquisition.DoStreamingAsync(ct, dataLeft, dataRight);
			if (ct.IsCancellationRequested || lrfs == null)
				return lrfs ?? new();

			{
                lrfs.TimeRslt = new();
                lrfs.TimeRslt.Left = newData.Left;
				lrfs.TimeRslt.Right = newData.Right;
                lrfs.TimeRslt.dt = 1.0 / (_qAnalyzer?.Params?.SampleRate ?? 1);
				if (ct.IsCancellationRequested)
					return lrfs;
			}

			if (getFreq)
			{
                lrfs.FreqRslt = new(); // fft

				var timeSeries = lrfs.TimeRslt;
				var m2 = Math.Sqrt(2);
				// Left channel
                // only take half of the data since it's symmetric so length of freq data = 1/2 length of time data
				var window = QAnalyzer.Params.GetWindowType(QAnalyzer.Params.WindowType);
				double[] windowed_measured = window.Apply(timeSeries.Left, true);
				System.Numerics.Complex[] spectrum_measured = FFT.Forward(windowed_measured).Take(timeSeries.Left.Length/2).ToArray();

				double[] windowed_ref = window.Apply(timeSeries.Right, true);
				System.Numerics.Complex[] spectrum_ref = FFT.Forward(windowed_ref).Take(timeSeries.Left.Length / 2).ToArray();

				lrfs.FreqRslt.Left = spectrum_measured.Select(x => x.Magnitude * m2).ToArray();
				lrfs.FreqRslt.Right = spectrum_ref.Select(x => x.Magnitude * m2).ToArray();
				var nca2 = (int)(0.01 + 1 / lrfs.TimeRslt.dt);      // total time in tics = sample rate
				lrfs.FreqRslt.Df = nca2 / (double)timeSeries.Left.Length; // ???
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
		public static bool InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation, bool setdefault = false)
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
				if (setdefault && qan != null)
                {
                    qan?.DataReader?.Reset();
                }
				if (qan == null || qan.Params == null)
					return false;

				qan.SetSampleRate((int)sampleRate);
				qan.SetInput(attenuation);
                qan.SetOutput(18);
                qan.Params.SetWindowing(Windowing);
				qan.Params.FFTSize = (int)fftsize;
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
        static public bool Close()
        {
            try
            {
                if (_qAnalyzer != null)
                {
					_qAnalyzer.Close();
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
