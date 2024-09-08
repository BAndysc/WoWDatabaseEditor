using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaloniaStyles.Controls;

public class ThreeSidesPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double totalWidth = 0;
        double maxHeight = 0;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            totalWidth += child.DesiredSize.Width;
        }
        return new Size(totalWidth, maxHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double leftDesiredWidth = 0;
        double rightDesiredWidth = 0;
        double centerDesiredWidth = 0;
        Control? leftChild = null, centerChild = null, rightChild = null;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var desiredWidth = child.DesiredSize.Width;
            if (index == 0)
            {
                leftDesiredWidth = desiredWidth;
                leftChild = child;
            }
            else if (index == 1)
            {
                rightDesiredWidth = desiredWidth;
                rightChild = child;
            }
            else if (index == 2)
            {
                centerDesiredWidth = 400;//desiredWidth;
                centerChild = child;
            }
        }

        var totalWidth = finalSize.Width;

        var leftSpaceLeft = Math.Max(0, totalWidth / 2 - leftDesiredWidth);
        var rightSpaceLeft = Math.Max(0, totalWidth / 2 - rightDesiredWidth);

        var spaceLeft = leftSpaceLeft + rightSpaceLeft;
        centerDesiredWidth = Math.Min(spaceLeft, centerDesiredWidth);

        leftDesiredWidth = Math.Min(totalWidth - rightDesiredWidth, leftDesiredWidth);

        leftChild?.Arrange(new Rect(0, 0, leftDesiredWidth, finalSize.Height));
        var centerRect = new Rect(Math.Max(leftDesiredWidth, finalSize.Width / 2 - centerDesiredWidth / 2), 0,
            centerDesiredWidth, finalSize.Height);
        var rightRect = new Rect(finalSize.Width - rightDesiredWidth, 0, rightDesiredWidth, finalSize.Height);
        var overflowRight = Math.Max(0, centerRect.Right - rightRect.Left);
        centerRect = new Rect(centerRect.X - overflowRight, centerRect.Y, centerRect.Width, centerRect.Height);
        centerChild?.Arrange(centerRect);
        rightChild?.Arrange(rightRect);

        return finalSize;
    }
}