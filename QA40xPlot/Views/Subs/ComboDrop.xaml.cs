using Newtonsoft.Json.Linq;
using QA40xPlot.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace QA40xPlot.Views
{
	/// <summary>
	/// Interaction logic for ComboDrop.xaml
	/// </summary>
	public partial class ComboDrop : UserControl
	{
		#region Dependency Properties

		// DependencyProperty registration for TheTitle
		public static readonly DependencyProperty TheTitleProperty =
			DependencyProperty.Register(
				nameof(TheTitle),
				typeof(string),
				typeof(ComboDrop),
				new FrameworkPropertyMetadata(
					default(string),
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		// DependencyProperty registration for TheText
		public static readonly DependencyProperty TheTextProperty =
			DependencyProperty.Register(
				nameof(TheText),
				typeof(string),
				typeof(ComboDrop),
				new FrameworkPropertyMetadata(
					default(string),
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnTheTextChanged));

		// DependencyProperty registration for TheSource
		public static readonly DependencyProperty TheSourceProperty =
			DependencyProperty.Register(
				nameof(TheSource),
				typeof(List<string>),
				typeof(ComboDrop),
				new FrameworkPropertyMetadata(
					default(List<string>),
					FrameworkPropertyMetadataOptions.None,
					OnTheSourceChanged));

		// DependencyProperty registration for ItemsSet
		public static readonly DependencyProperty ItemsSetProperty =
			DependencyProperty.Register(
				nameof(ItemsSet),
				typeof(SelectItemList),
				typeof(ComboDrop),
				new FrameworkPropertyMetadata(
					default(SelectItemList),
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnItemsSetChanged));

		// DependencyProperty registration for IsEditable
		public static readonly DependencyProperty IsEditableProperty =
			DependencyProperty.Register(
				nameof(IsEditable),
				typeof(bool),
				typeof(ComboDrop),
				new FrameworkPropertyMetadata(
					default(bool),
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnIsEditableChanged));
		#endregion
		// CLR wrapper for TheText
		public string TheTitle
		{
			get => (string)GetValue(TheTitleProperty);
			set => SetValue(TheTitleProperty, value);
		}

		// CLR wrapper for TheText
		public string TheText
		{
			get => (string)GetValue(TheTextProperty);
			set => SetValue(TheTextProperty, value);
		}

		// CLR wrapper for TheSource
		public List<string> TheSource
		{
			get => (List<string>)GetValue(TheSourceProperty);
			set => SetValue(TheSourceProperty, value);
		}

		// CLR wrapper for ItemsSet
		public SelectItemList ItemsSet
		{
			get => (SelectItemList)GetValue(ItemsSetProperty);
			set => SetValue(ItemsSetProperty, value);
		}

		// CLR wrapper for IsEditable
		public bool IsEditable
		{
			get => (bool)GetValue(IsEditableProperty);
			set => SetValue(IsEditableProperty, value);
		}

		public ComboBox ValueComboBox { get; }

		public ComboDrop()
		{
			InitializeComponent();
			ValueComboBox = cbCombo;
		}

		private void DownButton_Click(object sender, RoutedEventArgs e)
		{
			ItemsSet = SelectItemList.ParseList(TheText);
			ThePopup.IsOpen = !ThePopup.IsOpen;
		}

		private void ThePopup_Closed(object sender, EventArgs e)
		{
			if (!IsEditable)
			{
				for (int i = 0; i < ItemsSet.Count; i++)
				{
					if (!ItemsSet[i].IsSelected)
					{
						ItemsSet[i].Name = string.Empty;
					}
				}
			}
			TheText = ItemsSet.ParseableList("0.1", IsEditable);
		}
		#region IfChanged
		private static void OnItemsSetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// Placeholder for change handling if needed in future.
		}

		private static void OnIsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// Placeholder for change handling if needed in future.
		}

		private static void OnTheTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// Placeholder for change handling if needed in future.
			// var control = (ComboDrop)d;
			// var newValue = (string)e.NewValue;
		}

		private static void OnTheSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// Placeholder for change handling if needed in future.
		}
		#endregion

		private void DoAddRow_Click(object sender, RoutedEventArgs e)
		{
			//ItemsSet.Add(new SelectItem(true, string.Empty, (uint)ItemsSet.Count));
			// let the item highlight?
			try
			{
				var rowIndex = (sender as Button)?.Tag.ToString();
				if (rowIndex != null)
				{
					var rows = Int32.Parse(rowIndex);
					if (ItemsSet.Count > rows && rows >= 0)
						ItemsSet.Insert(rows+1, new SelectItem(true, string.Empty, 0));
				}
				uint i = 0;
				foreach (var u in ItemsSet)
				{
					u.Index = i++;
				}
			}
			catch (Exception)
			{
				// ignore
			}
		}
		private void DoRemoveRow_Click(object sender, RoutedEventArgs e)
		{
			if (ItemsSet.Count <= 1)
				return;
			// let the item highlight?
			try
			{
				var rowIndex = (sender as Button)?.Tag.ToString();
				if (rowIndex != null)
				{
					var rows = Int32.Parse(rowIndex);
					if (ItemsSet.Count > rows && rows >= 0 && ItemsSet.Count > 1)
						ItemsSet.RemoveAt(rows);
				}
				uint i = 0;
				foreach (var u in ItemsSet)
				{
					u.Index = i++;
				}
			}
			catch (Exception)
			{
				// ignore
			}
		}

		private void ClosePopup(object sender, RoutedEventArgs e)
		{
			ThePopup.IsOpen = false;
		}
    }
}
