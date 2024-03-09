using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using AvaloniaStyles.Controls;
using WDE.Common.Types;

namespace WDE.Common.Avalonia.Utils
{
    public class DataGridColumns
    {
        public static readonly AvaloniaProperty<IReadOnlyList<ColumnDescriptor>> ColumnsSourceProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumns, Control, IReadOnlyList<ColumnDescriptor>>("ColumnsSource",
            new List<ColumnDescriptor>(), coerce: ColumnsSourceChanged);

        public static IReadOnlyList<ColumnDescriptor> GetColumnsSource(Control obj) => (IReadOnlyList<ColumnDescriptor>?)obj.GetValue(ColumnsSourceProperty) ?? new List<ColumnDescriptor>();

        public static void SetColumnsSource(Control obj, IReadOnlyList<ColumnDescriptor> value) => obj.SetValue(ColumnsSourceProperty, value);

        static DataGridColumns()
        {
            ColumnsSourceProperty.Changed.AddClassHandler<Control>((c, e) =>
            {
                ColumnsSourceChanged(c, (IReadOnlyList<ColumnDescriptor>)e.NewValue!);
            });
        }
        
        private static IReadOnlyList<ColumnDescriptor> ColumnsSourceChanged(AvaloniaObject o, IReadOnlyList<ColumnDescriptor> arg2)
        {
            if (arg2 == null)
                return new List<ColumnDescriptor>();// arg2;
            
            if (o is DataGrid dataGrid)
            {
                dataGrid.Columns.Clear();

                foreach (var col in arg2)
                {
                    DataGridBoundColumn column = col.CheckboxMember ? new DataGridCheckBoxColumn() : new DataGridTextColumn();
                    column.Header = new DataGridColumnHeader()
                    {
                        Content = col.HeaderText
                    };
                    column.IsReadOnly = !col.CheckboxMember;
                    var displayMember = col.DisplayMember;
                    column.Binding = new Binding(displayMember);
                    dataGrid.Columns.Add(column);
                }
            }
            else if (o is GridView || o is VirtualizedGridView)
            {
                var columns = arg2.Select(col => new GridColumnDefinition()
                {
                    Name = col.HeaderText,
                    Property = col.DisplayMember,
                    PreferedWidth = (int) (col.PreferredWidth ?? 100),
                    Checkable = col.CheckboxMember,
                    DataTemplate = col.DataTemplate as IDataTemplate
                }).ToList();
                if (o is GridView gridView)
                    gridView.Columns = columns;
                else if (o is VirtualizedGridView virtualizedGridView)
                    virtualizedGridView.Columns = columns;
            }

            return arg2;
        }
    }
}
