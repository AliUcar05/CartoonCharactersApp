using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace CartoonCharacters.Converters;

public class BoolToPasswordCharConverter : IValueConverter
{
    public static readonly BoolToPasswordCharConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? '\0' : '•';
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}