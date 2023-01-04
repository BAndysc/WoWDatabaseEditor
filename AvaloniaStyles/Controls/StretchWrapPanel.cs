using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls
{
    public class StretchWrapPanel : Panel
    {
        private int usedLines = 0;
        private LineData[] itemsPerLine = new LineData[] {default};
        private struct LineData
        {
            public readonly int Items;
            public readonly double DesiredWidth;

            public LineData(int items, double desiredWidth)
            {
                Items = items;
                DesiredWidth = desiredWidth;
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            Size totalAvailableSize = availableSize;
            double width = 0;
            double height = 0;
            int itemsCount = 0;
            int currentLine = 0;

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            for (var i = 0; i < visualCount; i++)
            {
                Visual visual = visualChildren[i];

                if (visual is not Layoutable layoutable)
                    continue;
                
                layoutable.Measure(availableSize.WithWidth(Math.Max(availableSize.Width, 100)));
                if (itemsCount > 0 && layoutable.DesiredSize.Width > availableSize.Width)
                {
                    itemsPerLine[currentLine] = new LineData(itemsCount, width);
                    availableSize = totalAvailableSize;
                    width = 0;
                    itemsCount = 0;
                    currentLine++;
                    EnsureLinesArray(currentLine + 1);
                }

                itemsCount++;
                width += layoutable.DesiredSize.Width;
                height = Math.Max(height, layoutable.DesiredSize.Height);
                availableSize = new Size(availableSize.Width - layoutable.DesiredSize.Width, availableSize.Height);
            }

            if (itemsCount > 0)
                itemsPerLine[currentLine] = new LineData(itemsCount, width);

            usedLines = currentLine + (itemsCount > 0 ? 1 : 0);
            return new Size(usedLines == 1 ? width : totalAvailableSize.Width, height * usedLines);
        }

        private void EnsureLinesArray(int count)
        {
            if (itemsPerLine.Length < count)
                Array.Resize(ref itemsPerLine, Math.Max(itemsPerLine.Length * 2, count));   
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (itemsPerLine.Length == 0)
                return finalSize;
         
            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            double x = 0;
            double y = 0;
            int lineIndex = 0;
            int itemCounter = 0;
            double optimumWidth = finalSize.Width / itemsPerLine[lineIndex].Items;
            double leftExtraWidth = finalSize.Width - itemsPerLine[lineIndex].DesiredWidth;
            
            for (var i = 0; i < visualCount; i++)
            {
                Visual visual = visualChildren[i];

                if (visual is Layoutable layoutable)
                {
                    StyledElement? styledElement = layoutable as StyledElement;
                    if (styledElement == null)
                        continue;
                    double width = layoutable.DesiredSize.Width;
                    if (width < optimumWidth)
                    {
                        double d = Math.Min(optimumWidth - width, leftExtraWidth);
                        leftExtraWidth -= d;
                        width += d;
                    }
                    width = Math.Ceiling(Math.Max(width, layoutable.DesiredSize.Width));
                    layoutable.Arrange(new Rect(x, y, width, layoutable.DesiredSize.Height));
                    x += width;
                    itemCounter++;
                    if (itemCounter == itemsPerLine[lineIndex].Items)
                    {
                        itemCounter = 0;
                        lineIndex++;
                        x = 0;
                        y += layoutable.DesiredSize.Height;
                        optimumWidth = finalSize.Width / itemsPerLine[Math.Min(lineIndex, usedLines - 1)].Items;
                        leftExtraWidth =
                            finalSize.Width - itemsPerLine[Math.Min(lineIndex, usedLines - 1)].DesiredWidth;

                        styledElement?.Classes.Add("rightmost");
                    }
                    else
                        styledElement?.Classes.Remove("rightmost");
                }
            }

            return finalSize;
        }
    }
}