using FftSharp;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40x.BareMetal
{
	class AcqResult
	{
		public bool Valid = false;
		public double[] Left = [];
		public double[] Right = [];
	};

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
	class QaUsb
    {
        object ReadRegLock = new object();
		private readonly static int _RelayMilliseconds = 2000; // how long to wait for relays to settle down after changing input/output ranges

		List<AsyncResult> WriteQueue = new List<AsyncResult>();
        List<AsyncResult> ReadQueue = new List<AsyncResult>();
		/// Tracks whether or not an acq is in process. The count starts at one, and when it goes busy
		/// it will drop to zero, and then return to 1 when not busy
		static SemaphoreSlim AcqSemaphore = new SemaphoreSlim(1);

		readonly int RegReadWriteTimeout = 20;
        readonly int MainI2SReadWriteTimeout = 2000;

        public byte[] CalData { get; private set; } = []; // readonly
        public (double, double)[] FCalData { get; private set; } = []; // readonly

		// the usb device we talk to
		public UsbDevice? Device { get; private set; } = null;

		// the reader/writer pairs for register and data endpoints
		public UsbEndpointReader? RegisterReader { get; private set; } = null;
		public UsbEndpointWriter? RegisterWriter { get; private set; } = null;
		public UsbEndpointReader? DataReader { get; private set; } = null;
		public UsbEndpointWriter? DataWriter { get; private set; } = null;

        public QaUsb()
        {

        }

		/// <summary>
		/// Do the startup of the QA40x, attaching USB if needed
		/// </summary>
		/// <param name="sampleRate"></param>
		/// <param name="fftsize"></param>
		/// <param name="Windowing"></param>
		/// <param name="attenuation"></param>
		/// <param name="setdefault">this may take a little time, so do it once?</param>
		/// <returns>success true or false</returns>
		public static async Task<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation)
		{
			try
			{
                // ********************************************************************  
                // Load a settings we want
                // ********************************************************************  
				await QaComm.SetSampleRate(sampleRate);
                await QaComm.SetInputRange(attenuation);
				await QaComm.SetFftSize(fftsize);
				await QaComm.SetWindowing(Windowing);
				//qan.SetInput(attenuation);
				//qan.SetOutput(18); // this is set when we do an acquisition based on the voltage output data
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

		public bool IsOpen()
		{
			return Device != null;
		}

		/// <summary>
		/// Attempts to open the USB connection to a device
		/// </summary>
		/// <returns></returns>
		public bool Open()
        {
			Debug.Assert(Device == null, "Open called when device is already open");
			bool brslt = false;
			// Attempt to open QA402 or QA403 device
			try
			{
				Device = QaLowUsb.AttachDevice();
				RegisterReader = Device.OpenEndpointReader(ReadEndpointID.Ep01);
				RegisterWriter = Device.OpenEndpointWriter(WriteEndpointID.Ep01);
				DataReader = Device.OpenEndpointReader(ReadEndpointID.Ep02);
				DataWriter = Device.OpenEndpointWriter(WriteEndpointID.Ep02);

				CalData = LoadCalibration();
				// convert to doubles from byte stream
				var incals = Enumerable.Range(0, 7).Select(x => x * 6);
				var fcals = incals.Select(x => GetAdcCal(CalData, x)).ToList();
				incals = Enumerable.Range(0, 3).Select(x => 18 - x * 10);
				var gcals = incals.Select(x => GetDacCal(CalData, x)).ToList();
				fcals.AddRange(gcals);
				FCalData = fcals.ToArray();     // doubles instead of bytes mainly for debugging
				Debug.WriteLine($"Calibration data: {string.Join(", ", FCalData)}");
                brslt = true;
			}
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }

            return brslt;
        }

        /// <summary>
        /// Closes a USB connection (if it's open)
        /// </summary>
        /// <returns></returns>
        public bool Close(bool OnExit)
        {
			try
			{
				// Stop streaming. This also extinguishes the RUN led
				WriteRegister(8, 0);
                // if we're exiting really close this stuff
                if(OnExit)
                {
					// clear stuff
					DataReader = null;
					DataWriter = null;
					RegisterReader = null;
					RegisterWriter = null;
					QaLowUsb.DetachDevice(OnExit);
					Device = null;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"An error occurred during cleanup: {e.Message}");
			}
            return true;
		}

        /// <summary>
        /// Generates a random number, writes that to register 0, and attempts to read that same value back.
        /// </summary>
        /// <returns></returns>
        public bool VerifyConnection()
        {
			if (Device == null)
				return false;

			try
			{
				uint val;
				unchecked
				{
					val = Convert.ToUInt32(new Random().Next());
				}

				WriteRegister(0, val);

				if (ReadRegister(0) == val)
					return true;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"An error occurred during verifyconnection: {e.Message}");
			}
            return false;
        }

		/// <summary>
		/// read the calibration data from the device
		/// </summary>
		/// <returns>a list of calibration values in dB</returns>
		public byte[] LoadCalibration()
		{
			Debug.Assert(!ViewSettings.IsUseREST, "LoadCalibration using REST.");
			WriteRegister(0xD, 0x10);
			int pageSize = 512;
			byte[] calData = new byte[pageSize];

			for (int i = 0; i < pageSize / 4; i++)
			{
				uint d = (uint)(ReadRegister(0x19));
				byte[] array = BitConverter.GetBytes(d);
				Array.Copy(array, 0, calData, i * 4, 4);
			}

			return calData;
		}

		/// <summary>
		/// get ADC calibration data for a given full scale input setting
		/// </summary>
		/// <param name="calData">calibration data</param>
		/// <param name="fullScaleInputLevel">the full scale input setting</param>
		/// <returns>(left,right) multipliers</returns>
		/// <exception cref="ArgumentException"></exception>
		public static (double Left, double Right) GetAdcCal(byte[] calData, int fullScaleInputLevel)
		{
			Debug.Assert(!ViewSettings.IsUseREST, "GetAdcCal using REST.");
			var offsets = new Dictionary<int, int>
			{
				{ 0, 24 }, { 6, 36 }, { 12, 48 }, { 18, 60 }, { 24, 72 }, { 30, 84 }, { 36, 96 }, { 42, 108 }
			};

			if (!offsets.TryGetValue(fullScaleInputLevel, out int leftOffset))
				throw new ArgumentException("Invalid input level. Must be one of 0, 6, 12, 18, 24, 30, 36, 42.");

			int rightOffset = leftOffset + 6;

			float leftLevel = BitConverter.ToSingle(calData, leftOffset + 2);
			float rightLevel = BitConverter.ToSingle(calData, rightOffset + 2);

			double leftValue = Math.Pow(10, leftLevel / 20);
			double rightValue = Math.Pow(10, rightLevel / 20);

			return (leftValue, rightValue);
		}

		/// <summary>
		/// get ADC calibration data for a given full scale output setting
		/// </summary>
		/// <param name="calData">the calibration data</param>
		/// <param name="fullScaleOutputLevel">the current full scale output level</param>
		/// <returns>(left,right) multipliers</returns>
		/// <exception cref="ArgumentException"></exception>
		public static (double Left, double Right) GetDacCal(byte[] calData, int fullScaleOutputLevel)
		{
			Debug.Assert(!ViewSettings.IsUseREST, "GetDacCal using REST.");
			var offsets = new Dictionary<int, int>
			{
				{ 18, 156 }, { 8, 144 }, { -2, 132 }, { -12, 120 }
			};

			if (!offsets.TryGetValue(fullScaleOutputLevel, out int leftOffset))
				throw new ArgumentException("Invalid output level. Must be one of 18, 8, -2, -12.");

			int rightOffset = leftOffset + 6;

			float leftLevel = BitConverter.ToSingle(calData, leftOffset + 2);
			float rightLevel = BitConverter.ToSingle(calData, rightOffset + 2);

			double leftValue = Math.Pow(10, leftLevel / 20);
			double rightValue = Math.Pow(10, rightLevel / 20);

			return (leftValue, rightValue);
		}

		public static void DumpCalibrationData(byte[] calData)
		{
			string hexData = BitConverter.ToString(calData).Replace("-", " ");
			Debug.WriteLine(hexData);

			var (adcLeft, adcRight) = GetAdcCal(calData, 42);
			Debug.WriteLine($"ADC Left level: {adcLeft}, Right level: {adcRight}");

			var (dacLeft, dacRight) = GetDacCal(calData, -2);
			Debug.WriteLine($"DAC Left level: {dacLeft}, Right level: {dacRight}");
		}

		protected async Task showMessage(String msg, int delay = 0)
		{
			var vm = ViewSettings.Singleton.Main;
			await vm.SetProgressMessage(msg, delay);
		}

		private static int _LastInputRange = 0;
		private static int _LastOutputRange = 0;
		/// <summary>
		/// Provides an async method for doign the DAC/ADC streaming. You can submit separate buffers for the left and right channels.
		/// When the acquisition is finished, the AcqResult return value will contain the Left and Right values captured by the ADC
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="leftOut"></param>
		/// <param name="rightOut"></param>
		/// <returns></returns>
		public async Task<AcqResult> DoStreamingAsync(CancellationToken ct, double[] leftOut, double[] rightOut)
		{
			// this bunch of code is to handle the relay flipping.
			// We need to wait a bit for the relays to settle down
			try
			{
				var s = QaComm.GetInputRange();
				var u = QaComm.GetOutputRange();
				if (s != _LastInputRange || u != _LastOutputRange)
				{
					// if we flipped a relay, wait a while...
					bool doit = ((s <= 18 && _LastInputRange > 18) || (s > 18 && _LastInputRange <= 18));
					if (!doit)
						doit = (u != _LastOutputRange);
					if (doit)
					{
						await showMessage("Waiting for relays to settle...", _RelayMilliseconds);
						await showMessage("Acquiring data", 10);
						//// do at least 1.5 seconds of streaming
						//var ss = QaComm.GetSampleRate();
						//ss = ss + ss / 2;
						//var cnt = 32768;
						//while (cnt < ss)
						//	cnt *= 2;
						//double[] empty = new double[cnt];			// large empty buffer...
						//await SubStreamingAsync(ct, empty, empty);	// waste some time
					}
					_LastInputRange = s;
					_LastOutputRange = u;
				}
			}
			catch(Exception ex)
			{
				Debug.WriteLine($"Error: {ex.Message}");
			}
			// now do the real thing
			return await SubStreamingAsync(ct, leftOut, rightOut);
		}
		/// <summary>
		/// Provides an async method for doign the DAC/ADC streaming. You can submit separate buffers for the left and right channels.
		/// When the acquisition is finished, the AcqResult return value will contain the Left and Right values captured by the ADC
		/// </summary>
		/// <param name="ct"></param>
		/// <param name="leftOut"></param>
		/// <param name="rightOut"></param>
		/// <returns></returns>
		public async Task<AcqResult> SubStreamingAsync(CancellationToken ct, double[] leftOut, double[] rightOut)
		{
			Debug.Assert(!ViewSettings.IsUseREST, "Error*** Do streaming with REST enabled");
			AcqResult r = new AcqResult();
			r.Valid = true;

			// Check if acq is already in progress
			if (AcqSemaphore.CurrentCount > 0)
			{
				// In here, acq is not in progress. Take semaphore, waiting if needed. Since we checked above, we should never have to wait
				await AcqSemaphore.WaitAsync();

				// Start a new task to run the acquisition
				Task t = Task.Run(() =>
				{
					try
					{
						r = DoStreaming(ct, leftOut, rightOut);
					}
					catch (OperationCanceledException)
					{
						// If we cancel an acq via the CancellationToken, we'll end up here
						r.Valid = false;
					}
					catch (Exception ex)
					{
						// Other exceptions will end up here
						Debug.WriteLine($"Error in acquisition: {ex.Message}");
						r.Valid = false;
					}
					finally
					{
						// Indicate an acq is not longer in progress
						AcqSemaphore.Release();
					}
				});

				// Wait for the task above to complete. Note we're on the UI thread here, but the task code above will be running
				// in another thread. By "awaiting" on the task above, the UI thread blocks here BUT remains active, able to handle
				// other UI tasks. This is known in C# as the Task-based Asynchronous Pattern in case the syntax is confusing to
				// a non-c# developer
				await t;
			}
			else
			{
				// Acquisition is already in progress. 
				r.Valid = false;
			}
			// Return true to let the caller know the task succeeded and finished
			return r;
		}

		private static int _DelayOffset = 0;
		public AcqResult DoStreaming(CancellationToken ct, double[] leftOut, double[] rightOut)
		{
			AcqResult r = new AcqResult();
			r.Valid = true;

			Debug.Assert(leftOut.Length == rightOut.Length, "Out buffers must be the same length");
			var maxOutput = QaComm.GetOutputRange();
			var maxInput = QaComm.GetInputRange();
			var preBuf = QaComm.GetPreBuffer();
			var postBuf = QaComm.GetPostBuffer();
			var FftSize = QaComm.GetFftSize();

			var dacCal = GetDacCal(CalData, maxOutput);
			var adcCal = GetAdcCal(CalData, maxInput);

			// bufsize is leftout.length and usbBufSize is the size of the usb buffer
			// bufSize * 8 must be >= to usbBufSize, and bufSize * 8 must be an integer multiple of usbBufSize
			var usize = MathUtil.ToInt(ViewSettings.Singleton.SettingsVm.UsbBufferSize, 16384);
			double fusize = Math.Pow(2, Math.Floor(Math.Log(usize, 2)));     // nearest power of 2
			fusize = Math.Max(Math.Min(fusize, 131072), 2048);   // 2k to 128k ???

			int usbBufSize = (int)(0.1 + fusize);
			// (int)Math.Pow(2, 13);   // If bigger than 2^15, then OS USB code will chunk it down into 16K buffers (Windows). So, not much point making larger than 32K. 

			// The scale factor converts the volts to dBFS. The max output is 8Vrms = 11.28Vp = 0 dBFS. 
			// The above calcs assume DAC relays set to 18 dBV = 8Vrms full scale
			var dbfsAdjustment = Math.Pow(10, -((maxOutput + 3.0) / 20));
			List<double> lout = leftOut.Select(x => x * dbfsAdjustment * dacCal.Left).ToList();
			List<double> rout = rightOut.Select(x => x * dbfsAdjustment * dacCal.Right).ToList();

			// now pad front and back of the values via prebuf and postbuf 
			preBuf = Math.Max(preBuf, usbBufSize / 8);
			postBuf = Math.Max(postBuf, usbBufSize / 8);
			double[] prebuf = new double[preBuf];
			double[] postbuf = new double[postBuf];
			lout.InsertRange(0, prebuf);
			lout.AddRange(postbuf);
			rout.InsertRange(0, prebuf);
			rout.AddRange(postbuf);

			// Convert to byte stream. This will be sent over USB
			byte[] txData = ToByteStream(lout.ToArray(), rout.ToArray());
			byte[] rxData = new byte[txData.Length];

			// Determine the number of blocks needed
			int blocks = txData.Length / usbBufSize;
			int remainder = txData.Length - blocks * usbBufSize;

			// Verify we have integer number of blocks
			if (blocks == 0 || remainder != 0)
			{
				// Error! The bufSize must be an integer multiple of the usbBufSize. For example, a 16K bufSize will have 16K left doubles, and
				// 16K right doubles. This is 16K * 8 = 128K. The USB buffer size (bytes sent over the wire) can be 32K, 16K, 8K, etc.
				throw new Exception("bufSize * 8 must be >= to usbBufSize, and bufSize * 8 must be an integer multiple of usbBufSize");
			}

			InitOverlapped();

			// Start streaming DAC, with ADC autostreamed after DAC is seeing live data
			// Important! Enabled streaming AND THEN send data. This will also illuminate the 
			// RUN led
			WriteRegister(8, 0x5);

			// list of rx blocks
			List<byte[]> usbRxBuffers = new List<byte[]>();
			int prereader = 3;  // number of buffers to keep running

			// Prime the pump with two reads. This way we can handle one buffer while the other is being
			// used by the OS
			// Send out two data writes as we begin working our way through the txData buffer
			//
			// do these as close together as possible...
			ReadDataBegin(usbBufSize);
			WriteDataBegin(txData, 0, usbBufSize);
			//
			for (int i = 1; i < prereader; i++)
			{
				ReadDataBegin(usbBufSize);
				WriteDataBegin(txData, usbBufSize * i, usbBufSize);
			}

			var remaining = prereader;  // # of blocks still to read after all is sent

			// Loop and send/receive the remaining blocks. Everytime we get some RX data, we'll send another block of 
			// TX data. This is how we maintain timing with the hardware. 
			for (int i = prereader; i < blocks; i++)
			{
				// Wait for RX data to arrive then, for speed, just append the array to our list of receipts
				var bufr = ReadDataEnd();
				if (bufr.Length > 0)
				{
					usbRxBuffers.Add(bufr);
				}
				else
				{
					Debug.WriteLine("Empty buffer received from USB");
				}
				remaining--;

				if (ct.IsCancellationRequested == false && bufr.Length > 0)
				{
					// Kick off another read and write
					ReadDataBegin(usbBufSize);
					WriteDataBegin(txData, i * usbBufSize, usbBufSize);
					remaining++;
				}
				else
				{
					// Cancellation has been requested. At this point there is one buffer in flight. 
					// Break out of this loop and handle the rest of the cancellation below.
					break;
				}
			}

			// At this point, all buffers have been sent and there are two or three RX
			// buffers in-flight. Collect those
			for (int i = 0; i < remaining; i++)
			{
				var bufr = ReadDataEnd();
				usbRxBuffers.Add(bufr);
			}

			// Stop streaming. This also extinguishes the RUN led
			WriteRegister(8, 0);

			// we now have a list of all the rx buffers to convert to an array
			// use fixed size so that frombytestream and others work ok
			rxData = new byte[usbBufSize * usbRxBuffers.Count()];
			int offset = 0;
			foreach (var b in usbRxBuffers)
			{
				Buffer.BlockCopy(b, 0, rxData, offset, b.Length);
				offset += b.Length;
			}
			usbRxBuffers.Clear();   // empty the ram here

			// Note that left and right data is swapped on QA402, QA403, QA404. We do that via arg ordering below.
			FromByteStream(rxData, out r.Right, out r.Left);
			if (r.Valid == true && r.Left.Length > preBuf)
			{
				// Convert from dBFS to dBV. Note the 6 dB factor--the ADC is differential
				var adcCorrection = Math.Pow(10, (maxInput - 6.0) / 20);
				int tused = (int)FftSize;    // should be fftsize

				// Apply scaling factor to map from dBFS to Volts. 
				var loff = 0;
				// check delay offset
				if(leftOut.Max() > 1e-10 || rightOut.Max() > 1e-10)
				{
					var dcoffsetL = r.Left.Skip(preBuf).Take(tused).Average();
					var dcoffsetR = r.Right.Skip(preBuf).Take(tused).Average();
					for(int i= preBuf; i<r.Left.Length; i++)
					{
						// empirically we get -7e-5 until signal shows up
						// i assume that's dc offset...
						var inx = r.Left[i];
						var iny = r.Right[i];
						if ( Math.Abs(inx - dcoffsetL)*adcCal.Left* adcCorrection > 1e-3 || Math.Abs(iny- dcoffsetR) * adcCal.Right * adcCorrection > 1e-3)
						{
							loff = i;
							break;
						}
					}
					var samplerate = QaComm.GetSampleRate();
					// allow 2ms after the end of the prebuffer
					if (loff > (preBuf + samplerate / 2000))
						loff = (int)(preBuf + samplerate / 2000);
					// keep an extra 5 or .05ms at 96KHz
					loff = Math.Max(0, loff - preBuf - 5);
					Debug.WriteLine($"Delay offset: {loff:G3}   DC offset: {dcoffsetL:G3},{dcoffsetR:G3}");
					_DelayOffset = loff;
				}

				var rlf = r.Left.Skip(preBuf + loff).Take(tused);
				var roff = rlf.Average();  // dc offset
				r.Left = rlf.Select(x => (x - roff) * adcCal.Left * adcCorrection).ToArray();

				var rrf = r.Right.Skip(prebuf.Length + loff).Take(tused);
				roff = rrf.Average();  // dc offset
				r.Right = rrf.Select(x => (x - roff) * adcCal.Right * adcCorrection).ToArray();

				//Debug.WriteLine($"Attenuation: {maxInput}  Output Level: {maxOutput}");
				//Debug.WriteLine($"Peak Left: {r.Left.Max():0.000}   Peak right: {r.Right.Max():0.000}");
				//Debug.WriteLine($"Total Left: {Math.Sqrt(r.Left.Sum(x => x * x)):0.000}   Total right: {Math.Sqrt(r.Right.Sum(x => x * x)):0.000}");
			}
			else
			{
				r.Valid = false;
			}
			return r;
		}

		/// <summary>
		/// Converts left and right channels of doubles to interleaved byte stream suitable for transmission over USB wire
		/// </summary>
		/// <param name="leftData"></param>
		/// <param name="rightData"></param>
		/// <returns></returns>
		static public byte[] ToByteStream(double[] leftData, double[] rightData)
		{
			if (leftData.Length != rightData.Length)
				throw new InvalidOperationException("Data length must be the same");

			int[] ili = new int[leftData.Length * 2];

			// left and right are flipped so,...
			for (int i = 0; i < leftData.Length; i++)
			{
				ili[i * 2 + 1] = (int)(leftData[i] * int.MaxValue);
				ili[i * 2] = (int)(rightData[i] * int.MaxValue);
			}

			byte[] buffer = new byte[leftData.Length * 8];  // 4 bytes for right, 4 bytes for left
			Buffer.BlockCopy(ili, 0, buffer, 0, buffer.Length); // convert ints to bytes
			return buffer;
		}

		/// <summary>
		/// Converts interleaved data received over the USB wire to left and right doubles
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		static public void FromByteStream(byte[] buffer, out double[] left, out double[] right)
		{
			int[] ili = new int[buffer.Length / sizeof(int)];
			Buffer.BlockCopy(buffer, 0, ili, 0, buffer.Length);     // convert bytes to ints
			left = new double[ili.Length / 2];
			right = new double[left.Length];
			double ddiv = (double)int.MaxValue;     // the original python has a /2 here ?
			for (int j = 0; j < left.Length; j++)
			{
				left[j] = ili[j * 2 + 1] / ddiv;
				right[j] = ili[j * 2] / ddiv;
			}
		}

        /// <summary>
        /// Performs a read on a USB register
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        public UInt32 ReadRegister(byte reg)
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
                    RegisterReader?.Read(data, RegReadWriteTimeout, out len);

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
        public void WriteRegister(byte reg, uint val)
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

        void WriteRegisterRaw(byte[] data)
        {
            int len = data.Length;
            try
            {
				RegisterWriter?.Write(data, RegReadWriteTimeout, out len);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
				throw;
            }
        }

        byte[] WriteRegisterPrep(byte reg, uint val)
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

        public void InitOverlapped()
        {
            WriteQueue.Clear();
            ReadQueue.Clear();
        }

        /// <summary>
        /// Submits a buffer to be written and returns immediately. The submitted buffer is 
        /// copied to a local buffer before returning.
        /// </summary>
        /// <param name="data"></param>
        public void WriteDataBegin(byte[] data, int offset, int len)
        {
            ErrorCode ec;

            if (len == 0)
                return;

            byte[] localBuf = new byte[len];
            Array.Copy(data, offset, localBuf, 0, len);
			UsbTransfer? ar = null;
			ec = DataWriter?.SubmitAsyncTransfer(localBuf, 0, localBuf.Length, MainI2SReadWriteTimeout, out ar) ?? ErrorCode.UnknownError;
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
        public int WriteDataEnd()
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
        public void ReadDataBegin(int bufSize)
        {
            byte[] readBuffer = new byte[bufSize];
			UsbTransfer? ar = null;
			DataReader?.SubmitAsyncTransfer(readBuffer, 0, readBuffer.Length, MainI2SReadWriteTimeout, out ar);
            if(ar != null)
                ReadQueue.Add(new AsyncResult(ar, readBuffer));
        }

        /// <summary>
        /// Waits until the oldest submitted buffer has been read successfully OR timed out
        /// </summary>
        /// <returns></returns>
        public byte[] ReadDataEnd()
        {
            if(ReadQueue.Count > 0)
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
            return [];
		}

	}
}
