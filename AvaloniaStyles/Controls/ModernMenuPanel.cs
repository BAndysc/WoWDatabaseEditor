using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaloniaStyles.Controls;

public class ModernMenuPanel : Panel, INavigableContainer
{
    public static readonly AttachedProperty<ModernMenuPlacement?> PlacementProperty =
        AvaloniaProperty.RegisterAttached<ModernMenuPanel, Control, ModernMenuPlacement?>("Placement");

    public static readonly StyledProperty<double> TopRowSizeProperty = AvaloniaProperty.Register<ModernMenuPanel, double>(nameof(TopRowSize), defaultValue: 48);

    public double TopRowSize
    {
        get => GetValue(TopRowSizeProperty);
        set => SetValue(TopRowSizeProperty, value);
    }

    public static ModernMenuPlacement? GetPlacement(Control control)
    {
        return control.GetValue(PlacementProperty) ?? ModernMenuPlacement.Regular;
    }

    public static void SetPlacement(Control control, ModernMenuPlacement? value)
    {
        control.SetValue(PlacementProperty, value);
    }

    static ModernMenuPanel()
    {
        AffectsParentArrange<ModernMenuPanel>(PlacementProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var measuredWidth = 0.0;
        var measuredHeight = 0.0;
        var topPanelWidth = 0.0;
        var prevWasHalf = false;
        var prevHalfWidth = 0.0;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            var placement = GetPlacement(child) ?? ModernMenuPlacement.Regular;
            child.Classes.Set("icon-only", placement == ModernMenuPlacement.Top);

            child.Measure(availableSize);
            var desiredSize = child.DesiredSize;

            if (placement == ModernMenuPlacement.Top)
            {
                if (child is Separator)
                    continue;

                if (topPanelWidth == 0)
                {
                    // first top Element
                    measuredHeight += TopRowSize;
                }

                topPanelWidth += TopRowSize;
                measuredWidth = Math.Max(measuredWidth, topPanelWidth);
            }
            else if (placement == ModernMenuPlacement.Half)
            {
                if (prevWasHalf)
                {
                     // simply ignore
                     prevWasHalf = false; // next element will be regular
                     measuredWidth = Math.Max(measuredWidth, Math.Max(prevHalfWidth, desiredSize.Width));
                }
                else
                {
                    prevHalfWidth = desiredSize.Width;
                    measuredWidth = Math.Max(measuredWidth, prevHalfWidth * 2);
                    measuredHeight += desiredSize.Height;
                    prevWasHalf = true;
                }
            }
            else
            {
                measuredWidth = Math.Max(measuredWidth, desiredSize.Width);
                measuredHeight += desiredSize.Height;
                prevWasHalf = false;
            }
        }

        return new Size(measuredWidth, measuredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var stretchWidth = finalSize.Width;
        var nextRegularY = 0.00;
        var nextTopX = 0.0;
        var prevWasHalf = false;

        var topElements = Children.Count(x => x.IsVisible && GetPlacement(x) == ModernMenuPlacement.Top && x is not Separator);
        var topWidth = topElements * TopRowSize;
        var topFreeSpace = Math.Max(0, stretchWidth - topWidth);

        foreach (var child in Children)
        {
            var desiredSize = child.DesiredSize;

            if (!child.IsVisible)
                continue;

            var placement = GetPlacement(child);

            if (placement == ModernMenuPlacement.Top)
            {
                if (nextRegularY == 0)
                {
                    nextRegularY = TopRowSize;
                }

                if (child is Separator)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    nextTopX += topFreeSpace;
                }
                else
                {
                    child.Arrange(new Rect(nextTopX, 0, TopRowSize, TopRowSize));
                    nextTopX += TopRowSize;
                }
            }
            else if (placement == ModernMenuPlacement.Half)
            {
                if (prevWasHalf)
                {
                    prevWasHalf = false;
                    child.Arrange(new Rect(stretchWidth / 2, nextRegularY - desiredSize.Height /* assuming all have same desired height */, stretchWidth / 2, desiredSize.Height));
                }
                else
                {
                    child.Arrange(new Rect(0, nextRegularY, stretchWidth / 2, desiredSize.Height));
                    nextRegularY += desiredSize.Height;
                    prevWasHalf = true;
                }
            }
            else
            {
                child.Arrange(new Rect(0, nextRegularY, stretchWidth, desiredSize.Height));
                nextRegularY += desiredSize.Height;
                prevWasHalf = false;
            }
        }

        return new Size(stretchWidth, nextRegularY);
    }

    public IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        if (Children.Count == 0)
            return null;

        if (from is not Control controlFrom || Children.IndexOf(controlFrom) is var controlFromIndex && controlFromIndex == -1)
        {
            switch (direction)
            {
                case NavigationDirection.Next:
                case NavigationDirection.First:
                case NavigationDirection.Down:
                case NavigationDirection.PageUp:
                case NavigationDirection.Right:
                    return Children[0];
                case NavigationDirection.Previous:
                case NavigationDirection.PageDown:
                case NavigationDirection.Left:
                case NavigationDirection.Last:
                case NavigationDirection.Up:
                    return Children[^1];
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        int? GetUntilCriteria(int start, int dir, Func<Control, bool> met)
        {
            var current = start + dir;
            while (current >= 0 && current < Children.Count && Children[current] is {} c &&  (!c.IsVisible || !met(c) || c is Separator))
                current += dir;
            return current >= 0 && current < Children.Count ? current : null;
        }

        Control? GetControlUntilCriteria(int start, int dir, Func<Control, bool> met)
        {
            var index = GetUntilCriteria(start, dir, met);
            return index.HasValue ? Children[index.Value] : null;
        }

        switch (direction)
        {
            case NavigationDirection.Next:
                var nextIndex = controlFromIndex + 1;
                if (nextIndex >= Children.Count)
                    return wrap ? Children[0] : null;
                return Children[nextIndex];
            case NavigationDirection.First:
                return Children[0];
            case NavigationDirection.Last:
                return Children[^1];
            case NavigationDirection.Left:
                if (GetPlacement(controlFrom) == ModernMenuPlacement.Half)
                    return GetControlUntilCriteria(controlFromIndex, -1, c => GetPlacement(c) == ModernMenuPlacement.Half);
                else
                   return GetControlUntilCriteria(controlFromIndex, -1, c => GetPlacement(c) == ModernMenuPlacement.Top);
            case NavigationDirection.Right:
                if (GetPlacement(controlFrom) == ModernMenuPlacement.Half)
                    return GetControlUntilCriteria(controlFromIndex, 1, c => GetPlacement(c) == ModernMenuPlacement.Half);
                else
                    return GetControlUntilCriteria(controlFromIndex, 1, c => GetPlacement(c) == ModernMenuPlacement.Top);
            case NavigationDirection.Previous:
            case NavigationDirection.Up:
            case NavigationDirection.PageUp:
                if (controlFromIndex == 0 || GetPlacement(controlFrom) == ModernMenuPlacement.Top)
                    return wrap ? Children[^1] : null;
                var ctr = GetControlUntilCriteria(controlFromIndex, -1, c => true);
                if (ctr == null && GetPlacement(controlFrom) == ModernMenuPlacement.Regular)
                    return Children[0];
                return ctr;
            case NavigationDirection.Down:
            case NavigationDirection.PageDown:
                if (controlFromIndex == Children.Count - 1)
                    return wrap ? Children[0] : null;
                return GetControlUntilCriteria(controlFromIndex, 1, c => GetPlacement(c) != ModernMenuPlacement.Top);
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}

public enum ModernMenuPlacement
{
    Regular,
    Top,
    Half
}