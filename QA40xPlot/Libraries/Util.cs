using System.IO.Compression;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Text;
using Newtonsoft.Json;
using System.Drawing.Drawing2D;
using QA40xPlot.Data;
using QA40xPlot.ViewModels;

namespace QA40xPlot.Libraries
{
	public static class Util
	{
		/// <summary>
		/// Load a file into a DataTab
		/// </summary>
		/// <param name="fileName">full path name</param>
		/// <returns>a datatab with no frequency info</returns>
		public static async Task<DataTab<Model>> LoadFile<Model>(DataTab<Model> model, string fileName)
		{
			// a new DataTab
			var page = new DataTab<Model>(model.ViewModel, new LeftRightTimeSeries());
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
				// Deserialize the JSON string into an object
				var jsonObject = JsonConvert.DeserializeObject<DataTab<Model>>(jsonContent);
				if (jsonObject != null)
				{
					try
					{
						page.NoiseFloor = new LeftRightPair();

						// file pagedata with new stuff
						page.NoiseFloor = jsonObject.NoiseFloor;
						page.Definition = jsonObject.Definition;
						page.TimeRslt = jsonObject.TimeRslt;
						jsonObject.ViewModel.CopyPropertiesTo(page.ViewModel);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return page;
		}

		public static bool SaveToFile<Model>(DataTab<Model> page, string fileName)
		{
			if (page == null)
				return false;
			try
			{
				var container = new Dictionary<string, object>();
				container["PageData"] = page;
				// Serialize the object to a JSON string
				string jsonString = JsonConvert.SerializeObject(page, Formatting.Indented);

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
			if (vwsIn == null || dest == null)
				return;
			if (!vwsIn.ContainsKey(name))
				return;
			Dictionary<string, object> vws = (Dictionary<string, object>)vwsIn[name];

			Type type = dest.GetType();
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			try
			{
				foreach (PropertyInfo property in properties)
				{
					if (property.CanRead && property.CanWrite)
					{
						if (vws.ContainsKey(property.Name))
						{
							object value = vws[property.Name];
							try
							{
								//Debug.WriteLine("Property " + property.Name);
								property.SetValue(dest, Convert.ChangeType(value, property.PropertyType));
							}
							catch (Exception) { }    // for now ignore this
						}
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
