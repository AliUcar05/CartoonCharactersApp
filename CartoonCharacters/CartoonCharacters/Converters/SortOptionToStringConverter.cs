using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CartoonCharacters.ViewModels;

namespace CartoonCharacters.Converters;

public class SortOptionToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SortOption option && targetType == typeof(string))
        {
            return option switch
            {
                SortOption.OldestFirst => "📅 Plus ancien → récent",
                SortOption.NewestFirst => "📅 Plus récent → ancien",
                SortOption.RatingHighest => "⭐ Note la plus haute",
                SortOption.RatingLowest => "⭐ Note la plus basse",
                SortOption.NameAscending => "🔤 Nom A → Z",
                SortOption.NameDescending => "🔤 Nom Z → A",
                _ => "📅 Plus ancien → récent"
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}