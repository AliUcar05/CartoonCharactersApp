using System;
using MongoDB.Driver;
using CartoonCharacters.Models;

namespace CartoonCharacters.Services;

public partial class DatabaseServices
{
    private readonly IMongoCollection<CartoonCharacter> _cartoonCharacters;

    public DatabaseServices()
    {
        const string connectionUri = "mongodb://Meeeee:%20IAmTheBest@185.157.245.38:443/";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
        settings.SocketTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(settings);
        var database = client.GetDatabase("CartoonCharactersDB");

        _cartoonCharacters = database.GetCollection<CartoonCharacter>("CartoonCharactersCollection");

        Console.WriteLine(_cartoonCharacters);
    }
}