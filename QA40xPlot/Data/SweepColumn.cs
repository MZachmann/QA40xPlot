using QA40xPlot.ViewModels;

namespace QA40xPlot.Data
{
	public class SweepLine
	{
		public string Label { get; set; } = "";
		public SweepColumn[] Columns { get; set; } = [];
	}

	public class SweepDot
	{
		public string Label { get; set; } = "";
		public SweepColumn Column { get; set; } = new();
	}

	// helper for thd sweeps
	public class SweepColumn
	{
		public const int SweepColumnCount = 13;
		// readings
		public double Mag { get; set; }
		public double Phase { get; set; }
		public double THD { get; set; }
		public double THDN { get; set; }
		public double Noise { get; set; }
		public double NoiseFloor { get; set; }
		public double D2 { get; set; }
		public double D3 { get; set; }
		public double D4 { get; set; }
		public double D5 { get; set; }
		public double D6P { get; set; }
		// inputs
		public double GenVolts { get; set; }
		public double Freq { get; set; }

		public SweepColumn()
		{
		}
		public SweepColumn(SweepColumn src)
		{
			src.CopyPropertiesTo<SweepColumn>(this);
		}
	}
}
