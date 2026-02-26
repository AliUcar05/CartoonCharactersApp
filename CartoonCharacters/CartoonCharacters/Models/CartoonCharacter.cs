using MongoDB.Bson;
// ⚠️ SUPPRIMER: using Avalonia.Media.Imaging;

namespace CartoonCharacters.Models;

public class CartoonCharacter
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Remplacer Bitmap par string pour le chemin
    public string? ImagePath { get; set; }  // ← Nouveau
    // SUPPRIMER: public Bitmap? Picture { get; set; }
}