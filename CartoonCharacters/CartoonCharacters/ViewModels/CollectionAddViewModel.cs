using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;

namespace CartoonCharacters.ViewModels;

public partial class CollectionAddViewModel : ViewModelBase
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private Bitmap? picture;

    [ObservableProperty]
    private string[]? selectedFiles;

    private readonly Action _goBack;

    public CollectionAddViewModel(Action goBack)
    {
        _goBack = goBack;
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
    private async Task AddCartoonCharacter()
    {
        if (SelectedFiles == null || SelectedFiles.Length == 0)
            return;

        var fileName = Path.GetFileName(SelectedFiles[0]);

        var cartoonCharacter = new CartoonCharacter
        {
            Name = Name,
            Description = Description,
            ImagePath = $"avares://CartoonCharacters/Assets/{fileName}"
        };

        MyGlobals.MyCartoonCharacters.Add(cartoonCharacter);
        await MyGlobals.SaveDataAsync();

        _goBack.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        _goBack.Invoke();
    }
}