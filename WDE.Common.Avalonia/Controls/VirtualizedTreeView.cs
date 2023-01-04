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

public class VirtualizedTreeView : Panel
{
    public static readonly StyledProperty<IReadOnlyList<INodeType>?> ItemsProperty = AvaloniaProperty.Register<VirtualizedTreeView, IReadOnlyList<INodeType>?>(nameof(Items));
    public static readonly StyledProperty<bool> IsFilteredProperty = AvaloniaProperty.Register<VirtualizedTreeView, bool>(nameof(IsFiltered));
    public static readonly StyledProperty<bool> RequestRenderProperty = AvaloniaProperty.Register<VirtualizedTreeView, bool>(nameof(RequestRender));
    public static readonly StyledProperty<INodeType?> SelectedNodeProperty = AvaloniaProperty.Register<VirtualizedTreeView, INodeType?>(nameof(SelectedNode), defaultBindingMode: BindingMode.TwoWay);
    
    public const float RowHeight = 28;

    private RecyclableViewList[] views;

    static VirtualizedTreeView()
    {
        SelectedNodeProperty.Changed.AddClassHandler<VirtualizedTreeView>((tree, args) =>
        {
            if (args.NewValue is INodeType node)
                tree.ScrollToNode(node);
        });
        ItemsProperty.Changed.AddClassHandler<VirtualizedTreeView>((tree, args) => tree.ItemsChanged(args));
        AffectsArrange<VirtualizedTreeView>(IsFilteredProperty);
        AffectsMeasure<VirtualizedTreeView>(IsFilteredProperty);
        AffectsArrange<VirtualizedTreeView>(RequestRenderProperty);
        AffectsMeasure<VirtualizedTreeView>(RequestRenderProperty);
        AffectsArrange<VirtualizedTreeView>(SelectedNodeProperty);
        FocusableProperty.OverrideDefaultValue<VirtualizedTreeView>(true);
    }

    public VirtualizedTreeView()
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
            SelectedNode = parent;
            InvalidateArrange();
        }
        else if (obj is INodeType child)
        {
            SelectedNode = child;
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
                SelectedNode = Items?.FirstOrDefault(x => x.IsVisible);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                SelectedNode = Items?.LastOrDefault(x => x.IsVisible);
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
                SelectedNode = parentToCollapse;
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
                SelectedNode = items[nextIndex];

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

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (views.Length != DataTemplates.Count)
        {
            var oldViewsLength = views.Length;
            Array.Resize(ref views, DataTemplates.Count);
            for (int i = oldViewsLength; i < views.Length; ++i)
                views[i] = new RecyclableViewList(this);
        }

        for (var i = 0; i < views.Length; i++)
        {
            var template = DataTemplates[i];
            views[i].Reset(new FuncDataTemplate(template.Match, (_, _) => new VirtualizedTreeViewItem(){ContentTemplate = template}));
        }

        if (ScrollViewer is not { } scrollViewer || Items is not { } items)
        {
            for (int i = 0; i < views.Length; ++i)
                views[i].Finish();
            return finalSize;
        }
        
        // determine the first and last visible row
        var actualWidth = finalSize.Width;
        var startOffset = scrollViewer.Offset.Y;
        var endOffset = startOffset + scrollViewer.Viewport.Height;
        var startIndex = Math.Max(0, (int)(scrollViewer.Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + scrollViewer.Viewport.Height / RowHeight + 2, Items.Count);
        
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
                var isSelected = SelectedNode == row;

                VirtualizedTreeViewItem? element = null;
                for (int i = 0; i < views.Length; ++i)
                {
                    if (DataTemplates[i].Match(row))
                    {
                        element = (VirtualizedTreeViewItem)views[i].GetNext(row);
                        break;
                    }
                }
                
                element ??= new VirtualizedTreeViewItem()
                {
                    DataContext = row,
                    ContentTemplate = new FuncDataTemplate(_ => true,
                        (_, _) => new TextBlock() { Text = "No template! Define <DataTemplates>" })
                };

                element.Arrange(rowRect);
                element.IsSelected = isSelected;
            }
            
            y += RowHeight;

            if (y >= endOffset)
                break;
        }
        
        for (int i = 0; i < views.Length; ++i)
            views[i].Finish();

        return finalSize;
    }

    private IDisposable? visualTreeSubscription;
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        if (ScrollViewer is { } sc)
        {
            visualTreeSubscription = new CompositeDisposable(
                sc.GetObservable(ScrollViewer.OffsetProperty).SubscribeAction(_ =>
                {
                    InvalidateArrange();
                }),
                sc.GetObservable(BoundsProperty).SubscribeAction(_ =>
                {
                    InvalidateArrange();
                }));
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        visualTreeSubscription?.Dispose();
        visualTreeSubscription = null;
        base.OnDetachedFromVisualTree(e);
    }
    
    // @fixme avalonia 11
    public void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Items == null)
            return;
        
        if (ScrollViewer is not { } scrollViewer)
        {
            context.DrawText(
                new FormattedText("VirtualizedTreeView must be wrapped in ScrollViewer!", 
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

    protected ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();

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
}