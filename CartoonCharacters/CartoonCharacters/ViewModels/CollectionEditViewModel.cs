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

namespace CartoonCharacters.ViewModels;

public partial class CollectionEditViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private Bitmap? _picture;

    [ObservableProperty]
    private string[]? _selectedFiles;

    [ObservableProperty]
    private string _id;

    private readonly Func<string, Task> _onUpdateAsync;
    private readonly Action _onCancelUpdate;

    public CollectionEditViewModel(
        string characterId,
        Func<string, Task> onUpdateAsync,
        Action onCancelUpdate)
    {
        Id = characterId;
        _onUpdateAsync = onUpdateAsync;
        _onCancelUpdate = onCancelUpdate;

        var existingCharacter = MyGlobals.MyCartoonCharacters.FirstOrDefault(c => c.Id == Id);
        if (existingCharacter != null)
        {
            Name = existingCharacter.Name;
            Description = existingCharacter.Description;
            string? originalImagePath = existingCharacter.ImagePath;

            if (!string.IsNullOrEmpty(originalImagePath))
            {
                try
                {
                    if (originalImagePath.StartsWith("avares://"))
                    {
                        Picture = ImageHelper.LoadFromResource(new Uri(originalImagePath));
                    }
                    else if (File.Exists(originalImagePath))
                    {
                        using var fs = File.OpenRead(originalImagePath);
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
    private async Task SaveChanges()
    {
        var existingCharacter = MyGlobals.MyCartoonCharacters.FirstOrDefault(c => c.Id == Id);
        if (existingCharacter != null)
        {
            existingCharacter.Name = Name;
            existingCharacter.Description = Description;

            if (SelectedFiles != null && SelectedFiles.Length > 0)
            {
                var fileName = Path.GetFileName(SelectedFiles[0]);
                existingCharacter.ImagePath = $"avares://CartoonCharacters/Assets/{fileName}";
            }

            await MyGlobals.SaveDataAsync();
            await _onUpdateAsync(existingCharacter.Name);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancelUpdate.Invoke();
    }
}