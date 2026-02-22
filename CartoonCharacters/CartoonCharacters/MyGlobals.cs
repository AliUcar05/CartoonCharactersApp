using System.Collections.Generic;
using System.IO;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;

namespace CartoonCharacters;

public static class MyGlobals
{
    private static readonly string DataFilePath = 
        Path.Combine(Directory.GetCurrentDirectory(), "cartoon_characters.json");
    
    private static List<CartoonCharacter>? _myCartoonCharacters;
    
    public static List<CartoonCharacter> MyCartoonCharacters
    {
        get
        {
            if (_myCartoonCharacters == null)
            {
                // Charger depuis le JSON au premier accès
                _myCartoonCharacters = JsonDataService.LoadFromFile(DataFilePath);
            }
            return _myCartoonCharacters;
        }
        set
        {
            _myCartoonCharacters = value;
            // Sauvegarder automatiquement quand la liste est modifiée
            JsonDataService.SaveToFile(DataFilePath, _myCartoonCharacters);
        }
    }
    
    /// <summary>
    /// Sauvegarde manuelle des données
    /// </summary>
    public static void SaveData()
    {
        if (_myCartoonCharacters != null)
        {
            JsonDataService.SaveToFile(DataFilePath, _myCartoonCharacters);
        }
    }
}