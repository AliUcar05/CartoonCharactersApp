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
    private readonly IMongoCollection<UserProfile> _userProfiles;

    public DatabaseServices()
    {
        const string connectionUri = "mongodb://Meeeee:IAmTheBest@185.157.245.38:443/?authSource=admin";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(10);

        var client = new MongoClient(settings);
        var database = client.GetDatabase("CartoonCharactersDB");

        _cartoonCharacters = database.GetCollection<CartoonCharacter>("CartoonCharactersCollection");
        _userProfiles = database.GetCollection<UserProfile>("UserProfiles");

        Console.WriteLine("MongoDB : collections initialisées");
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

    public async Task<bool> InsertUserProfileAsync(UserProfile userProfile)
    {
        try
        {
            if (userProfile == null)
            {
                Console.WriteLine("MongoDB : userProfile null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(userProfile.Id))
                userProfile.Id = ObjectId.GenerateNewId().ToString();

            await _userProfiles.InsertOneAsync(userProfile);

            Console.WriteLine($"MongoDB : profil utilisateur inséré -> {userProfile.UserName}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MongoDB : insert profil KO -> {ex.Message}");
            return false;
        }
    }

    public async Task<List<UserProfile>> GetAllUserProfilesAsync()
    {
        try
        {
            return await _userProfiles
                .Find(Builders<UserProfile>.Filter.Empty)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MongoDB : lecture profils KO -> {ex.Message}");
            return [];
        }
    }
}