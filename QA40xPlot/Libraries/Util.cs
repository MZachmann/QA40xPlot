using System.IO.Compression;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Text;
using Newtonsoft.Json;
using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using Newtonsoft.Json.Linq;

namespace QA40xPlot.Libraries
{
	public static class Util
	{
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
					if(vtd == _LastCompEdit)
						return _MicCompensation;
				}
				_CompensationFile = string.Empty; // reset the file name
												  // not cached, load the file and parse it
				if (string.IsNullOrEmpty(compFile))
					throw new Exception("No mic compensation file was selected in settings.");
				if (!File.Exists(compFile))
					throw new Exception("Compensation file not found.");

				var txtData = File.ReadAllText(compFile);	// get the compensation data
				var lines = txtData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); // lines of text w/o \r\n
				int i = 0;
				lines = lines.Select(x => x.TrimStart()).ToArray(); // trim whitespace
				while (!char.IsDigit(lines[i][0]))
				{
					i++; // skip the header lines
				}
				if(i > 3)
					throw new Exception("Invalid mic compensation file format. Expected numeric data after header lines.");
				List<double> freq = new List<double>();
				List<double> gain = new List<double>();
				for(; i < lines.Length; i++)
				{
					var parts = lines[i].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length < 2)
						continue; // skip invalid lines
					var f = MathUtil.ToDouble(parts[0], -1230.0); // first part is frequency
					var g = MathUtil.ToDouble(parts[1], -1230.0); // second part is gain
					if(f != -1230 && g != -1230)
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
		/// <summary>
		/// Load a file into a DataTab
		/// </summary>
		/// <param name="fileName">full path name</param>
		/// <returns>a datatab with no frequency info</returns>
		public static DataTab<Model>? LoadFile<Model>(DataTab<Model> model, string fileName)
		{
			// a new DataTab
			var page = new DataTab<Model>(model.ViewModel, new LeftRightTimeSeries());
			page.Definition.FileName = fileName;
			try
			{
				string jsonContent = string.Empty;
				// unzip?
				if (fileName.Contains(".zip"))
				{
					// Read the JSON file into a string
					jsonContent = UncompressFileToText(fileName);
				}
				else
				{
					// Read the JSON file into a string
					jsonContent = File.ReadAllText(fileName);
				}
				// check which viewmodel this was built for
				bool isValid = false;
				string x = string.Empty;
				Dictionary<string, object>? oldTime = null;
				// generic deserialize first....
				{
					var u = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsonContent); // untyped seriale
					if (u == null)
						return null;
					x = u["ViewModel"]["Name"].ToString() ?? string.Empty;
					var z = model.ViewModel as BaseViewModel;
					isValid = z?.IsValidLoadModel(x ?? "") ?? false;
					if(u.ContainsKey("TimeRslt"))
						oldTime = u["TimeRslt"];
				}
				if (!isValid)
				{
					MessageBox.Show($"This {x} file is not compatible with this tab.", "Incompatible file", MessageBoxButton.OK, MessageBoxImage.Warning);
					return null;
				}

					// Deserialize the JSON string into an object
				var jsonObject = JsonConvert.DeserializeObject<DataTab<Model>>(jsonContent);
				if (jsonObject != null)
				{
					if( jsonObject.TimeSaver != null && jsonObject.TimeSaver.Left.Length > 0)
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
					page.Definition = jsonObject.Definition;
					page.TimeRslt = jsonObject.TimeRslt;
					page.Sweep = jsonObject.Sweep;
					if (page.ViewModel != null)
						jsonObject.ViewModel.CopyPropertiesTo(page.ViewModel);
					page.Definition.Id = id; // keep the same id
					page.Definition.FileName = fileName; // re-set the filename
					if(jsonObject.FreqRslt != null)
						page.FreqRslt = jsonObject.FreqRslt; // set the frequency result
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
				page = null;
			}
			return page;
		}

		public static bool SaveToFile<Model>(DataTab<Model> page, string fileName, bool saveFreq = false)
		{
			if (page == null)
				return false;
			try
			{
				// convert time data to longer stuff
				if(page.TimeRslt != null && page.TimeRslt.Left.Length > 0)
				{
					page.TimeSaver = new LeftRightTimeSaver();
					page.TimeSaver.FromSeries(page.TimeRslt);
				}

				if(saveFreq && page.FreqRslt != null && page.FreqRslt.Left.Length > 0)
				{
					page.FreqSaver = new LeftRightFreqSaver();
					page.FreqSaver.FromSeries(page.FreqRslt);
				}
				// Serialize the object to a JSON string
				string jsonString = JsonConvert.SerializeObject(page, Formatting.Indented);
				page.TimeSaver = null; // clear the time saver, we don't need it anymore
				page.FreqSaver = null; // clear the frequency saver, we don't need it anymore

				// Write the JSON string to a file
				// File.WriteAllText(fileName, jsonString);
				var fname = fileName;
				if( !fname.Contains(".zip"))
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

		static string UncompressFileToText(string filePath)
		{
			using (FileStream compressedFileStream = new FileStream(filePath, FileMode.Open))
			using (GZipStream decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
			using (StreamReader reader = new StreamReader(decompressionStream, Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}

		public static void CompressTextToFile(string text, string filePath)
		{
			byte[] textBytes = Encoding.UTF8.GetBytes(text);
			using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
			using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
			{
				gzipStream.Write(textBytes, 0, textBytes.Length);
			}
		}

		public static void CompressFile(string sourceFilePath, string destinationFilePath)
		{
			using (FileStream sourceFileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
			using (FileStream destinationFileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
			using (GZipStream compressionStream = new GZipStream(destinationFileStream, CompressionMode.Compress))
			{
				sourceFileStream.CopyTo(compressionStream);
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
				foreach(var prop in pending)
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
