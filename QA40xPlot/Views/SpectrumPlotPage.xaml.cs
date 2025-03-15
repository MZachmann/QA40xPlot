﻿using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class SpectrumPlotPage : UserControl
	{
		public SpectrumPlotPage()
		{
			InitializeComponent();
			var vm = ViewModels.ViewSettings.Singleton.SpectrumVm;
			this.DataContext = vm;
			vm.SetAction(this.WpfPlot1, this.Info1);
		}
	}
}
