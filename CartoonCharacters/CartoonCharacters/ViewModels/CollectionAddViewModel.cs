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

    public ScannerManager? MyScanner { get; private set; }

    private readonly Func<string, Task> _onAddAsync;
    private readonly Action _onCancelAdd;

    public CollectionAddViewModel(
        Func<string, Task> onAddAsync,
        Action onCancelAdd)
    {
        _onAddAsync = onAddAsync;
        _onCancelAdd = onCancelAdd;

        try
        {
            MyScanner = new ScannerManager();
            MyScanner.SerialBuffer.Changed += QrCodeManager;
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

        await _onAddAsync(cartoonCharacter.Name);
    }

    private void QrCodeManager(object? sender, EventArgs e)
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
        if (MyScanner != null)
        {
            MyScanner.SerialBuffer.Changed -= QrCodeManager;
            MyScanner.ClosePort();
        }

        base.Dispose();
    }
}