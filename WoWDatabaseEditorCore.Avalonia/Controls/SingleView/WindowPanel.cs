using System;
using Avalonia;
using Avalonia.Controls;

namespace WoWDatabaseEditorCore.Avalonia.Controls;

public class WindowPanel : Panel
{
    public static readonly AttachedProperty<double> DesiredWidthProperty = AvaloniaProperty.RegisterAttached<Control, double>("DesiredWidth", typeof(WindowPanel));
    public static readonly AttachedProperty<double> DesiredHeightProperty = AvaloniaProperty.RegisterAttached<Control, double>("DesiredHeight", typeof(WindowPanel));

    public static double GetDesiredWidth(Control obj)
    {
        return obj.GetValue(DesiredWidthProperty);
    }

    public static void SetDesiredWidth(Control obj, double value)
    {
        obj.SetValue(DesiredWidthProperty, value);
    }

    public static double GetDesiredHeight(Control obj)
    {
        return obj.GetValue(DesiredHeightProperty);
    }

    public static void SetDesiredHeight(Control obj, double value)
    {
        obj.SetValue(DesiredHeightProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width;
        var height = finalSize.Height;
        foreach (var child in Children)
        {
            var desiredWidth = child.IsSet(DesiredWidthProperty) ? (double?)GetDesiredWidth(child) : null;
            var desiredHeight = child.IsSet(DesiredHeightProperty) ? (double?)GetDesiredHeight(child) : null;

            desiredWidth ??= child.DesiredSize.Width;
            desiredHeight ??= child.DesiredSize.Height;

            desiredWidth = Math.Clamp(desiredWidth.Value, 100, Math.Max(100, width));
            desiredHeight = Math.Clamp(desiredHeight.Value, 100, Math.Max(100, height));

            child.Arrange(new Rect(width / 2 - desiredWidth.Value / 2, height / 2 - desiredHeight.Value / 2, desiredWidth.Value, desiredHeight.Value));
        }
        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
            return new Size(0, 0);

        foreach (var child in Children)
        {
            child.Measure(availableSize);
        }

        return availableSize;
    }
}