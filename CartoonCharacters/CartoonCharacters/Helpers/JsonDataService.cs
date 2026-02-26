using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
            var characters = JsonSerializer.Deserialize<List<CartoonCharacter>>(jsonString, _options);
            
            return characters ?? GetDefaultCharacters();
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
            string jsonString = JsonSerializer.Serialize(characters, _options);
            File.WriteAllText(filePath, jsonString);
            
            Console.WriteLine($"Données sauvegardées dans {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
        }
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
                ImagePath = "avares://CartoonCharacters/Assets/sponge_bob.png"
            },
        };
    }
}