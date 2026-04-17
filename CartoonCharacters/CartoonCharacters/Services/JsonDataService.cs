using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CartoonCharacters.Models;
using CartoonCharacters.Services;
using MongoDB.Bson;

namespace CartoonCharacters.Helpers;

public static class JsonDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });

    private const string BaseUrl = "http://185.157.245.38:8080/json";
    private const string RemoteFileName = "cartoon_characters.json";

    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private static async Task<List<CartoonCharacter>> LoadFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var defaults = GetDefaultCharacters();
                await SaveToFileAsync(filePath, defaults);
                return defaults;
            }

            await using var stream = File.OpenRead(filePath);
            var characters = await JsonSerializer.DeserializeAsync<List<CartoonCharacter>>(stream, SerializerOptions);

            return characters ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lecture JSON local : {ex.Message}");
            return GetDefaultCharacters();
        }
    }

    private static async Task SaveToFileAsync(string filePath, List<CartoonCharacter> characters)
    {
        try
        {
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, characters, SerializerOptions);
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur écriture JSON local : {ex.Message}");
        }
    }

    public static async Task<List<CartoonCharacter>> LoadFromServerAsync()
    {
        try
        {
            var url = $"{BaseUrl}?FileName={RemoteFileName}";

            using var response = await Client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Serveur indisponible : {response.StatusCode}");
                return [];
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<List<CartoonCharacter>>(contentStream, SerializerOptions)
                ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lecture JSON serveur : {ex.Message}");
            return [];
        }
    }

    public static async Task SaveToServerAsync(List<CartoonCharacter> characters)
    {
        await SyncLock.WaitAsync();

        try
        {
            await using var memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, characters, SerializerOptions);
            memoryStream.Position = 0;

            using var fileContent = new StreamContent(memoryStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var content = new MultipartFormDataContent();
            content.Add(fileContent, "file", RemoteFileName);

            using var response = await Client.PostAsync(BaseUrl, content);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"JSON serveur : envoi OK -> {characters.Count} élément(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur écriture JSON serveur : {ex.Message}");
        }
        finally
        {
            SyncLock.Release();
        }
    }

    public static async Task<List<CartoonCharacter>> InitializeAsync(string filePath)
    {
        var localData = await LoadFromFileAsync(filePath);

        var remoteData = await LoadFromServerAsync();
        if (remoteData.Count > 0)
        {
            await SaveToFileAsync(filePath, remoteData);
            return remoteData;
        }

        return localData;
    }

    public static async Task PersistAsync(string filePath, List<CartoonCharacter> characters)
    {
        await SaveToFileAsync(filePath, characters);
        await SaveToServerAsync(characters);

        try
        {
            var dbService = new DatabaseServices();
            var connected = await dbService.TestConnectionAsync();

            if (connected)
            {
                await dbService.ReplaceAllCharactersAsync(characters);
            }
            else
            {
                Console.WriteLine("MongoDB : connexion impossible, aucun envoi effectué.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur écriture MongoDB : {ex.Message}");
        }
    }

    public static async Task DeleteRecordAsync(string filePath, string id)
    {
        var list = (await LoadFromFileAsync(filePath)).ToList();
        list.RemoveAll(c => c.Id == id);
        await PersistAsync(filePath, list);
    }

    private static List<CartoonCharacter> GetDefaultCharacters() =>
    [
        new CartoonCharacter
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "SpongeBob",
            Description = "A cartoon character from SpongeBob.",
            ImagePath = "avares://CartoonCharacters/Assets/sponge_bob.png"
        }
    ];
}