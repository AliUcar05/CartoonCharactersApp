using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _version = "Version : 1.0";
    [ObservableProperty] private string _qrCode = "En attente d'un scan...";

    private readonly CsvServices _csvServices;
    private ScannerManager? _myScanner;

    public MainWindowViewModel(CsvServices cscServices)
    {
        _csvServices = cscServices;

        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);

        try
        {
            _myScanner = new ScannerManager();
            _myScanner.SerialBuffer.Changed += QRCodeManager;
            _myScanner.OpenPort();
        }
        catch (Exception e)
        {
            QrCode = $"Erreur scanner : {e.Message}";
            Console.WriteLine(e.ToString());
        }
    }

    private void QRCodeManager(object? sender, EventArgs e)
    {
        if (_myScanner == null || _myScanner.SerialBuffer.Count == 0)
            return;

        var valeur = _myScanner.SerialBuffer.Dequeue()?.ToString() ?? string.Empty;

        Dispatcher.UIThread.Post(() =>
        {
            QrCode = valeur.Trim();
        });

        Console.WriteLine($"QR Code scanné : {valeur}");
    }
    
    // Nouvelles commandes pour CSV
    [RelayCommand]
    private async Task ImportCsv()
    {
        // TODO: Étape 3 - Implémenter la logique d'import
        var csvService = new CsvServices(GetTopLevel());
        var importedCharacters = await csvService.LoadDataAsync();
        
        // Pour l'instant, juste un message pour voir si ça fonctionne
        if (importedCharacters.Any())
        {
            Console.WriteLine($"✅ {importedCharacters.Count} personnages chargés depuis le CSV");
        }
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) return;

            // Récupérer les personnages actuels depuis MyGlobals
            var characters = MyGlobals.MyCartoonCharacters;
        
            if (!characters.Any())
            {
                Console.WriteLine("⚠️ Aucune donnée à exporter");
                // TODO: Afficher message à l'utilisateur
                return;
            }

            // Créer une copie des personnages avec les IDs en string (déjà le cas)
            var csvService = new CsvServices(topLevel);
            await csvService.SaveDataAsync(characters);
        
            Console.WriteLine($"✅ Export terminé : {characters.Count} personnages");
            // TODO: Afficher message de succès
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur export CSV: {ex.Message}");
            // TODO: Afficher message d'erreur
        }
    }

    // Méthode utilitaire pour obtenir le TopLevel (nécessaire pour CsvServices)
    private TopLevel GetTopLevel()
    {
        // Cette méthode dépend de comment vous gérez la fenêtre principale
        // Si vous avez accès à la fenêtre, vous pouvez faire :
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return TopLevel.GetTopLevel(desktop.MainWindow);
        }
        return null;
    }

    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }

    [RelayCommand]
    private void GoToDetailsFromChild(string animalId)
    {
        CurrentPage = new CollectionDetailsViewModel(animalId);
    }

    [RelayCommand]
    private void GoToAddCartoonCharacters()
    {
        CurrentPage = new CollectionAddViewModel(BackToMain);
    }

    public void GoToEditCartoonCharacter(string id)
    {
        CurrentPage = new CollectionEditViewModel(id, BackToMain);
    }

    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);
    }
}