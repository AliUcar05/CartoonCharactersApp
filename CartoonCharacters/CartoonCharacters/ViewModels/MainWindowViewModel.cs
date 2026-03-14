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
using System.Collections.Generic;
using Avalonia.Platform.Storage;

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
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) 
            {
                // Attendre un peu et réessayer
                await Task.Delay(100);
                topLevel = GetTopLevel();
                if (topLevel == null)
                {
                    await DialogService.ShowMessage("Erreur", "Impossible d'accéder à la fenêtre");
                    return;
                }
            }

            // Petit délai pour être sûr que tout est prêt
            await Task.Delay(50);

            // Sélection avec filtre .csv
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Sélectionnez un fichier CSV",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Fichiers CSV")
                    {
                        Patterns = new[] { "*.csv" }
                    }
                }
            });

            if (files.Count == 0)
                return;

            var file = files[0];
        
            // Lire le CSV avec un nouveau service à chaque fois
            var csvService = new CsvServices(topLevel); // Si CsvServices est IDisposable
            var importedCharacters = await csvService.LoadDataAsync();

            if (!importedCharacters.Any())
            {
                await DialogService.ShowMessage("Import CSV", "Aucune donnée trouvée dans le fichier CSV.");
                return;
            }

            // Ouvrir la prévisualisation
            var previewViewModel = new CsvImportPreviewViewModel(
                file.Name,
                importedCharacters,
                MyGlobals.MyCartoonCharacters,
                OnImportConfirmed,
                OnImportCancelled);
        
            CurrentPage = previewViewModel;
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Erreur", $"❌ {ex.Message}");
        }
    }
    
    private async void OnImportConfirmed(List<CartoonCharacter> selectedCharacters)
    {
        try
        {
            // Fusionner avec les données existantes
            var existingCharacters = MyGlobals.MyCartoonCharacters;
        
            foreach (var selectedChar in selectedCharacters)
            {
                var existing = existingCharacters.FirstOrDefault(e => e.Id == selectedChar.Id);
                if (existing != null)
                {
                    // Mise à jour
                    existing.Name = selectedChar.Name;
                    existing.Description = selectedChar.Description;
                    existing.ImagePath = selectedChar.ImagePath;
                }
                else
                {
                    // Nouveau personnage
                    existingCharacters.Add(selectedChar);
                }
            }
        
            // Sauvegarder
            MyGlobals.SaveData();
        
            await DialogService.ShowMessage("Import réussi", 
                $"✅ {selectedCharacters.Count} personnage(s) ont été importés avec succès !");
        
            // Retour à l'accueil
            BackToMain();
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Erreur", $"❌ Erreur lors de l'import : {ex.Message}");
        }
    }
    
    private void OnImportCancelled()
    {
        // Retour à l'accueil sans importer
        BackToMain();
    }


    [RelayCommand]
    private void ExportCsv()
    {
        try
        {
            var characters = MyGlobals.MyCartoonCharacters;
        
            if (!characters.Any())
            {
                _ = DialogService.ShowMessage("Export CSV", "Aucun personnage à exporter.");
                return;
            }

            // Ouvrir la page de sélection
            var selectionViewModel = new CsvExportSelectionViewModel(
                characters,
                OnExportConfirmed,
                OnExportCancelled);
        
            CurrentPage = selectionViewModel;
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowMessage("Erreur", $"❌ Erreur : {ex.Message}");
        }
    }

    private async void OnExportConfirmed(List<CartoonCharacter> selectedCharacters)
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) return;

            var csvService = new CsvServices(topLevel);
            await csvService.SaveDataAsync(selectedCharacters);
        
            await DialogService.ShowMessage("Export réussi", 
                $"✅ {selectedCharacters.Count} personnage(s) ont été exportés avec succès !");
        
            BackToMain();
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Erreur", $"❌ Erreur lors de l'export : {ex.Message}");
        }
    }

    private void OnExportCancelled()
    {
        BackToMain();
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