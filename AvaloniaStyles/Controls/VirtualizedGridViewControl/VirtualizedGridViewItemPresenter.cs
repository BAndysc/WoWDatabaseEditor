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
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Collections;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls;

public class VirtualizedGridViewItemPresenter : Panel
{
    private double itemHeight = 24;
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, double> ItemHeightProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, double>(nameof(ItemHeight), o => o.ItemHeight, (o, v) => o.ItemHeight = v);
    
    private IIndexedCollection<object>? items = IIndexedCollection<object>.Empty;
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, IIndexedCollection<object>?> ItemsProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, IIndexedCollection<object>?>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

    private int? selectedIndex;
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, int?> SelectedIndexProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, int?>(nameof(SelectedIndex), o => o.SelectedIndex, (o, v) => o.SelectedIndex = v);

    private IEnumerable<GridColumnDefinition> columns = new AvaloniaList<GridColumnDefinition>();
    public static readonly DirectProperty<VirtualizedGridViewItemPresenter, IEnumerable<GridColumnDefinition>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<VirtualizedGridViewItemPresenter, IEnumerable<GridColumnDefinition>>(nameof(Columns), o => o.Columns, (o, v) => o.Columns = v);

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
            foreach (var column in Columns)
            {
                IControl control;
                if (column.DataTemplate is { } dt)
                {
                    control = dt.Build(column);
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
        AffectsArrange<VirtualizedGridViewItemPresenter>(SelectedIndexProperty);
        AffectsMeasure<VirtualizedGridViewItemPresenter>(ItemsProperty);
        AffectsArrange<VirtualizedGridViewItemPresenter>(ItemsProperty);
        SelectedIndexProperty.Changed.AddClassHandler<VirtualizedGridViewItemPresenter>((panel, e) => panel.AutoScrollToSelectedIndex());
        ItemsProperty.Changed.AddClassHandler<VirtualizedGridViewItemPresenter>((panel, e) =>
        {
            if (panel.attachedToVisualTree)
            {
                panel.UnbindItems();
                panel.BindItems();
            }
        });
    }

    private void AutoScrollToSelectedIndex()
    {
        if (ScrollViewer is not { } scrollViewer || !selectedIndex.HasValue)
            return;
        
        var visibleRect = new Rect(new Point(scrollViewer.Offset.X, scrollViewer.Offset.Y), scrollViewer.Viewport);
        var itemRect = new Rect(0, selectedIndex.Value * itemHeight, scrollViewer.Viewport.Width, itemHeight);

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
    
    public int? SelectedIndex
    {
        get => selectedIndex;
        set => SetAndRaise(SelectedIndexProperty, ref selectedIndex, value);
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
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnbindItems();
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
        for (int i = firstVisibleItem; i <= lastVisibleItem; i++)
        {
            var dataContext = items[i];
            var child = views.GetNext(dataContext);
            var rect = new Rect(0, i * itemHeight, finalSize.Width, itemHeight);
            ((ListBoxItem)child).IsSelected = selectedIndex == i;
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
        SetupGridColumns(Columns, grid, false);
        return grid;
    }

    internal static void SetupGridColumns(IEnumerable<GridColumnDefinition> columns, Grid grid, bool isHeader)
    {
        int i = 0;
        foreach (var column in columns)
        {
            var c = new ColumnDefinition(isHeader ? new GridLength(column.PreferedWidth, GridUnitType.Pixel) : default)
            {
                SharedSizeGroup = $"col{(i++)}",
                MinWidth = 10,
            };
            grid.ColumnDefinitions.Add(c);
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridView.SplitterWidth, GridUnitType.Pixel));
        }
    }
    
    private IDisposable visualTreeSubscription = Disposable.Empty;
    ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();
}