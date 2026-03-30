using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.BareMetal
{
	internal static class UsbSubs
	{
		private static DateTime StartingTime = DateTime.Now;

		public static void DebugLineIf(bool condition, string s)
		{
			if (condition)
			{
				DebugLine(s);
			}
		}

		public static void DebugLine(string s)
		{
#if DEBUG
			if (s.Contains("*****"))
			{
				StartingTime = DateTime.Now;
			}
			var u2 = (DateTime.Now - StartingTime).ToString(@"mm\:ss\.fff\:");
			Console.WriteLine(u2 + s);
#endif
		}
	}

	public class ReceiveJob
	{
		public uint JobNumber { get; set; }
		public LeftRightTimeSeries? LrtsJob { get; set; } = null;
		internal SendDoc TheSendDoc { get; set; }
		public Queue<AcqAsyncResult> SendPackets { get; set; } = new();
		public Queue<AcqAsyncResult> ReadPackets { get; set; } = new();
		public bool IsWatched { get; set; } = false;

		internal ReceiveJob(SendDoc sendDoc)
		{
			TheSendDoc = sendDoc;
		}

		public WaitHandle? AsyncHandle()
		{
			if (ReadPackets.Count > 0)
			{
				var lst = ReadPackets.Last();
				if (lst != null)
				{
					return lst.UsbXfer.AsyncWaitHandle;
				}
			}
			return null;
		}

		public bool IsFinished()
		{
			if(ReadPackets.Count > 0)
			{
				var lst = ReadPackets.Last();
				if (lst != null)
				{
					return lst.IsDone();
				}
			}
			return false;
		}

		public bool IsStarted()
		{
			if (SendPackets.Count > 0)
			{
				var fst = SendPackets.First();
				if (fst != null)
				{
					return fst.UsbXfer.Remaining != fst.ReadBuffer.Length;
				}
			}
			return false;
		}
	}


	internal class SendDoc
	{
		// actual data we're sending to the device
		public double[] LeftData { get; set; } = [];
		public double[] RightData { get; set; } = [];
		private byte[] ShaData { get; set; } = [];
		public List<byte[]> Buffers { get; set; } = [];
		// stuff from the hardware device
		public (double Left, double Right) DacCalibration { get; set; }
		public (double Left, double Right) AdcCalibration { get; set; }
		public int ParamInput { get; set; }    // input and output range during setup
		public int ParamOutput { get; set; }
		public uint UsbBuffSize { get; set; } = 16384;
		public double DbfsAdjustment { get; set; }
		public uint PreBufSize { get; set; }
		public uint PostBufSize { get; set; }
		public uint SampleRate { get; set; }
		public uint FFTSize { get; set; }

		public bool CompareDoc(SendDoc other)
		{
			if(ReferenceEquals(this, other))
				return true;
			var t1 =(ParamInput == other.ParamInput) && 
					(ParamOutput == other.ParamOutput) && 
					(FFTSize == other.FFTSize) &&
					(UsbBuffSize == other.UsbBuffSize);
			if(t1)
			{
				t1 = ShaData.SequenceEqual(other.ShaData);
			}
			return t1;
		}

		/// <summary>
		/// calculate the sha of our byte stream
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private static byte[] CalculateSha(double[] left, double[] right)
		{
			return System.Security.Cryptography.SHA256.HashData(QaUsb.ToByteStream(left, right));
		}

		/// <summary>
		/// Based on current settings, calculate any locals
		/// this also saves dataLeft and dataRight into the document and calculates the sha of the data
		/// </summary>
		public async Task<bool> CalculateParameters(BaseViewModel? bvm, double[] dataLeft, double[] dataRight)
		{
			SampleRate = QaComm.GetSampleRate();
			FFTSize = QaComm.GetFftSize();

			LeftData = (dataLeft == null || dataLeft.Length == 0) ? new double[FFTSize] : dataLeft;
			RightData = (dataRight == null || dataRight.Length == 0) ? new double[FFTSize] : dataRight;
			ShaData = CalculateSha(LeftData, RightData);

			// set the output amplitude to support the data
			var maxOut = Math.Max(LeftData.Max(), RightData.Max());
			var minOut = Math.Min(LeftData.Min(), RightData.Min());
			maxOut = Math.Max(Math.Abs(maxOut), Math.Abs(minOut));  // maximum output voltage
																	// don't bother setting output amplitude if we have no output
			var mlevel = IODevUSB.DetermineOutput((maxOut > 0) ? maxOut : 1e-8, 1.05); // the setting for our voltage + 10%
			var minRange = (int)MathUtil.ToDouble(ViewSettings.Singleton.SettingsVm.MinOutputRange, -12);
			mlevel = Math.Max(mlevel, minRange); // noop for now but keep this line in for testing

			await QaComm.SetOutputRange(mlevel);   // set the output voltage range based on the data
			ParamOutput = QaComm.GetOutputRange();
			ParamInput = QaComm.GetInputRange();

			DacCalibration = QaUsb.GetDacCal(QaComm.GetCalData(), ParamOutput);
			AdcCalibration = QaUsb.GetAdcCal(QaComm.GetCalData(), ParamInput);

			// bufsize is leftout.length and usbBufSize is the size of the usb buffer
			// bufSize * 8 must be >= to usbBufSize, and bufSize * 8 must be an integer multiple of usbBufSize
			var usize = MathUtil.ToInt(ViewSettings.Singleton.SettingsVm.UsbBufferSize, 16384);
			double fusize = Math.Pow(2, Math.Floor(Math.Log(usize, 2)));     // nearest power of 2
			fusize = Math.Max(Math.Min(fusize, 131072), 2048);   // 2k to 128k ???

			UsbBuffSize = (uint)(0.1 + fusize);
			DbfsAdjustment = Math.Pow(10, -((ParamOutput + 3.0) / 20));

			// now get the prebuf and postbuf sizes in samples
			var preBuf = QaComm.GetPreBuffer();
			var postBuf = QaComm.GetPostBuffer();
			PreBufSize = Math.Max((uint)preBuf, UsbBuffSize / 8);
			PostBufSize = Math.Max((uint)postBuf, UsbBuffSize / 8);
			DumpAllParams();
			return true;
		}

		/// <summary>
		/// convert stereo data into a set of buffers
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public List<byte[]> SplitIntoBuffers(double[] left, double[] right)
		{
			List<byte[]> sources = new List<byte[]>();

			// now the data buffers
			int blocks = (int)(left.Length / UsbBuffSize);
			if ((left.Length - blocks * UsbBuffSize) > 0)
			{
				throw new Exception("Left length is not an integer multiple of the USB buffer size");
			}
			int usbb = (int)UsbBuffSize;
			for (int i = 0; i < blocks; i++)
			{
				int offs = i * usbb;
				var Left = left[offs..(offs + usbb)];
				var Right = right[offs..(offs + usbb)];
				// scale to max and calibration
				Left = Left.Select(x => x * DbfsAdjustment * DacCalibration.Left).ToArray();
				Right = Right.Select(x => x * DbfsAdjustment * DacCalibration.Right).ToArray();
				var TheData = QaUsb.ToByteStream(Left, Right);   // convert to bytes
				sources.Add(TheData);
			}

			return sources;
		}


		private void DumpAllParams()
		{
			var uba = UsbDataService.Singleton;
			if (UsbDataService.ShowDebug)
			{
				UsbSubs.DebugLine($"****** Parameters *******");
				UsbSubs.DebugLine($"ParamInput: {ParamInput}, ParamOutput: {ParamOutput}, UsbBuffSize: {UsbBuffSize}");
				UsbSubs.DebugLine($"DacCalibration: {DacCalibration.Left}, {DacCalibration.Right}");
				UsbSubs.DebugLine($"AdcCalibration: {AdcCalibration.Left}, {AdcCalibration.Right}");
				UsbSubs.DebugLine($"DbfsAdjustment: {DbfsAdjustment}");
				UsbSubs.DebugLine($"PreBufSize: {PreBufSize}, PostBufSize: {PostBufSize}");
				UsbSubs.DebugLine($"****** Parameters *******");
			}
			else
				UsbSubs.DebugLine($"****** ParamInput: {ParamInput}, ParamOutput: {ParamOutput}, UsbBuffSize: {UsbBuffSize}");
		}
	}


}
