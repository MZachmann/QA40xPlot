using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using QA40xPlot.ViewModels;

namespace QA40xPlot.Views
{
    // this is a simple wrapper to implement DoAppear and DoDisappear
    // in the viewmodels
    public class AppearingPage : Window
    {
        private const bool LogVerbose = true;
        private BaseViewModel? _TheViewModel = null;
        public BaseViewModel? TheViewModel { get { return _TheViewModel; } set { _TheViewModel = value; } }
    }
}
