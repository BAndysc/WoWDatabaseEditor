using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace AvaloniaStyles.Controls;

public class GridEx : Grid
{
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var size = base.ArrangeOverride(arrangeSize);

        var lastChild = Children.LastOrDefault();

        if (lastChild != null)
        {
            var lastChildBounds = lastChild.Bounds;
            var lastChildRect = new Rect(lastChildBounds.X, lastChildBounds.Y, Math.Max(20, arrangeSize.Width - lastChildBounds.X), lastChildBounds.Height);
            lastChild.Arrange(lastChildRect);
        }

        return size;
    }
}