using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfMisc.Attached
{
    /// <summary>
    /// Contains attached properties for a Grid object, to automatically set Grid.Row and Grid.Column
    /// on each of its children, based on XAML order.
    /// This is particulary useful when editing large forms, where you would have to manually adapt
    /// the Grid.Row and Grid.Column properties on many elements, each time you move one part of your
    /// form.
    /// 
    /// This class respects the Grid.ColumnSpan property, and will ignore every child element with
    /// Row or Column set manually.
    /// 
    /// <example>
    /// Given a Grid with "AutoSetCells=true":
    /// 
    /// <code><![CDATA[
    /// <Grid xmlns:attached="clr-namespace:WpfMisc.Attached" attached:GridUtils.AutoSetCells="true">
    ///     <Grid.ColumnDefinitions>
    ///         <ColumnDefinition Width="Auto"/>
    ///         <ColumnDefinition Width="Auto"/>
    ///     </Grid.ColumnDefinitions>
    ///     <Grid.RowDefinitions>
    ///         <RowDefinition Height="Auto"/>
    ///         <RowDefinition Height="Auto"/>
    ///     </Grid.RowDefinitions>
    ///     
    ///     <TextBlock Text="A: "/>
    ///     <TextBox Text="{Binding A}"/>
    ///     <TextBlock Text="B: "/>
    ///     <TextBox Text="{Binding B}"/>
    /// </Grid>]]></code>
    /// 
    /// This class will set up its elements like this:
    /// 
    /// <code><![CDATA[
    ///     ...
    ///     <TextBlock Text="A: " Grid.Row="0" Grid.Column="0"/>
    ///     <TextBox Text="{Binding A}" Grid.Row="0" Grid.Column="1"/>
    ///     <TextBlock Text="B: " Grid.Row="1" Grid.Column="0"/>
    ///     <TextBox Text="{Binding B}" Grid.Row="1" Grid.Column="1"/>
    ///     ...
    /// ]]></code>
    /// </example>
    /// </summary>
    static class GridUtils
    {
        /// <summary>
        /// Determines if this class should automatically set the row and column of the Grid elements.
        /// This property is meant to be used on a Grid object
        /// </summary>
        public static readonly DependencyProperty AutoSetCellsProperty =
            DependencyProperty.RegisterAttached("AutoSetCells", typeof(Boolean), typeof(GridUtils),
                new FrameworkPropertyMetadata(false, OnAutoSetCellsChanged));

        /// <summary>
        /// When used on UIElement, contained in a Grid with AutoSetCells="true", this property
        /// determines if the element must be the first of a row.
        /// </summary>
        public static readonly DependencyProperty ForceRowBreakProperty =
            DependencyProperty.RegisterAttached("ForceRowBreak", typeof(Boolean), typeof(GridUtils),
                new FrameworkPropertyMetadata(false));

        public static void SetForceRowBreak(DependencyObject dp, Boolean value)
        {
            dp.SetValue(ForceRowBreakProperty, value);
        }

        public static Boolean GetForceRowBreak(DependencyObject dp)
        {
            return (Boolean)dp.GetValue(ForceRowBreakProperty);
        }

        public static void SetAutoSetCells(DependencyObject dp, Boolean value)
        {
            dp.SetValue(AutoSetCellsProperty, value);
        }

        public static Boolean GetAutoSetCells(DependencyObject dp)
        {
            return (Boolean)dp.GetValue(AutoSetCellsProperty);
        }

        #region Private members
        private static void OnAutoSetCellsChanged(DependencyObject dp,
            DependencyPropertyChangedEventArgs e)
        {
            Grid grid = dp as Grid;
            if (grid == null)
                return;

            if ((Boolean)e.NewValue == true && (Boolean)e.OldValue == false)
                grid.Loaded += OnGridLoaded;
            else if ((Boolean)e.NewValue == false && (Boolean)e.OldValue == true)
                grid.Loaded -= OnGridLoaded;
        }

        private static void OnGridLoaded(object sender, EventArgs e)
        {
            AutoSetCells((Grid)sender);
        }

        private static void AutoSetCells(Grid grid) {
            int columns = grid.ColumnDefinitions.Count;

            int currentRow = 0,
                currentColumn = 0;

            for (int i = 0; i < grid.Children.Count; ++i)
            {
                UIElement child = grid.Children[i];

                // If this element already has a Row or Column, skip it.
                if (child.ReadLocalValue(Grid.RowProperty) != DependencyProperty.UnsetValue ||
                    child.ReadLocalValue(Grid.ColumnProperty) != DependencyProperty.UnsetValue)
                    continue;

                // If there is not enough space in this row or ForceRowBreak="true", ensure this is 
                // the first column of a row.
                if (currentColumn != 0 && (currentColumn + Grid.GetColumnSpan(child) > columns || GetForceRowBreak(child)))
                {
                    currentColumn = 0;
                    ++currentRow;
                }

                Grid.SetRow(child, currentRow);
                Grid.SetColumn(child, currentColumn);

                currentColumn += Grid.GetColumnSpan(child);

                if (currentColumn >= columns)
                {
                    currentColumn = 0;
                    ++currentRow;
                }
            }
        }
        #endregion
    }
}
