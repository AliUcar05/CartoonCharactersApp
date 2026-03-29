using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CartoonCharacters.Models;

public class CartoonCharacter
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImagePath { get; set; }

    public double Rating { get; set; }
    public int RatingVotes { get; set; }
}