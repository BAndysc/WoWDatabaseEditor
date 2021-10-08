using Avalonia;
using Avalonia.Controls;

namespace WDE.Common.Avalonia.Utils
{
    public class GridUtils
    {
        public static readonly AttachedProperty<RowDefinitions> DynamicRowsProperty = AvaloniaProperty.RegisterAttached<Grid, RowDefinitions>("DynamicRows", typeof(GridUtils));

        public static RowDefinitions GetDynamicRows(Grid obj)
        {
            return obj.GetValue(DynamicRowsProperty);
        }

        public static void SetDynamicRows(Grid obj, RowDefinitions value)
        {
            obj.SetValue(DynamicRowsProperty, value);
        }

        static GridUtils()
        {
            DynamicColumnsProperty.Changed.AddClassHandler<Grid>(UpdateColumns);
            DynamicRowsProperty.Changed.AddClassHandler<Grid>(UpdateRows);
        }

        private static void UpdateRows(Grid obj, AvaloniaPropertyChangedEventArgs arg2)
        {
            obj.RowDefinitions.Clear();
            if (arg2.NewValue is null)
                return;
            foreach (var rowDef in ((RowDefinitions)arg2.NewValue!))
            {
                obj.RowDefinitions.Add(rowDef);
            }
        }

        private static void UpdateColumns(Grid obj, AvaloniaPropertyChangedEventArgs arg2)
        {
            obj.ColumnDefinitions.Clear();
            if (arg2.NewValue is null)
                return;
            foreach (var colDef in ((ColumnDefinitions)arg2.NewValue!))
            {
                obj.ColumnDefinitions.Add(colDef);
            }
        }

        public static readonly AttachedProperty<ColumnDefinitions> DynamicColumnsProperty = AvaloniaProperty.RegisterAttached<Grid, ColumnDefinitions>("DynamicColumns", typeof(GridUtils));

        public static ColumnDefinitions GetDynamicColumns(Grid obj)
        {
            return obj.GetValue(DynamicColumnsProperty);
        }

        public static void SetDynamicColumns(Grid obj, ColumnDefinitions value)
        {
            obj.SetValue(DynamicColumnsProperty, value);
        }
    }
}