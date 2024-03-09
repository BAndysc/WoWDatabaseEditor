using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace WDE.DatabaseEditors.Avalonia.Views.Template;

public class HorizontalGridFillPanel : Panel, INavigableContainer
{
    /// <summary>
    /// Defines the <see cref="Spacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SpacingProperty =
        StackLayout.SpacingProperty.AddOwner<HorizontalGridFillPanel>();

    public static readonly StyledProperty<double> ElementWidthProperty =
        AvaloniaProperty.Register<HorizontalGridFillPanel, double>(nameof(ElementWidth));
    
    public static readonly StyledProperty<double> LastElementMinWidthProperty =
        AvaloniaProperty.Register<HorizontalGridFillPanel, double>(nameof(LastElementMinWidth));

    /// <summary>
    /// Initializes static members of the <see cref="HorizontalGridFillPanel"/> class.
    /// </summary>
    static HorizontalGridFillPanel()
    {
        AffectsMeasure<HorizontalGridFillPanel>(SpacingProperty);
        AffectsMeasure<HorizontalGridFillPanel>(ElementWidthProperty);
        AffectsMeasure<HorizontalGridFillPanel>(LastElementMinWidthProperty);
    }

    /// <summary>
    /// Gets or sets the size of the spacing to place between child controls.
    /// </summary>
    public double Spacing
    {
        get { return GetValue(SpacingProperty); }
        set { SetValue(SpacingProperty, value); }
    }

    public double ElementWidth
    {
        get { return GetValue(ElementWidthProperty); }
        set { SetValue(ElementWidthProperty, value); }
    }

    public double LastElementMinWidth
    {
        get { return GetValue(LastElementMinWidthProperty); }
        set { SetValue(LastElementMinWidthProperty, value); }
    }

    /// <summary>
    /// Gets the next control in the specified direction.
    /// </summary>
    /// <param name="direction">The movement direction.</param>
    /// <param name="from">The control from which movement begins.</param>
    /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
    /// <returns>The control.</returns>
    IInputElement INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var result = GetControlInDirection(direction, (from as Control)!);

        if (result == null && wrap)
        {
            switch (direction)
            {
                case NavigationDirection.Left:
                case NavigationDirection.Previous:
                case NavigationDirection.PageUp:
                    result = GetControlInDirection(NavigationDirection.Last, null);
                    break;
                case NavigationDirection.Right:
                case NavigationDirection.Next:
                case NavigationDirection.PageDown:
                    result = GetControlInDirection(NavigationDirection.First, null);
                    break;
            }
        }

        return result!;
    }

    /// <summary>
    /// Gets the next control in the specified direction.
    /// </summary>
    /// <param name="direction">The movement direction.</param>
    /// <param name="from">The control from which movement begins.</param>
    /// <returns>The control.</returns>
    protected virtual IInputElement? GetControlInDirection(NavigationDirection direction, Control? from)
    {
        var horiz = true;
        int index = from != null ? Children.IndexOf(from) : -1;

        switch (direction)
        {
            case NavigationDirection.First:
                index = 0;
                break;
            case NavigationDirection.Last:
                index = Children.Count - 1;
                break;
            case NavigationDirection.Next:
                if (index != -1) ++index;
                break;
            case NavigationDirection.Previous:
                if (index != -1) --index;
                break;
            case NavigationDirection.Left:
                if (index != -1) index = horiz ? index - 1 : -1;
                break;
            case NavigationDirection.Right:
                if (index != -1) index = horiz ? index + 1 : -1;
                break;
            case NavigationDirection.Up:
                if (index != -1) index = horiz ? -1 : index - 1;
                break;
            case NavigationDirection.Down:
                if (index != -1) index = horiz ? -1 : index + 1;
                break;
            default:
                index = -1;
                break;
        }

        if (index >= 0 && index < Children.Count)
        {
            return Children[index];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// General StackFillPanel layout behavior is to grow unbounded in the "stacking" direction (Size To Content).
    /// Children in this dimension are encouraged to be as large as they like.  In the other dimension,
    /// StackFillPanel will assume the maximum size of its children.
    /// </summary>
    /// <param name="availableSize">Constraint</param>
    /// <returns>Desired size</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size stackDesiredSize = new Size();
        var children = Children;
        Size layoutSlotSize = availableSize;
        double spacing = Spacing;
        bool hasVisibleChild = false;

        var normalElements = Math.Max(0, Children.Count - 1);
        
        //
        // Initialize child sizing and iterator data
        // Allow children as much size as they want along the stack.
        layoutSlotSize = layoutSlotSize.WithWidth(Double.PositiveInfinity);

        //
        //  Iterate through children.
        //  While we still supported virtualization, this was hidden in a child iterator (see source history).
        //
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            // Get next child.
            var child = children[i];

            if (child == null)
            { continue; }

            bool isVisible = child.IsVisible;
            var isLast = i == count - 1; 

            if (isVisible && !hasVisibleChild)
            {
                hasVisibleChild = true;
            }

            // Measure the child.
            child.Measure(layoutSlotSize);
            Size childDesiredSize = child.DesiredSize;

            stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width + (isLast ? LastElementMinWidth : ElementWidth) + (isVisible ? spacing : 0));
            stackDesiredSize = stackDesiredSize.WithHeight(Math.Max(stackDesiredSize.Height, childDesiredSize.Height));
        }

        stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width - (hasVisibleChild ? spacing : 0));
        return stackDesiredSize;
    }

    /// <summary>
    /// Content arrangement.
    /// </summary>
    /// <param name="finalSize">Arrange size</param>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Children;
        Rect rcChild = new Rect(finalSize);
        var leftWidth = finalSize.Width;
        double previousChildSize = 0.0;
        var spacing = Spacing;

        //
        // Arrange and Position Children.
        //
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = children[i];

            if (child == null || !child.IsVisible)
            { continue; }

            var isLast = i == count - 1;

            rcChild = rcChild.WithX(rcChild.X + previousChildSize);
            previousChildSize = isLast ? Math.Max(LastElementMinWidth, leftWidth) : ElementWidth;
            rcChild = rcChild.WithWidth(previousChildSize);
            rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
            previousChildSize += spacing;
            leftWidth -= previousChildSize;

            ArrangeChild(child, rcChild, finalSize, default);
        }

        return finalSize;
    }

    internal virtual void ArrangeChild(
        Control child,
        Rect rect,
        Size panelSize,
        Orientation orientation)
    {
        child.Arrange(rect);
    }
}
