using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.WPF.Helpers
{
    public class GridColumnsBinder
    {
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached(
              "Columns",
              typeof(ObservableCollection<DatabaseColumnHeaderViewModel>),
              typeof(GridColumnsBinder),
              new FrameworkPropertyMetadata(OnColumnsChanged)
            );

        public static void SetColumns(UIElement element, ObservableCollection<DatabaseColumnHeaderViewModel> value)
        {
            element.SetValue(ColumnsProperty, value);
        }
        public static ObservableCollection<DatabaseColumnHeaderViewModel> GetColumns(UIElement element)
        {
            return (ObservableCollection<DatabaseColumnHeaderViewModel>)element.GetValue(ColumnsProperty);
        }


        public static readonly DependencyProperty ColumnsWithHeaderProperty = DependencyProperty.RegisterAttached(
              "ColumnsWithHeader",
              typeof(ObservableCollection<DatabaseColumnHeaderViewModel>),
              typeof(GridColumnsBinder),
              new FrameworkPropertyMetadata(OnColumnsWithHeaderChanged)
            );

        public static void SetColumnsWithHeader(UIElement element, ObservableCollection<DatabaseColumnHeaderViewModel> value)
        {
            element.SetValue(ColumnsWithHeaderProperty, value);
        }
        public static ObservableCollection<DatabaseColumnHeaderViewModel> GetColumnsWithHeader(UIElement element)
        {
            return (ObservableCollection<DatabaseColumnHeaderViewModel>)element.GetValue(ColumnsWithHeaderProperty);
        }

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid grid = (Grid)d;
            var newList = (ObservableCollection<DatabaseColumnHeaderViewModel>)e.NewValue;

            grid.ColumnDefinitions.Clear();
            foreach (var column in newList)
            {
                var definition = new ColumnDefinition();
                definition.SharedSizeGroup = column.DatabaseName;
                grid.ColumnDefinitions.Add(definition);

                definition = new ColumnDefinition();
                definition.Width = new GridLength(5, GridUnitType.Pixel);
                grid.ColumnDefinitions.Add(definition);
            }
        }

        private static void OnColumnsWithHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid grid = (Grid)d;
            var newList = (ObservableCollection<DatabaseColumnHeaderViewModel>)e.NewValue;

            void onChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                grid.Children.Clear();
                grid.ColumnDefinitions.Clear();
                int index = 0;
                foreach (var column in newList)
                {
                    var definition = new ColumnDefinition();
                    definition.Width = new GridLength(column.PreferredWidth ?? 120, GridUnitType.Pixel);
                    definition.SharedSizeGroup = column.DatabaseName;
                    definition.MinWidth = 30;
                    grid.ColumnDefinitions.Add(definition);

                    definition = new ColumnDefinition();
                    definition.Width = new GridLength(5, GridUnitType.Pixel);
                    grid.ColumnDefinitions.Add(definition);

                    var tb = new TextBlock() { Text = column.Name, Padding = new Thickness(16,6,6,6) };
                    tb.SetValue(Grid.ColumnProperty, index * 2);
                    grid.Children.Add(tb);

                    var splitter = new GridSplitter() { };
                    splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                    splitter.SetValue(Grid.ColumnProperty, index * 2 + 1);
                    grid.Children.Add(splitter);

                    index++;
                }
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30, GridUnitType.Pixel) });
            }

            newList.CollectionChanged += onChanged;
            onChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

    }
}
