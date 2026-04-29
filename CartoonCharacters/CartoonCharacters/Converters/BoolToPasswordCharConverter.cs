using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CartoonCharacters.Converters;

public class BoolToPasswordCharConverter : IValueConverter
{
    public static readonly BoolToPasswordCharConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
            return '\0';  // Aucun caractère = visible
            
        return '•';  // Masqué
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}