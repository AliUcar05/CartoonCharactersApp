using System.Collections.Generic;
using System.Threading.Tasks;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;

namespace CartoonCharacters;

public static class MyGlobals
{
    private static readonly string _dataFilePath = "cartoon_characters.json";

    private static List<CartoonCharacter>? _myCartoonCharacters;

    public static List<CartoonCharacter> MyCartoonCharacters
    {
        get
        {
            if (_myCartoonCharacters == null)
            {
                _myCartoonCharacters = new List<CartoonCharacter>();
            }

            return _myCartoonCharacters;
        }
        set
        {
            _myCartoonCharacters = value ?? new List<CartoonCharacter>();
        }
    }

    public static string DataFilePath => _dataFilePath;

    public static async Task InitializeAsync()
    {
        MyCartoonCharacters = await JsonDataService.InitializeAsync(_dataFilePath);
    }

    public static async Task SaveDataAsync()
    {
        await JsonDataService.PersistAsync(_dataFilePath, MyCartoonCharacters);
    }

    public static async Task DeleteDataAsync(string id)
    {
        await JsonDataService.DeleteRecordAsync(_dataFilePath, id);
    }
}