using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Collections;
using System.Windows.Threading;
using System.Globalization;

namespace WpfMisc.Controls
{
    /// <summary>
    /// Provides a ComboBox with filtering.
    /// When the user types a text, this ComboBox will show only the items which contain that text in any
    /// part of the shown string (using a case insensitive comparision).
    /// This control will only work if your items are strings or if you use "DisplayMemberPath" property
    /// 
    /// <example>
    /// Add xmlns:view="clr-namespace:WpfMisc.Controls" to your Window or any parent of this control, and use
    /// <code><![CDATA[
    ///  <view:FilteredComboBox ItemsSource="{Binding MyItemsSource}"/>
    /// ]]></code>
    /// </example>
    /// </summary>
    public class FilteredComboBox : ComboBox
    {
        static FilteredComboBox()
        {
            ItemsSourceProperty.OverrideMetadata(typeof(FilteredComboBox),
                new FrameworkPropertyMetadata() { CoerceValueCallback = CoerceItemsSource });
        }

        public FilteredComboBox()
        {
            
            SelectionChanged += OnSelectionChanged;

            // Should not use with IsTextSearchEnabled = true
            IsTextSearchEnabled = false;

            IsEditable = true;
            IsSynchronizedWithCurrentItem = false;

            Delay = 500;
            _timer.Tick += delegate { ProccessScheduledFiltering(); };
            ClearFilter();
            Unloaded += OnUnloaded;
        }

        public int Delay
        {
            get { return (int)_timer.Interval.TotalMilliseconds; }
            set { _timer.Interval = new TimeSpan(0, 0, 0, 0, value); }
        }

        #region Protected interface
        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);
            ClearTextBoxSelection();
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            ClearFilter();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            
            if ((e.Key == Key.Enter) || (e.Key == Key.Tab) || (e.Key == Key.Return))
            {
                // Don't show up when focus is changing
                _timer.Stop();

                var oldSelection = this.SelectedItem;
                // Close menu
                IsDropDownOpen = false;
                SelectedItem = oldSelection;
            }
            else if ((e.Key == Key.Down) || (e.Key == Key.Up))
            {
                IsDropDownOpen = true;
                if (SelectedIndex == -1)
                {
                    // Force selection of an item
                    ProccessScheduledFiltering();
                    if (Items != null)
                    {
                        if (e.Key == Key.Down)
                            SelectedIndex = 0;
                        else
                            SelectedIndex = Items.Count - 1;
                    }
                }
            }            
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var textBox = EditableTextBox;
            if (textBox != null)
                textBox.TextChanged += OnTextChanged;
        }
        #endregion

        #region Private members
        // Avoids updating filter when selecting with keyboard or mouse
        private bool _selectionChangedFlag = false;
        private bool _shouldResetCaretIndex = false;
        private DispatcherTimer _timer = new DispatcherTimer();
        private static readonly CompareInfo compareInfo = System.Threading.Thread.CurrentThread.CurrentUICulture.CompareInfo;
        private static readonly CompareOptions compareOptions = CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase;

        private static object CoerceItemsSource(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
                return null;

            if (baseValue is ICollectionView)
                return baseValue;

            // Return a new CollectionView, so this ComboBox does not affect (and is not affected)
            // other controls which use the default CollectionView
            return new CollectionViewSource()
            {
                Source = baseValue
            }.View;
        }

        private ICollectionView FilteringView
        {
            get
            {
                var view = ItemsSource as ICollectionView;
                if (view != null)
                    return view;
                return CollectionViewSource.GetDefaultView(ItemsSource);
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            // If caused by selection, consume the flag
            if (_selectionChangedFlag)
            {
                _selectionChangedFlag = false;
                return;
            }
            ScheduleFiltering();
        }

        private void ScheduleFiltering()
        {
            _timer.Stop();
            _timer.Start();
        }

        private void ProccessScheduledFiltering()
        {
            // When we reset selection or update the filter, the text gets erased,
            // so let's save it before making any change
            string oldText = this.Text;
            SelectedItem = null;
            Text = oldText;
            UpdateFilter();
            
            if (_shouldResetCaretIndex)
            {
                var textBox = EditableTextBox;
                if (textBox != null)
                    textBox.CaretIndex = Int32.MaxValue;

                _shouldResetCaretIndex = false;
            }

            if (Items != null)
            {
                // Select if only one element is shown
                if (Items.Count == 1)
                    SelectedIndex = 0;

                IsDropDownOpen = Items.Count > 1;
            }
            _timer.Stop();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectionChangedFlag = true;
            _shouldResetCaretIndex = true;
            if(!IsDropDownOpen)
                ClearFilter();
        }

        private void OnUnloaded(object sender, EventArgs e)
        {
            ClearFilter();
        }

        private TextBox EditableTextBox
        {
            get { return base.GetTemplateChild("PART_EditableTextBox") as TextBox; }
        }

        private void ClearTextBoxSelection()
        {
            var textBox = EditableTextBox;
            if (textBox == null)
                return;
            textBox.SelectionStart = textBox.SelectionLength = 0;
            textBox.CaretIndex = Int32.MaxValue;
        }

        private bool FilterPredicate(object value)
        {
            // No filters
            if (Text.Length == 0)
                return true;

            // null does not match any criteria
            if (value == null)
                return false;
            
            // Get the string which represents this item (given by DisplayMemberPath) through reflection
            string obj = value.GetType().GetProperty(DisplayMemberPath).GetValue(value, null).ToString();

            // Case insensitive contains
            return compareInfo.IndexOf(obj, Text, compareOptions) != -1;
        }

        private void UpdateFilter()
        {
            if (ItemsSource == null)
                return;
            var fv = FilteringView;
            if (fv == null)
                return;
            fv.Filter = FilterPredicate;
        }

        private void ClearFilter()
        {
            var fv = FilteringView;
            if (fv == null)
                return;
            fv.Filter = null;
        }
        #endregion
    }
}
