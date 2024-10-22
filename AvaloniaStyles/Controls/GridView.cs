using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.MVVM.Utils;

namespace AvaloniaStyles.Controls
{
    public class GridViewListBox : ListBox
    {
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new GridViewItem();
        }

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<GridViewItem>(item, out recycleKey);
        }
    }

    public class GridViewItem : ListBoxItem
    {
        protected override Type StyleKeyOverride => typeof(ListBoxItem);
        static GridViewItem()
        {
            ContentProperty.Changed.AddClassHandler<GridViewItem>((item, args) =>
            {
                var old = GetClassName(args.OldValue);
                var @new = GetClassName(args.NewValue);
                if (old != @new)
                {
                    if (old != null)
                        item.Classes.Remove(old);
                    if (@new != null)
                        item.Classes.Add(@new);
                }
            });
        }

        private static string? GetClassName(object? obj)
        {
            if (obj == null)
                return null;

            return obj.GetType().Name;
        }
    }
    
    public class GridView : TemplatedControl
    {
        internal const int SplitterWidth = 5;
        
        public static readonly DirectProperty<GridView, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<GridView, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);
        
        public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
            AvaloniaProperty.Register<GridView, SelectionMode>(nameof(SelectionMode));
        
        public SelectionMode SelectionMode
        {
            get => GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }
        
        private IEnumerable items = new AvaloniaList<object>();
        
        [Content]
        public IEnumerable Items
        {
            get => items;
            set => SetAndRaise(ItemsProperty, ref items, value);
        }
        
        public static readonly StyledProperty<ISelectionModel> SelectionProperty =
            AvaloniaProperty.Register<GridView, ISelectionModel>(
                nameof(Selection), new SelectionModel<object>());

        private IDisposable? selectionDisposable;
        public ISelectionModel Selection
        {
            get => GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        public static readonly DirectProperty<GridView, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<GridView, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay);

        public object? SelectedItem
        {
            get => Selection.SelectedItem;
            set => Selection.SelectedItem = value;
        }

        public static readonly StyledProperty<bool> AutoScrollToSelectedItemProperty = AvaloniaProperty.Register<GridView, bool>(nameof (AutoScrollToSelectedItem), true);

        public bool AutoScrollToSelectedItem
        {
            get => GetValue(AutoScrollToSelectedItemProperty);
            set => SetValue(AutoScrollToSelectedItemProperty, value);
        }
        
        public static readonly DirectProperty<GridView, IEnumerable<GridColumnDefinition>> ColumnsProperty =
            AvaloniaProperty.RegisterDirect<GridView, IEnumerable<GridColumnDefinition>>(nameof(Columns), o => o.Columns, (o, v) => o.Columns = v);
        
        private IEnumerable<GridColumnDefinition> columns = new AvaloniaList<GridColumnDefinition>();
        
        public IEnumerable<GridColumnDefinition> Columns
        {
            get => columns;
            set => SetAndRaise(ColumnsProperty, ref columns, value);
        }
        
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<GridView, IDataTemplate>(nameof(ItemTemplate));
        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
        
        private Grid? header;
        private ListBox? listBox;

        public ListBox? ListBoxImpl => listBox;
        
        private ScrollViewer? headerScroll;
        private ScrollViewer? contentScroll;
        
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (headerScroll is { })
                headerScroll.ScrollChanged -= OnHeaderScrollChanged;
            if (contentScroll is { })
                contentScroll.ScrollChanged -= OnContentScrollChanged;
            
            base.OnApplyTemplate(e);
            header = e.NameScope.Get<Grid>("PART_header");
            listBox = e.NameScope.Get<ListBox>("PART_listbox");
            headerScroll = e.NameScope.Get<ScrollViewer>("PART_HeaderScroll");
            contentScroll = e.NameScope.Get<ScrollViewer>("PART_ContentScroll");
            
            headerScroll.ScrollChanged += OnHeaderScrollChanged;
            contentScroll.ScrollChanged += OnContentScrollChanged;

            header.ColumnDefinitions.Clear();
            header.Children.Clear();
            SetupGridColumns(header, true);
            
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
                splitter.Focusable = false;
                Grid.SetColumn(splitter, i++);
                header.Children.Add(splitter);
            }
            //last
            var lastCell = new GridViewColumnHeader() { ColumnName = "" };
            Grid.SetColumn(lastCell, i++);
            header.Children.Add(lastCell);

            BindFixMultiSelect();
        }

        private void OnContentScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (contentScroll is not null && headerScroll is not null && !MathUtilities.IsZero(e.OffsetDelta.X))
                headerScroll.Offset = headerScroll.Offset.WithX(contentScroll.Offset.X);
        }

        private void OnHeaderScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (contentScroll is not null && headerScroll is not null && !MathUtilities.IsZero(e.OffsetDelta.X))
                contentScroll.Offset = contentScroll.Offset.WithX(headerScroll.Offset.X);
        }

        private void BindFixMultiSelect()
        {
            if (listBox == null)
                return;
            
            handlersDisposable?.Dispose();
            handlersDisposable = null;
            /*
             * GridView default selection algorithm doesn't support both multiselect and drag and drop
             * because by default, as soon as you start to drag a selected group of items, everything is deselected
             * and this item is selected. This fixes that behaviour, using non public ListBox API, this can break
             * in future Avalonia versions.
             */
            if (SelectionMode.HasFlagFast(SelectionMode.Multiple))
            {
                handlersDisposable = listBox.AddDisposableHandler(PointerPressedEvent, (_, e) =>
                {
                    if (listBox.Selection.Count <= 0) 
                        return;

                    if (e.Source is not Visual source)
                        return;
                    
                    var point = e.GetCurrentPoint(source);

                    if (!point.Properties.IsLeftButtonPressed && !point.Properties.IsRightButtonPressed) 
                        return;
                    
                    var containerIndex = GetContainerIndexFromEventSource(listBox, e.Source as Interactive);

                    if (!containerIndex.HasValue)
                        return;
                                
                    var range = e.KeyModifiers.HasFlagFast(KeyModifiers.Shift);
                    var toggle = e.KeyModifiers.HasFlagFast(KeyModifiers.Control);
                    var isSelected = listBox.Selection.SelectedIndexes.Contains(containerIndex.Value);

                    if (isSelected || toggle || range)
                        e.Handled = true;
                }, RoutingStrategies.Tunnel);
                
                handlersDisposable = handlersDisposable.Combine(listBox.AddDisposableHandler(PointerReleasedEvent, (sender, e) =>
                {
                    if (listBox.Selection.Count <= 0) 
                        return;
                    
                    if (e.Source is not Visual source) 
                        return;

                    var point = e.GetCurrentPoint(source);

                    if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased || 
                        point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
                    {
                        var containerIndex = GetContainerIndexFromEventSource(listBox, e.Source as Interactive);

                        if (containerIndex.HasValue)
                        {
                            typeof(SelectingItemsControl).GetMethod("UpdateSelection",
                                    BindingFlags.NonPublic | BindingFlags.Instance,
                                    null,
                                    new []{typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(bool)},
                                    null)!
                                .Invoke(listBox, new object[]
                                {
                                    containerIndex.Value, true,
                                    e.KeyModifiers.HasFlagFast(KeyModifiers.Shift),
                                    e.KeyModifiers.HasFlagFast(KeyModifiers.Control),
                                    point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased
                                });
                            e.Handled = true;
                        }
                    }
                }, RoutingStrategies.Tunnel));   
            }
        }

        private IDisposable? handlersDisposable;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            BindFixMultiSelect();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            handlersDisposable?.Dispose();
            handlersDisposable = null;
        }

        protected static int? GetContainerIndexFromEventSource(ListBox listBox, Interactive? eventSource)
        {
            for (var current = eventSource as Visual; current != null; current = current.GetVisualParent())
            {
                if (current is Control control && ReferenceEquals(control.GetLogicalParent(), listBox))
                {
                    int? index = listBox.IndexFromContainer(control);
                    return index == -1 ? null : index;
                }
            }

            return null;
        }

        private Grid ConstructGrid()
        {
            Grid grid = new();
            SetupGridColumns(grid, false);
            return grid;
        }

        private void SetupGridColumns(Grid grid, bool isHeader)
        {
            int i = 0;
            foreach (var column in Columns)
            {
                var c = new ColumnDefinition(isHeader ? new GridLength(column.PreferedWidth, GridUnitType.Pixel) : default)
                {
                    SharedSizeGroup = $"col{(i++)}",
                    MinWidth = 10,
                };
                grid.ColumnDefinitions.Add(c);
                grid.ColumnDefinitions.Add(new ColumnDefinition(SplitterWidth, GridUnitType.Pixel));
            }

            // additional column in header makes it easier to resize
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star){MinWidth = 20});
        }

        static GridView()
        {
            ColumnsProperty.Changed.AddClassHandler<GridView>(OnColumnsModified);

            SelectionProperty.Changed.AddClassHandler<GridView>((view, args) =>
            {
                view.selectionDisposable?.Dispose();
                view.selectionDisposable = null;
                if (args.NewValue == null)
                    return;
                view.selectionDisposable = ((ISelectionModel)args.NewValue).ToObservable(o => o.SelectedItem).SubscribeAction(sel =>
                {
                    if (view.items == null)
                        return; // can happen when detaching view, we don't want to modify selected item then
                    view.RaisePropertyChanged(SelectedItemProperty, null, sel);
                });
            });
        }

        private static void OnColumnsModified(GridView gridView, AvaloniaPropertyChangedEventArgs args)
        {
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
                return;

            if (e.Key == Key.Space)
            {
                ListBox? list = ListBoxImpl;
                if (list?.SelectedItem == null)
                    return;

                var selected = list.ContainerFromIndex(list.SelectedIndex);

                if (selected == null)
                    return;

                var checkBox = selected.FindDescendantOfType<CheckBox>();

                if (checkBox != null)
                {
                    checkBox.IsChecked = !checkBox.IsChecked;
                    e.Handled = true;
                }
            }
        }

        public GridView()
        {
            SetCurrentValue(SelectionProperty, new SelectionModel<object>());
            SetCurrentValue(ItemTemplateProperty, new FuncDataTemplate(_ => true, (_, _) =>
            {
                var parent = ConstructGrid();
                int i = 0;
                foreach (var column in Columns)
                {
                    Control control;
                    if (column.DataTemplate is { } dt)
                    {
                        control = dt.Build(column) ?? new TextBlock(){Text = "Couldn't instantiate column"};
                    } 
                    else if (column.Checkable)
                    {
                        control = new CheckBox()
                        {
                            [!ToggleButton.IsCheckedProperty] = new Binding(column.Property),
                            IsEnabled = !column.IsReadOnly
                        };
                    }
                    else
                    {
                        var displayMember = column.Property;
                        control = new TextBlock()
                        {
                            [!TextBlock.TextProperty] = new Binding(displayMember)
                        };
                    }
                    
                    Grid.SetColumn((Control)control, 2 * i++);
                    parent.Children.Add(control);
                }
                return parent;
            }, true));
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
        
        private string columnName = "";
        
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Content]
        public string ColumnName
        {
            get => columnName;
            set => SetAndRaise(ColumnNameProperty, ref columnName, value);
        }
    }

    public class GridViewHeader : ContentControl
    {
        
    }
    
    public class GridColumnDefinition
    {
        public string Name { get; set; } = "";
        public string Property { get; set; } = "";
        public bool Checkable { get; set; }
        public bool IsReadOnly { get; set; }
        public int PreferedWidth { get; set; } = 70;
        public IDataTemplate? DataTemplate { get; set; }
    }
}