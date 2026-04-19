using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CartoonCharacters.Converters;

public sealed class BooleanToRoleConverter : IValueConverter
{
    public static readonly BooleanToRoleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAdmin)
            return isAdmin ? "Administrateur" : "Utilisateur";

        return "Utilisateur";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}