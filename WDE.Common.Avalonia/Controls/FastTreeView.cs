using System;
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
    public static readonly StyledProperty<C?> SelectedSpawnProperty = AvaloniaProperty.Register<FastTreeView<P, C>, C?>(nameof(SelectedSpawn));

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
        var index = (int)(y / RowHeight);
        if (Items == null || index < 0 || index >= Items.Count)
            return null;
        return Items[index];
    }
        
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            var point = e.GetPosition(this);
            var obj = GetRowAtPosition(point.Y);
            if (obj is P parent)
            {
                parent.IsExpanded = !parent.IsExpanded;
            }
            else if (obj is C child)
            {
                SelectedSpawn = child;
            }
        }
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
        var startIndex = Math.Max(0, (int)(scrollViewer.Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + scrollViewer.Viewport.Height / RowHeight + 2, Items.Count);

        context.FillRectangle(Brushes.Transparent, new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height));
        
        var items = Items;
        var font = new Typeface(TextBlock.GetFontFamily(this));
        var foreground = TextBlock.GetForeground(this);
        var pen = new Pen(foreground, 2);

        double y = startIndex * RowHeight;
        for (int index = startIndex; index < endIndex; ++index)
        {
            var row = items[index];
            var rowRect = new Rect(0, y, actualWidth, RowHeight);

            DrawRow(font, pen, foreground, context, row, rowRect);
            
            y += RowHeight;
        }
    }

    protected abstract void DrawRow(Typeface typeface, Pen pen, IBrush foreground, DrawingContext context, object? row, Rect rect);
    
    protected void DrawToggleMark(Rect rect, DrawingContext context, IPen pen, bool isExpanded)
    {
        if (isExpanded)
        {
            context.DrawLine(pen, new Point(5, 7.5 + rect.Y), new Point(10, 13 + rect.Y));
            context.DrawLine(pen, new Point(9, 13 + rect.Y), new Point(14, 7.5 + rect.Y));
        }
        else
        {
            context.DrawLine(pen, new Point(7, 5 + rect.Y), new Point(13, 9 + rect.Y));
            context.DrawLine(pen, new Point(13, 9 + rect.Y), new Point(7, 13 + rect.Y));
        }
    }

    protected ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();

    private bool IsRowVisible(int row, ScrollViewer? scroll = null)
    {
        if (Items == null)
            return false;
        if (scroll == null)
            scroll = ScrollViewer;
        if (scroll == null)
            return false;
        var startIndex = Math.Max(0, (int)(scroll.Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + scroll.Viewport.Height / RowHeight + 2, Items.Count);
        return row >= startIndex && row < endIndex;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(availableSize.Width, RowHeight * (Items?.Count ?? 0));
    }

    public FlatTreeList<P, C>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public C? SelectedSpawn
    {
        get => GetValue(SelectedSpawnProperty);
        set => SetValue(SelectedSpawnProperty, value);
    }
}