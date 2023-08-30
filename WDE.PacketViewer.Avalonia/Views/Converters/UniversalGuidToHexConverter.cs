using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Avalonia.Views.Converters
{
    public class UniversalGuidToHexConverter : IValueConverter
    {
        public static UniversalGuidToHexConverter Instance { get; } = new();
        
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is UniversalGuid guid)
            {
                return guid.ToWowParserString();
            }

            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}