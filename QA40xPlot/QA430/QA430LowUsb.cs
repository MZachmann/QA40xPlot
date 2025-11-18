using LibUsbDotNet;
using LibUsbDotNet.Main;

// Written by MZachmann 10-4-2025
// much of the bare metal code comes originally from the QA430 App
// see https://github.com/QuantAsylum/QA430


namespace QA40xPlot.QA430
{
	public static class Qa430LowUsb
	{
		public static UsbDeviceFinder _USBFindQA430 = new UsbDeviceFinder(0x1724, 0x1002);
		private static UsbDevice? _AttachedDevice = null;
		private static string _ModelName = string.Empty;
		private static int _IdInterface = 0;

		/// <summary>
		/// from examining libusb it appears tha "Open" mainly looks for a device in the list
		/// then returns it as an object. So, this is toothless lookup of a QA device.
		/// </summary>
		/// <param name="idInterface">seems to be 0</param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public static UsbDevice? AttachDevice(int idInterface = 0)
		{
			if (_AttachedDevice != null)
				return _AttachedDevice;

			// Attempt to open QA430 device
			_ModelName = string.Empty;
			UsbDevice usbdev = UsbDevice.OpenUsbDevice(_USBFindQA430);
			if (usbdev != null)
			{
				_ModelName = "QA430";
			}
			if (usbdev != null)
			{
				// note that this is always null in my installation which seems to be WinUsb
				var iusbdev = usbdev as IUsbDevice;
				iusbdev?.ResetDevice();
				// Select config #1
				iusbdev?.SetConfiguration(1);
				iusbdev?.ClaimInterface((int)idInterface);
				_IdInterface = idInterface;
			}
			_AttachedDevice = usbdev;
			return usbdev;
		}

		public static string GetDeviceModel()
		{
			return _ModelName;
		}

		// this should probably be done on exit
		public static void DetachDevice(bool OnExit)
		{
			if (_AttachedDevice != null)
			{
				// for some reason close crashes when exiting
				if (_AttachedDevice.IsOpen && !OnExit)
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
			return (_AttachedDevice != null);
		}
	}
}
