using QA40xPlot.BareMetal;
using QA40xPlot.Data;
using QA40xPlot.Libraries;
using QA40xPlot.QA430;
using QA40xPlot.ViewModels;

namespace QA40xPlot.Actions
{
	public partial class ActOpamp : ActBase
	{
		protected static SweepColumn ArrayToColumn(double[] rawData, uint startIdx)
		{
			SweepColumn col = new();
			col.Freq = rawData[startIdx];
			col.Mag = rawData[startIdx + 1];
			col.Phase = rawData[startIdx + 2];
			col.THD = rawData[startIdx + 3];
			col.THDN = rawData[startIdx + 4];
			col.Noise = rawData[startIdx + 5];
			col.NoiseFloor = rawData[startIdx + 6];
			col.GenVolts = rawData[startIdx + 7];
			col.D2 = rawData[startIdx + 8];
			col.D3 = rawData[startIdx + 9];
			col.D4 = rawData[startIdx + 10];
			col.D5 = rawData[startIdx + 11];
			col.D6P = rawData[startIdx + 12];
			return col;
		}

		/// <summary>
		/// convert raw binary data into a list of columns grouped by sweep value
		/// </summary>
		/// <param name="steps"></param>
		/// <param name="raw"></param>
		/// <param name="hasQa430"></param>
		/// <param name="isFreq"></param>
		/// <returns></returns>
		protected static List<SweepLine> RawToColumns(AcquireStep[] steps, double[] raw, bool hasQa430, bool isFreq)
		{
			if (raw.Length == 0)
				return [];
			List<SweepColumn> columns = new();
			// make columns from the raw data
			int i;
			for (i = 0; i < raw.Length; i += SweepColumn.SweepColumnCount)
			{
				var col = ArrayToColumn(raw, (uint)i);
				columns.Add(col);
			}
			// convert into freqsweeplines
			var showX = false;
			List<SweepLine> lines = new();
			var line = new SweepLine();
			var stepNo = 0;
			line.Label = steps[stepNo].ToSuffix(showX, hasQa430);
			for (i = 0; i < columns.Count; i++)
			{
				var c = columns[i];
				var cd = line.Columns.Append(c);
				line.Columns = cd.ToArray();
				if (i < (columns.Count - 1))
				{
					var useC = isFreq ? (columns[i + 1].Freq <= c.Freq) : (columns[i + 1].GenVolts <= c.GenVolts);
					if (useC)
					{
						showX = true;    // more than one line
						line.Label = steps[stepNo].ToSuffix(showX, hasQa430);
						lines = lines.Append(line).ToList();
						line = new();
						stepNo++;
					}
				}
			}
			if (line.Columns.Length > 0)
			{
				line.Label = steps[stepNo].ToSuffix(showX, hasQa430);
				lines = lines.Append(line).ToList();
			}
			return lines;
		}

		protected double[] ColumnToArray(SweepColumn col)
		{
			return new double[] { col.Freq, col.Mag, col.Phase, col.THD, col.THDN, col.Noise, col.NoiseFloor, col.GenVolts, col.D2, col.D3, col.D4, col.D5, col.D6P };
		}

		/// <summary>
		/// do post processing, just a bunch of easy math and moving stuff into viewmodels
		/// </summary>
		/// <param name="ct">Cancellation token</param>
		/// <returns>result. false if cancelled</returns>
		protected (SweepColumn?, SweepColumn?) CalculateColumn(LeftRightFrequencySeries? lfrs, BaseViewModel bvm, LeftRightPair noiseFloor, double dFreq,
			CancellationToken ct, AcquireStep acqConfig, LeftRightFrequencySeries? lfrsNoise)
		{
			if (lfrs == null)
			{
				return (null, null);
			}

			// left and right channels summary info to fill in
			var left = new SweepColumn();
			var right = new SweepColumn();
			SweepColumn[] steps = [left, right];

			var maxf = .95 * lfrs.Df * lfrs.Left.Length;    // skip the end-gunk
			var maxScan = Math.Min(ViewSettings.NoiseBandwidth, maxf);  // opamps use 80KHz bandwidth, audio uses 20KHz

			LeftRightPair thds = QaCompute.GetThdDb(bvm.WindowingMethod, lfrs, dFreq, 20.0, maxf);
			LeftRightPair thdN = QaCompute.GetThdnDb(bvm.WindowingMethod, lfrs, dFreq, 20.0, maxScan, ViewSettings.NoiseWeight);

			var floor = noiseFloor;
			double dmult = acqConfig.Distgain;
			// here steps just counts left then right
			foreach (var step in steps)
			{
				bool bl = step == left;     // stepping left?
				step.Freq = dFreq;
				var frqsr = bl ? lfrs.Left : lfrs.Right;
				step.Mag = QaMath.MagAtFreq(frqsr, lfrs.Df, dFreq);
				step.THD = step.Mag * Math.Pow(10, (bl ? thds.Left : thds.Right) / 20); // in volts from dB relative to mag
				step.THDN = step.Mag * Math.Pow(10, (bl ? thdN.Left : thdN.Right) / 20); // in volts from dB relative to mag
				step.Phase = 0;
				step.D2 = (maxf > (2 * dFreq)) ? QaMath.MagAtFreq(frqsr, lfrs.Df, 2 * dFreq) : 1e-10;
				step.D3 = (maxf > (3 * dFreq)) ? QaMath.MagAtFreq(frqsr, lfrs.Df, 3 * dFreq) : step.D2;
				step.D4 = (maxf > (4 * dFreq)) ? QaMath.MagAtFreq(frqsr, lfrs.Df, 4 * dFreq) : step.D3;
				step.D5 = (maxf > (5 * dFreq)) ? QaMath.MagAtFreq(frqsr, lfrs.Df, 5 * dFreq) : step.D4;
				step.D6P = 0;
				if (maxf > (6 * dFreq))
				{
					for (int i = 6; i < 10; i++)
					{
						step.D6P += (maxf > (i * dFreq)) ? QaMath.MagAtFreq(frqsr, lfrs.Df, i * dFreq) : 0;
					}
				}
				else
				{
					step.D6P = step.D5;
				}
				if (!bl)
				{
					step.NoiseFloor = floor.Right / dmult; // noise floor
					if (lfrsNoise != null)
						step.Noise = GetNoiseSmooth(lfrsNoise.Right, lfrsNoise.Df, dFreq) / dmult; // noise density smoothed
				}
				else
				{
					step.NoiseFloor = floor.Left / dmult; // noise floor
					if (lfrsNoise != null)
						step.Noise = GetNoiseSmooth(lfrsNoise.Left, lfrsNoise.Df, dFreq) / dmult; // noise density smoothed
				}

				// divide by the amount of distortion gain since that is a voltage gain
				step.THD /= dmult;
				step.THDN /= dmult;
			}

			return (left, right);
		}

		// input noises is uniform frequency based on fft
		public static double GetNoiseSmooth(double[] noises, double binSize, double dFreq)
		{
			var bin = QaLibrary.GetBinOfFrequency(dFreq, binSize);  // which frequency bin
			var bincnt = dFreq / (20 * binSize);  // #bins == +-1/10 of an octave
			bincnt = Math.Min(10, bincnt);     // limit to 100 bins
			var minbin = Math.Max(0, (int)(bin - bincnt));
			var maxbin = Math.Min(noises.Length - 1, (int)(bin + bincnt));
			var avenoise = noises.Skip(minbin).Take(maxbin - minbin).Average(x => x); // average noise within these bins
			return avenoise / Math.Sqrt(binSize);   // ? i think
		}



	}
}
