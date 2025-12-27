using QA40xPlot.Data;

namespace QA40xPlot.Actions
{
	public partial class ActOpamp : ActBase
	{
		protected SweepColumn ArrayToColumn(double[] rawData, uint startIdx)
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

		protected double[] ColumnToArray(SweepColumn col)
		{
			return new double[] { col.Freq, col.Mag, col.Phase, col.THD, col.THDN, col.Noise, col.NoiseFloor, col.GenVolts, col.D2, col.D3, col.D4, col.D5, col.D6P };
		}
	}
}
