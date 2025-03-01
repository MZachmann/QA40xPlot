
namespace QA40xPlot.Libraries
{
    internal class Generator
    {
        /// <summary>
        /// Generate a sine wave buffer
        /// </summary>
        /// <param name="number_of_samples">Number of samples to generate</param>
        /// <param name="sample_rate">Sample rate</param>
        /// <param name="frequency">Frequency of the sinewave</param>
        /// <param name="amplitude">Amplitude of the sinewave</param>
        /// <param name="phase">Phase of the signwave in degrees</param>
        /// <returns></returns>
        public static double[] GenerateSine(int number_of_samples, int sample_rate, double frequency, double amplitude, double phase)
        {
            double incr_theta = (2.0 * Math.PI * frequency) / sample_rate;
            double phase_rads = phase * (Math.PI / 180);

            double theta = 0.0;
            double[] source_buffer = new double[number_of_samples];
            for (int curr_sample = 0; curr_sample < number_of_samples; curr_sample++)
            {
                source_buffer[curr_sample] = amplitude * Math.Sin(theta + phase_rads);

                theta += incr_theta;
            }
            return source_buffer;
        }
    }
}
