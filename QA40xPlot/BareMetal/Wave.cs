
namespace QA40xPlot.BareMetal
{
	public class Wave
	{
		private AnalyzerParams _params;
		private double[] _buffer;
		private FFTProcessor? _fftPlot;
		private FFTProcessor? _fftEnergy;
		private double[]? _fftPlotSignal;
		private double[]? _fftEnergySignal;

		private string _amplitudeUnit;
		private string _distortionUnit;
		private string _energyUnit;

		public Wave(AnalyzerParams parameters, string amplitudeUnit = "dbv", string distortionUnit = "db", string energyUnit = "dbv")
		{
			_params = parameters;
			_buffer = new double[_params.PreBuffer + _params.FFTSize + _params.PostBuffer];

			_fftPlot = null;
			_fftEnergy = null;
			_fftPlotSignal = null;
			_fftEnergySignal = null;

			_amplitudeUnit = amplitudeUnit.ToLower();
			_distortionUnit = distortionUnit.ToLower();
			_energyUnit = energyUnit.ToLower();
		}

		public void SetBuffer(double[] buffer)
		{
			if (buffer.Length != _buffer.Length)
				throw new ArgumentException("Buffer shape does not match the expected shape");

			_fftPlot = null;
			_fftEnergy = null;
			_fftPlotSignal = null;
			_fftEnergySignal = null;
			_buffer = buffer;
		}

		public double[] GetBuffer()
		{
			return _buffer;
		}

		public double[] GetMainBuffer()
		{
			int startIdx = _params.PreBuffer;
			int endIdx = startIdx + _params.FFTSize;
			return _buffer.Skip(startIdx).Take(endIdx - startIdx).ToArray();
		}

		public void ComputeFFT()
		{
			_fftPlot = new FFTProcessor(_params);
			_fftEnergy = new FFTProcessor(_params);

			_fftPlotSignal = _fftPlot?.FftForward(GetMainBuffer()).ApplyAcf().GetResult();
			_fftEnergySignal = _fftEnergy?.FftForward(GetMainBuffer()).ApplyEcf().GetResult();
		}

		public void ComputeFFTIfNeeded()
		{
			if (_fftPlot == null || _fftEnergy == null)
			{
				ComputeFFT();
			}
		}

		public (double[] dbsplValues, double[] timeValues) ComputeInstantaneousDbSpl(double dbSplAt0Dbv, double rmsSliceIntervalMs)
		{
			var signal = GetMainBuffer();
			double segmentDuration = rmsSliceIntervalMs / 1000.0;
			int segmentSamples = (int)(segmentDuration * _params.SampleRate);

			var dbsplValues = new System.Collections.Generic.List<double>();

			for (int start = 0; start < signal.Length; start += segmentSamples)
			{
				var segment = signal.Skip(start).Take(segmentSamples).ToArray();
				if (segment.Length == 0)
					break;

				double rmsValue = Math.Sqrt(segment.Select(x => x * x).Average());
				double dbSpl = 20 * Math.Log10(rmsValue / 1) + dbSplAt0Dbv;
				dbsplValues.Add(dbSpl);
			}

			double[] timeValues = Enumerable.Range(0, dbsplValues.Count)
				.Select(i => i * segmentDuration)
				.ToArray();

			return (dbsplValues.ToArray(), timeValues);
		}

		public double ConvertToAmplitudeUnits(double value, string unit)
		{
			return unit.ToLower() switch
			{
				"dbv" => 20*Math.Log10(value),
				"dbu" => 20 * Math.Log10(value*2.2),
				"v" => value,
				_ => throw new ArgumentException($"Unknown unit: {unit}")
			};
		}

		public double ConvertToEnergyUnits(double value, string unit)
		{
			return unit.ToLower() switch
			{
				"db" => 20 * Math.Log10(value),
				"pct" => value * 100,
				_ => throw new ArgumentException($"Unknown unit: {unit}")
			};
		}

		public double ComputeThd(double fundamental, int numHarmonics = 5, string? unit = null, bool debug = false)
		{
			ComputeFFTIfNeeded();
			if(_fftPlotSignal == null)
				throw new InvalidOperationException("FFT has not been computed yet.");

			double thdLinear = QaCompute.ComputeThdLinear(_fftPlotSignal, GetFrequencyArray(), fundamental, numHarmonics, debug);

			unit ??= _distortionUnit;
			double thdConverted = ConvertToEnergyUnits(thdLinear, unit);

			if (debug)
				Console.WriteLine($"THD ({unit}): {thdConverted:F2} {unit}");

			return thdConverted;
		}

		public double ComputeThdn(double fundamental, double notchOctaves = 0.5, double startFreq = 20.0, double stopFreq = 20000.0, string? unit = null, bool debug = false)
		{
			ComputeFFTIfNeeded();
			if(_fftPlotSignal == null)
				throw new InvalidOperationException("FFT has not been computed yet.");
			double thdnLinear = QaCompute.ComputeThdnLinear(_fftPlotSignal, GetFrequencyArray(), fundamental, notchOctaves, startFreq, stopFreq, debug);

			unit ??= _distortionUnit;
			double thdnConverted = ConvertToEnergyUnits(thdnLinear, unit);

			if (debug)
				Console.WriteLine($"THDN ({unit}): {thdnConverted:F2} {unit}");

			return thdnConverted;
		}

		public double ComputeRmsFreq(double startFreq, double stopFreq, string? unit = null, bool debug = false)
		{
			ComputeFFTIfNeeded();
			if(_fftEnergySignal == null)
				throw new InvalidOperationException("FFT has not been computed yet.");
			double rmsValue = QaCompute.ComputeRmsFreq(_fftEnergySignal, GetFrequencyArray(), startFreq, stopFreq);

			unit ??= _energyUnit;
			double rmsConverted = ConvertToAmplitudeUnits(rmsValue, unit);

			if (debug)
				Console.WriteLine($"RMS Value ({unit}): {rmsConverted:F2} {unit}");

			return rmsConverted;
		}

		public void RemoveDc()
		{
			double mean = _buffer.Average();
			for (int i = 0; i < _buffer.Length; i++)
			{
				_buffer[i] -= mean;
			}
		}

		public double[] GetFrequencyArray()
		{
			ComputeFFTIfNeeded();
			return _fftPlot!.GetFrequencies();
		}

		public double[] GetAmplitudeArray(string? amplitudeUnit = null)
		{
			ComputeFFTIfNeeded();
			if(_fftPlotSignal == null)
				throw new InvalidOperationException("FFT has not been computed yet.");	

			amplitudeUnit ??= _amplitudeUnit;

			return amplitudeUnit.ToLower() switch
			{
				"dbv" => Helpers.LinearArrayToDbV(_fftPlotSignal),
				"dbu" => Helpers.LinearArrayToDbU(_fftPlotSignal),
				_ => throw new ArgumentException($"Unknown amplitude units: {amplitudeUnit}")
			};
		}
	}
}
