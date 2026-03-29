using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CartoonCharacters.ViewModels;

namespace CartoonCharacters.Converters;

public class StatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ImportItemStatus status)
        {
            return status switch
            {
                ImportItemStatus.New => "Nouveau",
                ImportItemStatus.Modified => "Modifié",
                ImportItemStatus.Unchanged => "Inchangé",
                ImportItemStatus.Conflict => "Conflit",
                _ => "Inconnu"
            };
        }

        return "Inconnu";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}