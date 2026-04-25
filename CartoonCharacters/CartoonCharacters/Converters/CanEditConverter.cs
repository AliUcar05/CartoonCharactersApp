using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using CartoonCharacters.ViewModels;

namespace CartoonCharacters.Converters;

public class CanEditConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is CollectionViewModel viewModel && values[1] is string characterId)
        {
            return viewModel.CanEditOrDelete(characterId);
        }
        
        return false;
    }
}