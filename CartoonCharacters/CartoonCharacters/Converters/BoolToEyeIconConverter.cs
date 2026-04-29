using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CartoonCharacters.Converters;

public class BoolToEyeIconConverter : IValueConverter
{
    public static readonly BoolToEyeIconConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
            return "👁️";  // Œil ouvert = visible
            
        return "👁️‍🗨️";  // Œil barré = masqué
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}