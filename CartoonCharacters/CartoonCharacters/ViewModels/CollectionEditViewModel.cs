using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CartoonCharacters.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using CartoonCharacters.Models;

namespace CartoonCharacters.ViewModels;

public partial class CollectionEditViewModel : ViewModelBase
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private Bitmap? picture;

    [ObservableProperty]
    private string[]? selectedFiles;
    
    [ObservableProperty]
    private string id;
    
    private readonly Action _goBack;
    private string? _originalImagePath;

    public CollectionEditViewModel(string characterId, Action goBack)
    {
        Id = characterId;
        _goBack = goBack;
        
        // Charger les données existantes
        var existingCharacter = MyGlobals.MyCartoonCharacters.FirstOrDefault(c => c.Id == Id);
        if (existingCharacter != null)
        {
            Name = existingCharacter.Name;
            Description = existingCharacter.Description;
            _originalImagePath = existingCharacter.ImagePath;
            
            // Charger l'image pour l'aperçu
            if (!string.IsNullOrEmpty(_originalImagePath))
            {
                try
                {
                    if (_originalImagePath.StartsWith("avares://"))
                    {
                        Picture = ImageHelper.LoadFromResource(new Uri(_originalImagePath));
                    }
                    else if (File.Exists(_originalImagePath))
                    {
                        using var fs = File.OpenRead(_originalImagePath);
                        Picture = new Bitmap(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur chargement image: {ex.Message}");
                }
            }
        }
    }
    
    [RelayCommand]
    private async Task SelectFilesAsync()
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider is null)
            return;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                }
            }
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        SelectedFiles = new[] { path };
        
        await using var fs = File.OpenRead(path);
        Picture = new Bitmap(fs);
    }

    private static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow?.StorageProvider;

        return null;
    }

    [RelayCommand]
    private void SaveChanges()
    {
        var existingCharacter = MyGlobals.MyCartoonCharacters.FirstOrDefault(c => c.Id == Id);
        if (existingCharacter != null)
        {
            // Mettre à jour les propriétés
            existingCharacter.Name = Name;
            existingCharacter.Description = Description;
            
            // Mettre à jour l'image si une nouvelle a été sélectionnée
            if (SelectedFiles != null && SelectedFiles.Length > 0)
            {
                var fileName = Path.GetFileName(SelectedFiles[0]);
                // Stocker en format avares://
                existingCharacter.ImagePath = $"avares://CartoonCharacters/Assets/{fileName}";  // ← CORRECTION ICI
            }
            
            // Sauvegarder dans le JSON
            MyGlobals.SaveData();
        }
        
        _goBack.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        _goBack.Invoke();
    }
}