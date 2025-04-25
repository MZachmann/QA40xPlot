using LibUsbDotNet;
using LibUsbDotNet.Main;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40xPlot.BareMetal
{
	public static class QaLowUsb
	{
		public static UsbDeviceFinder _USBFindQA402 = new UsbDeviceFinder(0x16c0, 0x4e37);
		public static UsbDeviceFinder _USBFindQA403 = new UsbDeviceFinder(0x16c0, 0x4e39);
		private static UsbDevice? _AttachedDevice = null;
		private static int _IdInterface = 0;

		/// <summary>
		/// from examining libusb it appears tha "Open" mainly looks for a device in the list
		/// then returns it as an object. So, this is toothless lookup of a QA device.
		/// </summary>
		/// <param name="idInterface">seems to be 0</param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public static UsbDevice AttachDevice(int idInterface = 0)
		{
			if (_AttachedDevice != null)
				return _AttachedDevice;

			// Attempt to open QA402 or QA403 device
			UsbDevice usbdev = UsbDevice.OpenUsbDevice(_USBFindQA402);
			if(usbdev == null)
			{
				usbdev = UsbDevice.OpenUsbDevice(_USBFindQA403);
			}
			if (usbdev == null)
			{
				throw new Exception("No QA402/QA403 analyzer found");
			}
			// note that this is always null in my installation which seems to be WinUsb
			var iusbdev = usbdev as IUsbDevice;
			iusbdev?.ResetDevice();
			// Select config #1
			iusbdev?.SetConfiguration(1); 
			iusbdev?.ClaimInterface((int)idInterface);
			_IdInterface = idInterface;
			// keep the found device around
			_AttachedDevice = usbdev;
			return usbdev;
		}

		// this should probably be done on exit
		public static void DetachDevice(bool OnExit)
		{
			if (_AttachedDevice != null)
			{
				// for some reason close crashes when exiting
				if(_AttachedDevice.IsOpen && !OnExit)
				{
					var iusbdev = _AttachedDevice as IUsbDevice;
					iusbdev?.ReleaseInterface(_IdInterface);
					_AttachedDevice?.Close();
				}
				_AttachedDevice = null;
			}
		}

		public static bool IsDeviceConnected()
		{
			if( _AttachedDevice == null)
			{
				return false;
			}
			// Check if the device is still connected

			return true;
		}
	}
}
