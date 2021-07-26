using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace AvaloniaStyles.Controls
{
    public class ToolbarPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var spacing = 4;
            double desiredWidth = 0;
            double desiredHeight = 0;
            var children = Children;
            bool any = false;
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;

                if (child is ToolbarSpacer)
                    continue;
                
                any = true;
                child.Measure(availableSize.WithWidth(double.PositiveInfinity));
                desiredWidth += child.DesiredSize.Width + spacing;
                desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
            }

            return new Size(any ? desiredWidth - spacing : 0, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = Children;
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0;
            var spacing = 4;

            int spacerCount = 0;
            double totalDesiredWidth = 0;
            bool any = false;
            
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;
                if (child is ToolbarSpacer)
                    spacerCount++;
                else
                {
                    any = true;
                    totalDesiredWidth += child.DesiredSize.Width + spacing;
                }
            }
            if (any)
                totalDesiredWidth -= spacing;

            double leftSpace = finalSize.Width - totalDesiredWidth;
            double spacerWidth = spacerCount == 0 ? 0 : (leftSpace > 0 ? leftSpace / spacerCount : 0.0);
            
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;

                rcChild = rcChild.WithX(rcChild.X + previousChildSize);
                rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
                // we want to stretch only content presenters, not buttons
                // if it doesn't work for future toolbars, it can be changed 
                if (child.HorizontalAlignment == HorizontalAlignment.Stretch && child is ContentPresenter)
                {
                    previousChildSize = child.DesiredSize.Width + Math.Max(0, leftSpace);
                    rcChild = rcChild.WithWidth(previousChildSize);
                }
                else if (child is ToolbarSpacer)
                {
                    previousChildSize = spacerWidth;
                    rcChild = rcChild.WithWidth(previousChildSize);
                }
                else
                {
                    previousChildSize = child.DesiredSize.Width;
                    rcChild = rcChild.WithWidth(previousChildSize);
                    previousChildSize += spacing;
                }

                ArrangeChild(child, rcChild, finalSize);
            }

            return finalSize;
        }

        internal virtual void ArrangeChild(
            IControl child,
            Rect rect,
            Size panelSize)
        {
            child.Arrange(rect);
        }
    }

    public class ToolbarSpacer : Control
    {
    }
}