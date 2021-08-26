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
        private IColorGenerator generator = new ColorsGenerator();
        private Dictionary<T, IBrush> entryToColor = new();
        
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is T entry)
            {
                if (entryToColor.TryGetValue(entry, out var color))
                    return color;

                color = new ImmutableSolidColorBrush(generator.GetNext());
                entryToColor[entry] = color;
                return color;
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}