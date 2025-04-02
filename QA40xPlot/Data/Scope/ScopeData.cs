namespace QA40xPlot.Data
{

	public class ScopeData
	{
		public string MeasurementType { get; } = "THD_VS_FREQUENCY";            // To identify the data type in json 

		public string Title { get; set; }
		public DateTime CreateDate { get; set; }

		public List<ScopeMeasurementResult> Measurements { get; set; }


		public ScopeData()
		{
			Title = string.Empty;
			CreateDate = DateTime.Now;
			Measurements = [];
		}
	}
}
