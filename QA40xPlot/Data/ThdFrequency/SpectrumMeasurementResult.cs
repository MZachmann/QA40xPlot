using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using System.Text.Json.Serialization;

namespace QA40xPlot.Data
{
    public class SpectrumMeasurementResult : IMeasurementResult
    {
        public string Title { get; set; }                                               // Measurement title
        public string Description { get; set; }                                         // Description
        public DateTime CreateDate { get; set; }                                        // Measurement date time
        public bool Show { get; set; }                                                  // Show in graph
        public bool Saved { get; set; }                                                 // Has been saved
        public List<ThdFrequencyStep> FrequencySteps { get; set; }                      // Measurement data
        public SpectrumViewModel MeasurementSettings { get; set; }                       //  Settings used for this measurement

        [JsonIgnore]
        public LeftRightSeries? NoiseFloor { get; set; }                                 // Noise floor measurement

        public SpectrumMeasurementResult()
        {
            FrequencySteps = [];
            MeasurementSettings = new();
			NoiseFloor = null;
		}
    }
}
