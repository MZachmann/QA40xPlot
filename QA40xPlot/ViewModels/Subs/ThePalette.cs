using ScottPlot;
using System.Diagnostics;

namespace QA40xPlot.ViewModels.Subs
{
	public class ThePalette : IPalette
	{
		public static int PaletteSize { get => 20; }        // expected palette length
		private string _HexString = string.Empty;
		private string[] _HexList = [];
		private uint[] _HexColors = [];
		public string Name { get; } = "Main Palette";

		public string Description { get; } = "The default color palette";

		public Color[] Colors { get => this.GetColors(HexColors.Length); }

		private uint[] ParseColors(string hexColors)
		{
			if (_HexString != hexColors)
			{
				try
				{
					_HexString = hexColors;
					_HexList = hexColors.Split(',').Select(x => x.TrimStart()).ToArray();
					_HexColors = new uint[PaletteSize];
					for (int i = 0; i < PaletteSize; i++)
					{
						var clr = (i >= _HexList.Length) ? _HexList[i - _HexList.Length] : _HexList[i];
						_HexColors[i] = Libraries.PlotUtil.StrToColor(clr).ARGB;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error parsing colors: {ex.Message}");
				}
			}
			return _HexColors;
		}

		private uint[] HexColors { get { return ParseColors(ViewSettings.Singleton.SettingsVm.PaletteColors); } }

		public Color GetColor(int index)
		{
			return Color.FromARGB(HexColors[index % HexColors.Length]);
		}

		public string GetColorName(int index)
		{
			if (_HexList.Length == 0)
				return "Transparent"; // default to transparent if no colors are defined
			return _HexList[index % _HexList.Length];
		}
	}

}
