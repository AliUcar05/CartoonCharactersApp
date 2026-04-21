using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CartoonCharacters.Models;

public class UserProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string UserName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
    
    public List<string> CartoonCharacterIds { get; set; } = [];

    // Key   = Id du personnage
    // Value = note donnée par l'utilisateur (1 à 5)
    public Dictionary<string, int> CharacterRatings { get; set; } = [];
}