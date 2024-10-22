using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Collections;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls;

public class VirtualizedGridViewItemPresenter : Panel
{
    private double itemHeight = 24;
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, double> ItemHeightProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, double>(nameof(ItemHeight), o => o.ItemHeight, (o, v) => o.ItemHeight = v);
    
    private IIndexedCollection<object>? items = IIndexedCollection<object>.Empty;
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, IIndexedCollection<object>?> ItemsProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, IIndexedCollection<object>?>(nameof(Items), o => o.Items, (o, v) => o.Items = v);
    
    private IMultiIndexContainer selection = new MultiIndexContainer();
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, IMultiIndexContainer> SelectionProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, IMultiIndexContainer>(nameof(Selection), o => o.Selection, (o, v) => o.Selection = v, new MultiIndexContainer(), BindingMode.OneWay);

    private IMultiIndexContainer checkedIndices = new MultiIndexContainer();
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, IMultiIndexContainer> CheckedIndicesProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, IMultiIndexContainer>(nameof(CheckedIndices), o => o.CheckedIndices, (o, v) => o.CheckedIndices = v, new MultiIndexContainer(), BindingMode.OneWay);

    private IEnumerable<GridColumnDefinition> columns = new AvaloniaList<GridColumnDefinition>();
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, IEnumerable<GridColumnDefinition>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, IEnumerable<GridColumnDefinition>>(nameof(Columns), o => o.Columns, (o, v) => o.Columns = v);

    public static readonly StyledProperty<int?> FocusedIndexProperty = AvaloniaProperty.Register<VirtualizedGridViewItemPresenter, int?>(nameof(FocusedIndex));
    
    private bool useCheckBoxes;
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, bool> UseCheckBoxesProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, bool>(nameof(UseCheckBoxes), o => o.UseCheckBoxes, (o, v) => o.UseCheckBoxes = v);

    private RecyclableViewList views;
    private IDataTemplate itemTemplate;
    private IDataTemplate containerTemplate;
    
    public VirtualizedGridViewItemPresenter()
    {
        views = new RecyclableViewList(this);
        itemTemplate = new FuncDataTemplate(_ => true, (_, _) =>
        {
            var parent = ConstructGrid();
            int i = 0;

            if (UseCheckBoxes)
            {
                Control checkbox = new VirtualizedGridCheckBox()
                {
                    [!VirtualizedGridCheckBox.CheckedIndicesProperty] = this[!CheckedIndicesProperty],
                    [!VirtualizedGridCheckBox.IndexProperty] = new Binding("$parent[1].Tag")
                };
                Grid.SetColumn(checkbox, 2 * i++);
                parent.Children.Add(checkbox);
            }
            
            foreach (var column in Columns)
            {
                Control control;
                if (column.DataTemplate is { } dt)
                {
                    control = dt.Build(column) ?? new TextBlock(){Text = "Error: data template for column returned null control"};
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
        }, true);
        containerTemplate = new FuncDataTemplate(_ => true, (_, _) => 
            new ListBoxItem()
        {
            ContentTemplate = itemTemplate,
            [!ContentControl.ContentProperty] = new Binding(".")
        }, true);
    }
    
    static VirtualizedGridViewItemPresenter()
    {
        AffectsArrange<VirtualizedGridViewItemPresenter>(FocusedIndexProperty);
        AffectsMeasure<VirtualizedGridViewItemPresenter>(ItemsProperty);
        AffectsArrange<VirtualizedGridViewItemPresenter>(ItemsProperty);
        FocusedIndexProperty.Changed.AddClassHandler<VirtualizedGridViewItemPresenter>((panel, e) =>
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
            DispatcherTimer.RunOnce(panel.AutoScrollToFocusedIndex, TimeSpan.FromMilliseconds(1));
        });
        ItemsProperty.Changed.AddClassHandler<VirtualizedGridViewItemPresenter>((panel, e) =>
        {
            if (panel.attachedToVisualTree)
            {
                panel.UnbindItems();
                panel.BindItems();
            }
        });
        SelectionProperty.Changed.AddClassHandler<VirtualizedGridViewItemPresenter>((panel, e) =>
        {
            panel.BindSelection();
        });
    }

    private IMultiIndexContainer? boundSelection;
    
    private void BindSelection()
    {
        UnbindSelection();
        if (selection != null && attachedToVisualTree)
        {
            boundSelection = selection;
            boundSelection.Cleared += OnSelectionCleared;
            boundSelection.Changed += OnSelectionChanged;
        }
    }

    private void OnSelectionChanged() => InvalidateArrange();

    private void UnbindSelection()
    {
        if (boundSelection != null)
        {
            boundSelection.Cleared -= OnSelectionCleared;
            boundSelection.Changed -= OnSelectionChanged;
            boundSelection = null;
        }
    }

    private void OnSelectionRemoved(int index) => InvalidateArrange();
    private void OnSelectionAdded(int index) => InvalidateArrange();
    private void OnSelectionCleared() => InvalidateArrange();

    private void AutoScrollToFocusedIndex()
    {
        if (ScrollViewer is not { } scrollViewer || !FocusedIndex.HasValue)
            return;
        
        var visibleRect = new Rect(new Point(scrollViewer.Offset.X, scrollViewer.Offset.Y), scrollViewer.Viewport);
        var itemRect = new Rect(0, FocusedIndex.Value * itemHeight, scrollViewer.Viewport.Width, itemHeight);

        if (visibleRect.Contains(itemRect))
            return;
        
        if (itemRect.Top < visibleRect.Top)
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, itemRect.Top);
        else
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, itemRect.Bottom - visibleRect.Height);
    }

    public double ItemHeight
    {
        get => itemHeight;
        set => SetAndRaise(ItemHeightProperty, ref itemHeight, value);
    }

    public IIndexedCollection<object>? Items
    {
        get => items;
        set => SetAndRaise(ItemsProperty, ref items, value);
    }

    #pragma warning disable AVP1030
    public int? FocusedIndex
    {
        get
        {
            var index = GetValue(FocusedIndexProperty);
            if (!IsIndexValid(index))
                return null;
            return index;
        }
        set => SetValue(FocusedIndexProperty, value);
    }
    #pragma warning restore AVP1030

    public IMultiIndexContainer Selection
    {
        get => selection;
        set => SetAndRaise(SelectionProperty, ref selection, value);
    }
    
    public bool UseCheckBoxes
    {
        get => useCheckBoxes;
        set => SetAndRaise(UseCheckBoxesProperty, ref useCheckBoxes, value);
    }

    public IMultiIndexContainer CheckedIndices
    {
        get => checkedIndices;
        set => SetAndRaise(CheckedIndicesProperty, ref checkedIndices, value);
    }
    
    public IEnumerable<GridColumnDefinition> Columns
    {
        get => columns;
        set => SetAndRaise(ColumnsProperty, ref columns, value);
    }
    
    private IIndexedCollection<object>? subscribedItems;

    private bool attachedToVisualTree;
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        attachedToVisualTree = true;
        if (ScrollViewer is { } sc)
        {
            visualTreeSubscription = sc
                .GetObservable(ScrollViewer.OffsetProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .ObserveOn(AvaloniaScheduler.Instance)
                .SubscribeAction(_ =>
            {
                InvalidateArrange();
            });
        }
        
        BindItems();
        BindSelection();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnbindItems();
        UnbindSelection();
        visualTreeSubscription.Dispose();
        visualTreeSubscription = Disposable.Empty;
        attachedToVisualTree = false;
    }

    private void BindItems()
    {
        if (subscribedItems != null)
            UnbindItems();

        if (items != null)
        {
            items.OnCountChanged += OnCountChange;
            subscribedItems = items;
        }
    }

    private void UnbindItems()
    {
        if (subscribedItems != null)
        {
            subscribedItems.OnCountChanged -= OnCountChange;
            subscribedItems = null;
        }
    }
    
    private void OnCountChange()
    {
        InvalidateArrange();
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(availableSize.Width, itemHeight * (items?.Count ?? 0));
    }
    
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (items == null)
            return default;
        
        var scrollViewer = ScrollViewer;
        if (scrollViewer == null)
            return default;
        
        var scrollOffset = scrollViewer.Offset.Y;
        var scrollHeight = scrollViewer.Viewport.Height;
    
        var firstVisibleItem = Math.Max(0, (int)(scrollOffset / itemHeight));
        var lastVisibleItem = Math.Min(items.Count - 1, (int)((scrollOffset + scrollHeight) / itemHeight));
    
        views.Reset(containerTemplate);
        var selectionIterator = selection.ContainsIterator;
        for (int i = firstVisibleItem; i <= lastVisibleItem; i++)
        {
            var dataContext = items[i];
            var child = views.GetNext(dataContext);
            var rect = new Rect(0, i * itemHeight, finalSize.Width, itemHeight);
            ((ListBoxItem)child).IsSelected = selectionIterator.Contains(i);
            ((ListBoxItem)child).Tag = i;
            child.Measure(rect.Size);
            child.Arrange(rect);
        }
        views.Finish();
        return finalSize;
    }
    
    private Grid ConstructGrid()
    {
        Grid grid = new();
        SetupGridColumns(Columns, grid, false, UseCheckBoxes);
        return grid;
    }

    internal static void SetupGridColumns(IEnumerable<GridColumnDefinition> columns, Grid grid, bool isHeader, bool addCheckboxColumn)
    {
        int i = 0;
        void AddColumn(int preferredWidth)
        {
            var c = new ColumnDefinition(isHeader ? new GridLength(preferredWidth, GridUnitType.Pixel) : default)
            {
                SharedSizeGroup = $"col{(i++)}",
                MinWidth = 10,
            };
            grid.ColumnDefinitions.Add(c);
            AddSplitter(grid);
        }

        if (addCheckboxColumn)
        {
            AddColumn(50);
        }
        
        foreach (var column in columns)
            AddColumn(column.PreferedWidth);

        // last
        var c = new ColumnDefinition(isHeader ? new GridLength(1, GridUnitType.Star) : default)
        {
            SharedSizeGroup = $"col{(i++)}",
            MinWidth = 20,
        };
        grid.ColumnDefinitions.Add(c);
    }

    internal static void AddSplitter(Grid grid)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridView.SplitterWidth, GridUnitType.Pixel));
    }

    private bool IsIndexValid(int? index) => index != null && index >= 0 && index < items?.Count;
    
    private IDisposable visualTreeSubscription = Disposable.Empty;
    ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();
}