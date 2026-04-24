using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CartoonCharacters.ViewModels;

namespace CartoonCharacters.Converters;

public class IsAllCollectionsFilterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string filter && filter == CollectionViewModel.AllCollectionsFilter;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}