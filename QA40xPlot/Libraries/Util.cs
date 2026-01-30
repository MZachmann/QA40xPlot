using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Windows;

namespace QA40xPlot.Libraries
{
	public static class Util
	{
		public enum CompressType
		{
			Unknown,
			Zip,
			Gzip
		}

		// ZIP files start with the bytes: 50 4B 03 04 (PK..)
		private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };
		private static readonly byte[] GzipSignature = { 0x1F, 0x8B };
		private static string _CompensationFile = string.Empty; // mic compensation file, if any
		private static LeftRightFrequencySeries? _MicCompensation = null; // mic compensation data, if any
		private static DateTime? _LastCompEdit = null; // last time the mic compensation file was edited
		public static LeftRightFrequencySeries LoadMicCompensation()
		{
			var compFile = ViewSettings.Singleton.SettingsVm.MicCompFile;
			DateTime? vtd = null;
			var lrfs = new LeftRightFrequencySeries(); // return empty if we fail
			try
			{
				if (compFile.Length > 0 && compFile == _CompensationFile && _MicCompensation != null)
				{
					vtd = File.GetLastWriteTime(compFile); // get the last write time of the file
					if (vtd == _LastCompEdit)
						return _MicCompensation;
				}
				_CompensationFile = string.Empty; // reset the file name
												  // not cached, load the file and parse it
				if (string.IsNullOrEmpty(compFile))
					throw new Exception("No mic compensation file was selected in settings.");
				if (!File.Exists(compFile))
					throw new Exception("Compensation file not found.");

				var txtData = File.ReadAllText(compFile);   // get the compensation data
				var lines = txtData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); // lines of text w/o \r\n
				int i = 0;
				lines = lines.Select(x => x.TrimStart()).ToArray(); // trim whitespace
				while (!char.IsDigit(lines[i][0]))
				{
					i++; // skip the header lines
				}
				if (i > 3)
					throw new Exception("Invalid mic compensation file format. Expected numeric data after header lines.");
				List<double> freq = new List<double>();
				List<double> gain = new List<double>();
				for (; i < lines.Length; i++)
				{
					var parts = lines[i].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length < 2)
						continue; // skip invalid lines
					var f = MathUtil.ToDouble(parts[0], -1230.0); // first part is frequency
					var g = MathUtil.ToDouble(parts[1], -1230.0); // second part is gain
					if (f != -1230 && g != -1230)
					{
						freq.Add(f);
						gain.Add(g);
					}
				}
				// hacky stick freq/gain into LeftRightFrequencySeries
				lrfs.Left = freq.ToArray();
				var mix = gain.Max(); // find the max gain value
				lrfs.Right = gain.Select(x => x - mix).ToArray();    // convert gain curve to all less than 1
				_CompensationFile = compFile;   // remember the file
				_LastCompEdit = vtd;            // remember the last edit time
				_MicCompensation = lrfs;        // cache the mic compensation data
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading mic compensation file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return new LeftRightFrequencySeries();
			}
			return lrfs; // return the loaded mic compensation data
		}

		// read a possibly compressed file
		public static string LoadFileText(string fileName)
		{
			string jsonContent = string.Empty;
			try
			{
				// unzip?
				if (fileName.Contains(".zip"))
				{
					// Read the JSON file into a string. So check for
					// file compression type then uncompress accordingly
					var ctype = IdentifyCompression(fileName);
					if (ctype == CompressType.Zip)
						jsonContent = UncompressZipFileToText(fileName);
					else if (ctype == CompressType.Gzip)
						jsonContent = UncompressGzipFileToText(fileName);
				}
				else
				{
					// Read the JSON file into a string
					jsonContent = File.ReadAllText(fileName);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return jsonContent;
		}

		/// <summary>
		/// part of the load process needs this simple json deserialize
		/// into a dictionary of dictionaries of string->object
		/// </summary>
		/// <param name="jsonText"></param>
		/// <returns></returns>
		public static Dictionary<string, Dictionary<string, object>>? Deserialize(string jsonText)
		{
			try
			{
				// generic deserialize first....
				var u = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsonText);
				return u;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return null;
		}

		/// <summary>
		/// Load a file into a DataTab
		/// </summary>
		/// <param name="fileName">full path name</param>
		/// <returns>a datatab with no frequency info</returns>
		public static DataTab<Model>? LoadFile<Model>(DataTab<Model> model, string fileName) where Model : BaseViewModel
		{
			// a new DataTab
			var page = new DataTab<Model>(model.ViewModel, new LeftRightTimeSeries());
			page.Definition.FileName = fileName;
			try
			{
				var jsonContent = LoadFileText(fileName);
				// check which viewmodel this was built for
				bool isValid = false;
				Dictionary<string, object>? oldTime = null;
				// generic deserialize first....
				string? viewName = string.Empty;
				int viewVersion = 1;
				{
					var dict = Deserialize(jsonContent);
					if (dict == null)
						return null;
					if(dict.ContainsKey("ViewModel"))
					{
						var myvm = dict["ViewModel"];
						if (myvm != null)
						{
							if(myvm.ContainsKey("Name"))
								viewName = myvm["Name"]?.ToString();
							if (myvm.ContainsKey("Version"))
							{
								var verstr = myvm["Version"]?.ToString();
								viewVersion = MathUtil.ToInt(verstr, 1);
							}
							else
								viewVersion = 1;
							var z = model.ViewModel;
							isValid = z?.IsValidLoadModel(viewName, viewVersion) ?? false;
						}
					}
					if (dict.ContainsKey("TimeRslt"))
						oldTime = dict["TimeRslt"];
				}
				if (!isValid)
				{
					MessageBox.Show($"This {viewName} file is not compatible with this tab.", "Incompatible file", MessageBoxButton.OK, MessageBoxImage.Warning);
					return null;
				}

				// Deserialize the JSON string into an object
				var jsonObject = JsonConvert.DeserializeObject<DataTab<Model>>(jsonContent);
				if (jsonObject != null)
				{
					if (jsonObject.TimeSaver != null && jsonObject.TimeSaver.Left.Length > 0)
					{
						// we have a time saver, convert it to a time series
						jsonObject.TimeRslt = jsonObject.TimeSaver.ToSeries(); // convert the time saver to a time series
						jsonObject.TimeSaver = null; // clear the time saver since it's just for load/save
					}
					else if (oldTime != null && jsonObject.TimeRslt == null)
					{
						// this handles prior version where TimeRslt was serialized
						jsonObject.TimeRslt = new LeftRightTimeSeries(); // create a new time series
						jsonObject.TimeRslt.dt = (double)oldTime["dt"];
						jsonObject.TimeRslt.Left = (oldTime["Left"] as JArray)?.Select(x => (double)x)?.ToArray() ?? [];
						jsonObject.TimeRslt.Right = (oldTime["Right"] as JArray)?.Select(x => (double)x)?.ToArray() ?? [];
					}
					if (jsonObject.FreqSaver != null && jsonObject.FreqSaver.Left.Length > 0)
					{
						// we have a time saver, convert it to a time series
						jsonObject.FreqRslt = jsonObject.FreqSaver.ToSeries(); // convert the time saver to a time series
						jsonObject.FreqSaver = null; // clear the time saver since it's just for load/save
					}
					// fill pagedata with new stuff
					var id = page.Definition.Id;
					page.NoiseFloor = jsonObject.NoiseFloor;
					page.NoiseFloorA = jsonObject.NoiseFloorA;
					page.NoiseFloorC = jsonObject.NoiseFloorC;
					page.Definition = jsonObject.Definition;
					page.TimeRslt = jsonObject.TimeRslt;
					page.SweepSteps = jsonObject.SweepSteps;
					page.Sweep = jsonObject.Sweep;
					if (page.ViewModel != null)
					{
						// copy all but name
						var srcMod = jsonObject.ViewModel as Model;
						var destMod = page.ViewModel;
						if(srcMod != null && destMod != null)
						{
							destMod.LoadViewFrom(srcMod);
							//jsonObject.ViewModel.CopyPropertiesTo(page.ViewModel, ["Name"]);
						}
					}
					page.Definition.Id = id; // keep the same id
					page.Definition.FileName = fileName; // re-set the filename
					if (jsonObject.FreqRslt != null)
						page.FreqRslt = jsonObject.FreqRslt; // set the frequency result
					page.Definition.IsOnL = true;
					page.Definition.IsOnR = false;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
				page = null;
			}
			return page;
		}

		public static string ConvertToJson(object tcv)
		{
			string jsonString = string.Empty;
			try
			{
				using (var sw = new StringWriter())
				using (var writer = new JsonTextWriter(sw))
				{
					writer.Formatting = Formatting.Indented; // Enable pretty-print
					writer.Indentation = 1;                   // Number of tabs per indent level
					var serializer = new JsonSerializer();
					serializer.Serialize(writer, tcv);
					jsonString = sw.ToString();
				}
			}
			catch
			{
				Debug.WriteLine("Error converting to JSON.");
			}
			return jsonString;
		}

		public static bool SaveToFile<Model>(DataTab<Model> page, Model GuiModel, string fileName, bool saveFreq = false) where Model : BaseViewModel
		{
			if (page == null)
				return false;
			try
			{
				// convert time data to longer stuff
				if (page.TimeRslt != null && page.TimeRslt.Left.Length > 0)
				{
					page.TimeSaver = new LeftRightTimeSaver();
					page.TimeSaver.FromSeries(page.TimeRslt);
				}

				if (saveFreq && page.FreqRslt != null && page.FreqRslt.Left.Length > 0)
				{
					page.FreqSaver = new LeftRightFreqSaver();
					page.FreqSaver.FromSeries(page.FreqRslt);
				}
				// Serialize the object to a JSON string
				// before we do this, copy the graph viewing stuff to the viewmodel
				page.ViewModel.CopyGraphSettingsFromGui(GuiModel);
				//string jsonString = JsonConvert.SerializeObject(page, Formatting.Indented);
				string jsonString = ConvertToJson(page);
				page.TimeSaver = null; // clear the time saver, we don't need it anymore
				page.FreqSaver = null; // clear the frequency saver, we don't need it anymore

				// Write the JSON string to a file
				// File.WriteAllText(fileName, jsonString);
				var fname = fileName;
				if (!fname.Contains(".zip", StringComparison.OrdinalIgnoreCase))
					fname += ".zip";
				Util.CompressTextToFile(jsonString, fname); // zip it
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A save error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}
			return true;
		}

		public static string GetDefaultConfigPath()
		{
			// look for a default config file before we paint the windows for theme setting...
			var fdocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var ffile = ((App)Application.Current).DefaultCfg;
			string fload = string.Empty;
			string fpath;
			if (Path.IsPathRooted(ffile))
			{
				fpath = ffile;
			}
			else
			{
				fpath = fdocs + @"\" + ffile;
			}
			return fpath;
		}

		private static bool IsThisFile(string filePath, byte[] signature)
		{
			try
			{
				// Check file signature first
				using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				{
					if (fs.Length < signature.Length)
						return false;

					byte[] buffer = new byte[signature.Length];
					fs.ReadExactly(buffer);

					for (int i = 0; i < signature.Length; i++)
					{
						if (buffer[i] != signature[i])
							return false;
					}
				}
			}
			catch
			{
				return false; // Any error means it's not a valid ZIP
			}
			return true;
		}

		/// <summary>
		/// Checks if the file is a ZIP by signature 
		/// </summary>
		private static CompressType IdentifyCompression(string filePath)
		{
			if( IsThisFile(filePath, ZipSignature))
				return CompressType.Zip;
			if (IsThisFile(filePath, GzipSignature))
				return CompressType.Gzip;
			return CompressType.Unknown;
		}

		static string UncompressZipFileToText(string filePath)
		{
			try
			{
				// Open the ZIP archive for reading
				using (ZipArchive archive = ZipFile.OpenRead(filePath))
				{
					var fname = Path.GetFileName(filePath);
					// remove zip suffix
					var fbegin = fname.Substring(0, fname.LastIndexOf('.'));
					// Find the specific file inside the ZIP
					// if we just do this then rename the file we can't load it
					ZipArchiveEntry? entry = archive.GetEntry(fbegin);
					if (entry == null)
					{
						// file was renamed? just use the first entry
						var entries = archive.Entries;
						if (entries.Count >= 1)
						{
							entry = entries[0];
						}
					}
					if (entry == null)
					{
						MessageBox.Show("Error", "Unable to load the file. Zip entry conflict.", MessageBoxButton.OK, MessageBoxImage.Error);
						return string.Empty;
					}

					// Read the file content into a string
					using (StreamReader reader = new StreamReader(entry.Open()))
					{
						string fileContent = reader.ReadToEnd();
						return fileContent;
					}
				}
			}
			catch (Exception )
			{
				//Console.WriteLine($"I/O Error: {ex.Message}");
			}
			return string.Empty;
		}

		/// <summary>
		/// uncompress a gzip file to text
		/// this takes care of the historical zip/gzip confusion
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		static string UncompressGzipFileToText(string filePath)
		{
			try
			{
				using (FileStream compressedFileStream = new FileStream(filePath, FileMode.Open))
				using (GZipStream decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
				using (StreamReader reader = new StreamReader(decompressionStream, Encoding.UTF8))
				{
					return reader.ReadToEnd();
				}
			}
			catch (Exception )
			{
				//MessageBox.Show(ex.Message, "File load error", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return string.Empty;
		}

		/// <summary>
		/// compress a string into a Zip file
		/// </summary>
		/// <param name="text"></param>
		/// <param name="filePath"></param>
		public static void CompressTextToFile(string text, string filePath)
		{
			try
			{
				var fname = Path.GetFileName(filePath);
				var fbegin = fname.Substring(0, fname.LastIndexOf('.'));    // remove zip suffix

				// Create or overwrite the ZIP file
				using (FileStream zipToOpen = new FileStream(filePath, FileMode.Create))
				using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
				{
					// Create a new entry (file) inside the ZIP
					ZipArchiveEntry entry = archive.CreateEntry(fbegin);

					// Write the string content into the entry
					using (StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8))
					{
						writer.Write(text);
					}
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "File save error", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		public static void GetPropertiesFrom(Dictionary<string, Dictionary<string, object>> vwsIn, string name, object dest)
		{
			List<string> delayed = new List<string>();// { "Gen1Voltage", "Gen2Voltage", "GenVoltage", "StartVoltage", "EndVoltage" };
			if (vwsIn == null || dest == null)
				return;
			if (!vwsIn.ContainsKey(name))
				return;
			Dictionary<string, object> vws = (Dictionary<string, object>)vwsIn[name];

			try
			{
				Type type = dest.GetType();
				// get all public properties that aren't static
				PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				List<PropertyInfo> pending = new List<PropertyInfo>();
				foreach (PropertyInfo property in properties)
				{
					if (property.CanRead && property.CanWrite)
					{
						if (vws.ContainsKey(property.Name))
						{
							if (delayed.Contains(property.Name))  // these are set later
							{
								pending.Add(property);
								continue; // skip these for now
							}
							object value = vws[property.Name];
							try
							{
								//Debug.WriteLine("Property " + property.Name);
								// note this will raise property changed events
								property.SetValue(dest, Convert.ChangeType(value, property.PropertyType));
							}
							catch (Exception) { }    // for now ignore this
						}
					}
				}
				foreach (var prop in pending)
				{
					if (vws.ContainsKey(prop.Name))
					{
						object value = vws[prop.Name];
						try
						{
							//Debug.WriteLine("Property " + prop.Name);
							prop.SetValue(dest, Convert.ChangeType(value, prop.PropertyType));
						}
						catch (Exception) { }    // for now ignore this
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "An error occurred", MessageBoxButton.OK, MessageBoxImage.Information);
			}

		}

	}
}
