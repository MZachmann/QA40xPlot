using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Web;
using QA40xPlot.Data;

// Note! Install System.Text.Json via NuGet if you are taking Qa402.cs into another project!

namespace QA40xPlot.Libraries
{
    public enum OutputSources { Off, User, Sine, Multitone, WhiteNoise, ExpoChirp, Invalid }

    public enum Windowing { Rectangle, Bartlett, Hamming, Hann, FlatTop }

    public enum Weighting { None, AWeighting, User }

    public static class ConvertUtil
    {
		// convert a double to an 8 byte string value
		public static ulong CvtFromDouble(double d)
		{
			ulong bits = BitConverter.DoubleToUInt64Bits(d);
			return bits; // Convert.ToString(bits);
		}

		/// <summary>
		/// convert an 8 byte string value to a double. This is the reverse of CvtFromDouble
		/// </summary>
		/// <param name="sd"></param>
		/// <returns></returns>
		public static double CvtToDouble(ulong sd)
		{
			//UInt64 bits = UInt64.Parse(sd);
			double db = BitConverter.UInt64BitsToDouble(sd);
			return db;
		}

		/// <summary>
		/// convert a double array to a base64 string. This is used to save the Left and Right arrays
		/// </summary>
		/// <param name="arr"></param>
		/// <returns></returns>
		public static string CvtFromArray(double[] arr)
		{
			if (arr == null || arr.Length == 0)
				return string.Empty;
			byte[] byteArray = new byte[arr.Length * sizeof(double)];
			Buffer.BlockCopy(arr, 0, byteArray, 0, byteArray.Length);
			return Convert.ToBase64String(byteArray, Base64FormattingOptions.None);
		}

		/// <summary>
		/// convert a base64 string to a double array. This is used to restore the Left and Right arrays
		/// </summary>
		/// <param name="bda">the base64 representation of the double array</param>
		/// <returns></returns>
		public static double[] CvtToArray(string bda)
		{
			if (string.IsNullOrEmpty(bda))
				return new double[0];
			byte[] byteArray = Convert.FromBase64String(bda);
			double[] doubleArray = new double[byteArray.Length / sizeof(double)];
			Buffer.BlockCopy(byteArray, 0, doubleArray, 0, byteArray.Length);
			return doubleArray;
		}
	}

	public class LeftRightPair
    {
        public double Left { get; set; }
        public double Right { get; set; }
        public LeftRightPair(double left, double right)
		{
			Left = left;
			Right = right;
		}
        public LeftRightPair() { }
        public void Divby(double dX)
        {
            Left /= dX;
            Right /= dX;
		}
	}

    public class LeftRightFreqSaver
    {
		/// <summary>
		/// dt is the time between samples. 1/dt is the sample rate
		/// </summary>
		public ulong Df { get; set; }// = string.Empty;
		public string Left { get; set; } = string.Empty;
		public string Right { get; set; } = string.Empty;

		// to avoid warnings. Note this never doesn't get set during real 'new'
		public LeftRightFreqSaver()
		{
		}

		public void FromSeries(LeftRightFrequencySeries lrft)
		{
			Df = ConvertUtil.CvtFromDouble(lrft.Df);
			Left = ConvertUtil.CvtFromArray(lrft.Left); // lrft.Left.Select(CvtFromDouble).ToArray();
			Right = ConvertUtil.CvtFromArray(lrft.Right); // lrft.Right.Select(CvtFromDouble).ToArray();
		}

		public LeftRightFrequencySeries ToSeries()
		{
			LeftRightFrequencySeries lrft = new LeftRightFrequencySeries();
			lrft.Df = ConvertUtil.CvtToDouble(Df);
			lrft.Left = ConvertUtil.CvtToArray(Left); // Left.Select(CvtToDouble).ToArray();
			lrft.Right = ConvertUtil.CvtToArray(Right); // Right.Select(CvtToDouble).ToArray();
			return lrft;
		}

	}

	public class  LeftRightTimeSaver
	{
        /// <summary>
        /// dt is the time between samples. 1/dt is the sample rate
        /// </summary>
        public ulong dt { get; set; }// = string.Empty;
        public string Left { get; set; } = string.Empty;
        public string Right { get; set; } = string.Empty;

		// to avoid warnings. Note this never doesn't get set during real 'new'
		public LeftRightTimeSaver()
		{
		}

		public void FromSeries(LeftRightTimeSeries lrft)
        {
            dt = ConvertUtil.CvtFromDouble(lrft.dt);
            Left = ConvertUtil.CvtFromArray(lrft.Left); // lrft.Left.Select(CvtFromDouble).ToArray();
            Right = ConvertUtil.CvtFromArray(lrft.Right); // lrft.Right.Select(CvtFromDouble).ToArray();
		}

		public LeftRightTimeSeries ToSeries()
		{
            LeftRightTimeSeries lrft = new LeftRightTimeSeries();
			lrft.dt = ConvertUtil.CvtToDouble(dt);
			lrft.Left = ConvertUtil.CvtToArray(Left); // Left.Select(CvtToDouble).ToArray();
			lrft.Right = ConvertUtil.CvtToArray(Right); // Right.Select(CvtToDouble).ToArray();
			return lrft;
		}
	}

	public class LeftRightTimeSeries
    {
        /// <summary>
        /// dt is the time between samples. 1/dt is the sample rate
        /// </summary>
        public double dt { get; set; }
        public double[] Left { get; set; }
        public double[] Right { get; set; }

        // to avoid warnings. Note this never doesn't get set during real 'new'
        public LeftRightTimeSeries()
        {
            dt = 0.0;
            Left = new double[0];
            Right = new double[0];
        }
    }

    public class LeftRightFrequencySeries
    {
        /// <summary>
        /// df is the frequency spacing of FFT bins
        /// </summary>
        public double Df { get; set; }
        public double[] Left { get; set; }
        public double[] Right { get; set; }

        // this is only invoked with real data during an acquisition, so ignore the init
        public LeftRightFrequencySeries()
        {
            Df = 1.0;
            Left = new double[0];
            Right = new double[0];
        }
    }

    public class LeftRightSeries
    {
  
        public LeftRightFrequencySeries? FreqRslt;
        public LeftRightTimeSeries? TimeRslt;
    }

    public class Qa40x
    {
        static HttpClient Client = new HttpClient();
        static string RootUrl;

        static Qa40x()
        {
			RootUrl = "http://localhost:9402";
			Client = new HttpClient
			{
				BaseAddress = new Uri(RootUrl)
			};
		}

        static public async Task<double> GetVersion()
        {
            string s = await Get("/Status/Version", "Value");
            return MathUtil.ToDouble(s);
        }

        static public async Task<bool> IsConnected()
        {
            string s = await Get("/Status/Connection", "Value");
            return Convert.ToBoolean(s);
        }

        static public async Task SetDefaults(string fileName = "")
        {
            if (fileName == "")
            {
                await Put("/Settings/Default");
            }
            else
            {
                await Put(string.Format("/Settings/LoadFromFile/{0}", HttpUtility.UrlEncode(fileName)));
            }
        }

        static public async Task SetBufferSize(uint bufferSizePowerOfTwo)
        {
            await Put(string.Format("/Settings/BufferSize/{0}", bufferSizePowerOfTwo));
        }

        static public async Task SetGraphXAxis(uint min, uint max)
        {
            await Put(string.Format("/Graph/XAxis/{0}/{1}", min, max));
        }

        static public async Task SetGraphYAxis(uint min, uint max)
        {
            await Put(string.Format("/Graph/YAxis/{0}/{1}", min, max));
        }

        static public async Task SetSampleRate(uint sampleRate)
        {
            await Put(string.Format("/Settings/SampleRate/{0}", sampleRate));
        }

        static public async Task SetIdleGen(bool enable)
        {
            await Put(string.Format("/Settings/IdleGen/{0}", enable ? "On" : "Off"));
        }

        static public async Task SetI2sGen(bool enable)
        {
            await Put(string.Format("/Settings/I2sGen/{0}", enable ? "On" : "Off"));
        }

        static public async Task SetI2sGenWidth(int width)
        {
            await Put(string.Format("/Settings/I2sGen/Width/{0}", width));
        }

        static public async Task SetWindowing(string w)
        {
            await Put(string.Format("/Settings/Windowing/{0}", w.ToString()));
        }

        static public async Task SetRoundFrequencies(bool enable)
        {
            await Put(string.Format("/Settings/RoundFrequencies/{0}", enable ? "On" : "Off"));
        }

        static public async Task SetWeighting(Weighting w)
        {
            await Put(string.Format("/Settings/Weighting/{0}", w.ToString()));
        }

        static public async Task SetOutputSource(OutputSources source)
        {
            await Put(string.Format("/Settings/OutputSource/{0}", source.ToString()));
        }

        static public async Task SetInputRange(int maxInputDbv, bool roundToNearest = false)
        {
            if (roundToNearest)
            {
                maxInputDbv = (int)Math.Round(maxInputDbv / 6f) * 6 + 6;

                if (maxInputDbv > 42)
                    maxInputDbv = 42;
                    
                if (maxInputDbv < 0)
                    maxInputDbv = 0;
            }

            await Put(string.Format("/Settings/Input/Max/{0}", maxInputDbv));
        }

        static public async Task SetGen1(double freqHz, double ampV, bool enabled)
        {
            var genv = QaLibrary.ConvertVoltage(ampV, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
            await Put(string.Format("/Settings/AudioGen/Gen1/{0}/{1}/{2}", enabled ? "On" : "Off", freqHz.ToString(), genv.ToString()));
        }

		static public async Task SetGen2(double freqHz, double ampV, bool enabled)
		{
			var genv = QaLibrary.ConvertVoltage(ampV, E_VoltageUnit.Volt, E_VoltageUnit.dBV);
			await Put(string.Format("/Settings/AudioGen/Gen2/{0}/{1}/{2}", enabled ? "On" : "Off", freqHz.ToString(), genv.ToString()));
		}


		static public async Task SetNoiseGen(double ampDbv)
        {
            await Put(string.Format("/Settings/NoiseGen/{0}", ampDbv.ToString()));
        }

        static public async Task SetExpoChirpGen(double ampDbv, double windowSec, int smoothDenominator, bool rightAsReference)
        {
            await Put(string.Format("/Settings/ExpoChirpGen/{0}/{1}/{2}/{3}",
                ampDbv.ToString("0.0"),
                windowSec.ToString("0.000"),
                smoothDenominator.ToString(),
                rightAsReference ? "True" : "False"
                //displayAsGain ? "True" : "False"
                )); ;
        }

        /// <summary>
        /// Performs an acquisition with user-submitted data being used as the stimulus. 
        /// Function will await until acqusition is completed. 
        /// /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        static public async Task DoUserAcquisition(double[] left, double[] right)
        {
            string l = Convert.ToBase64String(GetBytes(left), Base64FormattingOptions.None);
            string r = Convert.ToBase64String(GetBytes(right), Base64FormattingOptions.None);

            string s = $"{{ \"Left\":\"{l}\", \"Right\":\"{r}\" }}";

            await Post("/Acquisition", s);
        }

        /// <summary>
        /// Performs an acquisition. Function will await until the acquisition is completed
        /// </summary>
        /// <returns></returns>
        static public async Task DoAcquisition()
        {
            await Post("/Acquisition");
        }

        /// <summary>
        /// Performs an acquisition and returns immediately. IsBusy() must be called to 
        /// determine when acquisition is finished.
        /// </summary>
        /// <returns></returns>
        static public void DoAcquisitionAsync()
        {
            _ = Task.Factory.StartNew(() =>
            {
                _ = Post("/AcquisitionAsync");
            });
        }

        static public async Task<bool> IsBusy()
        {
            string s = await Get("/AcquisitionBusy", "Value");
            return Convert.ToBoolean(s);
        }

        static public async Task<bool> AuditionStart(string fileName, int dacMaxOutput, double volume, bool repeat)
        {
            string s = $"/AuditionStart/{fileName}/{dacMaxOutput.ToString()}/{volume.ToString()}/" + (repeat ? "True" : "False");
            await Post(s);
            return true;
        }

        static public async Task<bool> AuditionStop()
        {
            await Post($"/AuditionStop");
            return true;
        }

        static public async Task<LeftRightPair> GetThdDb(double fundFreq, double maxFreq)
        {
            Dictionary<string, string> d = await Get(string.Format("/ThdDb/{0}/{1}", fundFreq, maxFreq));

            LeftRightPair lrp = new LeftRightPair() { Left = MathUtil.ToDouble(d["Left"]), Right = MathUtil.ToDouble(d["Right"]) };
            return lrp;
        }

        static public async Task<LeftRightPair> GetSnrDb(double fundFreq, double minFreq, double maxFreq)
        {
            Dictionary<string, string> d = await Get(string.Format("/SnrDb/{0}/{1}/{2}", fundFreq, minFreq, maxFreq));

            LeftRightPair lrp = new LeftRightPair() { Left = MathUtil.ToDouble(d["Left"]), Right = MathUtil.ToDouble(d["Right"]) };
            return lrp;
        }

        static public async Task<LeftRightPair> GetThdnDb(double fundFreq, double minFreq, double maxFreq)
        {
            Dictionary<string, string> d = await Get(string.Format("/ThdnDb/{0}/{1}/{2}", fundFreq, minFreq, maxFreq));

            LeftRightPair lrp = new LeftRightPair() { Left = MathUtil.ToDouble(d["Left"]), Right = MathUtil.ToDouble(d["Right"]) };
            return lrp;
        }

        static public async Task<LeftRightPair> GetRmsDbv(double startFreq, double endFreq)
        {
            Dictionary<string, string> d = await Get(string.Format("/RmsDbv/{0}/{1}",  startFreq, endFreq));

            LeftRightPair lrp = new LeftRightPair() { Left = MathUtil.ToDouble(d["Left"]), Right = MathUtil.ToDouble(d["Right"]) };
            return lrp;
        }

        static public async Task<LeftRightPair> GetPeakDbv(double startFreq, double endFreq)
        {
            Dictionary<string, string> d = await Get(string.Format("/PeakDbv/{0}/{1}", startFreq, endFreq));

            LeftRightPair lrp = new LeftRightPair() { Left = MathUtil.ToDouble(d["Left"]), Right = MathUtil.ToDouble(d["Right"]) };
            return lrp;
        }

        static public async Task<LeftRightTimeSeries> GetInputTimeSeries()
        {
            Dictionary<string, string> d = await Get(string.Format("/Data/Time/Input"));

            LeftRightTimeSeries lrts = new LeftRightTimeSeries() { dt = MathUtil.ToDouble(d["Dx"]), Left = ConvertBase64ToDoubles(d["Left"]), Right = ConvertBase64ToDoubles(d["Right"]) };

            return lrts;
        }

        static public async Task<LeftRightTimeSeries> GetOutputTimeSeries()
        {
            Dictionary<string, string> d = await Get(string.Format("/Data/Time/Output"));

            LeftRightTimeSeries lrts = new LeftRightTimeSeries() { dt = MathUtil.ToDouble(d["Dx"]), Left = ConvertBase64ToDoubles(d["Left"]), Right = ConvertBase64ToDoubles(d["Right"]) };

            return lrts;
        }

        static public async Task<LeftRightFrequencySeries> GetInputFrequencySeries()
        {
            DateTime now = DateTime.Now;

            Dictionary<string, string> d = await Get(string.Format("/Data/Frequency/Input"));
            LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries() { Df = MathUtil.ToDouble(d["Dx"]), Left = ConvertBase64ToDoubles(d["Left"]), Right = ConvertBase64ToDoubles(d["Right"]) };

            double elapsedMs = DateTime.Now.Subtract(now).TotalMilliseconds;
            //Console.WriteLine($"Left Array Size: {lrfs.Left.Length}  Elapsed Ms: {elapsedMs:0.0}");

            return lrfs;
        }

        static public async Task<LeftRightFrequencySeries> GetOutputFrequencySeries()
        {
            DateTime now = DateTime.Now;

            Dictionary<string, string> d = await Get(string.Format("/Data/Frequency/Output"));
            LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries() { Df = MathUtil.ToDouble(d["Dx"]), Left = ConvertBase64ToDoubles(d["Left"]), Right = ConvertBase64ToDoubles(d["Right"]) };

            double elapsedMs = DateTime.Now.Subtract(now).TotalMilliseconds;
            //Console.WriteLine($"Left Array Size: {lrfs.Left.Length}  Elapsed Ms: {elapsedMs:0.0}");

            return lrfs;
        }

        /*
        static public async Task<Bitmap> GetGraph()
        {
            Dictionary<string, string> d = await Get(string.Format("/Data/Frequency/Input"));

            LeftRightFrequencySeries lrfs = new LeftRightFrequencySeries() { df = MathUtil.ToDouble(d["Dx"]), Left = ConvertBase64ToDoubles(d["Left"]), Right = ConvertBase64ToDoubles(d["Right"]) };

            return lrfs;
        }*/




        //*****************************
        //*** Helpers for the above ***
        //*****************************


        static double[] ConvertBase64ToDoubles(string base64DoubleArray)
        {
            byte[] byteArray = Convert.FromBase64String(base64DoubleArray); 
            double[] doubleArray = new double[byteArray.Length / sizeof(double)];
            Buffer.BlockCopy(byteArray, 0, doubleArray, 0, byteArray.Length);
            return doubleArray;
        }
        static private async Task Put(string url, string token = "", int value = 0)
        {
            string json;

            if (token != "")
                json = string.Format("{{\"{0}\":{1}}}", token, value);
            else
                json = "{{}}";

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Client.PutAsync(url, content);

            // Throw an exception if not successful
            response.EnsureSuccessStatusCode();
            response.Dispose();
        }

        static private async Task Post(string url, string value = "")
        {
            StringContent content = new StringContent(value, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Client.PostAsync(url, content);

            // Throw an exception if not successful
            response.EnsureSuccessStatusCode();
            response.Dispose();
        }

        /// <summary>
        /// This function allows us to get JSON fields. For example, if you do a GET
        /// and a JSON struct is returned:
        /// {
        ///    "Dogs" : "3"
        ///    "Cats" : "5"
        /// }
        /// 
        /// this function puts the return IDs into a dictionary for easy access. So:
        ///      Get("/my/url")["Dogs"] will return "3"
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static private async Task<Dictionary<string, string>> Get(string url)
        {
            string content;

            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            content = response.Content.ReadAsStringAsync().Result;

            // You need to use NUGET to install System.Text.Json from MSFT
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            if(result == null)
			{
				result = new Dictionary<string, string>();
			}

			return result;
        }

        static private async Task<string> Get(string url, string token)
        {
            Dictionary<string, string> dict = await Get(url);
            return dict[token].ToString();
        }

        static byte[] GetBytes(double[] vals)
        {
			var byteArray = new byte[vals.Length * sizeof(double)];
            Buffer.BlockCopy(vals, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}
