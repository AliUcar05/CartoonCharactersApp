namespace CartoonCharacters.Models;

/// <summary>
/// Classe intermédiaire pour la sérialisation JSON des personnages
/// </summary>
public class CartoonCharacterJson
{
    /// Identifiant unique du personnage (au format string pour JSON)
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// Chemin vers l'image (URL, chemin local, ou ressource Avalonia)
    public string ImagePath { get; set; } = string.Empty;
}