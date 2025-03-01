namespace QA40xPlot.Data
{

    public class ThdFrequencyData
    {
        public string MeasurementType { get; } = "THD_VS_FREQUENCY";            // To identify the data type in json 

        public string Title { get; set; }
        public DateTime CreateDate { get; set; }

        public ThdFrequencyMeasurementSettings MeasurementSettings { get; set; }
        public ThdFrequencyGraphSettings GraphSettings { get; set; }
        public List<ThdFrequencyMeasurementResult> Measurements { get; set; }

        
        public ThdFrequencyData()
        {
            Measurements = [];
            MeasurementSettings = new();
            GraphSettings = new();
        }
    }
}           
    