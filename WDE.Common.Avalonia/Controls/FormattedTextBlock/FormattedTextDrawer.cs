using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Controls
{
    public class FormattedTextDrawer
    {
        private double lineHeight = 0;
        private FormattedTextCache cache = new();
        private Dictionary<int, (IBrush brush, IPen pen, int yoffset)> styles = new();
        public double LineHeight => lineHeight;

        public void AddStyle(int index, Typeface fontFamily, int fontSize, IBrush color, int yoffset)
        {
            cache.AddStyle(index, fontFamily, fontSize);
            styles[index] = (color, new Pen(color), yoffset);
        }
        
        public (bool, Rect) Draw(DrawingContext context, string text, int styleId, bool canWrap, ref double x, ref double y, double leftPadding, double maxX)
        {
            var ft = cache.GetFormattedText(text, styleId);
            var ftHeight = ft.Bounds.Height;
            if (text.Length == 0 || text.Length == 1 && char.IsWhiteSpace(text[0]))
                ftHeight = 0;
            lineHeight = Math.Max(lineHeight, ftHeight);

            if (!styles.TryGetValue(styleId, out var style))
                return (false, Rect.Empty);

            bool wrapped = false;

            if (canWrap && (x + ft.Bounds.Width > maxX || text == "\n"))
            {
                x = leftPadding;
                y += lineHeight;
                wrapped = true;
            }
            
            double alignedY = y + (lineHeight - ft.Bounds.Height);
            
            context.DrawText(style.brush, new Point(x, alignedY + style.yoffset), ft);

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
            var ftHeight = ft.Bounds.Height;
            if (text.Length == 0 || text.Length == 1 && char.IsWhiteSpace(text[0]))
                ftHeight = 0;
            
            lineHeight = Math.Max(lineHeight, ftHeight);

            bool wrapped = false;
            if (canWrap && (x + ft.Bounds.Width > maxX || text == "\n"))
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