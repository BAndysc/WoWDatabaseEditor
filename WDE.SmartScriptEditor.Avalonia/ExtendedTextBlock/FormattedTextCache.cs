using System.Collections.Generic;
using Avalonia.Media;

namespace WDE.SmartScriptEditor.Avalonia.ExtendedTextBlock
{
    public class FormattedTextCache
    {
        private Dictionary<(string, int), FormattedText> cache = new();
        private Dictionary<int, (Typeface family, double size)> styles = new();
        
        public void AddStyle(int index, Typeface fontFamily, int fontSize)
        {
            styles[index] = (fontFamily, fontSize);
        }
        
        public FormattedText GetFormattedText(string text, int styleId)
        {
            if (cache.TryGetValue((text, styleId), out var formattedText))
                return formattedText;

            formattedText = new FormattedText();
            formattedText.Text = text;

            if (!styles.TryGetValue(styleId, out var style))
                return formattedText;

            formattedText.FontSize = style.size;
            formattedText.Typeface = style.family;

            cache[(text, styleId)] = formattedText;
            
            return formattedText;
        }
        
        public void InvalidateCache()
        {
            cache.Clear();
        }
    }
}