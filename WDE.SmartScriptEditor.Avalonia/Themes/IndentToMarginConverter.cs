using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace WDE.SmartScriptEditor.Avalonia.Themes
{
    public class IndentToMarginConverter : IValueConverter
    {
        public double LeftMultiplier { get; set; }
        public double RightMultiplier { get; set; }
        public double TopMultiplier { get; set; }
        public double BottomMultiplier { get; set; }
        
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int i)
                return new Thickness(i * LeftMultiplier, i * TopMultiplier, i * RightMultiplier, i * BottomMultiplier);
            return new Thickness();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}