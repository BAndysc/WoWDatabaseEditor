using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Utils.NiceColorGenerator
{
    public class NiceColorConverter<T> : IValueConverter where T : notnull
    {
        private IColorGenerator generator;
        private Dictionary<T, Color> entryToColor = new();

        public NiceColorConverter()
        {
            generator = new ColorsGenerator();
        }
        
        public NiceColorConverter(double baseSaturation, double customLuminance)
        {
            generator = new ColorsGenerator(baseSaturation, customLuminance);
        }

        protected Color ConvertT(T entry)
        {
            if (entryToColor.TryGetValue(entry, out var color))
                return color;

            var col = generator.GetNext();
            color = new Color(col.A, col.R, col.G, col.B);
            entryToColor[entry] = color;
            return color;
        }
        
        public virtual object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is T entry)
            {
                return ConvertT(entry);
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}