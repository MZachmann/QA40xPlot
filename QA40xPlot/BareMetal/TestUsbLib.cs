// Written by MZachmann 4-2-2026
// some way to exercise the USB ports to see what happens with edge conditions
// and to verify timing of usb transfers

using LibUsbDotNet.Main;
using QA40xPlot.Libraries;

namespace QA40xPlot.BareMetal
{
	public static class TestUsbLib
	{
		/// <summary>
		/// This runs through a few USB tests to evaluate some edge conditions
		/// </summary>
		/// <returns></returns>
		public static async Task EvaluateUsb()
		{
			await TestUsbLib.InitializeTests();
			await TestUsbLib.BasicAsyncTest();
			await TestUsbLib.BasicAsyncTest2();
			await TestUsbLib.BasicAsyncTest3();
			var bfr = await TestUsbLib.GetUsbSyncBuffer();
			await Task.Delay(5);    // a place to debug
		}

		internal static async Task<bool> InitializeTests()
		{
			try
			{
				UsbSubs.DebugLine("***** InitializeTest");
				var qausb = QaComm.GetUsb();
				if (qausb == null)
					return false;

				await QaComm.InitializeDevice(96000, 32768, "Hann", 42);
				if (qausb.DataReader == null || qausb.DataWriter == null)
					return false;
				return true;
			}
			catch (Exception ex)
			{
				UsbSubs.DebugLine($"Failed to initialize device for usb test: {ex.Message}");
			}
			return false;
		}
		// this test starts with transfer disabled and then 
		// submits one asynchronous 64KB write and two 32KB reads
		// then it waits for them all to complete
		internal static async Task<bool> BasicAsyncTest()
		{
			try
			{
				UsbSubs.DebugLine("***** BasicAsyncTest");
				var qausb = QaComm.GetUsb();
				if (qausb == null || qausb.DataReader == null || qausb.DataWriter == null)
					return false;

				// create an input buffer and output buffer
				var obuf = new byte[8 * 1024];
				var inbuf = new byte[32 * 1024];
				obuf = obuf.Select(x => (byte)1).ToArray();
				//
				// test with usb off
				//
				qausb.WriteRegister(8, 0); // turn off usb data transfer
				await Task.Delay(100);
				//
				UsbSubs.DebugLine("Submitting 3 async transfers with usb off.");
				var ec2 = qausb.DataWriter.SubmitAsyncTransfer(obuf, 0, obuf.Length, 4000, out UsbTransfer contextW);
				var ec2a = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out UsbTransfer contextR);
				var ec2b = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out UsbTransfer contextR2);
				UsbSubs.DebugLine("Submitted 3 async transfers with usb off.");
				bool worksW = false;
				bool worksR = false;
				bool worksR2 = false;
				qausb.WriteRegister(8, 0x5); // turn on usb data transfer
				UsbSubs.DebugLine("Enabled usb...");
				int retry = 0;
				while (retry < 100)
				{
					worksW = contextW?.IsCompleted ?? false;
					worksR = contextR?.IsCompleted ?? false;
					worksR2 = contextR2?.IsCompleted ?? false;
					await Task.Delay(50);
					retry++;
					if (worksR2 == true)
					{
						break;
					}
				}
				UsbSubs.DebugLine($"Finished off usb test with {worksW} {worksR} {worksR2}.");
				if(worksW && worksW && worksR2)
				{
					UsbSubs.DebugLine($"Read infinite after any 8K or more write.");
				}

				contextR?.Cancel();
				contextR?.Dispose();
				contextR2?.Cancel();
				contextR2?.Dispose();
				contextW?.Cancel();
				contextW?.Dispose();

				qausb.WriteRegister(8, 0); // turn off usb data transfer
				return worksW && worksR && worksR2;
			}
			catch (Exception ex)
			{
				UsbSubs.DebugLine($"Failed in usb test: {ex.Message}");
			}
			return false;
		}

		// this test starts with transfer enabled and then 
		// submits one asynchronous 64KB write and two 32KB reads
		// then it waits for them all to complete
		internal static async Task<bool> BasicAsyncTest2()
		{
			try
			{
				UsbSubs.DebugLine("***** BasicAsyncTest2");
				var qausb = QaComm.GetUsb();
				if (qausb == null || qausb.DataReader == null || qausb.DataWriter == null)
					return false;

				// create an input buffer and output buffer
				var obuf = new byte[64 * 1024];
				var inbuf = new byte[32 * 1024];
				obuf = obuf.Select(x => (byte)1).ToArray();
				//
				// test with usb on
				//
				qausb.WriteRegister(8, 0x5); // turn on usb data transfer
				await Task.Delay(100);
				//
				UsbSubs.DebugLine("Submitting 3 async transfers with usb on.");
				var ec2 = qausb.DataWriter.SubmitAsyncTransfer(obuf, 0, obuf.Length, 4000, out UsbTransfer contextW);
				var ec2a = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out UsbTransfer contextR);
				var ec2b = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out UsbTransfer contextR2);
				UsbSubs.DebugLine("Submitted 3 async transfers with usb on.");
				bool worksW = false;
				bool worksR = false;
				bool worksR2 = false;
				qausb.WriteRegister(8, 0x5); // turn on usb data transfer
				UsbSubs.DebugLine("re-Enabled usb...");
				int retry = 0;
				while (retry < 100)
				{
					worksW = contextW?.IsCompleted ?? false;
					worksR = contextR?.IsCompleted ?? false;
					worksR2 = contextR2?.IsCompleted ?? false;
					await Task.Delay(50);
					retry++;
					if (worksR2 == true)
					{
						break;
					}
				}
				UsbSubs.DebugLine($"Finished on usb test with {worksW} {worksR} {worksR2}.");

				contextR?.Cancel();
				contextR?.Dispose();
				contextR2?.Cancel();
				contextR2?.Dispose();
				contextW?.Cancel();
				contextW?.Dispose();

				qausb.WriteRegister(8, 0); // turn off usb data transfer
				return worksW && worksR && worksR2;
			}
			catch (Exception ex)
			{
				UsbSubs.DebugLine($"Failed in on usb test: {ex.Message}");
			}
			return false;
		}

		// this test starts with transfer disabled and then 
		// submits no asynchronous 64KB write but two 32KB reads
		// then it waits for them all to complete
		internal static async Task<bool> BasicAsyncTest3()
		{
			try
			{
				UsbSubs.DebugLine("***** BasicAsyncTest3");
				var qausb = QaComm.GetUsb();
				if (qausb == null || qausb.DataReader == null || qausb.DataWriter == null)
					return false;

				// create an input buffer and output buffer
				var obuf = new byte[64 * 1024];
				var inbuf = new byte[32 * 1024];
				obuf = obuf.Select(x => (byte)1).ToArray();
				//
				// test with usb off
				//
				qausb.WriteRegister(8, 0); // turn off usb data transfer
				await Task.Delay(100);
				//
				UsbSubs.DebugLine("Submitting 2 async transfers with usb off.");
				var ec2a = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out UsbTransfer contextR);
				var ec2b = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out UsbTransfer contextR2);
				UsbSubs.DebugLine("Submitted 2 async transfers with usb off.");
				bool worksR = false;
				bool worksR2 = false;
				qausb.WriteRegister(8, 0x5); // turn on usb data transfer
				UsbSubs.DebugLine("Enabled usb...");
				int retry = 0;
				while (retry < 10)
				{
					worksR = contextR?.IsCompleted ?? false;
					worksR2 = contextR2?.IsCompleted ?? false;
					await Task.Delay(50);
					retry++;
					if (worksR2 == true)
					{
						break;
					}
				}
				UsbSubs.DebugLine($"Finished nowrite usb test with n/a {worksR} {worksR2}.");
				if(worksR || worksR2)
				{
					UsbSubs.DebugLine("*** Able to async read data without a write!");
				}
				else
				{
					UsbSubs.DebugLine("*** Without a write there is no read!");
					var ec2c = qausb.DataWriter.SubmitAsyncTransfer(obuf, 0, obuf.Length, 4000, out UsbTransfer contextW);
					retry = 0;
					while (retry < 10)
					{
						worksR = contextR?.IsCompleted ?? false;
						worksR2 = contextR2?.IsCompleted ?? false;
						await Task.Delay(50);
						retry++;
						if (worksR2 == true)
						{
							break;
						}
					}
					UsbSubs.DebugLine($"Finished nowrite usb test after queueing async write with n/a {worksR} {worksR2} ...");
					if(worksR && worksR2)
					{
						UsbSubs.DebugLine($"Submitting any async write much after reads still enables the async reads.");
						UsbTransfer contxt = default!;
						int ctr = 0;
						List<int> tlist = new();
						DateTime tnow = DateTime.Now;
						var tdiff = (DateTime.Now - tnow).Milliseconds;
						for (; ctr < 10; ctr++)
						{
							var ecout = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out contxt);
							if (!await contxt.AsyncWaitHandle.WaitHandleAsync(300))
							{
								UsbSubs.DebugLine($"Unable to async read data without a write after {ctr} reads!");
								break;
							}
							tdiff = (int)(DateTime.Now - tnow).TotalMilliseconds;
							tlist.Add(tdiff);
							tnow = DateTime.Now;
						}
						// wait 5 seconds to see if we can keep reading after a delay
						await Task.Delay(5000);
						tlist.Add(5000);
						for (;ctr>=10 && ctr < 20; ctr++)
						{
							var ecout = qausb.DataReader.SubmitAsyncTransfer(inbuf, 0, inbuf.Length, 4000, out contxt);
							if (!await contxt.AsyncWaitHandle.WaitHandleAsync(300))
							{
								UsbSubs.DebugLine($"Unable to async read data without a write after {ctr} reads!");
								break;
							}
							tdiff = (int)(DateTime.Now - tnow).TotalMilliseconds;
							tlist.Add(tdiff);
							tnow = DateTime.Now;
						}
						if (ctr == 20)
						{
							UsbSubs.DebugLine($"Able to async read data without a write after many writes!");
						}
						UsbSubs.DebugLine($"AsyncRead time list: {string.Join(',', tlist)}.");
						contxt?.Cancel();
						contxt?.Dispose();
					}
				}

				contextR?.Cancel();
				contextR?.Dispose();
				contextR2?.Cancel();
				contextR2?.Dispose();

				qausb.WriteRegister(8, 0); // turn off usb data transfer
				return worksR && worksR2;
			}
			catch (Exception ex)
			{
				UsbSubs.DebugLine($"Failed in usb test: {ex.Message}");
			}
			return false;
		}

		// this test starts with transfer enabled and then 
		// submits a synchronous write stream then a synchronous read stream
		// and sees if there flow is continuous or stops for both
		internal static async Task<long> GetUsbSyncBuffer()
		{
			try
			{
				UsbSubs.DebugLine("***** GetUsbSyncBuffer");
				var qausb = QaComm.GetUsb();
				if (qausb == null || qausb.DataReader == null || qausb.DataWriter == null)
					return 0;

				// create an input buffer and output buffer
				var obuf = new byte[64 * 1024];
				var inbuf = new byte[32 * 1024];
				obuf = obuf.Select(x => (byte)1).ToArray();
				//
				// test with usb off
				//
				qausb.WriteRegister(8, 0x5); // turn on usb data transfer
				await Task.Delay(100);
				//
				// send synchronous data until it throws up
				var ec = ErrorCode.None;
				// write one buffers of data i think
				ec = qausb.DataWriter.Write(obuf, 500, out int transfLen1);
				var tdelay = 1000;
				var tnow = DateTime.Now;
				var tdiff = (DateTime.Now - tnow).Milliseconds;
				var copies = 0;
				List<int> tdifList = new();
				// reading
				while (tdiff < tdelay && ec == ErrorCode.None && copies < 25)
				{
					ec = qausb.DataReader.Read(inbuf, 1000, out int transfLen);
					tdiff = (int)(DateTime.Now - tnow).TotalMilliseconds;
					tdifList.Add(tdiff);
					tnow = DateTime.Now;
					copies++;
				}
				if (copies == 25)
					UsbSubs.DebugLine("Sync read can go forever...");
				else if(copies > 1)
					UsbSubs.DebugLine($"Sync read capacity {copies * obuf.Length}...");
				else
				{
					UsbSubs.DebugLine($"Errorcode {ec}...");
				}
				// writing
				ec = ErrorCode.None;
				copies = 0;
				tdelay = 1000;
				tnow = DateTime.Now;
				tdiff = (DateTime.Now - tnow).Milliseconds;
				tdifList.Add(100000);
				UsbSubs.DebugLine("Submitting sync writes with usb on...");
				while (tdiff < tdelay && ec == ErrorCode.None && copies < 35)
				{
					ec = qausb.DataWriter.Write(obuf, 1000, out int transfLen);
					tdiff = (int)(DateTime.Now - tnow).TotalMilliseconds;
					tdifList.Add(tdiff);
					tnow = DateTime.Now;
					copies++;
				}
				if(copies == 35)
					UsbSubs.DebugLine("Sync write can go forever...");
				else
					UsbSubs.DebugLine($"Sync write capacity {copies * obuf.Length}...");
				// reading
				ec = ErrorCode.None;
				copies = 0;
				tdelay = 1000;
				tnow = DateTime.Now;
				tdiff = (DateTime.Now - tnow).Milliseconds;
				tdifList.Add(100000);
				// reading
				while (tdiff < tdelay && ec == ErrorCode.None && copies < 75)
				{
					ec = qausb.DataReader.Read(inbuf, 1000, out int transfLen);
					tdiff = (int)(DateTime.Now - tnow).TotalMilliseconds;
					tdifList.Add(tdiff);
					tnow = DateTime.Now;
					copies++;
				}
				if (copies == 75)
					UsbSubs.DebugLine("Sync read can go forever...");
				else if (copies > 1)
					UsbSubs.DebugLine($"Sync read capacity {copies * obuf.Length}...");
				else
				{
					UsbSubs.DebugLine($"Errorcode {ec}...");
				}
				UsbSubs.DebugLine($"Sync time list: {string.Join(',', tdifList)}...");

				qausb.WriteRegister(8, 0); // turn off usb data transfer
				return copies * obuf.Length;
			}
			catch (Exception ex)
			{
				UsbSubs.DebugLine($"Failed in usb test: {ex.Message}");
			}
			return 0;
		}
	}
}
