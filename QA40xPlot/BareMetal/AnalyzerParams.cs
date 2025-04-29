
using QA40xPlot.Libraries;

// Written by MZachmann 4-24-2025
// much of the bare metal code comes originally from the PyQa40x library and from the Qa40x_BareMetal library on github
// see https://github.com/QuantAsylum/QA40x_BareMetal
// and https://github.com/QuantAsylum/PyQa40x


namespace QA40xPlot.BareMetal
{
	public class AnalyzerParams
	{
		public int SampleRate { get;  set; }
		public int MaxInputLevel { get;  set; }
		public int MaxOutputLevel { get;  set; }
		public int PreBuffer { get;  set; }
		public int PostBuffer { get;  set; }
		public int FFTSize { get;  set; }
		public OutputSources OutputSource { get; set; } = OutputSources.Off;
		public string WindowType { get;  set; }
		public double ACF { get; private set; }
		public double ECF { get; private set; }

		public AnalyzerParams(
			int sampleRate = 48000,
			int maxInputLevel = 42,
			int maxOutputLevel = 18,
			int preBuffer = 2048,
			int postBuffer = 2048,
			int fftSize = 16384,
			OutputSources outputSource = OutputSources.Sine,
			string windowType = "Hann")
		{
			SampleRate = sampleRate;
			MaxInputLevel = maxInputLevel;
			MaxOutputLevel = maxOutputLevel;
			PreBuffer = preBuffer;
			PostBuffer = postBuffer;
			FFTSize = fftSize;
			WindowType = windowType;
			OutputSource = outputSource;

			var window = GetWindowing(WindowType, FFTSize);
			double meanW = window.Average();
			ACF = 1 / meanW;
			double rmsW = Math.Sqrt(window.Select(w => w * w).Average());
			ECF = 1 / rmsW;
		}

		public AnalyzerParams(AnalyzerParams other)
		{
			SampleRate = other.SampleRate;
			MaxInputLevel = other.MaxInputLevel;
			MaxOutputLevel = other.MaxOutputLevel;
			PreBuffer = other.PreBuffer;
			PostBuffer = other.PostBuffer;
			FFTSize = other.FFTSize;
			OutputSource = other.OutputSource;
			WindowType = other.WindowType;
			ACF = other.ACF;
			ECF = other.ECF;
		}

		public void SetWindowing(string windowType)
		{
			WindowType = windowType;
			var window = GetWindowing(WindowType, FFTSize);
			double meanW = window.Average();
			ACF = 1 / meanW;
			double rmsW = Math.Sqrt(window.Select(w => w * w).Average());
			ECF = 1 / rmsW;
		}

		private double[] GetWindowing(string windowType, int size)
		{
			var wind = QAMath.GetWindowType(windowType);
			double[] wdw = new double[size];
			wdw = wdw.Select(x => 1.0).ToArray();
			return wind.Apply(wdw).ToArray();
		}

		public override string ToString()
		{
			var parameters = new (string Name, string Value)[]
			{
				("Sample Rate", $"{SampleRate} Hz"),
				("Max Input Level", $"{MaxInputLevel} dBV"),
				("Max Output Level", $"{MaxOutputLevel} dBV"),
				("Pre Buffer", $"{PreBuffer}"),
				("Post Buffer", $"{PostBuffer}"),
				("Buffer Size", $"{FFTSize}"),
				("Duration", $"{(double)FFTSize / SampleRate:0.00} sec"),
				("Window Type", $"{WindowType}")
			};

			int colWidthName = parameters.Max(p => p.Name.Length);
			int colWidthValue = parameters.Max(p => p.Value.Length) + 5;

			string table = "===== ACQUISITION PARAMETERS =====\n";
			for (int i = 0; i < parameters.Length; i += 3)
			{
				string row = "";
				for (int j = 0; j < 3; j++)
				{
					if (i + j < parameters.Length)
					{
						var (name, value) = parameters[i + j];
						row += $"{name.PadRight(colWidthName)} : {value.PadRight(colWidthValue)}   ";
					}
				}
				table += row.TrimEnd() + "\n";
			}

			return table;
		}
	}
}