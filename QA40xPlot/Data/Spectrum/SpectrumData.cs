﻿namespace QA40xPlot.Data
{

	public class SpectrumData
	{
		public string MeasurementType { get; } = "THD_VS_FREQUENCY";            // To identify the data type in json 

		public string Title { get; set; }
		public DateTime CreateDate { get; set; }

		public List<SpectrumMeasurementResult> Measurements { get; set; }


		public SpectrumData()
		{
			Title = string.Empty;
			CreateDate = DateTime.Now;
			Measurements = [];
		}
	}
}
