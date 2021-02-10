using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using WoWDatabaseEditorCore.Extensions;

namespace WoWDatabaseEditorCore.Avalonia.Extensions
{
    public class DataGridColumns
    {
        public static readonly AvaloniaProperty ColumnsSourceProperty = AvaloniaProperty.RegisterAttached<DataGridColumns, DataGrid, IList<ColumnDescriptor>>("ColumnsSource",
            null, coerce: ColumnsSourceChanged);

        public static List<ColumnDescriptor> GetColumnsSource(IControl obj) => (List<ColumnDescriptor>)obj.GetValue(ColumnsSourceProperty);

        public static void SetColumnsSource(IControl obj, List<ColumnDescriptor> value) => obj.SetValue(ColumnsSourceProperty, value);

        private static IList<ColumnDescriptor> ColumnsSourceChanged(IAvaloniaObject o, IList<ColumnDescriptor> arg2)
        {
            var dataGrid = o as DataGrid;

            dataGrid.Columns.Clear();

            foreach (var col in arg2)
            {
                DataGridBoundColumn column = col.CheckboxMember ? new DataGridCheckBoxColumn() : new DataGridTextColumn();
                column.Header = new DataGridColumnHeader()
                {
                    Content = col.HeaderText
                };
                column.Binding = new Binding(col.DisplayMember);
                dataGrid.Columns.Add(column);
            }
            
            return arg2;
        }
    }
}