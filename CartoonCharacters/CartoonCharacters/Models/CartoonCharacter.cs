using Avalonia.Media.Imaging;
using MongoDB.Bson;

namespace CartoonCharacters.Models;

public class CartoonCharacter
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Bitmap? Picture { get; set; }
}