using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Avalonia.Converters
{
    // Colours an inspection diagnostic by severity in the action list.
    public class DiagnosticSeverityToBrushConverter : IValueConverter
    {
        public static readonly DiagnosticSeverityToBrushConverter Instance = new();

        private static readonly IBrush Error = new SolidColorBrush(Color.FromRgb(0xE0, 0x5A, 0x4A));
        private static readonly IBrush Warning = new SolidColorBrush(Color.FromRgb(0xE0, 0xA0, 0x30));
        private static readonly IBrush Info = new SolidColorBrush(Color.FromRgb(0x80, 0x90, 0xA0));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value switch
            {
                DbScriptDiagnosticSeverity.Error => Error,
                DbScriptDiagnosticSeverity.Warning => Warning,
                _ => Info,
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
