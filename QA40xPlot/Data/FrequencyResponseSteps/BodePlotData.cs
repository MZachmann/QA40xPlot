
namespace QA40xPlot.Data
{

    public class BodePlotData
    {
        public string MeasurementType { get; } = "FREQUENCY_RESPONSE_STEPS";            // To identify the data type in json 

        public string Title { get; set; }
        public DateTime CreateDate { get; set; }

        public BodePlotMeasurementSettings MeasurementSettings { get; set; }
        public BodePlotGraphSettings GraphSettings { get; set; }
        public List<BodePlotMeasurementResult> Measurements { get; set; }


        public BodePlotData()
        {
            Measurements = [];
            MeasurementSettings = new();
            GraphSettings = new();
        }
    }
}           
    