﻿using QA40xPlot.Actions;
using QA40xPlot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.ViewManagement;

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
