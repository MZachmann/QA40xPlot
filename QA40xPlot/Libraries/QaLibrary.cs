using System.Net.Sockets;
using ScottPlot.Plottables;
using ScottPlot;
using QA40xPlot.Data;
using QA40xPlot.Views;
using System.Windows;
using System.Runtime.InteropServices;

namespace QA40xPlot.Libraries
{

    public class QaLibrary
    {
        public static ScottPlot.Color BlueColor { get => new ScottPlot.Color(1, 97, 170, 255); }
        public static ScottPlot.Color RedColor { get => new ScottPlot.Color(220, 5, 46, 255); }
        public static ScottPlot.Color RedXColor { get => new ScottPlot.Color(220, 5, 46, 150); }
		public static ScottPlot.Color GreenXColor { get => new ScottPlot.Color(40, 150, 0, 150); }
		public static ScottPlot.Color OrangeXColor { get => new ScottPlot.Color(110, 0, 95, 150); }

		public static double MINIMUM_GENERATOR_VOLTAGE_V = 0.001;
        public static double MAXIMUM_GENERATOR_VOLTAGE_V = 7.9;
        public static double MINIMUM_GENERATOR_VOLTAGE_MV = 1;
        public static double MAXIMUM_GENERATOR_VOLTAGE_MV = 7900;
        public static double MINIMUM_GENERATOR_VOLTAGE_DBV = -165;
        public static double MAXIMUM_GENERATOR_VOLTAGE_DBV = 18;

        public static double MINIMUM_GENERATOR_FREQUENCY_HZ = 1;
        public static double MAXIMUM_GENERATOR_FREQUENCY_HZ = 96000;

        public static double MINIMUM_LOAD = 0;
        public static double MAXIMUM_LOAD = 100000;

        public static double MINIMUM_DEVICE_INPUT_VOLTAGE_DBV = -120;
        public static double MAXIMUM_DEVICE_INPUT_VOLTAGE_DBV = 32;
        public static double MINIMUM_DEVICE_INPUT_VOLTAGE_V = 1E-6;
        public static double MAXIMUM_DEVICE_INPUT_VOLTAGE_V = 40;
        public static double MINIMUM_DEVICE_INPUT_VOLTAGE_MV = 1E-3;
        public static double MAXIMUM_DEVICE_INPUT_VOLTAGE_MV = 40000;

        public static int DEVICE_MIN_ATTENUATION = 0;
        public static int DEVICE_MAX_ATTENUATION = 42;

        /// <summary>
        /// Do the startup of the QA40x, checking the rest interface for existance
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="fftsize"></param>
        /// <param name="Windowing"></param>
        /// <param name="attenuation"></param>
        /// <param name="setdefault">this may take a little time, so do it once?</param>
        /// <returns>success true or false</returns>
        public static async Task<bool> InitializeDevice(uint sampleRate, uint fftsize, string Windowing, int attenuation, bool setdefault = false)
        {
            try
            {
                // ********************************************************************  
                // Load a settings we want
                // ********************************************************************  
                if (setdefault)
                {
                    // Check if REST interface is available and device connected
                    if (await QaLibrary.CheckDeviceConnected() == false)
                        return false;

                    await Qa40x.SetDefaults();
                    await Qa40x.SetOutputSource(OutputSources.Off);
                }
                await Qa40x.SetSampleRate(sampleRate);
                await Qa40x.SetBufferSize(fftsize);
                await Qa40x.SetWindowing(Windowing);
                await Qa40x.SetRoundFrequencies(true);
                await Qa40x.SetInputRange(attenuation);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// Calculates fft bin size in Hz
        /// </summary>
        /// <param name="sampleRate">Sample rate in samples per second</param>
        /// <param name="fftSize">fft buffer size</param>
        /// <returns>The frequency span of a bin</returns>
        static public double CalcBinSize(uint sampleRate, uint fftSize)
        {
            return (double)sampleRate / (double)fftSize;
        }


        /// <summary>
        /// Calculates in which bin the supplied frequency is
        /// </summary>
        /// <param name="frequency">The frequency to query</param>
        /// <param name="sampleRate">Sample rate in samples per second</param>
        /// <param name="fftSize">The fft buffer size</param>
        /// <returns>The bin containing the frequncy</returns>
        static public uint GetBinOfFrequency(double frequency, uint sampleRate, uint fftSize)
        {
            double binSize = CalcBinSize(sampleRate, fftSize);
            uint binNumber = (uint)Math.Round(frequency / binSize);
            if (binNumber > fftSize)
                throw new ArgumentOutOfRangeException();                    // Frequency does not exist in the fft
            return binNumber;
        }

        /// <summary>
        /// Calculates in which bin the supplied frequency is
        /// </summary>
        /// <param name="frequency">The frequency to query</param>
        /// <param name="binSize">Frequency span of a single bin</param>
        /// <returns>The bin containing the frequncy</returns>
        static public uint GetBinOfFrequency(double frequency, double binSize)
        {
            return (uint)Math.Round(frequency / binSize);
        }

        /// <summary>
        /// Get the actual generator frequency when 'round to eliminate leakage' is enabled
        /// </summary>
        /// <param name="bin">The fft bin of the frequency</param>
        /// <param name="binSize">The fft binsize</param>
        /// <returns></returns>
        static public double GetBinFrequency(uint bin, double binSize)
        {
            return bin * binSize;
        }

        /// <summary>
        /// Get the actual generator frequency when 'round to eliminate leakage' is enabled
        /// </summary>
        /// <param name="setFrequency">The frequency to query</param>
        /// <param name="sampleRate">Sample rate in samples per second</param>
        /// <param name="fftSize">The fft buffer size</param>
        /// <returns>The center frequency of the nearest bin</returns>
        static public double GetNearestBinFrequency(double setFrequency, uint sampleRate, uint fftSize)
        {
            uint binOfFreq = GetBinOfFrequency(setFrequency, sampleRate, fftSize);
            double binSize = CalcBinSize(sampleRate, fftSize);

            return binOfFreq * binSize;
        }

        /// <summary>
        /// Get a list of x frequencies per octave between a start and stop frequency in a way that they are spaced equally on a graph with logarithmic scale.
        /// </summary>
        /// <param name="startFrequency">Start frequency</param>
        /// <param name="stopFrequency">Stop frequency</param>
        /// <param name="stepsPerOctave">Steps per octave</param>
        /// <returns></returns>
        public static double[] GetLinearSpacedLogarithmicValuesPerOctave(double start, double stop, uint stepsPerOctave)
        {
            // Calculate the number of octaves between start and stop frequencies
            double octaves = Math.Max(1, Math.Log(stop / start, 2));

            // Calculate the total number of steps based on steps per octave
            int totalSteps = Math.Max(2, (int)(stepsPerOctave * octaves));

            // Calculate the increment in logarithmic space (base 2)
            double logStep = octaves / totalSteps;

            // Generate the frequencies array
            double[] values = new double[totalSteps + 1];
            for (int i = 0; i <= totalSteps; i++)
            {
                // Calculate the frequency by raising 2 to the power of the current log position
                values[i] = start * Math.Pow(2, i * logStep);
                if (values[i] > stop)
                    values[i] = stop;
            }

            return values;
        }

        /// <summary>
        /// Changes all frequencies in an array to center frequencies of a bin.
        /// </summary>
        /// <param name="frequencies">List of frequencies to translate</param>
        /// <param name="sampleRate">Sample rate</param>
        /// <param name="fftSize">fft size</param>
        /// <returns>A list of frequencies</returns>
        public static double[] TranslateToBinFrequencies(double[] frequencies, uint sampleRate, uint fftSize)
        {
            double[] binnedFrequencies = new double[frequencies.Length];
            for (int i = 0; i < frequencies.Length; i++)
            {
                binnedFrequencies[i] = GetNearestBinFrequency(frequencies[i], sampleRate, fftSize);
            }

            // Remove duplicates (occurs when many steps at low frequencies). Remove frequencies lower than 1 and higher than 95500
            binnedFrequencies = binnedFrequencies.Where(x => x >= 1 && x <= 95500).GroupBy(x => x).Select(y => y.First()).ToArray();

            return binnedFrequencies;
        }

        static public async Task<LeftRightSeries> DoAcquisitions(uint averages, CancellationToken ct, bool getFrequencySeries = true, bool getTimeSeries = true)
        {
            LeftRightSeries lrfs = new LeftRightSeries();

            await Qa40x.DoAcquisition();
            if (ct.IsCancellationRequested || lrfs == null)
                return lrfs ?? new();
            if (getFrequencySeries)
            {
                lrfs.FreqRslt = await Qa40x.GetInputFrequencySeries();
                if (ct.IsCancellationRequested || lrfs.FreqRslt == null)
                    return lrfs;
            }
            if (getTimeSeries)
            {
                lrfs.TimeRslt = await Qa40x.GetInputTimeSeries();
                if (ct.IsCancellationRequested)
                    return lrfs;
            }

            if (averages <= 1)
                return lrfs;        // Only one measurement

            if (getFrequencySeries && lrfs.FreqRslt != null)
            {
                for (int i = 1; i < averages; i++)
                {
                    await Qa40x.DoAcquisition();
                    if (ct.IsCancellationRequested)
                        return lrfs;
                    LeftRightSeries lrfs2 = new LeftRightSeries();
                    lrfs2.FreqRslt = await Qa40x.GetInputFrequencySeries();
                    for (int j = 0; j < lrfs.FreqRslt.Left.Length; j++)
                    {
                        lrfs.FreqRslt.Left[j] += lrfs2.FreqRslt.Left[j];
                        lrfs.FreqRslt.Right[j] += lrfs2.FreqRslt.Right[j];
                    }
                }

                for (int j = 0; j < lrfs.FreqRslt.Left.Length; j++)
                {
                    lrfs.FreqRslt.Left[j] = lrfs.FreqRslt.Left[j] / averages;
                    lrfs.FreqRslt.Right[j] = lrfs.FreqRslt.Right[j] / averages;
                }
            }

            return lrfs;
        }

        static public async Task<LeftRightSeries> DoAcquireUser(CancellationToken ct, double[] datapt, bool getFreq)
        {
            LeftRightSeries lrfs = new LeftRightSeries();
            await Qa40x.DoUserAcquisition(datapt, datapt);
            if (ct.IsCancellationRequested || lrfs == null)
                return lrfs ?? new();

            if (getFreq)
            {
                lrfs.FreqRslt = await Qa40x.GetInputFrequencySeries();
                if (ct.IsCancellationRequested || lrfs.FreqRslt == null)
                    return lrfs;
            }

            {
                lrfs.TimeRslt = await Qa40x.GetInputTimeSeries();
                if (ct.IsCancellationRequested)
                    return lrfs;
            }

            return lrfs;        // Only one measurement
        }

        /// <summary>
        /// Determine the attenuation needed for the input signal to be in the range of the hardware
        /// </summary>
        /// <param name="dBV">The maximum level in dBV</param>
        /// <returns>The attenuation in dB</returns>
        public static int DetermineAttenuation(double dBV)
        {
            double testdBV = dBV + 5; // Add 5 dBV extra for better thd measurement
            if (testdBV <= 0) return 0;
            if (testdBV <= 6) return 6;
            if (testdBV <= 12) return 12;
            if (testdBV <= 18) return 18;
            if (testdBV <= 24) return 24;
            if (testdBV <= 30) return 30;
            if (testdBV <= 36) return 36;
            return 42;
        }

        /// <summary>
        /// This method checks if the server is running by attempting to connect to it on localhost at port 9402.
        /// </summary>
        /// <returns></returns>
        public static bool IsServerRunning()
        {
            using (Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var result = socket.BeginConnect("localhost", 9402, null, null);

                // test the connection for 3 seconds
                bool success = result.AsyncWaitHandle.WaitOne(1000, false);

                var resturnVal = socket.Connected;
                if (socket.Connected)
                    socket.Disconnect(true);

                return resturnVal;
            }
        }

        static public async Task<bool> CheckDeviceConnected()
        {
            // Check if webserver available
            if (!IsServerRunning())
            {
                MessageBox.Show($"QA40X application is not running.\nPlease start the application first.", "Could not reach webserver", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // Check if device connected
            if (!await Qa40x.IsConnected())
            {
                MessageBox.Show($"QA40X analyser is not connected via USB.\nPlease connect the device first.", "QA40X not connected", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            bool busy = await Qa40x.IsBusy();
            if (busy)
            {
                MessageBox.Show($"The QA40x seems to be already runnng. Stop the aqcuisition and generator in the QuantAsylum software manually.", "QA40X busy", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        public static double ConvertVoltage(double voltage, E_VoltageUnit fromUnit, E_VoltageUnit toUnit)
        {
            if (fromUnit == E_VoltageUnit.MilliVolt)
            {
                if (toUnit == E_VoltageUnit.MilliVolt)
                    return voltage;
                else if (toUnit == E_VoltageUnit.Volt)
                    return voltage / 1000.0;
                else
                    return 20 * Math.Log10(voltage / 1000.0);
            }
            else if (fromUnit == E_VoltageUnit.Volt)
            {
                if (toUnit == E_VoltageUnit.MilliVolt)
                    return voltage * 1000;
                else if (toUnit == E_VoltageUnit.Volt)
                    return voltage;
                else
                    return 20 * Math.Log10(voltage);
            }
            else
            {   // dBV
                if (toUnit == E_VoltageUnit.MilliVolt)
                    return Math.Pow(10, voltage / 20) * 1000;
                else if (toUnit == E_VoltageUnit.Volt)
                    return Math.Pow(10, voltage / 20);
                else
                    return voltage;
            }

        }

        /// <summary>
        /// Plots a vertical cursor marker line 
        /// </summary>
        /// <param name="lineWidth">Line width of the marker</param>
        /// <param name="linePattern">Pattern of the line</param>
        /// <param name="point">Data point to draw the line at</param>
        public static void PlotCursorLine(PlotControl plot, float lineWidth, LinePattern linePattern, DataPoint point)
        {
            ScottPlot.Plot myPlot = plot.ThePlot;

            myPlot.Remove<Crosshair>();               // Remove any current marker

            var myCrosshair = myPlot.Add.Crosshair(point.X, point.Y);
            myCrosshair.IsVisible = true;
            myCrosshair.LineWidth = lineWidth;
            myCrosshair.LineColor = Colors.Magenta;
            myCrosshair.MarkerShape = MarkerShape.None;
            myCrosshair.MarkerSize = 1;
            myCrosshair.LinePattern = linePattern;
            myCrosshair.HorizontalLine.IsVisible = false;
            myCrosshair.Position = point.Coordinates;

            plot.Refresh();
        }


        /// <summary>
        /// Initlialize the THD frequency plot
        /// </summary>
        /// <param name="startFrequency"></param>
        /// <param name="endFrequency"></param>
        /// <param name="minDbV"></param>
        /// <param name="maxDbV"></param>
        public static void InitMiniFftPlot(PlotControl plot, double startFrequency, double endFrequency, double minDbV, double maxDbV)
        {
            ScottPlot.Plot myPlot = plot.ThePlot;
            myPlot.Clear();

            // create a minor tick generator that places log-distributed minor ticks
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new()
            {
                Divisions = 1
            };

            // create a numeric tick generator that uses our custom minor tick generator
            ScottPlot.TickGenerators.NumericAutomatic tickGen = new()
            {
                MinorTickGenerator = minorTickGen
            };


            // tell the left axis to use our custom tick generator
            myPlot.Axes.Left.TickGenerator = tickGen;


            // create a minor tick generator that places log-distributed minor ticks
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGenX = new();

            // create a manual tick generator and add ticks
            ScottPlot.TickGenerators.NumericManual tickGenX = new();

            // add major ticks with their labels
            tickGenX.AddMajor(Math.Log10(1), "1");
            tickGenX.AddMajor(Math.Log10(10), "10");
            tickGenX.AddMajor(Math.Log10(100), "100");
            tickGenX.AddMajor(Math.Log10(1000), "1k");
            tickGenX.AddMajor(Math.Log10(10000), "10k");
            tickGenX.AddMajor(Math.Log10(100000), "100k");

            myPlot.Axes.Bottom.TickGenerator = tickGenX;


            // show grid lines for minor ticks
            myPlot.Grid.MajorLineColor = Colors.Black.WithOpacity(.25);
            myPlot.Grid.MinorLineColor = Colors.Black.WithOpacity(.08);
            myPlot.Grid.MinorLineWidth = 1;

            myPlot.Axes.SetLimits(startFrequency < 10 ? Math.Log10(1) : Math.Log10(10), Math.Log10(100000), minDbV, maxDbV);
            myPlot.Title("dBV (output)");
            myPlot.Axes.Title.Label.FontSize = 12;
            myPlot.Axes.Title.Label.OffsetY = 8;
            myPlot.Axes.Title.Label.Bold = false;

            myPlot.XLabel("Hz");
            myPlot.Axes.Bottom.Label.OffsetX = 85;
            myPlot.Axes.Bottom.Label.OffsetY = -5;
            myPlot.Axes.Bottom.Label.FontSize = 12;
            myPlot.Axes.Bottom.Label.Bold = false;
            myPlot.Axes.Bottom.Label.IsVisible = true;

            myPlot.Legend.IsVisible = false;

            PixelPadding padding = new(40, 20, 50, 20);
            myPlot.Layout.Fixed(padding);

            plot.Refresh();
        }


        public static void PlotMiniFftGraph(PlotControl plot, LeftRightFrequencySeries? fftData, bool leftEnabled, bool rightEnabled)
        {
            if (null == fftData)
                return;

            ScottPlot.Plot myPlot = plot.ThePlot;
            myPlot.Clear();

            List<double> freqX = [];
            List<double> dBV_Left_Y = [];
            List<double> dBV_Right_Y = [];
            double frequency = 0;

            for (int f = 1; f < fftData.Left.Length; f++)   // Skip dc bin
            {
                frequency += fftData.Df;
                freqX.Add(frequency);
                if (leftEnabled)
                    dBV_Left_Y.Add(20 * Math.Log10(fftData.Left[f]));
                if (rightEnabled)
                    dBV_Right_Y.Add(20 * Math.Log10(fftData.Right[f]));
            }

            // add a scatter plot to the plot
            double[] logFreqX = freqX.Select(Math.Log10).ToArray();
            double[] logHTot_Left_Y = dBV_Left_Y.ToArray();
            double[] logHTot_Right_Y = dBV_Right_Y.ToArray();

            if (leftEnabled)
            {
                Scatter plotTot_Left = myPlot.Add.Scatter(logFreqX, logHTot_Left_Y);
                plotTot_Left.LineWidth = 1;
                plotTot_Left.Color = BlueColor;  // Blue
                plotTot_Left.MarkerSize = 1;
            }

            if (rightEnabled)
            {
                Scatter plotTot_Right = myPlot.Add.Scatter(logFreqX, logHTot_Right_Y);
                plotTot_Right.LineWidth = 1;
                if (leftEnabled)
                    plotTot_Right.Color = RedXColor; // Red transparant
                else
                    plotTot_Right.Color = RedColor; // Red
                plotTot_Right.MarkerSize = 1;
            }

            var limitY = myPlot.Axes.GetLimits().YRange.Max;
            var max_dBV_left = leftEnabled ? dBV_Left_Y.Max(f => f) : -150;
            var max_dBV_right = rightEnabled ? dBV_Right_Y.Max(f => f) : -150;
            var max_dBV = (max_dBV_left > max_dBV_right) ? max_dBV_left : max_dBV_right;
            if (max_dBV + 10 > limitY)
            {
                limitY += 10;
                myPlot.Axes.SetLimits(Math.Log10(10), Math.Log10(100000), -180, limitY);
            }

            plot.Refresh();
        }



        public static void InitMiniTimePlot(PlotControl plot, double startTime, double endTime, double minVoltage, double maxVoltage)
        {
            ScottPlot.Plot myPlot = plot.ThePlot;
            myPlot.Clear();

            ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGenX = new(2);
            ScottPlot.TickGenerators.NumericAutomatic tickGenX = new();
            tickGenX.TargetTickCount = 4;
            tickGenX.MinorTickGenerator = minorTickGenX;
            myPlot.Axes.Bottom.TickGenerator = tickGenX;

            ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGenY = new(2);
            ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
            tickGenY.TargetTickCount = 4;
            tickGenY.MinorTickGenerator = minorTickGenY;
            myPlot.Axes.Left.TickGenerator = tickGenY;



            // show grid lines for minor ticks
            myPlot.Grid.MajorLineColor = Colors.Black.WithOpacity(.25);
            myPlot.Grid.MinorLineColor = Colors.Black.WithOpacity(.08);
            myPlot.Grid.MinorLineWidth = 1;


            //thdmyPlot.Axes.AutoScale();
            myPlot.Axes.SetLimits(startTime, endTime, minVoltage, maxVoltage);
            myPlot.Title("V (output)");
            myPlot.Axes.Title.Label.FontSize = 12;
            myPlot.Axes.Title.Label.OffsetY = 8;
            myPlot.Axes.Title.Label.Bold = false;

            myPlot.XLabel("ms");
            myPlot.Axes.Bottom.Label.OffsetX = 90;
            myPlot.Axes.Bottom.Label.OffsetY = -5;
            myPlot.Axes.Bottom.Label.FontSize = 12;
            myPlot.Axes.Bottom.Label.Bold = false;


            myPlot.Legend.IsVisible = false;

            PixelPadding padding = new(40, 20, 50, 20);
            myPlot.Layout.Fixed(padding);

            plot.Refresh();

        }


        public static void PlotMiniTimeGraph(PlotControl plot, LeftRightTimeSeries? timeData, double fundamantalFrequency, bool leftEnabled, bool rightEnabled, bool plotChirp = false)
        {
            if (null == timeData)
            {
                return;
            }

            ScottPlot.Plot myPlot = plot.ThePlot;
            myPlot.Clear();

            List<double> timeX = [];
            List<double> voltY_left = [];
            List<double> voltY_right = [];
            double time = 0;
            double displaySteps = 0;

            double period = 1 / fundamantalFrequency;
            double displayTime = period * 1;
            if (period < 0.00005)
                displayTime = period * 4;
            else if (period < 0.0001)
                displayTime = period * 2;
            else if (period < 0.0002)
                displayTime = period * 1.5;

            // Get first zero-crossing
            int startStep = 0;
            if (!plotChirp)
            {
                if (leftEnabled)
                {
                    for (int f = 1; f < timeData.Left.Length; f++)
                    {
                        if (timeData.Left[f - 1] < 0 && timeData.Left[f] >= 0)
                        {
                            startStep = f;
                            break;
                        }
                    }
                }
                else
                {
                    for (int f = 1; f < timeData.Right.Length; f++)
                    {
                        if (timeData.Right[f - 1] < 0 && timeData.Right[f] >= 0)
                        {
                            startStep = f;
                            break;
                        }
                    }
                }

                // Determine start index of array at zero-crossing
                displaySteps = (displayTime / timeData.dt);
                if (displaySteps > timeData.Left.Length)
                    displaySteps = timeData.Left.Length;
            }
            else
            {
                // Plot Chirp. Plot half the data.
                startStep = 0;
                displaySteps = timeData.Left.Length / 2;
            }


            double maxVolt = 0;
            for (int f = startStep; f < startStep + displaySteps && f < timeData.Left.Length; f++)
            {
                timeX.Add(time);
                voltY_left.Add(timeData.Left[f]);
                voltY_right.Add(timeData.Right[f]);
                if (maxVolt < Math.Abs(timeData.Left[f]))
                    maxVolt = Math.Abs(timeData.Left[f]);
                if (maxVolt < Math.Abs(timeData.Right[f]))
                    maxVolt = Math.Abs(timeData.Right[f]);
                time += period * 1000;
            }

            maxVolt *= 1.1;
            if (maxVolt > 1)
                maxVolt = Math.Ceiling(maxVolt);
            else if (maxVolt > 0.1)
                maxVolt = Math.Ceiling(maxVolt * 10) / 10;
            else if (maxVolt > 0.01)
                maxVolt = Math.Ceiling(maxVolt * 100) / 100;
            else if (maxVolt > 0.001)
                maxVolt = Math.Ceiling(maxVolt * 1000) / 1000;
            else if (maxVolt > 0.0001)
                maxVolt = Math.Ceiling(maxVolt * 10000) / 10000;
            else if (maxVolt > 0.00001)
                maxVolt = Math.Ceiling(maxVolt * 100000) / 100000;

            if (leftEnabled)
            {
                Scatter plot_left = myPlot.Add.Scatter(timeX, voltY_left);
                plot_left.LineWidth = 1;
                plot_left.Color = BlueColor;  // Blue
                plot_left.MarkerSize = 2;
            }

            if (rightEnabled)
            {
                Scatter plot_right = myPlot.Add.Scatter(timeX, voltY_right);
                plot_right.LineWidth = 1;
                if (leftEnabled)
                    plot_right.Color = RedXColor; // Red transparant if left channel behind it
                else
                    plot_right.Color = RedColor; // Red
                plot_right.MarkerSize = 2;
            }

            myPlot.Axes.SetLimits(0, time, -maxVolt, maxVolt);

            plot.Refresh();
        }
    }
}           
    