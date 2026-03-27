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

    private readonly Action _goBack;

    public CollectionAddViewModel(Action goBack)
    {
        _goBack = goBack;

        try
        {
            MyScanner = new ScannerManager();
            MyScanner.SerialBuffer.Changed += QRCodeManager;
            MyScanner.OpenPort();
        }
        catch (Exception e)
        {
            QrCode = $"Erreur scanner : {e.Message}";
            Console.WriteLine(e.ToString());
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

    private void QRCodeManager(object? sender, EventArgs e)
    {
        if (MyScanner == null || MyScanner.SerialBuffer.Count == 0)
            return;

        QrCode = MyScanner.SerialBuffer.Dequeue()?.ToString() ?? string.Empty;
        Console.WriteLine($"QR Code scanné : {QrCode}");
    }

    [RelayCommand]
    private async Task AddCartoonCharacterWithScanner()
    {
        if (string.IsNullOrWhiteSpace(QrCode) ||
            QrCode == "En attente d'un scan..." ||
            QrCode.StartsWith("Erreur scanner"))
            return;

        string[] parties = QrCode.Split(',');

        if (parties.Length < 3)
        {
            Console.WriteLine("QR code invalide. Format attendu : nom,description,imagePath");
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
            ImagePath = imagePath
        };

        MyGlobals.MyCartoonCharacters.Add(cartoonCharacter);

        Console.WriteLine("Objet créé avec succès");
        Console.WriteLine($"Nom : {cartoonCharacter.Name}");
        Console.WriteLine($"Description : {cartoonCharacter.Description}");
        Console.WriteLine($"Image : {cartoonCharacter.ImagePath}");

        await MyGlobals.SaveDataAsync();

        _goBack.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        _goBack.Invoke();
    }

    public override void Dispose()
    {
        if (MyScanner != null)
            MyScanner.SerialBuffer.Changed -= QRCodeManager;

        base.Dispose();
    }
}