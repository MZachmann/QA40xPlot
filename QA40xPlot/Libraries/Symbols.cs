// simply a list of used symbols
// and a dummy fuction to remove warnings

namespace QA40xPlot.Libraries
{
	internal static class Symbols
	{
		static string Ohms = "Ω";
		static string Micro = "μ";
		internal static string ShowSymbols()
		{
			string sout = Symbols.Ohms + Symbols.Micro;
			return sout;
		}
	}

}
