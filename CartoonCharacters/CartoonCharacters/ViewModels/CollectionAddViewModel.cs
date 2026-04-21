using System;
using System.Collections.Generic;
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
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class CollectionAddViewModel : ViewModelBase
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
    private string _qrCode = "En attente d'un scan...";

    private ScannerManager? _scanner;

    private readonly Func<string, Task> _onAddAsync;
    private readonly Action _onCancelAdd;
    private readonly DatabaseServices _databaseServices = new();

    public CollectionAddViewModel(
        Func<string, Task> onAddAsync,
        Action onCancelAdd)
    {
        _onAddAsync = onAddAsync;
        _onCancelAdd = onCancelAdd;

        try
        {
            _scanner = new ScannerManager();
            _scanner.SerialBuffer.Changed += QrCodeManager;
            _scanner.OpenPort();
        }
        catch (Exception e)
        {
            QrCode = $"Erreur scanner : {e.Message}";
            PopupService.Error("Scanner", $"Erreur scanner : {e.Message}");
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
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"]
                }
            ]
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        SelectedFiles = [path];

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
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            missingFields.Add("le nom");

        if (string.IsNullOrWhiteSpace(Description))
            missingFields.Add("la description");

        var selectedFiles = SelectedFiles;
        if (selectedFiles is null || selectedFiles.Length == 0)
            missingFields.Add("l'image");

        if (missingFields.Count > 0)
        {
            PopupService.Warning(
                "Champs obligatoires",
                $"Merci de renseigner : {string.Join(", ", missingFields)}.");
            return;
        }

        var fileName = Path.GetFileName(selectedFiles![0]);

        var cartoonCharacter = new CartoonCharacter
        {
            Name = Name.Trim(),
            Description = Description.Trim(),
            ImagePath = $"avares://CartoonCharacters/Assets/{fileName}",
            Rating = 0,
            RatingVotes = 0
        };

        MyGlobals.MyCartoonCharacters.Add(cartoonCharacter);

        if (MyGlobals.CurrentUser != null &&
            !MyGlobals.CurrentUser.CartoonCharacterIds.Contains(cartoonCharacter.Id))
        {
            MyGlobals.CurrentUser.CartoonCharacterIds.Add(cartoonCharacter.Id);
            await _databaseServices.UpdateUserProfileAsync(MyGlobals.CurrentUser);
        }

        await MyGlobals.SaveDataAsync();

        await _onAddAsync(cartoonCharacter.Name);
    }

    private void QrCodeManager(object? sender, EventArgs e)
    {
        if (_scanner == null || _scanner.SerialBuffer.Count == 0)
            return;

        QrCode = _scanner.SerialBuffer.Dequeue()?.ToString() ?? string.Empty;
        Console.WriteLine($"QR Code scanné : {QrCode}");
    }

    [RelayCommand]
    private async Task AddCartoonCharacterWithScanner()
    {
        if (string.IsNullOrWhiteSpace(QrCode) ||
            QrCode == "En attente d'un scan...")
        {
            PopupService.Warning("Scanner", "Aucun QR code valide n'a été scanné.");
            return;
        }

        if (QrCode.StartsWith("Erreur scanner"))
        {
            PopupService.Error("Scanner", QrCode);
            return;
        }

        string[] parties = QrCode.Split(',');

        if (parties.Length < 3)
        {
            PopupService.Warning("QR code invalide", "Format attendu : nom,description,imagePath");
            return;
        }

        string nom = parties[0].Trim();
        string description = parties[1].Trim();
        string imagePath = parties[2].Trim();

        Name = nom;
        Description = description;

        var cartoonCharacter = new CartoonCharacter
        {
            Name = nom,
            Description = description,
            ImagePath = imagePath,
            Rating = 0,
            RatingVotes = 0
        };

        MyGlobals.MyCartoonCharacters.Add(cartoonCharacter);

        if (MyGlobals.CurrentUser != null &&
            !MyGlobals.CurrentUser.CartoonCharacterIds.Contains(cartoonCharacter.Id))
        {
            MyGlobals.CurrentUser.CartoonCharacterIds.Add(cartoonCharacter.Id);
            await _databaseServices.UpdateUserProfileAsync(MyGlobals.CurrentUser);
        }

        await MyGlobals.SaveDataAsync();

        await _onAddAsync(cartoonCharacter.Name);
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancelAdd.Invoke();
    }

    public override void Dispose()
    {
        if (_scanner != null)
        {
            _scanner.SerialBuffer.Changed -= QrCodeManager;
            _scanner.ClosePort();
        }

        base.Dispose();
    }
}