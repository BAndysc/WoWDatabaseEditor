﻿using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Microsoft.Extensions.Logging;

namespace WoWDatabaseEditorCore.Avalonia.Controls.LogViewer.Converters;

public class EventIdConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return "0";

        EventId eventId = (EventId)value;

        return eventId.ToString();
    }

    // If not implemented, an error is thrown
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new EventId(0, value?.ToString() ?? string.Empty);
}