namespace QA40xPlot.Data
{
	// helper for thd sweeps
	public class ThdColumn
	{
		public const int ThdColumnCount = 11;
		// readings
		public double Mag { get; set; }
		public double THD { get; set; }
		public double THDN { get; set; }
		public double Noise { get; set; }
		public double D2 { get; set; }
		public double D3 { get; set; }
		public double D4 { get; set; }
		public double D5 { get; set; }
		public double D6P { get; set; }
		// inputs
		public double GenVolts { get; set; }
		public double Freq { get; set; }

		public ThdColumn()
		{
		}

		public ThdColumn(object o)
		{

		}
	}
}
