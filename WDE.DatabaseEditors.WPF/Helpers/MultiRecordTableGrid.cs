using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WDE.DatabaseEditors.Models;

#nullable disable
namespace WDE.DatabaseEditors.WPF.Helpers
{
    public static class MultiRecordTableGrid
    {
        public static readonly DependencyProperty ColumnsSourceProperty = DependencyProperty.RegisterAttached(
            "ColumnsSource", typeof(object), typeof(MultiRecordTableGrid),
            new UIPropertyMetadata(null, ColumnsSourceChanged));

        public static readonly DependencyProperty BindFieldProperty = DependencyProperty.RegisterAttached(
            "BindField", typeof(bool), typeof(MultiRecordTableGrid),
            new UIPropertyMetadata(false));
        //
        // public static readonly DependencyProperty ColumnRowsSourceProperty = DependencyProperty.RegisterAttached(
        //     "ColumnRowsSource", typeof(string), typeof(MultiRecordTableGrid),
        //     new UIPropertyMetadata(null));
        
        [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        public static string GetColumnsSource(DependencyObject obj)
        {
            return obj.GetValue(ColumnsSourceProperty) as string;
        }

        public static void SetColumnsSource(DependencyObject obj, object value)
        {
            obj.SetValue(ColumnsSourceProperty, value);
        }
        
        [AttachedPropertyBrowsableForType(typeof(DataGridCell))]
        public static bool GetBindField(DependencyObject obj)
        {
            return (bool)obj.GetValue(BindFieldProperty);
        }

        public static void SetBindField(DependencyObject obj, object value)
        {
            obj.SetValue(BindFieldProperty, value);
        }
        
        // [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        // public static string GetHeaderTextSource(DependencyObject obj)
        // {
        //     return obj.GetValue(HeaderTextSourceProperty) as string;
        // }
        //
        // public static void SetHeaderTextSource(DependencyObject obj, object value)
        // {
        //     obj.SetValue(HeaderTextSourceProperty, value);
        // }
        //
        // [AttachedPropertyBrowsableForType(typeof(DataGrid))]
        // public static string GetColumnRowsSource(DependencyObject obj)
        // {
        //     return obj.GetValue(HeaderTextSourceProperty) as string;
        // }
        //
        // public static void SetColumnRowsSource(DependencyObject obj, object value)
        // {
        //     obj.SetValue(ColumnRowsSourceProperty, value);
        // }
        
        private static void ColumnsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid grid = d as DataGrid;

            if (grid == null)
                return;
            
            if (!(e.NewValue is List<IDbTableColumn> columns))
                return;

            if (columns.Count == 0)
                return;

            DataTable dataTable = new DataTable();

            foreach (var column in columns)
                dataTable.Columns.Add(column.ColumnName);

            var amountOfRows = columns[0].Fields.Count;

            for (int i = 0; i < amountOfRows; ++i)
            {
                var row = dataTable.NewRow();

                foreach (var column in columns)
                    row[column.ColumnName] = column.Fields[i];
                
                dataTable.Rows.Add(row);
            }
            
            grid.DataContext = dataTable.DefaultView;
        }
    }
}