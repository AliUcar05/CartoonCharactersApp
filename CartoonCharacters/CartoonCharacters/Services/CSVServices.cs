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
    private const char DefaultSeparator = ';';

    private static readonly string[] ExpectedHeaders =
    [
        "Id",
        "Name",
        "Description",
        "ImagePath",
        "Rating",
        "RatingVotes"
    ];

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

        if (!string.IsNullOrWhiteSpace(localPath) &&
            !Path.GetExtension(localPath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Le fichier sélectionné doit être un fichier .csv.");
        }

        await using var stream = await selectedFile.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Le fichier CSV est vide.");

        var separator = DetectSeparator(content);
        var records = ParseCsv(content, separator)
            .Where(r => r.Any(v => !string.IsNullOrWhiteSpace(v)))
            .ToList();

        if (records.Count == 0)
            throw new InvalidOperationException("Le fichier CSV est vide.");

        var headers = records[0]
            .Select(NormalizeCsvValue)
            .ToArray();

        if (!HeadersAreValid(headers))
        {
            throw new InvalidOperationException(
                "Structure CSV invalide.\n\n" +
                "Colonnes attendues : Id;Name;Description;ImagePath;Rating;RatingVotes\n" +
                "Astuce LibreOffice : enregistrez en CSV UTF-8 avec un séparateur ';' ou ','.");
        }

        var properties = typeof(CartoonCharacter).GetProperties();

        for (int i = 1; i < records.Count; i++)
        {
            var values = records[i]
                .Select(NormalizeCsvValue)
                .ToArray();

            if (values.Length != headers.Length)
            {
                throw new InvalidOperationException(
                    $"La ligne {i + 1} ne contient pas le bon nombre de colonnes. " +
                    $"Attendu : {headers.Length}, trouvé : {values.Length}. " +
                    "Vérifiez les guillemets et les séparateurs dans LibreOffice.");
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
        var separator = DefaultSeparator;

        csv.AppendLine(string.Join(separator, properties.Select(p => EscapeCsvValue(p.Name, separator))));

        foreach (var item in data)
        {
            var values = properties.Select(p => EscapeCsvValue(p.GetValue(item), separator));
            csv.AppendLine(string.Join(separator, values));
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
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteAsync(csv.ToString());
    }

    private static bool HeadersAreValid(string[] headers)
    {
        return headers.Length == ExpectedHeaders.Length &&
               headers.SequenceEqual(ExpectedHeaders, StringComparer.OrdinalIgnoreCase);
    }

    private static char DetectSeparator(string content)
    {
        foreach (var separator in new[] { ';', ',', '\t' })
        {
            var firstRecord = ParseCsv(content, separator).FirstOrDefault();

            if (firstRecord == null)
                continue;

            var headers = firstRecord.Select(NormalizeCsvValue).ToArray();

            if (HeadersAreValid(headers))
                return separator;
        }

        return DefaultSeparator;
    }

    private static List<string[]> ParseCsv(string content, char separator)
    {
        var records = new List<string[]>();
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldHasContent = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                if (!fieldHasContent && string.IsNullOrWhiteSpace(field.ToString()))
                {
                    field.Clear();
                    inQuotes = true;
                    fieldHasContent = true;
                }
                else
                {
                    field.Append(c);
                    fieldHasContent = true;
                }
            }
            else if (c == separator)
            {
                record.Add(field.ToString());
                field.Clear();
                fieldHasContent = false;
            }
            else if (c == '\r' || c == '\n')
            {
                record.Add(field.ToString());
                field.Clear();
                fieldHasContent = false;

                if (record.Any(v => !string.IsNullOrWhiteSpace(v)))
                    records.Add(record.ToArray());

                record.Clear();

                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    i++;
            }
            else
            {
                field.Append(c);

                if (!char.IsWhiteSpace(c))
                    fieldHasContent = true;
            }
        }

        if (inQuotes)
            throw new InvalidOperationException("CSV invalide : guillemet fermant manquant.");

        if (field.Length > 0 || fieldHasContent || record.Count > 0)
        {
            record.Add(field.ToString());

            if (record.Any(v => !string.IsNullOrWhiteSpace(v)))
                records.Add(record.ToArray());
        }

        return records;
    }

    private static string NormalizeCsvValue(string value)
    {
        return value
            .Trim()
            .Trim('\uFEFF')
            .Trim();
    }

    private static string EscapeCsvValue(object? value, char separator)
    {
        var text = value switch
        {
            null => string.Empty,
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        var mustQuote = text.Contains(separator) ||
                        text.Contains('"') ||
                        text.Contains('\r') ||
                        text.Contains('\n');

        if (text.Contains('"'))
            text = text.Replace("\"", "\"\"");

        return mustQuote ? $"\"{text}\"" : text;
    }

    private static double ParseCsvDouble(string value)
    {
        var normalized = value
            .Trim()
            .Replace("\u00A0", "")
            .Replace(" ", "")
            .Replace(',', '.');

        return double.Parse(normalized, CultureInfo.InvariantCulture);
    }
}