using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CartoonCharacters.ViewModels;

namespace CartoonCharacters.Converters;

public class StatusToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ImportItemStatus status)
        {
            return status switch
            {
                ImportItemStatus.New => new SolidColorBrush(Color.Parse("#27AE60")),     // Vert
                ImportItemStatus.Modified => new SolidColorBrush(Color.Parse("#F39C12")), // Orange
                ImportItemStatus.Unchanged => new SolidColorBrush(Color.Parse("#3498DB")), // Bleu
                ImportItemStatus.Conflict => new SolidColorBrush(Color.Parse("#E74C3C")), // Rouge
                _ => new SolidColorBrush(Color.Parse("#7F8C8D"))                          // Gris
            };
        }
        return new SolidColorBrush(Color.Parse("#7F8C8D"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}