using QA40x_BareMetal;
using QA40xPlot.BareMetal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QA40xPlot.BareMetal
{
    class AcqResult
    {
        public bool Valid = false;
        public double[] Left = [];
        public double[] Right = [];
    }

    static class Acquisition
    {
       

        /// <summary>
        /// Tracks whether or not an acq is in process. The count starts at one, and when it goes busy
        /// it will drop to zero, and then return to 1 when not busy
        /// </summary>
        static SemaphoreSlim AcqSemaphore = new SemaphoreSlim(1);



        /// <summary>
        /// Provides an async method for doign the DAC/ADC streaming. You can submit separate buffers for the left and right channels.
        /// When the acquisition is finished, the AcqResult return value will contain the Left and Right values captured by the ADC
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="leftOut"></param>
        /// <param name="rightOut"></param>
        /// <returns></returns>
        static public async Task<AcqResult> DoStreamingAsync(CancellationToken ct, double[] leftOut, double[] rightOut)
        {
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
                    catch (OperationCanceledException )
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

                // Return true to let the caller know the task succeeded and finished
                return r;
            }
            else
            {
                // Acquisition is already in progress. 
                r.Valid = false;
                return r;
            }
        }

        static AcqResult DoStreaming(CancellationToken ct, double[] leftOut, double[] rightOut)
        {
            AcqResult r = new AcqResult();
            r.Valid = true;

            var aControl = QaUsb.QAnalyzer?.Control;
            var aParams = QaUsb.QAnalyzer?.Params;

			if (QaUsb.QAnalyzer == null || aControl == null || aParams == null)
            { 
                r.Valid = false;
                return r;
            }
			Debug.Assert(leftOut.Length == rightOut.Length, "Out buffers must be the same length");
			var dacCal = aControl.GetDacCal(QaUsb.QAnalyzer.CalData, aParams.MaxOutputLevel);
			var adcCal = aControl.GetAdcCal(QaUsb.QAnalyzer.CalData, aParams.MaxInputLevel);

			Int32 bufSize = leftOut.Length;          // Buffer size of user data. For example, 16384 means user will have 16K left and right double samples. 
			int usbBufSize = (int)Math.Pow(2, 12);   // If bigger than 2^15, then OS USB code will chunk it down into 16K buffers (Windows). So, not much point making larger than 32K. 

			// The scale factor converts the volts to dBFS. The max output is 8Vrms = 11.28Vp = 0 dBFS. 
			// The above calcs assume DAC relays set to 18 dBV = 8Vrms full scale
			double scaleFactor = 1.0 / (8.0 * Math.Sqrt(2));
            var dbfsAdjustment = Math.Pow(10, -((aParams.MaxOutputLevel + 3.0) / 20));
			leftOut = leftOut.Select(x => x * dbfsAdjustment * dacCal.Left).ToArray();
            rightOut = rightOut.Select(x => x * dbfsAdjustment * dacCal.Right).ToArray();

			// now pad front and back of the values via prebuf and postbuf 
			double[] prebuf = new double[aParams.PreBuffer];
			double[] postbuf = new double[aParams.PostBuffer];
			var lout = new List<double>(leftOut);
			var rout = new List<double>(rightOut);
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

            QaUsb.InitOverlapped();

            // Start streaming DAC, with ADC autostreamed after DAC is seeing live data
            // Important! Enabled streaming AND THEN send data. This will also illuminate the 
            // RUN led
            QaUsb.WriteRegister(8, 0x5);

            // Big buffer to hold received blocks. These will be copied as received into the larger rxData buffer
            byte[] usbRxBuffer;

            // Prime the pump with two reads. This way we can handle one buffer while the other is being
            // used by the OS
            QaUsb.ReadDataBegin(usbBufSize);
            QaUsb.ReadDataBegin(usbBufSize);

            // Send out two data writes as we begin working our way through the txData buffer
            QaUsb.WriteDataBegin(txData, 0, usbBufSize);
            QaUsb.WriteDataBegin(txData, usbBufSize, usbBufSize);

            // Loop and send/receive the remaining blocks. Everytime we get some RX data, we'll send another block of 
            // TX data. This is how we maintain timing with the hardware. 
            for (int i = 2; i < blocks; i++)
            {
                // Wait for RX data to arrive
                usbRxBuffer = QaUsb.ReadDataEnd();
                Array.Copy(usbRxBuffer, 0, rxData, (i - 2) * usbBufSize, usbBufSize);

                if (ct.IsCancellationRequested == false)
                {
                    // Kick off another read and write
                    QaUsb.ReadDataBegin(usbBufSize);
                    QaUsb.WriteDataBegin(txData, i * usbBufSize, usbBufSize);
                }
                else
                {
                    // Cancellation has been requested. At this point there is one buffer in flight. 
                    // Break out of this loop and handle the rest of the cancellation below.
                    break;
                }
            }

            // Check if the user wanted to cancel
            if (ct.IsCancellationRequested)
            {
                // Here the user has requested cancellation. We know there's a single buffer in flight.
                r.Valid = false;

                // Stop streaming
                QaUsb.WriteRegister(8, 0);

                // Grab the buffer in flight
                usbRxBuffer = QaUsb.ReadDataEnd();

				// Throw an exception that will be caught by the calling task. The code below this block below won't be executed
				ct.ThrowIfCancellationRequested();
            }

            // At this point, all buffers have been sent and there are two RX
            // buffers in-flight. Collect those
            for (int i = 0; i < 2; i++)
            {
                usbRxBuffer = QaUsb.ReadDataEnd();
                Array.Copy(usbRxBuffer, 0, rxData, (blocks - 2 - i) * usbBufSize, usbBufSize);
            }

            // Stop streaming. This also extinguishes the RUN led
            QaUsb.WriteRegister(8, 0);

            // Note that left and right data is swapped on QA402, QA403, QA404. We do that via arg ordering below.
            FromByteStream(rxData, out r.Right, out r.Left);

            var adcCorrection = Math.Pow(10, (aParams.MaxInputLevel - 6.0) / 20);
            var tbytes = r.Left.Length - aParams.PreBuffer - aParams.PostBuffer;    // should be fftsize

			// Apply scaling scaling factor to map from dBFS to Volts. This is emperically determined for the QA402, but should
			// be fairly tight on unit to unit as 5.371 ?
			r.Left = r.Left.Skip(aParams.PreBuffer).Take(tbytes).Select(x => x * adcCal.Left * adcCorrection).ToArray();
            r.Right = r.Right.Skip(aParams.PreBuffer).Take(tbytes).Select(x => x * adcCal.Right * adcCorrection).ToArray();

			Debug.WriteLine($"Peak Left: {r.Left.Max():0.000}   Peak right: {r.Right.Max():0.000}");

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

            int idx = 0;
			foreach (var item in leftData)
			{
                ili[idx*2] = (int)(item * int.MaxValue);
                idx++;
			}
            idx = 0;
			foreach (var item in rightData)
			{
				ili[idx * 2 + 1] = (int)(item * int.MaxValue);
                idx++;
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
			left = new double[buffer.Length / 8];
            right = new double[buffer.Length / 8];
            double ddiv = (double)int.MaxValue / 2;

            left = left.Select((x,index) => ili[index*2] / ddiv).ToArray();
			right = right.Select((x, index) => ili[index*2+1] / ddiv).ToArray();
        }


        /// <summary>
        /// converts an array of doubles into an array of bytes
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static byte[] DoublesTo4Bytes(double[] data)
        {
            byte[] buffer = new byte[data.Length * 4];

            for (int i = 0; i < data.Length; i++)
            {
                int val = (int)(data[i] * int.MaxValue);

                buffer[i * 4 + 3] = (byte)(val >> 24);
                buffer[i * 4 + 2] = (byte)(val >> 16);
                buffer[i * 4 + 1] = (byte)(val >> 8);
                buffer[i * 4 + 0] = (byte)(val >> 0);
            }

            return buffer;
        }
    }
}
