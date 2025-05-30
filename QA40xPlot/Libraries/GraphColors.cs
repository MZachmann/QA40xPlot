using ScottPlot;

// https://api.flutter.dev/flutter/material/Colors-class.html


namespace QA40xPlot.Libraries
{
    public class GraphColors
    {
        public Color[] Blues { get; } = Color.FromHex(BlueColors);
        public Color[] Greens { get; } = Color.FromHex(GreenColors);
        public Color[] Cyans { get; } = Color.FromHex(CyanColors);
        public Color[] Purples { get; } = Color.FromHex(PurpleColors);
        public Color[] Ambers { get; } = Color.FromHex(AmberColors);
        public Color[] Oranges { get; } = Color.FromHex(OrangeColors);
        public Color[] Reds { get; } = Color.FromHex(RedColors);
        public Color[] Teals { get; } = Color.FromHex(TealColors);
        public Color[] Greys { get; } = Color.FromHex(GreyColors);
        public Color[] BlueGreys { get; } = Color.FromHex(BlueGreyColors);

		private static readonly string[] OrangeColors = { "#E65100", "#EF6C00", "#F57C00", "#FB8C00", "#FF9800", "#FFA726", "#FFB74D", "#FFCC80", "#FFE0B2", "#FFF3E0" };
		private static readonly string[] BlueColors = { "#0D47A1", "#1565C0", "#1976D2", "#1E88E5", "#2196F3", "#42A5F5", "#64B5F6", "#90CAF9", "#BBDEFB", "#E3F2FD" };
		private static readonly string[] GreenColors = { "#1B5E20", "#2E7D32", "#388E3C", "#43A047", "#4CAF50", "#66BB6A", "#81C784", "#A5D6A7", "#C8E6C9", "#E8F5E9" };
		private static readonly string[] CyanColors = { "#006064", "#00838F", "#0097A7", "#00ACC1", "#00BCD4", "#26C6DA", "#4DD0E1", "#80DEEA", "#B2EBF2", "#E0F7FA" };
		private static readonly string[] PurpleColors = { "#4A148C", "#6A1B9A", "#7B1FA2", "#8E24AA", "#9C27B0", "#AB47BC", "#BA68C8", "#CE93D8", "#E1BEE7", "#F3E5F5" };
		private static readonly string[] AmberColors = { "#FF6F00", "#FF8F00", "#FFA000", "#FFB300", "#FFC107", "#FFCA28", "#FFD54F", "#FFE082", "#FFECB3", "#FFF8E1" };
		private static readonly string[] RedColors = { "#B71C1C", "#C62828", "#D32F2F", "#E53935", "#F44336", "#EF5350", "#E57373", "#EF9A9A", "#FFCDD2", "#FFEBEE" };
		private static readonly string[] TealColors = { "#004D40", "#00695C", "#00796B", "#00897B", "#009688", "#26A69A", "#4DB6AC", "#80CBC4", "#B2DFDB", "#E0F2F1" };
		private static readonly string[] GreyColors = { "#212121", "#424242", "#616161", "#757575", "#9E9E9E", "#BDBDBD", "#D6D6D6", "#E0E0E0", "#EEEEEE", "#F5F5F5" };
		private static readonly string[] BlueGreyColors = { "#263238", "#37474F", "#455A64", "#546E7A", "#607D8B", "#78909C", "#90A4AE", "#B0BEC5", "#CFD8DC", "#ECEFF1" };

        public Color GetColor(int range, int index)
        {
			switch (range)
            {
                case 0:
                    return Blues[index % Blues.Length];
                case 1:
                    return Oranges[index % Oranges.Length];
				case 2:
					return Purples[index % Purples.Length];
				case 3:
					return Cyans[index % Cyans.Length];
				case 4:
                    return Reds[index % Reds.Length];
				case 5:
					return Greens[index % Greens.Length];
				case 6:
                    return Ambers[index % Ambers.Length];
                case 7:
                    return Teals[index % Teals.Length];
                case 8:
                    return BlueGreys[index % BlueGreys.Length];
                default:
                    return Greys[index % Greys.Length];
            }
        }

		public string GetColorHex(int range, int index)
		{
			switch (range)
			{
				case 0:
					return BlueColors[index % Blues.Length];
				case 1:
					return OrangeColors[index % Oranges.Length];
				case 2:
					return PurpleColors[index % Purples.Length];
				case 3:
					return CyanColors[index % Cyans.Length];
				case 4:
					return RedColors[index % Reds.Length];
				case 5:
					return GreenColors[index % Greens.Length];
				case 6:
					return AmberColors[index % Ambers.Length];
				case 7:
					return TealColors[index % Teals.Length];
				case 8:
					return BlueGreyColors[index % BlueGreys.Length];
				default:
					return GreyColors[index % Greys.Length];
			}
		}
	}
}
