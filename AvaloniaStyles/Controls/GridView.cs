using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;

namespace AvaloniaStyles.Controls
{
    public class GridView : TemplatedControl
    {
        private const int SplitterWidth = 5;
        
        public static readonly DirectProperty<GridView, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<GridView, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);
        
        private IEnumerable _items = new AvaloniaList<object>();
        
        [Content]
        public IEnumerable Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        public static readonly DirectProperty<GridView, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<GridView, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);

        private object _selectedItem;
        
        public object SelectedItem
        {
            get => _selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
        }
        
        public static readonly DirectProperty<GridView, IEnumerable<GridColumnDefinition>> ColumnsProperty =
            AvaloniaProperty.RegisterDirect<GridView, IEnumerable<GridColumnDefinition>>(nameof(Columns), o => o.Columns, (o, v) => o.Columns = v);
        
        private IEnumerable<GridColumnDefinition> _columns = new AvaloniaList<GridColumnDefinition>();
        
        public IEnumerable<GridColumnDefinition> Columns
        {
            get => _columns;
            set => SetAndRaise(ColumnsProperty, ref _columns, value);
        }
        
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<GridView, IDataTemplate>(nameof(ItemTemplate));
        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
        
        private Grid header;
        private ListBox listBox;

        public ListBox ListBoxImpl => listBox;
        
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            header = e.NameScope.Find<Grid>("PART_header");
            listBox = e.NameScope.Find<ListBox>("PART_listbox");
                
            header.ColumnDefinitions.Clear();
            header.Children.Clear();
            SetupGridColumns(header, true);
            
            // additional column in header makes it easier to resize
            header.ColumnDefinitions.Add(new ColumnDefinition(10, GridUnitType.Pixel));
            
            int i = 0;
            foreach (var column in Columns)
            {
                var headerCell = new GridViewColumnHeader()
                {
                    ColumnName = column.Name
                };
                headerCell.ColumnName = column.Name;
                Grid.SetColumn(headerCell, i++);
                header.Children.Add(headerCell);
                    
                var splitter = new GridSplitter();
                Grid.SetColumn(splitter, i++);
                header.Children.Add(splitter);
            }
        }

        private Grid ConstructGrid()
        {
            Grid grid = new();
            SetupGridColumns(grid, false);
            return grid;
        }

        private void SetupGridColumns(Grid grid, bool header)
        {
            int i = 0;
            foreach (var column in Columns)
            {
                var c = new ColumnDefinition(header ? new GridLength(column.PreferedWidth, GridUnitType.Pixel) : default)
                {
                    SharedSizeGroup = $"col{(i++)}",
                    MinWidth = 10,
                };
                grid.ColumnDefinitions.Add(c);
                grid.ColumnDefinitions.Add(new ColumnDefinition(SplitterWidth, GridUnitType.Pixel));
            }
        }

        static GridView()
        {
            ColumnsProperty.Changed.AddClassHandler<GridView>(OnColumnsModified);
        }

        private static void OnColumnsModified(GridView gridView, AvaloniaPropertyChangedEventArgs args)
        {
        }

        public GridView()
        {
            ItemTemplate = new FuncDataTemplate((o) => true, (o, scope) =>
            {
                var parent = ConstructGrid();
                int i = 0;
                foreach (var column in Columns)
                {
                    Control control;
                    if (column.Checkable)
                    {
                        control = new CheckBox()
                        {
                            [!ToggleButton.IsCheckedProperty] = new Binding(column.Property),
                            IsEnabled = !column.IsReadOnly
                        };
                    }
                    else
                    {
                        control = new TextBlock()
                        {
                            [!TextBlock.TextProperty] = new Binding(column.Property)
                        };
                    }
                    
                    Grid.SetColumn(control, 2 * i++);
                    parent.Children.Add(control);
                    
                }
                return parent;
            }, true);
        }
    }

    public class GridViewColumnHeader : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="ColumnName"/> property.
        /// </summary>
        public static readonly DirectProperty<GridViewColumnHeader, string> ColumnNameProperty =
            AvaloniaProperty.RegisterDirect<GridViewColumnHeader, string>(
                nameof(ColumnName),
                o => o.ColumnName,
                (o, v) => o.ColumnName = v);
        
        private string _columnName;
        
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Content]
        public string ColumnName
        {
            get { return _columnName; }
            set { SetAndRaise(ColumnNameProperty, ref _columnName, value); }
        }
    }

    public class GridViewHeader : ContentControl
    {
        
    }
    
    public class GridColumnDefinition
    {
        public string Name { get; set; }
        public string Property { get; set; }
        public bool Checkable { get; set; }
        public bool IsReadOnly { get; set; }
        public int PreferedWidth { get; set; } = 70;
    }
}