using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CartoonCharacters.Models;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Linq;
using CartoonCharacters.Models;

namespace CartoonCharacters.Helpers;

public static class JsonDataService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Charge les personnages depuis un fichier JSON
    /// </summary>
    public static List<CartoonCharacter> LoadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Fichier {filePath} introuvable, chargement des données par défaut");
                return GetDefaultCharacters();
            }

            string jsonString = File.ReadAllText(filePath);
            var characters = JsonSerializer.Deserialize<List<CartoonCharacterJson>>(jsonString, _options);
            
            if (characters == null || !characters.Any())
                return GetDefaultCharacters();

            // Convertir les données JSON en CartoonCharacter avec images
            return ConvertToCartoonCharacters(characters);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du chargement du JSON : {ex.Message}");
            return GetDefaultCharacters();
        }
    }

    /// <summary>
    /// Sauvegarde les personnages dans un fichier JSON
    /// </summary>
    public static void SaveToFile(string filePath, List<CartoonCharacter> characters)
    {
        try
        {
            var jsonCharacters = characters.Select(c => new CartoonCharacterJson
            {
                Id = c.Id.ToString(),
                Name = c.Name,
                Description = c.Description,
                ImagePath = GetImagePathFromBitmap(c.Picture)
            }).ToList();

            string jsonString = JsonSerializer.Serialize(jsonCharacters, _options);
            File.WriteAllText(filePath, jsonString);
            
            Console.WriteLine($"Données sauvegardées dans {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
        }
    }

    /// <summary>
    /// Convertit les données JSON en CartoonCharacter avec images
    /// </summary>
    private static List<CartoonCharacter> ConvertToCartoonCharacters(List<CartoonCharacterJson> jsonCharacters)
    {
        var characters = new List<CartoonCharacter>();
        var defaultImage = ImageHelper.LoadFromResource(
            new Uri("avares://CartoonCharacters/Assets/uzun.jpg"));
            //new Uri("avares://CartoonCharacters/Assets/default_character.png"));

        foreach (var jsonChar in jsonCharacters)
        {
            Bitmap? image = defaultImage;
            
            // Essayer de charger l'image si un chemin est spécifié
            if (!string.IsNullOrEmpty(jsonChar.ImagePath))
            {
                try
                {
                    if (jsonChar.ImagePath.StartsWith("http"))
                    {
                        // Charger depuis une URL
                        var task = ImageHelper.LoadFromWeb(new Uri(jsonChar.ImagePath));
                        task.Wait();
                        image = task.Result ?? defaultImage;
                    }
                    else if (File.Exists(jsonChar.ImagePath))
                    {
                        // Charger depuis un fichier local
                        using var fs = File.OpenRead(jsonChar.ImagePath);
                        image = new Bitmap(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur chargement image {jsonChar.ImagePath}: {ex.Message}");
                    image = defaultImage;
                }
            }

            characters.Add(new CartoonCharacter
            {
                Id = MongoDB.Bson.ObjectId.Parse(jsonChar.Id),
                Name = jsonChar.Name,
                Description = jsonChar.Description,
                Picture = image
            });
        }

        return characters;
    }

    /// <summary>
    /// Extrait le chemin de l'image depuis un Bitmap
    /// </summary>
    private static string GetImagePathFromBitmap(Bitmap? bitmap)
    {
        // Note: Cette méthode est simplifiée. Dans un cas réel,
        // vous devriez stocker le chemin original lors de la sélection
        return string.Empty;
    }

    /// <summary>
    /// Données par défaut si le fichier JSON n'existe pas
    /// </summary>
    private static List<CartoonCharacter> GetDefaultCharacters()
    {
        return new List<CartoonCharacter>
        {
            new()
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId(),
                Name = "SpongeBob",
                Description = "A cartoon character from SpongeBob.",
                Picture = ImageHelper.LoadFromResource(
                    new Uri("avares://CartoonCharacters/Assets/sponge_bob.png"))
            },
            new()
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId(),
                Name = "PatrickStar",
                Description = "Loves donuts and beer.",
                Picture = ImageHelper.LoadFromResource(
                    new Uri("avares://CartoonCharacters/Assets/patrick_star.jpeg"))
            }
        };
    }
}