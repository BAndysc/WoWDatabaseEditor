using System;
using System.Globalization;
using Avalonia.Media;
using WDE.Common.Avalonia.Utils.NiceColorGenerator;

namespace WDE.PacketViewer.Avalonia.Views
{
    public class EntryToNiceBackgroundConverter : NiceBackgroundConverter<uint>
    {
        public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is uint and > 0)
                return base.Convert(value, targetType, parameter, culture);
            return Brushes.Transparent;
        }
    }
}