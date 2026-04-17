using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using CartoonCharacters.Models;

namespace CartoonCharacters.Services;

public partial class DatabaseServices
{
    private readonly IMongoCollection<CartoonCharacter> _cartoonCharacters;

    public DatabaseServices()
    {
        const string connectionUri = "mongodb://Meeeee:IAmTheBest@185.157.245.38:443/?authSource=admin";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(10);

        var client = new MongoClient(settings);
        var database = client.GetDatabase("CartoonCharactersDB");

        _cartoonCharacters = database.GetCollection<CartoonCharacter>("CartoonCharactersCollection");

        Console.WriteLine("MongoDB : collection initialisée");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _cartoonCharacters.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1));

            Console.WriteLine("MongoDB : connexion OK");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MongoDB : connexion KO -> {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ReplaceAllCharactersAsync(List<CartoonCharacter> characters)
    {
        try
        {
            if (characters == null)
            {
                Console.WriteLine("MongoDB : liste null");
                return false;
            }

            foreach (var character in characters)
            {
                if (string.IsNullOrWhiteSpace(character.Id))
                    character.Id = ObjectId.GenerateNewId().ToString();
            }

            await _cartoonCharacters.DeleteManyAsync(
                Builders<CartoonCharacter>.Filter.Empty);

            if (characters.Count > 0)
            {
                await _cartoonCharacters.InsertManyAsync(characters);
            }

            Console.WriteLine($"MongoDB : envoi total OK -> {characters.Count} document(s)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MongoDB : envoi total KO -> {ex.Message}");
            return false;
        }
    }

    public async Task<List<CartoonCharacter>> GetAllCharactersAsync()
    {
        try
        {
            return await _cartoonCharacters
                .Find(Builders<CartoonCharacter>.Filter.Empty)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MongoDB : lecture KO -> {ex.Message}");
            return [];
        }
    }
}