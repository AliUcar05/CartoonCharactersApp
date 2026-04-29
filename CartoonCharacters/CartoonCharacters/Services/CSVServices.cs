using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CartoonCharacters.Models;

namespace CartoonCharacters.Services;

public class CsvServices
{
    private readonly TopLevel _topLevel;

    public CsvServices(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task<List<CartoonCharacter>> LoadDataAsync()
    {
        List<CartoonCharacter> list = [];

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionnez un fichier CSV",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Fichier CSV")
                {
                    Patterns = ["*.csv"],
                    MimeTypes = ["text/csv"]
                }
            ]
        });

        if (files.Count == 0)
            return list;

        var selectedFile = files[0];
        var localPath = selectedFile.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(localPath) ||
            !Path.GetExtension(localPath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Le fichier sélectionné doit être un fichier .csv.");
        }

        await using var stream = await selectedFile.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        List<string> lines = [];
        string? line;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            lines.Add(line);
        }

        if (lines.Count == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            throw new InvalidOperationException("Le fichier CSV est vide.");
        }

        var headers = lines[0]
            .Split(';')
            .Select(h => h.Trim())
            .ToArray();

        string[] expectedHeaders =
        [
            "Id",
            "Name",
            "Description",
            "ImagePath",
            "Rating",
            "RatingVotes"
        ];

        var sameHeaders =
            headers.Length == expectedHeaders.Length &&
            headers.SequenceEqual(expectedHeaders, StringComparer.OrdinalIgnoreCase);

        if (!sameHeaders)
        {
            throw new InvalidOperationException(
                "Structure CSV invalide.\n\n" +
                "Colonnes attendues : Id;Name;Description;ImagePath;Rating;RatingVotes");
        }

        var properties = typeof(CartoonCharacter).GetProperties();

        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var values = lines[i].Split(';');

            if (values.Length != headers.Length)
            {
                throw new InvalidOperationException(
                    $"La ligne {i + 1} ne contient pas le bon nombre de colonnes.");
            }

            var obj = new CartoonCharacter();

            for (int j = 0; j < headers.Length; j++)
            {
                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));

                if (property == null || string.IsNullOrWhiteSpace(values[j]))
                    continue;

                try
                {
                    object? convertedValue;

                    if (property.PropertyType == typeof(string))
                    {
                        convertedValue = values[j];
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        convertedValue = int.Parse(values[j], CultureInfo.InvariantCulture);
                    }
                    else if (property.PropertyType == typeof(double))
                    {
                        convertedValue = ParseCsvDouble(values[j]);
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(values[j], property.PropertyType, CultureInfo.InvariantCulture);
                    }

                    property.SetValue(obj, convertedValue);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Erreur à la ligne {i + 1}, colonne '{headers[j]}' : {ex.Message}");
                }
            }

            list.Add(obj);
        }

        return list;
    }

    public async Task SaveDataAsync<T>(List<T> data)
    {
        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties();

        csv.AppendLine(string.Join(";", properties.Select(p => p.Name)));

        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);

                return value switch
                {
                    double d => d.ToString(CultureInfo.InvariantCulture),
                    _ => value?.ToString() ?? string.Empty
                };
            });

            csv.AppendLine(string.Join(";", values));
        }

        var csvFileType = new FilePickerFileType("Fichier CSV")
        {
            Patterns = ["*.csv"],
            MimeTypes = ["text/csv"]
        };

        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer le fichier CSV",
            SuggestedFileName = "data.csv",
            DefaultExtension = "csv",
            FileTypeChoices = [csvFileType],
            SuggestedFileType = csvFileType,
            ShowOverwritePrompt = true
        });

        if (file == null)
            return;

        var localPath = file.TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(localPath) &&
            !Path.GetExtension(localPath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("L’export doit être enregistré au format .csv.");
        }

        await using var stream = await file.OpenWriteAsync();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(csv.ToString());
    }

    private static double ParseCsvDouble(string value)
    {
        var normalized = value.Trim().Replace(',', '.');
        return double.Parse(normalized, CultureInfo.InvariantCulture);
    }
}