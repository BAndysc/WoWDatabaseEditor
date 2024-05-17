using System;
using Avalonia;
using Avalonia.Controls;

namespace WDE.Common.Avalonia.Controls;

public class EditorHeaderPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 4)
            return default;

        double width = 0;
        double height = 0;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 4)
            return default;

        var id = Children[0];
        var name = Children[1];
        var toolbar = Children[2];
        var rightbar = Children[3];

        var requiredWidth = id.DesiredSize.Width + toolbar.DesiredSize.Width + rightbar.DesiredSize.Width;
        var desiredWidth = requiredWidth + name.DesiredSize.Width;

        if (finalSize.Width >= desiredWidth)
        {
            double x = 0;
            id.Arrange(new Rect(x, 0, id.DesiredSize.Width, finalSize.Height));
            x += id.DesiredSize.Width;
            name.Arrange(new Rect(x, 0, name.DesiredSize.Width, finalSize.Height));
            x += name.DesiredSize.Width;
            toolbar.Arrange(new Rect(x, 0, toolbar.DesiredSize.Width, finalSize.Height));
            rightbar.Arrange(new Rect(finalSize.Width - rightbar.DesiredSize.Width, 0, rightbar.DesiredSize.Width, finalSize.Height));
        }
        else if (finalSize.Width >= requiredWidth)
        {
            id.Arrange(new Rect(0, 0, id.DesiredSize.Width, finalSize.Height));

            double x = finalSize.Width - rightbar.DesiredSize.Width;
            rightbar.Arrange(new Rect(x, 0, rightbar.DesiredSize.Width, finalSize.Height));
            x -= toolbar.DesiredSize.Width;
            toolbar.Arrange(new Rect(x, 0, toolbar.DesiredSize.Width, finalSize.Height));

            var leftWidth = finalSize.Width - requiredWidth;
            name.Arrange(new Rect(id.DesiredSize.Width, 0, leftWidth, finalSize.Height));
        }
        else
        {
            double x = 0;
            id.Arrange(new Rect(0, 0, id.DesiredSize.Width, finalSize.Height));

            var overflow = requiredWidth - finalSize.Width;
            var toolbarWidth = toolbar.DesiredSize.Width;
            toolbarWidth -= overflow;
            toolbarWidth = Math.Max(48, toolbarWidth);

            overflow = (requiredWidth - finalSize.Width) - (toolbar.DesiredSize.Width - toolbarWidth);

            var righBarWidth = rightbar.DesiredSize.Width;
            righBarWidth -= overflow;
            righBarWidth = Math.Max(48, righBarWidth);

            x += id.DesiredSize.Width;
            toolbar.Arrange(new Rect(x, 0, toolbarWidth, finalSize.Height));
            x += toolbarWidth;
            rightbar.Arrange(new Rect(x, 0, righBarWidth, finalSize.Height));
        }

        return finalSize;
    }
}