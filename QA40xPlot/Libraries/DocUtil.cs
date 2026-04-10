using Microsoft.Win32;
using Newtonsoft.Json;
using QA40xPlot.Actions;
using QA40xPlot.Data;
using QA40xPlot.Dialogs;
using QA40xPlot.ViewModels;
using System.Diagnostics;
using System.Windows;

namespace QA40xPlot.Libraries
{
	public struct DocHeader
	{
		public string Version;
		public string AppVersion;
		public string Date;
		public string Notes;
	}

	public class DocUtil
	{
		public static List<ActBase> ActionList = new List<ActBase>();
		public static bool DoLoadConfig = true;
		public static bool DoLoadTests = true;

		public static void DoOpenDocument()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				FileName = string.Empty, // Default file name
				InitialDirectory = ViewSettings.Singleton.SettingsVm.DataFolder,
				DefaultExt = ".plx", // Default file extension
				Filter = "Document Files|*.plx.zip|All files|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = openFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				LoadQuestionDlg ldq = new LoadQuestionDlg(DoLoadConfig, DoLoadTests);
				ldq.ShowDialog();

				if (ldq.DialogResult == true)
				{
					// Save document
					string filename = openFileDialog.FileName;
					var filetext = Util.LoadFileText(filename);
					DoLoadTests = ldq.IsLoadData;
					DoLoadConfig = ldq.IsLoadConfig;
					OpenDocument(filetext, filename, DoLoadTests, DoLoadConfig);
				}
			}
		}

		public static void OpenDocument(string docText, string fileName, bool loadTests, bool loadConfig)
		{
			var actList = ActionList;
			Dictionary<string, string>? docDict = null;
			try
			{
				var u = JsonConvert.DeserializeObject<Dictionary<string, string>>(docText);
				if (u != null) 
				{
					docDict = u;
				}
			}
			catch { }

			if (docDict == null || docDict.Count == 0)
			{
				MessageBox.Show("The document is empty or invalid.", "A load error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			try
			{
				var ky = docDict.Keys.Select(x => x.ToString()).ToArray();
				var kys = string.Join(",", ky);
				Debug.WriteLine($"Dict keys: {kys}");
				docDict["FileName"] = fileName;
				if(docDict.ContainsKey("Configuration"))
				{
					var cfg = docDict["Configuration"];
					if(loadConfig)
						ViewSettings.Singleton.MainVm.LoadFromSettingsText(cfg);
					docDict.Remove("Configuration");
				}
				if(loadTests)
				{
					foreach (var act in actList)
					{
						act.LoadFromDictionary(docDict, true);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A save error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		public static void DoSaveDocument()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = String.Format("QaDoc{0}", FloorViewModel.FileAddon()), // Default file name
				InitialDirectory = ViewSettings.Singleton.SettingsVm.DataFolder,
				DefaultExt = ".plx", // Default file extension
				Filter = "Document Files|*.plx.zip|All files|*.*" // Filter files by extension
			};

			// Show save file dialog box
			bool? result = saveFileDialog.ShowDialog();

			// Process save file dialog box results
			if (result == true)
			{
				// Save document
				string filename = saveFileDialog.FileName;
				SaveDocument(filename);
			}
		}

		public static void SaveDocument(string filepath)
		{
			var actList = ActionList;
			Dictionary<string, string> docDict = new();
			var dh = new DocHeader
			{
				Version = "1.0",
				AppVersion = MainWindow.GetVersionInfo(),
				Date = DateTime.Now.ToString(),
				Notes = "A QA40xPlot document."
			};
			var sdh = Util.ConvertToJson(dh);
			if (sdh != null && sdh.Length > 0)
				docDict.Add("Header", sdh);
			var cfg = MainViewModel.GetConfigText();
			if (cfg != null && cfg.Length > 0)
				docDict.Add("Configuration", cfg);
			try
			{
				string jsonString = string.Empty;
				foreach (var act in actList)
				{
					var vm = act.PageData?.ViewModel;
					if (act.PageData == null || vm == null)
						continue;
					bool saveFreq = (vm.Averages > 1) && (vm.Name == "Spectrum" || vm.Name == "Intermodulation");
					if (act.HasDataAvailable())
					{
						jsonString = act.PageToText(null, saveFreq);
						if (jsonString != null && jsonString.Length > 0)
							docDict.Add(act.PageData.ViewModel.Name, jsonString);
					}
					if(act.OtherTabs != null && act.OtherTabs.Count > 0)
					{
						int cntr = 0;
						foreach (var ot in act.OtherTabs)
						{
							if (ot.ViewModel == null)
								continue;
							jsonString = act.PageToText(ot, saveFreq);
							if (jsonString != null && jsonString.Length > 0)
								docDict.Add($"{ot.ViewModel.Name}:{++cntr}", jsonString);
						}
					}
				}
				// Write the JSON string to a file
				var docString = Util.ConvertToJson(docDict);
				var fname = filepath;
				if (!fname.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
					fname += ".zip";
				Util.CompressTextToFile(docString, fname); // zip it
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A save error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		public static bool SaveToFile<Model>(DataTab page, Model GuiModel, string fileName, bool saveFreq = false) where Model : BaseViewModel
		{
			if (page == null)
				return false;
			try
			{
				var jsonString = PageToText(page, GuiModel, saveFreq);
				// Write the JSON string to a file
				var fname = fileName;
				if (!fname.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
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

		/// <summary>
		/// create a json string from a workspace page
		/// </summary>
		/// <typeparam name="Model"></typeparam>
		/// <param name="page"></param>
		/// <param name="GuiModel"></param>
		/// <param name="fileName"></param>
		/// <param name="saveFreq"></param>
		/// <returns></returns>
		public static string PageToText<Model>(DataTab page, Model GuiModel, bool saveFreq = false) where Model : BaseViewModel
		{
			if (page == null)
				return string.Empty;
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
				string jsonString = Util.ConvertToJson(page);
				page.TimeSaver = null; // clear the time saver, we don't need it anymore
				page.FreqSaver = null; // clear the frequency saver, we don't need it anymore
				return jsonString;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "A save error occurred.", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			return string.Empty;
		}

	}
}

