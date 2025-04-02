using QA40xPlot.Libraries;
using QA40xPlot.ViewModels;
using Newtonsoft.Json;

namespace QA40xPlot.Data
{
	public class ScopeMeasurementResult : IMeasurementResult
	{
		public string Title { get; set; }                                               // Measurement title
		public string Description { get; set; }                                         // Description
		public DateTime CreateDate { get; set; }                                        // Measurement date time
		public bool Show { get; set; }                                                  // Show in graph
		public bool Saved { get; set; }                                                 // Has been saved
		public List<ThdFrequencyStep> FrequencySteps { get; set; }                      // Measurement data
		public ScopeViewModel MeasurementSettings { get; set; }                       //  Settings used for this measurement

		[JsonIgnore]
		public LeftRightSeries? NoiseFloor { get; set; }                                 // Noise floor measurement

		public ScopeMeasurementResult(ScopeViewModel vm)
		{
			Title = string.Empty;
			Description = string.Empty;
			CreateDate = DateTime.Now;
			Show = false;
			Saved = false;
			FrequencySteps = [];
			MeasurementSettings = new();
			vm.CopyPropertiesTo(MeasurementSettings);
			NoiseFloor = null;
		}
	}
}
