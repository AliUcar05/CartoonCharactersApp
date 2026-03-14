using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CartoonCharacters.ViewModels;

namespace CartoonCharacters.Converters;

public class StatusToEnabledConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ImportItemStatus status)
        {
            return status != ImportItemStatus.Conflict; // Désactiver les conflits
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}