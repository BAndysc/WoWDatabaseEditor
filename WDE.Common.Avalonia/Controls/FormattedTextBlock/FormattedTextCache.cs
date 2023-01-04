using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Controls
{
    public class FormattedTextCache
    {
        private Dictionary<(string, int), FormattedText> cache = new();
        private Dictionary<int, (Typeface family, double size, IBrush brush)> styles = new();
        
        public void AddStyle(int index, Typeface fontFamily, int fontSize, IBrush brush)
        {
            styles[index] = (fontFamily, fontSize, brush);
        }
        
        public FormattedText GetFormattedText(string text, int styleId)
        {
            if (cache.TryGetValue((text, styleId), out var formattedText))
                return formattedText;

            if (!styles.TryGetValue(styleId, out var style))
                throw new Exception("Couldn't find style " + styleId);

            formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, style.family, style.size, style.brush);

            cache[(text, styleId)] = formattedText;
            
            return formattedText;
        }
        
        public void InvalidateCache()
        {
            cache.Clear();
        }
    }
}