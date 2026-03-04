using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CartoonCharacters.Models;
using MongoDB.Bson;

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
    /// Supprime un personnage dans un fichier JSON
    /// </summary>

    public static void DeleteRecordFromFile(string filePath, ObjectId id)
    {
        try
        {
            // recupère tout le json. 
            var json = File.ReadAllText(filePath);
            // remplie la liste de character avec le json.
            var characterList = JsonSerializer.Deserialize<List<CartoonCharacter>>(json) ?? new List<CartoonCharacter>();
            // retire le character pas désirée.
            characterList.RemoveAll(c => c.Id == id);
            // reécrit le fichier en entier
            File.WriteAllText(filePath, JsonSerializer.Serialize(characterList));
            
            Console.WriteLine($"Données supprimées dans {filePath}");
            MyGlobals.MyCartoonCharacters = LoadFromFile(MyGlobals.GetDataFilePath());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression : {ex.Message}");
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