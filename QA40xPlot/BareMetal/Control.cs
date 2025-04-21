using QA40x_BareMetal;
using System.Diagnostics;

namespace QA40xPlot.BareMetal
{
	public class Control
	{
		private readonly QaAnalyzer _Analyzer;
		private readonly Dictionary<int, int> _Output2Reg = new() { { 18, 3 }, { 8, 2 }, { -2, 1 }, { -12, 0 } };
		private readonly Dictionary<int, int> _Input2Reg = new() { { 0, 0 }, { 6, 1 }, { 12, 2 }, { 18, 3 }, { 24, 4 }, { 30, 5 }, { 36, 6 }, { 42, 7 } };
		private readonly Dictionary<int, int> _Samplerate2Reg = new() { { 48000, 0 }, { 96000, 1 }, { 192000, 2 } };

		public Control(QaAnalyzer analyzer)
		{
			_Analyzer = analyzer;
		}

		public void SetOutput(int gain)
		{
			if (!_Output2Reg.TryGetValue(gain, out int val))
				throw new ArgumentException("Invalid output gain value.");
			QaUsb.WriteRegister(6, (byte)val);
		}

		public void SetInput(int gain)
		{
			if (!_Input2Reg.TryGetValue(gain, out int val))
				throw new ArgumentException("Invalid input gain value.");
			QaUsb.WriteRegister(5, (byte)val);
		}

		public void SetSampleRate(int rate)
		{
			if (!_Samplerate2Reg.TryGetValue(rate, out int val))
				throw new ArgumentException("Invalid sample rate value.");
			QaUsb.WriteRegister(9, (byte)val);
			Thread.Sleep(100); // Small delay to ensure the sample rate is set
		}

		public void SetWindowing(string window)
		{
			_Analyzer.Params?.SetWindowing(window);
		}

		/// <summary>
		/// read the calibration data from the device
		/// </summary>
		/// <returns>a list of calibration values in dB</returns>
		public byte[] LoadCalibration()
		{
			QaUsb.WriteRegister(0xD, 0x10);
			int pageSize = 512;
			byte[] calData = new byte[pageSize];

			for (int i = 0; i < pageSize / 4; i++)
			{
				uint d = (uint)(QaUsb.ReadRegister(0x19));
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

		public void DumpCalibrationData(byte[] calData)
		{
			string hexData = BitConverter.ToString(calData).Replace("-", " ");
			Debug.WriteLine(hexData);

			var (adcLeft, adcRight) = GetAdcCal(calData, 42);
			Debug.WriteLine($"ADC Left level: {adcLeft}, Right level: {adcRight}");

			var (dacLeft, dacRight) = GetDacCal(calData, -2);
			Debug.WriteLine($"DAC Left level: {dacLeft}, Right level: {dacRight}");
		}
	}

}