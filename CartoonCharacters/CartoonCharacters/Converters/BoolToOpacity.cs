using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CartoonCharacters.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ? 1.0 : 0.5; // Opacité réduite pour non sélectionné
        }
        return 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}