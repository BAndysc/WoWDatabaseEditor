using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit.Editing;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.MVVM.Utils;

namespace WDE.Common.Avalonia.Controls;

public class VirtualizedTreeView : TemplatedControl
{
    public static readonly StyledProperty<IReadOnlyList<INodeType>?> ItemsProperty = AvaloniaProperty.Register<VirtualizedTreeView, IReadOnlyList<INodeType>?>(nameof(Items));
    public static readonly StyledProperty<bool> IsFilteredProperty = AvaloniaProperty.Register<VirtualizedTreeView, bool>(nameof(IsFiltered));
    public static readonly StyledProperty<bool> RequestRenderProperty = AvaloniaProperty.Register<VirtualizedTreeView, bool>(nameof(RequestRender));
    public static readonly StyledProperty<INodeType?> SelectedNodeProperty = AvaloniaProperty.Register<VirtualizedTreeView, INodeType?>(nameof(SelectedNode), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<VirtualizedTreeView, float> RowHeightProperty = AvaloniaProperty.RegisterDirect<VirtualizedTreeView, float>(nameof(RowHeight), x => x.RowHeight, (x, v) => x.RowHeight = v);
    public static readonly StyledProperty<bool> NeverShrinkWidthProperty = AvaloniaProperty.Register<VirtualizedTreeView, bool>("NeverShrinkWidth");

    private float rowHeight = 28;

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

    public IReadOnlyList<INodeType>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public INodeType? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public bool NeverShrinkWidth
    {
        get => GetValue(NeverShrinkWidthProperty);
        set => SetValue(NeverShrinkWidthProperty, value);
    }

    public float RowHeight
    {
        get => rowHeight;
        set => SetAndRaise(RowHeightProperty, ref rowHeight, value);
    }
}

public class VirtualizedTreeViewPresenter : RenderedPanel, ILogicalScrollable
{
    public static readonly StyledProperty<IReadOnlyList<INodeType>?> ItemsProperty = AvaloniaProperty.Register<VirtualizedTreeViewPresenter, IReadOnlyList<INodeType>?>(nameof(Items));
    public static readonly StyledProperty<bool> IsFilteredProperty = AvaloniaProperty.Register<VirtualizedTreeViewPresenter, bool>(nameof(IsFiltered));
    public static readonly StyledProperty<bool> RequestRenderProperty = AvaloniaProperty.Register<VirtualizedTreeViewPresenter, bool>(nameof(RequestRender));
    public static readonly StyledProperty<INodeType?> SelectedNodeProperty = AvaloniaProperty.Register<VirtualizedTreeViewPresenter, INodeType?>(nameof(SelectedNode), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<VirtualizedTreeViewPresenter, float> RowHeightProperty = AvaloniaProperty.RegisterDirect<VirtualizedTreeViewPresenter, float>(nameof(RowHeight), x => x.RowHeight, (x, v) => x.RowHeight = v);
    public static readonly StyledProperty<bool> NeverShrinkWidthProperty = AvaloniaProperty.Register<VirtualizedTreeViewPresenter, bool>("NeverShrinkWidth");

    private float rowHeight = 28;
    private RecyclableViewList[] views;

    private bool pendingScrollToSelected;

    static VirtualizedTreeViewPresenter()
    {
        SelectedNodeProperty.Changed.AddClassHandler<VirtualizedTreeViewPresenter>((tree, args) =>
        {
            if (args.NewValue is INodeType node)
            {
                tree.pendingScrollToSelected = true;
                tree.InvalidateMeasure();
                tree.InvalidateArrange();
            }
        });
        ItemsProperty.Changed.AddClassHandler<VirtualizedTreeViewPresenter>((tree, args) => tree.ItemsChanged(args));
        AffectsArrange<VirtualizedTreeViewPresenter>(IsFilteredProperty);
        AffectsMeasure<VirtualizedTreeViewPresenter>(IsFilteredProperty);
        AffectsArrange<VirtualizedTreeViewPresenter>(RequestRenderProperty);
        AffectsMeasure<VirtualizedTreeViewPresenter>(RequestRenderProperty);
        AffectsArrange<VirtualizedTreeViewPresenter>(SelectedNodeProperty);
        FocusableProperty.OverrideDefaultValue<VirtualizedTreeViewPresenter>(true);

        AffectsMeasure<VirtualizedTreeViewPresenter>(BoundsProperty);
        AffectsArrange<VirtualizedTreeViewPresenter>(BoundsProperty);
    }

    public float RowHeight
    {
        get => rowHeight;
        set => SetAndRaise(RowHeightProperty, ref rowHeight, value);
    }

    public VirtualizedTreeViewPresenter()
    {
        views = new RecyclableViewList[0];
    }
    
    private void ItemsChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.OldValue is INotifyCollectionChanged old)
            old.CollectionChanged -= OldOnCollectionChanged;
        
        if (args.NewValue is INotifyCollectionChanged nnew)
            nnew.CollectionChanged += OldOnCollectionChanged;
    }

    private void OldOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateMeasure();
        InvalidateArrange();
        InvalidateVisual();
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Handled)
            return;
        
        if (e.Source is Visual source)
        {
            var point = e.GetCurrentPoint(source);

            if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source as Control,
                    true,
                    e.KeyModifiers.HasFlagFast(KeyModifiers.Shift),
                    e.KeyModifiers.HasFlagFast(GetPlatformCommandKey()),
                    point.Properties.IsRightButtonPressed,
                    e.ClickCount);
            }
        }
    }
    
    private static KeyModifiers GetPlatformCommandKey()
    {            
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return KeyModifiers.Meta;
        }

        return KeyModifiers.Control;
    }

    protected bool UpdateSelectionFromEventSource(Control? eventSource,
        bool select = true,
        bool rangeModifier = false,
        bool toggleModifier = false,
        bool rightButton = false, 
        int clickCount = 1)
    {
        var obj = eventSource?.DataContext;
        if (obj is IParentType parent)
        {
            // commented, because ConnectionListToolView handles DoubleTapped event
            // and it clashed with this code
            //if (clickCount == 2)
            //    parent.IsExpanded = !parent.IsExpanded;
            SetCurrentValue(SelectedNodeProperty, parent);
            InvalidateArrange();
        }
        else if (obj is INodeType child)
        {
            SetCurrentValue(SelectedNodeProperty, child);
            InvalidateArrange();
        }

        return false;
    }
    
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (SelectedNode == null || !SelectedNode.IsVisible)
        {
            if (e.Key == Key.Down)
            {
                SetCurrentValue(SelectedNodeProperty, Items?.FirstOrDefault(x => x.IsVisible));
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                SetCurrentValue(SelectedNodeProperty, Items?.LastOrDefault(x => x.IsVisible));
                e.Handled = true;
            }
            return;
        }
        
        int moveDownUp = 0;
        if (e.Key == Key.Space)
        {
            if (SelectedNode is IParentType parent)
            {
                parent.IsExpanded = !parent.IsExpanded;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Right)
        {
            if (SelectedNode is IParentType parent)
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
            var parentToCollapse = SelectedNode is IParentType {IsExpanded: true, CanBeExpanded: true} p ? p : SelectedNode.Parent;
            if (parentToCollapse != null && parentToCollapse.IsExpanded)
            {
                parentToCollapse.IsExpanded = false;
                SetCurrentValue(SelectedNodeProperty, parentToCollapse);
                e.Handled = true;
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
        InvalidateArrange();
    }

    private int GetNextIndex(IReadOnlyList<INodeType> items, int currentIndex, int direction)
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

    private void ScrollToNode(INodeType node)
    {
        if (Items is not { } items)
            return;

        var index = items.IndexOf(node);
        var rowRect = GetRowRect(index);

        if (rowRect == null)
            return;
        
        var startOffset = Offset.Y;
        var endOffset = startOffset + Viewport.Height;

        var visibleRect = new Rect(0, startOffset, Viewport.Width, Viewport.Height);

        if (visibleRect.Intersects(rowRect.Value))
            return;
        
        if (rowRect.Value.Bottom > endOffset)
            Offset = new Vector(Offset.X, rowRect.Value.Bottom - Viewport.Height);
        else if (rowRect.Value.Top < startOffset)
            Offset = new Vector(Offset.X, rowRect.Value.Top);
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

    protected override Size ArrangeOverride(Size finalSize)
    {
        var dataTemplates = this.FindAncestorOfType<VirtualizedTreeView>()?.DataTemplates ?? [];
        if (views.Length != dataTemplates.Count)
        {
            var oldViewsLength = views.Length;
            Array.Resize(ref views, dataTemplates.Count);
            for (int i = oldViewsLength; i < views.Length; ++i)
                views[i] = new RecyclableViewList(this);
        }

        for (var i = 0; i < views.Length; i++)
        {
            var template = dataTemplates[i];
            views[i].Reset(new FuncDataTemplate(template.Match, (_, _) => new VirtualizedTreeViewItem(){ContentTemplate = template}));
        }

        if (Items is not { } items)
        {
            for (int i = 0; i < views.Length; ++i)
                views[i].Finish();
            return finalSize;
        }
        
        // determine the first and last visible row
        var extentHeight = Math.Max(Extent.Height, finalSize.Height);
        var extentWidth = Math.Max(Extent.Width, finalSize.Width);
        var startOffset = Offset.Y;
        var endOffset = startOffset + Viewport.Height;
        var startIndex = Math.Max(0, (int)(Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + Viewport.Height / RowHeight + 2, Items.Count);
        var xOffset = -Offset.X;
        
        Renderer.Arrange(new Rect(-Offset.X, -Offset.Y, extentWidth, extentHeight));

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
                var rowRect = new Rect(xOffset, y - startOffset, extentWidth, RowHeight);
                var isSelected = SelectedNode == row;

                VirtualizedTreeViewItem? element = null;
                for (int i = 0; i < views.Length; ++i)
                {
                    if (dataTemplates[i].Match(row))
                    {
                        element = (VirtualizedTreeViewItem)views[i].GetNext(row);
                        break;
                    }
                }
                
                element ??= new VirtualizedTreeViewItem()
                {
                    DataContext = row,
                    ContentTemplate = new FuncDataTemplate(_ => true,
                        (_, _) => new TextBlock() { Text = "No template! Define <dataTemplates>" })
                };

                element.Measure(rowRect.Size);
                element.Arrange(rowRect);
                element.IsSelected = isSelected;
            }
            
            y += RowHeight;

            if (y >= endOffset)
                break;
        }
        
        for (int i = 0; i < views.Length; ++i)
            views[i].Finish();

        RaiseScrollInvalidated(EventArgs.Empty);

        if (pendingScrollToSelected && SelectedNode != null)
        {
            ScrollToNode(SelectedNode);
            pendingScrollToSelected = false;
        }

        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        if (Items == null)
            return;

        if (this.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer)
        {
            context.DrawText(
                new FormattedText("VirtualizedTreeViewPresenter must be wrapped in ScrollViewer!",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    14,
                    Brushes.Red)
                {
                    TextAlignment = TextAlignment.Left,
                    MaxTextWidth = Bounds.Width,
                    MaxTextHeight = Bounds.Height,
                    // TextWrapping.Wrap
                }, default);
            return;
        }

        context.FillRectangle(Brushes.Transparent, new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height));
    }

    //protected ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();

    protected double lastDesiredWidth = 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (CanHorizontallyScroll)
            availableSize = availableSize.WithWidth(double.PositiveInfinity);
        double desiredWidth = 0;
        foreach (var child in Children)
        {
            child.Measure(availableSize.WithHeight(RowHeight));
            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
        }

        if (NeverShrinkWidth)
            desiredWidth = Math.Max(lastDesiredWidth, desiredWidth);
        lastDesiredWidth = desiredWidth;

        var finalSize = new Size(0, 0);

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
            finalSize = new Size(desiredWidth, RowHeight * visibleCount);
        }
        else
            finalSize = new Size(desiredWidth, RowHeight * (Items?.Count ?? 0));

        Extent = finalSize;
        Viewport = Bounds.Size;
        return finalSize;
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

    public IReadOnlyList<INodeType>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public INodeType? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public bool NeverShrinkWidth
    {
        get => GetValue(NeverShrinkWidthProperty);
        set => SetValue(NeverShrinkWidthProperty, value);
    }

    public bool CanVerticallyScroll { get; set; }

    public bool IsLogicalScrollEnabled => true;

    public Size ScrollSize => new Size(rowHeight, rowHeight);

    public Size PageScrollSize => new Size(rowHeight, rowHeight * (int)((Viewport.Height + rowHeight - 1) / rowHeight));

    public bool BringIntoView(Control target, Rect targetRect)
    {
        return false;
    }

    public Control? GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return from;
    }

    public void RaiseScrollInvalidated(EventArgs e)
    {
        ScrollInvalidated?.Invoke(this, e);
    }

    private bool canHorizontallyScroll = true;

    public bool CanHorizontallyScroll
    {
        get => canHorizontallyScroll;
        set
        {
            if (canHorizontallyScroll != value)
            {
                canHorizontallyScroll = value;
                InvalidateMeasure();
            }
        }
    }

    public event EventHandler? ScrollInvalidated;

    private Size extent;
    public Size Extent
    {
        get => extent;
        set
        {
            if (extent != value)
            {
                extent = value;
                ScrollInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private Vector offset;
    public Vector Offset
    {
        get => offset;
        set
        {
            if (offset != value)
            {
                offset = value;
                ScrollInvalidated?.Invoke(this, EventArgs.Empty);
                InvalidateArrange();
            }
        }
    }

    private Size viewport;
    public Size Viewport
    {
        get => viewport;
        set
        {
            if (viewport != value)
            {
                viewport = value;
                ScrollInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
