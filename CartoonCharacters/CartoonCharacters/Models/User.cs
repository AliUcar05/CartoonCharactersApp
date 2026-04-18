using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CartoonCharacters.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    public string Name { get; set; } = string.Empty;
    
    public string PassWord { get; set; } = string.Empty;
    
    public bool isAdmin { get; set; } = false;
}