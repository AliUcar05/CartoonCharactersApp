using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MongoDB.Bson;
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
        var list = new List<CartoonCharacter>();

        // ================================
        // ANCIEN CODE DU PROF (CONSERVÉ)
        // ================================
        /*
        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionnez un fichier CSV",
            AllowMultiple = false
        });

        if (files.Count <= 0) return list;
        
        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string?>();

        while (!reader.EndOfStream)lines.Add(await reader.ReadLineAsync());

        if (lines.Count == 0) return list; 

        var headers = lines[0].Split(';');
        var properties = typeof(CartoonCharacter).GetProperties();

        for (var i = 1; i < lines.Count; i++)
        {
            var obj = new CartoonCharacter();
            var values = lines[i]?.Split(';');

            if (values != null)
            {
                for (var j = 0; j < headers.Length && j < values.Length; j++)
                {
                    var property = properties.FirstOrDefault(p =>
                        p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));
                    if (property == null || string.IsNullOrWhiteSpace(values[j])) continue;

                    try
                    {
                        if (property.PropertyType == typeof(ObjectId))
                        {
                            var objectIdValue = new ObjectId(values[j]); 
                            property.SetValue(obj, objectIdValue);
                        }
                        else
                        {
                            var value = Convert.ChangeType(values[j], property.PropertyType);
                            property.SetValue(obj, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(ex.Message);
                    }
                }
            }

            list.Add(obj);
        }
        return list;
        */

        // =====================================
        // DÉBUT MODIFICATIONS ÉTUDIANTES
        // =====================================

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionnez un fichier CSV",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Fichier CSV")
                {
                    Patterns = new[] { "*.csv" },
                    MimeTypes = new[] { "text/csv" }
                }
            }
        });

        // L'utilisateur a annulé
        if (files.Count <= 0)
            return list;

        var selectedFile = files[0];
        var localPath = selectedFile.TryGetLocalPath();

        // Vérification stricte de l'extension
        if (string.IsNullOrWhiteSpace(localPath) ||
            !Path.GetExtension(localPath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Le fichier sélectionné doit être un fichier .csv.");
        }

        await using var stream = await selectedFile.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string?>();

        while (!reader.EndOfStream)
            lines.Add(await reader.ReadLineAsync());

        if (lines.Count == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            throw new InvalidOperationException("Le fichier CSV est vide.");
        }

        // Vérification stricte de l'en-tête
        var headers = lines[0]
            .Split(';')
            .Select(h => h.Trim())
            .ToArray();

        var expectedHeaders = new[] { "Id", "Name", "Description", "ImagePath" };

        var sameHeaderCount = headers.Length == expectedHeaders.Length;
        var sameHeaders = sameHeaderCount &&
                          headers.SequenceEqual(expectedHeaders, StringComparer.OrdinalIgnoreCase);

        if (!sameHeaders)
        {
            throw new InvalidOperationException(
                "Structure CSV invalide.\n\n" +
                "Colonnes attendues : Id;Name;Description;ImagePath");
        }

        var properties = typeof(CartoonCharacter).GetProperties();

        for (var i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var obj = new CartoonCharacter();
            var values = lines[i]?.Split(';');

            if (values == null)
                continue;

            // Vérification stricte du nombre de colonnes
            if (values.Length != headers.Length)
            {
                throw new InvalidOperationException(
                    $"La ligne {i + 1} ne contient pas le bon nombre de colonnes.");
            }

            for (var j = 0; j < headers.Length; j++)
            {
                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));

                if (property == null || string.IsNullOrWhiteSpace(values[j]))
                    continue;

                try
                {
                    if (property.PropertyType == typeof(ObjectId))
                    {
                        var objectIdValue = new ObjectId(values[j]);
                        property.SetValue(obj, objectIdValue);
                    }
                    else
                    {
                        var value = Convert.ChangeType(values[j], property.PropertyType);
                        property.SetValue(obj, value);
                    }
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

        // =====================================
        // FIN MODIFICATIONS ÉTUDIANTES
        // =====================================
    }

    public async Task SaveDataAsync<T>(List<T> data)
    {
        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties();
        csv.AppendLine(string.Join(";", properties.Select(p => p.Name)));

        foreach (var item in data)
        {
            var values = properties.Select(p => p.GetValue(item)?.ToString() ?? string.Empty);
            csv.AppendLine(string.Join(";", values));
        }

        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer le fichier CSV",
            SuggestedFileName = "data.csv" 
        });

        if (file != null)
        {
            await using (var stream = await file.OpenWriteAsync())
            {
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(csv.ToString());
            }
        }
    }
}