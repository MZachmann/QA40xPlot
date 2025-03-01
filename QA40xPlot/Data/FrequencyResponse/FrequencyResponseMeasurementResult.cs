using QA40xPlot.Libraries;

namespace QA40xPlot.Data
{
    public class FrequencyResponseMeasurementResult : IMeasurementResult
    {
        public string Title { get; set; }                                               // Measurement title
        public string Description { get; set; }                                         // Description
        public DateTime CreateDate { get; set; }                                        // Measurement date time
        public bool Show { get; set; }                                                  // Show in graph
        public bool Saved { get; set; }                                                 // Has been saved
        public LeftRightSeries FrequencyResponseData { get; set; }                      // Measurement data
        public double[] GainData { get; set; }                                          // Calculated gain over frequency
        public FrequencyResponseMeasurementSettings MeasurementSettings { get; set; }   //  Settings used for this measurement

        public FrequencyResponseMeasurementResult()
        {
            FrequencyResponseData = new();
            MeasurementSettings = new();
        }
    }
}
