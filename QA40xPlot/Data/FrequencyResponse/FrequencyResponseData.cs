namespace QA40xPlot.Data
{

    public class FrequencyResponseData
    {
        public string MeasurementType { get; } = "FREQUENCY_RESPONSE_CHIRP";            // To identify the data type in json 

        public string Title { get; set; }
        public DateTime CreateDate { get; set; }

        public FrequencyResponseMeasurementSettings MeasurementSettings { get; set; }
        public FrequencyResponseGraphSettings GraphSettings { get; set; }
        public List<FrequencyResponseMeasurementResult> Measurements { get; set; }


        public FrequencyResponseData()
        {
            Title = string.Empty;
            MeasurementType = string.Empty;
            CreateDate = DateTime.Now;
            Measurements = [];
            MeasurementSettings = new();
            GraphSettings = new();
        }
    }
}           
    