using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WDE.Common.Avalonia.Utils.NiceColorGenerator;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views;

public class ColorIdToNiceColorConverter : IValueConverter
{
    private Dictionary<int, IColorGenerator> generators = new();
    private Dictionary<(int, long), IBrush> entryToColor = new();
    
    protected IBrush ConvertT(int kind, long entry)
    {
        if (entryToColor.TryGetValue((kind, entry), out var color))
            return color;

        if (!generators.TryGetValue(kind, out var generator))
            generators[kind] = generator = new ColorsGenerator();
            
        var col = generator.GetNext();
        color = new SolidColorBrush(new Color(col.A, col.R, col.G, col.B));
        entryToColor[(kind, entry)] = color;
        return color;
    }
        
    public virtual object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is (int kind, long entry) && entry > 0)
        {
            var color = ConvertT(kind, entry);
            return color;
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
