namespace CartoonCharacters.Models;

/// <summary>
/// Classe intermédiaire pour la sérialisation JSON des personnages
/// </summary>
public class CartoonCharacterJson
{
    /// <summary>
    /// Identifiant unique du personnage (au format string pour JSON)
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Nom du personnage
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description du personnage
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Chemin vers l'image (URL, chemin local, ou ressource Avalonia)
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;
}