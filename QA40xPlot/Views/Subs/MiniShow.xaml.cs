﻿using QA40xPlot.Data;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for TabAbout.xaml
	/// </summary>
	public partial class MiniShow : UserControl
	{
		public MiniShow()
		{
			InitializeComponent();
		}

		public void SetDataContext(DataDescript dataDef)
		{
			this.DataContext = dataDef;
		}
	}
}
