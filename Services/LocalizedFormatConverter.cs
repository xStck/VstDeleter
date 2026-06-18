using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace VstDeleter.Services;

public class LocalizedFormatConverter : IMultiValueConverter
{
    public static LocalizedFormatConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0) return null;
        
        var format = values[0]?.ToString();
        if (string.IsNullOrEmpty(format)) return null;
        
        var args = values.Skip(1).ToArray();
        try 
        {
            return string.Format(format, args);
        } 
        catch 
        {
            return format;
        }
    }
}
