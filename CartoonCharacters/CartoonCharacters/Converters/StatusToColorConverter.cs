using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CartoonCharacters.ViewModels;

namespace CartoonCharacters.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ImportItemStatus status)
        {
            return status switch
            {
                ImportItemStatus.New => new SolidColorBrush(Color.Parse("#27AE60")),
                ImportItemStatus.Modified => new SolidColorBrush(Color.Parse("#F39C12")),
                ImportItemStatus.Unchanged => new SolidColorBrush(Color.Parse("#3498DB")),
                ImportItemStatus.Conflict => new SolidColorBrush(Color.Parse("#E74C3C")),
                _ => new SolidColorBrush(Color.Parse("#7F8C8D"))
            };
        }

        return new SolidColorBrush(Color.Parse("#7F8C8D"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}