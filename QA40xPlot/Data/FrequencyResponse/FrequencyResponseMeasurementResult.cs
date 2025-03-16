using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;

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
        public List<System.Numerics.Complex> GainData { get; set; }
        public List<double> GainFrequencies { get; set; }
            // Calculated gain over frequency
        public FreqRespViewModel MeasurementSettings { get; set; }   //  Settings used for this measurement

        public FrequencyResponseMeasurementResult(FreqRespViewModel vm)
        {
            FrequencyResponseData = new();
			MeasurementSettings = new();        // make a static copy of the settings so we don't worry about threads
			vm.CopyPropertiesTo(MeasurementSettings);
		}
    }
}
