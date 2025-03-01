namespace QA40xPlot.Data
{

    public class ThdAmplitudeData
    {
        public string MeasurementType { get; } = "THD_VS_AMPLITUDE";            // To identify the data type in json 

        public string Title { get; set; }
        public DateTime CreateDate { get; set; }

        public ThdAmplitudeMeasurementSettings MeasurementSettings { get; set; }
        public ThdAmplitudeGraphSettings GraphSettings { get; set; }
        public List<ThdAmplitudeMeasurementResult> Measurements { get; set; }


        public ThdAmplitudeData()
        {
            Measurements = [];
            MeasurementSettings = new();
            GraphSettings = new();
        }
    }
}           
    