using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public abstract class FastTreeView<P, C> : Control where P : IParentType where C : IChildType
{
    protected static SolidColorBrush HoverRowBackground = new SolidColorBrush(Color.FromRgb(87, 124, 219));
    protected static SolidColorBrush SelectedRowBackground = new SolidColorBrush(Color.FromRgb(87, 124, 219));
    public static readonly StyledProperty<FlatTreeList<P, C>?> ItemsProperty = AvaloniaProperty.Register<FastTreeView<P, C>, FlatTreeList<P, C>?>(nameof(Items));
    public static readonly StyledProperty<bool> IsFilteredProperty = AvaloniaProperty.Register<FastTreeView<P, C>, bool>(nameof(IsFiltered));
    public static readonly StyledProperty<INodeType?> SelectedNodeProperty = AvaloniaProperty.Register<FastTreeView<P, C>, INodeType?>(nameof(SelectedNode));

    public const float RowHeight = 20;
    public const float Indent = 20;

    private static bool GetResource<T>(string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        if (Application.Current.Styles.TryGetResource(key, out var res) && res is T t)
        {
            outT = t;
            return true;
        }
        return false;
    }
    
    static FastTreeView()
    {
        GetResource("FastTableView.SelectedRowBackground", HoverRowBackground, out HoverRowBackground);
        GetResource("FastTableView.HoverRowBackground", HoverRowBackground, out HoverRowBackground);
        ItemsProperty.Changed.AddClassHandler<FastTreeView<P, C>>((tree, args) => tree.ItemsChanged(args));
        AffectsRender<FastTreeView<P, C>>(IsFilteredProperty);
        AffectsMeasure<FastTreeView<P, C>>(IsFilteredProperty);
        FocusableProperty.OverrideDefaultValue<FastTreeView<P, C>>(true);
    }

    private void ItemsChanged(AvaloniaPropertyChangedEventArgs args)
    {
        var old = (FlatTreeList<P,C>?)args.OldValue;
        var nnew = (FlatTreeList<P,C>?)args.NewValue;
        
        if (old != null)
            old.CollectionChanged -= OldOnCollectionChanged;
        
        if (nnew != null)
            nnew.CollectionChanged += OldOnCollectionChanged;
    }

    private void OldOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateMeasure();
        InvalidateArrange();
        InvalidateVisual();
    }

    protected object? mouseOverRow;

    private object? MouseOverRow
    {
        get => mouseOverRow;
        set
        {
            if (mouseOverRow == value)
                return;
            mouseOverRow = value;
            InvalidateVisual();
        }
    }
    
    private object? GetRowAtPosition(double y)
    {
        if (IsFiltered)
        {
            var items = Items;
            float _y = 0;
            if (items != null)
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    if (!items[i].IsVisible)
                        continue;
                    if (y >= _y && y < _y + RowHeight)
                        return items[i];
                    _y += RowHeight;
                }
            }

            return null;
        }
        else
        {
            var index = (int)(y / RowHeight);
            if (Items == null || index < 0 || index >= Items.Count)
                return null;
            return Items[index];   
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        var currentPoint = e.GetCurrentPoint(this);
        var obj = GetRowAtPosition(currentPoint.Position.Y);
        if (obj is P parent)
        {
            if (currentPoint.Position.X <= Indent * parent.NestLevel || e.ClickCount == 2)
                parent.IsExpanded = !parent.IsExpanded;
            SelectedNode = parent;
        }
        else if (obj is C child)
        {
            SelectedNode = child;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (SelectedNode == null)
            return;
        
        int moveDownUp = 0;
        if (e.Key == Key.Space || e.Key == Key.Enter)
        {
            if (SelectedNode is P parent)
            {
                parent.IsExpanded = !parent.IsExpanded;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Right)
        {
            if (SelectedNode is P parent)
            {
                if (!parent.IsExpanded)
                {
                    parent.IsExpanded = true;
                    e.Handled = true;
                }
                else
                    moveDownUp = 1;
            }
            else
                moveDownUp = 1;
        }
        else if (e.Key == Key.Left)
        {
            if (SelectedNode is P parent)
            {
                if (parent.IsExpanded)
                {
                    parent.IsExpanded = false;
                    e.Handled = true;
                }
                else
                    moveDownUp = -1;
            }
            else
                moveDownUp = -1;
        }
        else if (e.Key == Key.Up)
            moveDownUp = -1;
        else if (e.Key == Key.Down)
            moveDownUp = 1;

        if (moveDownUp != 0)
        {
            var items = Items;
            if (items == null)
                return;

            var currentIndex = items.IndexOf(SelectedNode);
            int nextIndex = GetNextIndex(items, currentIndex, moveDownUp);
            if (nextIndex != -1)
                SelectedNode = items[nextIndex];

            e.Handled = true;
        }
    }

    private int GetNextIndex(FlatTreeList<P, C> items, int currentIndex, int direction)
    {
        currentIndex += direction;
        if (IsFiltered)
        {
            while (currentIndex + direction >= 0 && currentIndex + direction < items.Count && !items[currentIndex].IsVisible)
                currentIndex += direction;
        }
        if (currentIndex < 0 || currentIndex >= items.Count)
            currentIndex = -1;
        return currentIndex;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        MouseOverRow = GetRowAtPosition(point.Y);
    }

    protected override void OnPointerLeave(PointerEventArgs e)
    {
        base.OnPointerLeave(e);
        MouseOverRow = null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Items == null)
            return;
        
        var actualWidth = Bounds.Width;

        var scrollViewer = this.FindAncestorOfType<ScrollViewer>();

        // determine the first and last visible row
        var startOffset = scrollViewer.Offset.Y;
        var endOffset = startOffset + scrollViewer.Viewport.Height;
        var startIndex = Math.Max(0, (int)(scrollViewer.Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + scrollViewer.Viewport.Height / RowHeight + 2, Items.Count);

        context.FillRectangle(Brushes.Transparent, new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height));
        
        var items = Items;
        var font = new Typeface(TextBlock.GetFontFamily(this));
        var foreground = TextBlock.GetForeground(this);
        var pen = new Pen(foreground, 2);

        var hasFilter = IsFiltered;
        if (hasFilter)
        {
            startIndex = 0;
            endIndex = items.Count;
        }
        
        double y = startIndex * RowHeight;
        for (int index = startIndex; index < endIndex; ++index)
        {
            var row = items[index];
            if (hasFilter && !row.IsVisible)
                continue;
            
            if (y + RowHeight >= startOffset)
            {
                var rowRect = new Rect(0, y, actualWidth, RowHeight);

                DrawRow(font, pen, foreground, context, row, rowRect);
            }
            
            y += RowHeight;

            if (y >= endOffset)
                break;
        }
    }

    protected abstract void DrawRow(Typeface typeface, Pen pen, IBrush foreground, DrawingContext context, object? row, Rect rect);
    
    protected void DrawToggleMark(Rect rect, DrawingContext context, IPen pen, bool isExpanded)
    {
        if (isExpanded)
        {
            context.DrawLine(pen, new Point(5 + rect.X, 7.5 + rect.Y), new Point(10 + rect.X, 13 + rect.Y));
            context.DrawLine(pen, new Point(9 + rect.X, 13 + rect.Y), new Point(14 + rect.X, 7.5 + rect.Y));
        }
        else
        {
            context.DrawLine(pen, new Point(7 + rect.X, 5 + rect.Y), new Point(13 + rect.X, 9 + rect.Y));
            context.DrawLine(pen, new Point(13 + rect.X, 9 + rect.Y), new Point(7 + rect.X, 13 + rect.Y));
        }
    }

    protected ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();

    // private bool IsRowVisible(int row, ScrollViewer? scroll = null)
    // {
    //     if (Items == null)
    //         return false;
    //     if (scroll == null)
    //         scroll = ScrollViewer;
    //     if (scroll == null)
    //         return false;
    //     var startIndex = Math.Max(0, (int)(scroll.Offset.Y / RowHeight) - 1);
    //     var endIndex = Math.Min(startIndex + scroll.Viewport.Height / RowHeight + 2, Items.Count);
    //     return row >= startIndex && row < endIndex;
    // }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsFiltered)
        {
            int visibleCount = 0;
            var items = Items;
            if (items != null)
            {
                for (int i = 0; i < items.Count; ++i)
                    if (items[i].IsVisible)
                        visibleCount++;
            }
            return new Size(availableSize.Width, RowHeight * visibleCount);
        }
        else
            return new Size(availableSize.Width, RowHeight * (Items?.Count ?? 0));
    }

    public bool IsFiltered
    {
        get => GetValue(IsFilteredProperty);
        set => SetValue(IsFilteredProperty, value);
    }

    public FlatTreeList<P, C>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
    
    public INodeType? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }
}