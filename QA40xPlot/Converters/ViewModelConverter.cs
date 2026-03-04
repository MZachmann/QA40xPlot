using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QA40xPlot.ViewModels;

namespace QA40xPlot.Converters
{
	// when we load a test result this reads the ViewModel member as the appropriate actual viewmodel type
	// otherwise it doesn't know how to translate that object
	public class ViewModelConverter : JsonConverter
	{
		private readonly Type[] _types;
		private bool AllowRead = true;

		public ViewModelConverter()
		{
			_types = [typeof(BaseViewModel)];
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			throw new NotImplementedException("The default writer should work fine");
		}

		public override bool CanWrite
		{
			// force default writer
			get { return false; }
		}


		public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			// Load JSON into JObject for inspection
			JObject jo = JObject.Load(reader);
			var uname = jo["Name"]?.ToString() ?? string.Empty;
			// hacky switch to allow not infinite loop from base viewmodel for some reason
			AllowRead = false;
			BaseViewModel? uu = null;
			switch (uname)
			{
				case "Spectrum":
					uu = jo.ToObject<SpectrumViewModel>();
					break;
				case "Intermodulation":
					uu = jo.ToObject<ImdViewModel>();
					break;
				case "Scope":
					uu = jo.ToObject<ScopeViewModel>();
					break;
				case "Response":
					uu = jo.ToObject<FreqRespViewModel>();
					break;
				case "Frqa430":
					uu = jo.ToObject<FrQa430ViewModel>();
					break;
				case "FreqSweep":   // qa430 opamp tab & freq sweep
					uu = jo.ToObject<FreqSweepViewModel>();
					break;
				case "AmpSweep":   // qa430 opamp tab & amp sweep
					uu = jo.ToObject<AmpSweepViewModel>();
					break;
				case "Settings":
					uu = jo.ToObject<SettingsViewModel>();
					break;
			}
			AllowRead = true;
			return uu ?? new SpectrumViewModel();
			//throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
		}

		public override bool CanRead
		{
			get { return AllowRead; }
		}

		public override bool CanConvert(Type objectType)
		{
			return _types.Any(t => t == objectType);
		}
	}
}