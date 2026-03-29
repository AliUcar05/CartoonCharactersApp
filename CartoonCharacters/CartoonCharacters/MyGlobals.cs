using System.Collections.Generic;
using System.Threading.Tasks;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;

namespace CartoonCharacters;

public static class MyGlobals
{
    public static string DataFilePath { get; } = "cartoon_characters.json";

    public static List<CartoonCharacter> MyCartoonCharacters { get; private set; } = [];

    public static async Task InitializeAsync()
    {
        MyCartoonCharacters = await JsonDataService.InitializeAsync(DataFilePath);
    }

    public static Task SaveDataAsync() =>
        JsonDataService.PersistAsync(DataFilePath, MyCartoonCharacters);

    public static Task DeleteDataAsync(string id) =>
        JsonDataService.DeleteRecordAsync(DataFilePath, id);
}