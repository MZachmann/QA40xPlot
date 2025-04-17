using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace QA40xPlot.BareMetal
{
	public static class QaLowUsb
	{
		public static UsbDeviceFinder _USBFindQA402 = new UsbDeviceFinder(0x16c0, 0x4e37);
		public static UsbDeviceFinder _USBFindQA403 = new UsbDeviceFinder(0x16c0, 0x4e39);

		public static UsbDevice AttachDevice(uint idInterface = 0)
		{ 
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
			var iusbdev = usbdev as IUsbDevice;
			iusbdev?.ResetDevice();
			// Select config #1
			iusbdev?.SetConfiguration(1); 
			iusbdev?.ClaimInterface((int)idInterface);
			return usbdev;
		}
	}
}
