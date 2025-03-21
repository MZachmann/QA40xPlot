namespace QA40xPlot.Data
{

    public class ThdFrequencyData
    {
        public string MeasurementType { get; } = "THD_VS_FREQUENCY";            // To identify the data type in json 

        public string Title { get; set; }
        public DateTime CreateDate { get; set; }

        public List<ThdFrequencyMeasurementResult> Measurements { get; set; }

        
        public ThdFrequencyData()
        {
            Title = string.Empty;
            CreateDate = DateTime.Now;
            Measurements = [];
        }
    }
}           
    