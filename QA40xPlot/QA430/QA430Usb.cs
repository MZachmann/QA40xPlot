using LibUsbDotNet;
using LibUsbDotNet.Main;
using QA40xPlot.ViewModels;
using System.Diagnostics;


// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40xPlot.QA430
{
	/// <summary>
	/// This class is solely used to communicate with the USB device. It handles the
	/// higher level calls and basic read/writes. It also has replacment calls
	/// for the common QaLibrary methods such as DoAcquisition and InitializeDevice.
	/// </summary>
	class Qa430Usb
	{
		public QA430Model QAModel { get; } = new();
		object ReadRegLock = new object();
		readonly int RegReadWriteTimeout = 100;
		private static Qa430Usb _Usb430 = new();      // our local Usb controller
		public static Qa430Usb Singleton { get { return _Usb430; } }

		// the usb device we talk to
		public UsbDevice? Device { get; private set; } = null;

		// the reader/writer pairs for register and data endpoints
		public UsbEndpointReader? RegisterReader { get; private set; } = null;
		public UsbEndpointWriter? RegisterWriter { get; private set; } = null;

		public Qa430Usb()
		{

		}

		/// <summary>
		/// Do the startup of the QA430, attaching USB if needed
		/// </summary>
		/// <returns>success true or false</returns>
		public bool InitializeConnection()
		{
			bool brslt = false;
			try
			{
				brslt = IsOpen();
				if (!brslt)
				{
					brslt = Open();
					if (brslt)
					{
						brslt = VerifyConnection();
						if (!brslt)
						{
							// hmm it's not us I guess
							Close(false);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error: {ex.Message}");
			}
			return brslt;
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
			// Attempt to open QA430 device
			try
			{
				Device = Qa430LowUsb.AttachDevice();
				if (null == Device)
				{
					Debug.WriteLine("No QA430 opamp analyzer found");
					return false;
				}
				RegisterReader = Device.OpenEndpointReader(ReadEndpointID.Ep01);
				RegisterWriter = Device.OpenEndpointWriter(WriteEndpointID.Ep01);
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
				// if we're exiting really close this stuff
				if (OnExit)
				{
					QAModel.KillTimer();
					// clear stuff
					RegisterReader = null;
					RegisterWriter = null;
					Qa430LowUsb.DetachDevice(OnExit);
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
				var rval = ReadRegister(0);

				if (rval == val)
					return true;
				Debug.WriteLine($"Verification check comparison failure {rval} sb {val} ");
			}
			catch (Exception)
			{
			}
			return false;
		}


		protected async Task showMessage(String msg, int delay = 0)
		{
			var vm = ViewSettings.Singleton.MainVm;
			await vm.SetProgressMessage(msg, delay);
		}

		/// <summary>
		/// Performs a read on a USB register
		/// </summary>
		/// <param name="reg"></param>
		/// <returns></returns>
		public Int32 ReadRegister(byte reg)
		{
			byte[] data = new byte[4];
			Int32 val;

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

					val = ((data[0] << 24) + (data[1] << 16) + (data[2] << 8) + data[3]);

				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error: {ex.Message}");
					throw;
				}
			}
			//Debug.WriteLine($"Read register {reg} has {val:x}");
			return val;
		}

		/// <summary>
		/// Writes to a USB register
		/// </summary>
		/// <param name="reg"></param>
		/// <param name="val"></param>
		public void WriteRegister(byte reg, uint val)
		{
			//Debug.WriteLine($"Write register {reg} with {val:x}");
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
	}
}
