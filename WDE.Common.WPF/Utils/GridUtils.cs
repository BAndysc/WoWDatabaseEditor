using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace WDE.Common.WPF.Utils
{
    public static class GridUtils
    {
        public static string GetRows(DependencyObject obj) => (string)obj.GetValue(RowsProperty);
        public static void SetRows(DependencyObject obj, string value) => obj.SetValue(RowsProperty, value);
        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.RegisterAttached("Rows", typeof(string), typeof(GridUtils), new PropertyMetadata("", UpdateRows));

        public static string GetColumns(DependencyObject obj) => (string)obj.GetValue(ColumnsProperty);
        public static void SetColumns(DependencyObject obj, string value) => obj.SetValue(ColumnsProperty, value);
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.RegisterAttached("Columns", typeof(string), typeof(GridUtils), new PropertyMetadata("", UpdateColumns));

        private static void UpdateColumns(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Grid grid))
                return;
            
            var columns = GetColumns(d).Split(",");
            
            for (int i = grid.ColumnDefinitions.Count; i < columns.Length; ++i)
                grid.ColumnDefinitions.Add(new ColumnDefinition());

            while (grid.ColumnDefinitions.Count > columns.Length)
                grid.ColumnDefinitions.RemoveAt(grid.ColumnDefinitions.Count - 1);

            for (var index = 0; index < columns.Length; index++)
            {
                var column = columns[index];
                var length = ParseLength(column);
                if (length != null)
                    grid.ColumnDefinitions[index].Width = length.Value;
            }
        }

        private static void UpdateRows(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Grid grid))
                return;

            var rows = GetRows(d).Split(",");
            
            for (int i = grid.RowDefinitions.Count; i < rows.Length; ++i)
                grid.RowDefinitions.Add(new RowDefinition());

            while (grid.RowDefinitions.Count > rows.Length)
                grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);

            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                var length = ParseLength(row);
                if (length != null)
                    grid.RowDefinitions[index].Height = length.Value;
            }
        }

        private static GridLength? ParseLength(string text)
        {
            if (text == "auto")
                return GridLength.Auto;
            
            if (text[^1] == '*')
            {
                if (double.TryParse(((ReadOnlySpan<char>) text).Slice(0, text.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var length ))
                    return new GridLength(length, GridUnitType.Auto);
            }
            else if (double.TryParse(text,NumberStyles.Any, CultureInfo.InvariantCulture, out var length))
            {
                return new GridLength(length, GridUnitType.Pixel);
            }

            return null;
        }
    }
}