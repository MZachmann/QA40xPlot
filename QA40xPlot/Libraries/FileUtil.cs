using QA40xPlot.Data;
using QA40xPlot.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QA40xPlot.Libraries
{
	public class FileUtil
	{
		public static void SaveDocument(string filepath)
		{
		}

		public static bool SaveToFile<Model>(DataTab<Model> page, Model GuiModel, string fileName, bool saveFreq = false) where Model : BaseViewModel
		{
			if (page == null)
				return false;
			try
			{
				var jsonString = PageToText(page, GuiModel, fileName, saveFreq);
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
		public static string PageToText<Model>(DataTab<Model> page, Model GuiModel, string fileName, bool saveFreq = false) where Model : BaseViewModel
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

