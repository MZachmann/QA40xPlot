using QA40x_BareMetal;
using QA40xPlot.Libraries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.BareMetal
{
	internal class TestUsbLib
	{
		private static QaAnalyzer? TestCal()
		{
			var tstA = QA40x_BareMetal.QaUsb.QAnalyzer;
			var calData = tstA?.CalData;
			if (calData?.Length != 512)
			{
				var msg = "Calibration data is not 512 bytes long";
				Debug.WriteLine(msg);
				throw new Exception(msg);
			}
			var uu = calData.Select(x => (int)x).Sum();
			// relies on MY qa403 settings for testing...
			if( uu != 14719)
			{
				var msg = $"Calibration data sum is not 14719, it is {uu}";
				Debug.WriteLine(msg);
				throw new Exception(msg);
			}
			Control.DumpCalibrationData(calData);

			return tstA;
		}

		public static async Task Test()
		{
			QA40x_BareMetal.QaUsb.Open();
			TestCal();
			var tstA = QA40x_BareMetal.QaUsb.QAnalyzer;
			var chirp = QAMath.CalculateChirp(20, 20000, 0.1, 16384, 48000);
			CancellationToken ct = new CancellationToken();
			var newData = await Acquisition.DoStreamingAsync(ct, chirp.ToArray(), chirp.ToArray());
			var x = newData.Left;
			var y = newData.Right;
			var z = x.Length;
		}
	}
}
