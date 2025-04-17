using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using Newtonsoft.Json;

namespace QA40xPlot.Data
{
    public class ThdFrequencyMeasurementResult : IMeasurementResult
    {
        public string Title { get; set; }                                               // Measurement title
        public string Description { get; set; }                                         // Description
        public DateTime CreateDate { get; set; }                                        // Measurement date time
        public bool Show { get; set; }                                                  // Show in graph
        public bool Saved { get; set; }                                                 // Has been saved
        public List<ThdFrequencyStep> FrequencySteps { get; set; }                      // Measurement data
        public ThdFreqViewModel MeasurementSettings { get; set; }                       //  Settings used for this measurement
		public List<ThdColumn> LeftColumns { get; set; }
		public List<ThdColumn> RightColumns { get; set; }

		[JsonIgnore]
        public LeftRightSeries? NoiseFloor { get; set; }                                 // Noise floor measurement

        public ThdFrequencyMeasurementResult(ThdFreqViewModel vm)
        {
            FrequencySteps = [];
			LeftColumns = new List<ThdColumn>();
			RightColumns = new List<ThdColumn>();
            Title = string.Empty;
            Description = string.Empty;

			MeasurementSettings = new();
			vm.CopyPropertiesTo(MeasurementSettings);
		}
    }
}
