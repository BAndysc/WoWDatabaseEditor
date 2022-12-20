using System;
using Avalonia;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public partial class VirtualizedSmartScriptPanel
{
    private readonly struct SizingContext
    {
        public readonly HorizRect eventRect;
        public readonly HorizRect groupedEventRect;
        public readonly HorizRect actionRect;
        public readonly HorizRect conditionRect;
        public readonly HorizRect grupedConditionRect;
        public readonly HorizRect groupRect;
        public readonly HorizRect variableRect;
        public readonly double totalWidth;
        public readonly Thickness padding;
        public readonly double eventPadding;
        public readonly Rect visibleRect;

        public SizingContext(double totalWidth, Thickness padding, double eventPadding, Rect visibleRect)
        {
            this.totalWidth = totalWidth;
            this.padding = padding;
            this.eventPadding = eventPadding;
            this.visibleRect = visibleRect;
            eventRect = EventWidth(totalWidth);
            groupedEventRect = eventRect.SkipLeft(20);
            conditionRect = ConditionWidth(totalWidth);
            grupedConditionRect = conditionRect.SkipLeft(20);
            variableRect = HorizRect.FromRight(padding.Left, totalWidth - padding.Right);
            actionRect = HorizRect.FromRight(eventRect.Right, totalWidth - padding.Right);
            groupRect = HorizRect.FromRight(padding.Left + eventPadding, totalWidth - padding.Right);
        }
    }

    private readonly struct HorizRect
    {
        public readonly double X;
        public readonly double Width;
        public double Right { get; }

        public HorizRect(double x, double width)
        {
            X = x;
            Width = width;
            Right = x + width;
        }

        public static HorizRect FromRight(double left, double right)
        {
            right = Math.Max(left, right);
            return new HorizRect(left, right - left);
        }

        public Rect WithVertical(double y, double height)
        {
            return new Rect(X, y, Width, height);
        }

        public HorizRect SkipLeft(double d)
        {
            return FromRight(X + d, Right);
        }
    }
}