using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using Newtonsoft.Json;

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

		public SpectrumMeasurementResult(SpectrumViewModel vm)
		{
			FrequencySteps = [];
			MeasurementSettings = new();
			vm.CopyPropertiesTo(MeasurementSettings);
			NoiseFloor = null;
		}
	}
}
