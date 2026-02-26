using System.Collections.Generic;
using System.IO;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;

namespace CartoonCharacters;

public static class MyGlobals
{
    // Récupère le dossier où se trouve l'exécutable
    private static readonly string AppFolder = 
        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
        ?? Directory.GetCurrentDirectory();
    
    private static readonly string DataFilePath = 
        Path.Combine(AppFolder, "cartoon_characters.json");
    
    private static List<CartoonCharacter>? _myCartoonCharacters;
    
    public static List<CartoonCharacter> MyCartoonCharacters
    {
        get
        {
            if (_myCartoonCharacters == null)
            {
                _myCartoonCharacters = JsonDataService.LoadFromFile(DataFilePath);
            }
            return _myCartoonCharacters;
        }
        set
        {
            _myCartoonCharacters = value;
            JsonDataService.SaveToFile(DataFilePath, _myCartoonCharacters);
        }
    }
    
    public static void SaveData()
    {
        if (_myCartoonCharacters != null)
        {
            JsonDataService.SaveToFile(DataFilePath, _myCartoonCharacters);
        }
    }
}