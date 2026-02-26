using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using CartoonCharacters.Helpers;

namespace CartoonCharacters.Converters;

public class PathToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                if (path.StartsWith("avares://"))
                    return ImageHelper.LoadFromResource(new Uri(path));
                
                if (File.Exists(path))
                {
                    using var fs = File.OpenRead(path);
                    return new Bitmap(fs);
                }
            }
            catch
            {
                // Silently fail, return default image
            }
        }
        
        // Image par défaut
        try
        {
            return ImageHelper.LoadFromResource(
                new Uri("avares://CartoonCharacters/Assets/uzun.jpg"));
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}