namespace QA40xPlot.Data
{

	public class ImdData
	{
		public string MeasurementType { get; } = "IMDB_VS_FREQUENCY";            // To identify the data type in json 

		public string Title { get; set; }
		public DateTime CreateDate { get; set; }

		public List<ImdMeasurementResult> Measurements { get; set; }


		public ImdData()
		{
			Title = string.Empty;
			CreateDate = DateTime.Now;
			Measurements = [];
		}
	}
}
