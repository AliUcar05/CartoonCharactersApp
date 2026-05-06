using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace CartoonCharacters.Converters;

public class BoolToEyeIconConverter : IValueConverter
{
    public static readonly BoolToEyeIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "👁️" : "👁️‍🗨️";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}