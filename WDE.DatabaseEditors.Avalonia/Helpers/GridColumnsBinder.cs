using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.Avalonia.Helpers
{
    public class GridColumnsBinder
    {
        public static readonly AttachedProperty<ObservableCollection<DatabaseColumnHeaderViewModel>> ColumnsProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, ObservableCollection<DatabaseColumnHeaderViewModel>>("Columns", typeof(GridColumnsBinder));

        public static ObservableCollection<DatabaseColumnHeaderViewModel> GetColumns(IAvaloniaObject obj)
        {
            return obj.GetValue(ColumnsProperty);
        }

        public static void SetColumns(IAvaloniaObject obj, ObservableCollection<DatabaseColumnHeaderViewModel> value)
        {
            obj.SetValue(ColumnsProperty, value);
        }

        public static readonly AttachedProperty<ObservableCollection<DatabaseColumnHeaderViewModel>> ColumnsWithHeaderProperty = AvaloniaProperty.RegisterAttached<IAvaloniaObject, ObservableCollection<DatabaseColumnHeaderViewModel>>("ColumnsWithHeader", typeof(GridColumnsBinder));

        public static ObservableCollection<DatabaseColumnHeaderViewModel> GetColumnsWithHeader(IAvaloniaObject obj)
        {
            return obj.GetValue(ColumnsWithHeaderProperty);
        }

        public static void SetColumnsWithHeader(IAvaloniaObject obj, ObservableCollection<DatabaseColumnHeaderViewModel> value)
        {
            obj.SetValue(ColumnsWithHeaderProperty, value);
        }
        
        static GridColumnsBinder()
        {
            ColumnsProperty.Changed.SubscribeAction(ev =>
            {
                if (ev.Sender is not Grid grid)
                    return;

                var newList = ev.NewValue.Value;

                if (newList == null)
                    return;

                grid.ColumnDefinitions.Clear();
                foreach (var column in newList)
                {
                    var definition = new ColumnDefinition();
                    definition.SharedSizeGroup = column.ColumnIdForUi;
                    grid.ColumnDefinitions.Add(definition);

                    definition = new ColumnDefinition(5, GridUnitType.Pixel);
                    grid.ColumnDefinitions.Add(definition);
                }
            });

            ColumnsWithHeaderProperty.Changed.SubscribeAction(ev =>
            {
                if (ev.Sender is not Grid grid)
                    return;

                var newList = ev.NewValue.Value;

                if (newList == null)
                    return;

                void onChanged(object? sender, NotifyCollectionChangedEventArgs e)
                {
                    grid.Children.Clear();
                    grid.ColumnDefinitions.Clear();
                    int index = 0;
                    foreach (var column in newList)
                    {
                        var definition = new ColumnDefinition(column.Width, GridUnitType.Pixel);
                        definition.SharedSizeGroup = column.ColumnIdForUi;
                        definition.MinWidth = 30;
                        column.AutoDispose(definition.GetObservable(ColumnDefinition.WidthProperty).SubscribeAction(
                        width =>
                        {
                            column.Width = width.Value;
                        }));
                        grid.ColumnDefinitions.Add(definition);

                        definition = new ColumnDefinition(5, GridUnitType.Pixel);
                        grid.ColumnDefinitions.Add(definition);

                        var tb = new TextBlock() {Text = column.Name};
                        tb.SetValue(Grid.ColumnProperty, index * 2);
                        tb.SetValue(ToolTip.TipProperty, column.Help);
                        grid.Children.Add(tb);

                        var splitter = new GridSplitter() {};
                        splitter.SetValue(Grid.ColumnProperty, index * 2 + 1);
                        grid.Children.Add(splitter);
                    
                        index++;
                    }
                    grid.ColumnDefinitions.Add(new ColumnDefinition(30, GridUnitType.Pixel));
                }
                
                newList.CollectionChanged += onChanged;
                onChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            });
        }

    }
}