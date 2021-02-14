using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace WDE.SmartScriptEditor.Avalonia.ExtendedTextBlock
{
    public class FormattedTextDrawer
    {
        private double lineHeight = 0;
        private FormattedTextCache cache = new();
        private Dictionary<int, (IBrush brush, IPen pen)> styles = new();
        public double LineHeight => lineHeight;

        public void AddStyle(int index, Typeface fontFamily, int fontSize, IBrush color)
        {
            cache.AddStyle(index, fontFamily, fontSize);
            styles[index] = (color, new Pen(color));
        }
        
        public (bool, Rect) Draw(DrawingContext context, string text, int styleId, bool canWrap, ref double x, ref double y, double leftPadding, double maxX)
        {
            var ft = cache.GetFormattedText(text, styleId);
            lineHeight = Math.Max(lineHeight, ft.Bounds.Height);

            if (!styles.TryGetValue(styleId, out var style))
                return (false, Rect.Empty);

            bool wrapped = false;

            if (canWrap && x + ft.Bounds.Width > maxX)
            {
                x = leftPadding;
                y += lineHeight;
                wrapped = true;
            }
            
            double alignedY = y + (lineHeight - ft.Bounds.Height);
            
            context.DrawText(style.brush, new Point(x, alignedY), ft);

            double startX = x;
            x += ft.Bounds.Width;

            return (wrapped, new Rect(startX, y, x - startX, lineHeight));
        }

        public void DrawUnderline(DrawingContext context, int styleId, Rect bounds)
        {
            if (!styles.TryGetValue(styleId, out var style))
                return;
            
            context.DrawLine(style.pen, new Point(bounds.X, bounds.Bottom), new Point(bounds.X + bounds.Width, bounds.Bottom));
        }

        public (bool wasWrapped, Rect bounds) Measure(string text, int styleId, bool canWrap, ref double x, ref double y, double maxX)
        {
            var ft = cache.GetFormattedText(text, styleId);
            lineHeight = Math.Max(lineHeight, ft.Bounds.Height);

            bool wrapped = false;
            if (canWrap && x + ft.Bounds.Width > maxX)
            {
                x = 0;
                y += lineHeight;
                wrapped = true;
            }
            
            double startX = x;
            x += ft.Bounds.Width;
            
            return (wrapped, new Rect(startX, y, ft.Bounds.Width, lineHeight));
        }
    }
}