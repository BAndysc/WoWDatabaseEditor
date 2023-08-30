using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace WDE.Common.Avalonia.Utils.NiceColorGenerator
{
    public class NiceBackgroundConverter<T> : IValueConverter where T : notnull
    {
        private IColorGenerator generator;
        private Dictionary<T, IBrush> entryToColor = new();

        public NiceBackgroundConverter()
        {
            generator = new ColorsGenerator();
        }
        
        public NiceBackgroundConverter(double baseSaturation, double customLuminance)
        {
            generator = new ColorsGenerator(baseSaturation, customLuminance);
        }

        protected IBrush ConvertT(T entry)
        {
            if (entryToColor.TryGetValue(entry, out var color))
                return color;

            color = new ImmutableSolidColorBrush(generator.GetNext());
            entryToColor[entry] = color;
            return color;
        }
        
        public virtual object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is T entry)
            {
                return ConvertT(entry);
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}