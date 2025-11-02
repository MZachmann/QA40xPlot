namespace QA40xPlot.Data
{
	public class FreqSweepLine
	{
		public string Label { get; set; } = "";
		public FreqSweepColumn[] Columns { get; set; } = [];
	}

	public class FreqSweepDot
	{
		public string Label { get; set; } = "";
		public FreqSweepColumn Column { get; set; } = new();
	}

	// helper for thd sweeps
	public class FreqSweepColumn
    {
		public const int FreqSweepColumnCount = 7;
		// readings
		public double Mag { get; set; }
		public double Phase { get; set; }
		public double THD { get; set; }
		public double THDN { get; set; }
		public double Noise { get; set; }
		// inputs
		public double GenVolts { get; set; }
		public double Freq { get; set; }

		public FreqSweepColumn()
		{
		}

		public FreqSweepColumn(object o)
		{

		}
	}
}
