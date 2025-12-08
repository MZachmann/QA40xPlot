using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QA40xPlot.Libraries
{
	internal static class TabUtil
	{
		internal static void SetTabPanelVisibility(bool isVisible, DependencyObject theTabs)
		{
			var tabPanel = FindVisualChild<System.Windows.Controls.Primitives.TabPanel>(theTabs);
			if (tabPanel != null)
			{
				tabPanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		internal static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
		{
			for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

				if (child is T result)
				{
					return result;
				}

				T? childResult = FindVisualChild<T>(child);
				if (childResult != null)
				{
					return childResult;
				}
			}
			return null;
		}
	}
}
