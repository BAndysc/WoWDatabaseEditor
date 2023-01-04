using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public abstract class FastTreeView<P, C> : Control where P : IParentType where C : IChildType
{
    protected static SolidColorBrush HoverRowBackground = new SolidColorBrush(Color.FromRgb(87, 124, 219));
    protected static SolidColorBrush SelectedRowBackground = new SolidColorBrush(Color.FromRgb(87, 124, 219));
    #pragma warning disable AVP1002
    public static readonly StyledProperty<FlatTreeList<P, C>?> ItemsProperty = AvaloniaProperty.Register<FastTreeView<P, C>, FlatTreeList<P, C>?>(nameof(Items));
    public static readonly StyledProperty<bool> IsFilteredProperty = AvaloniaProperty.Register<FastTreeView<P, C>, bool>(nameof(IsFiltered));
    public static readonly StyledProperty<bool> RequestRenderProperty = AvaloniaProperty.Register<FastTreeView<P, C>, bool>(nameof(RequestRender));
    public static readonly StyledProperty<INodeType?> SelectedNodeProperty = AvaloniaProperty.Register<FastTreeView<P, C>, INodeType?>(nameof(SelectedNode), defaultBindingMode: BindingMode.TwoWay);
    #pragma warning restore AVP1002
    
    public const float RowHeight = 24;
    public const float Indent = 24;
    
    static FastTreeView()
    {
        ResourcesUtils.Get("FastTableView.SelectedRowBackground", HoverRowBackground, out HoverRowBackground);
        ResourcesUtils.Get("FastTableView.HoverRowBackground", HoverRowBackground, out HoverRowBackground);
        SelectedNodeProperty.Changed.AddClassHandler<FastTreeView<P, C>>((tree, args) =>
        {
            if (args.NewValue is INodeType node)
                tree.ScrollToNode(node);
        });
        ItemsProperty.Changed.AddClassHandler<FastTreeView<P, C>>((tree, args) => tree.ItemsChanged(args));
        AffectsRender<FastTreeView<P, C>>(IsFilteredProperty);
        AffectsMeasure<FastTreeView<P, C>>(IsFilteredProperty);
        AffectsRender<FastTreeView<P, C>>(RequestRenderProperty);
        AffectsMeasure<FastTreeView<P, C>>(RequestRenderProperty);
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
        Dispatcher.UIThread.Post(() =>
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual(); 
        });
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
            SetCurrentValue(SelectedNodeProperty, parent);
            InvalidateVisual();
        }
        else if (obj is C child)
        {
            SetCurrentValue(SelectedNodeProperty, child);
            InvalidateVisual();
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
                SetCurrentValue(SelectedNodeProperty, items[nextIndex]);

            e.Handled = true;
        }
        InvalidateVisual();
    }

    private int GetNextIndex(FlatTreeList<P, C> items, int currentIndex, int direction)
    {
        currentIndex += direction;
        if (IsFiltered)
        {
            while (currentIndex + direction >= 0 && currentIndex + direction < items.Count && !items[currentIndex].IsVisible)
                currentIndex += direction;
        }
        if (currentIndex < 0 || currentIndex >= items.Count || !items[currentIndex].IsVisible)
            currentIndex = -1;
        return currentIndex;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        MouseOverRow = GetRowAtPosition(point.Y);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        MouseOverRow = null;
    }

    private void ScrollToNode(INodeType node)
    {
        if (Items is not { } items)
            return;
        
        if (ScrollViewer is not { } scrollViewer)
            return;

        var index = items.IndexOf(node);
        var rowRect = GetRowRect(index);

        if (rowRect == null)
            return;
        
        var startOffset = scrollViewer.Offset.Y;
        var endOffset = startOffset + scrollViewer.Viewport.Height;

        var visibleRect = new Rect(0, startOffset, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height);

        if (visibleRect.Intersects(rowRect.Value))
            return;
        
        if (rowRect.Value.Bottom > endOffset)
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, rowRect.Value.Bottom - scrollViewer.Viewport.Height);
        else if (rowRect.Value.Top < startOffset)
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, rowRect.Value.Top);
    }

    private Rect? GetRowRect(int index)
    {
        if (Items is not { } items)
            return default;
        
        if (IsFiltered)
        {
            float y = 0;
            for (int i = 0; i < items.Count; ++i)
            {
                var row = items[i];

                if (i == index)
                {
                    if (row.IsVisible)
                        return new Rect(0, y, Bounds.Width, RowHeight);
                    return null;
                }
                
                if (!row.IsVisible)
                    continue;

                y += RowHeight;
            }

            return null;
        }
        else
        {
            if (index < 0 || index >= items.Count)
                return null;
            
            return new Rect(0, index * RowHeight, Bounds.Width, RowHeight);
        }
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Items == null)
            return;
        
        var actualWidth = Bounds.Width;
        
        if (ScrollViewer is not { } scrollViewer)
        {
            var ft = new FormattedText("FastTreeView must be wrapped in ScrollViewer!",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                14,
                Brushes.Red)
            {
                MaxTextWidth = Bounds.Width
            };
            context.DrawText(ft, default);
            return;
        }
        
        // determine the first and last visible row
        var startOffset = scrollViewer.Offset.Y;
        var endOffset = startOffset + scrollViewer.Viewport.Height;
        var startIndex = Math.Max(0, (int)(scrollViewer.Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + scrollViewer.Viewport.Height / RowHeight + 2, Items.Count);

        context.FillRectangle(Brushes.Transparent, new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height));
        
        var items = Items;
        var font = new Typeface(TextElement.GetFontFamily(this));
        var foreground = TextElement.GetForeground(this);
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

                DrawRow(font, pen, foreground ?? Brushes.Red, context, row, rowRect);
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
    
    public bool RequestRender
    {
        get => GetValue(RequestRenderProperty);
        set => SetValue(RequestRenderProperty, value);
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