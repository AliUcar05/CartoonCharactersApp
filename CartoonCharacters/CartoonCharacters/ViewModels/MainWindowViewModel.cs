using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;
using CartoonCharacters.Services;
using System.Collections.Generic;

namespace CartoonCharacters.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _version = "Version : 1.0";
    [ObservableProperty] private string _qrCode = "En attente d'un scan...";
    [ObservableProperty] private bool _isBusy;

    private readonly CsvServices _csvServices;
    private ScannerManager? _myScanner;

    public MainWindowViewModel(CsvServices cscServices)
    {
        _csvServices = cscServices;

        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);

        _ = InitializeDataAsync();

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

    private async Task InitializeDataAsync()
    {
        try
        {
            IsBusy = true;
            await MyGlobals.InitializeAsync();
            BackToMain();
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Erreur", $"❌ Erreur lors du chargement des données : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
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

    [RelayCommand]
    private async Task ImportCsv()
    {
        try
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) return;

            var csvService = new CsvServices(topLevel);
            var importedCharacters = await csvService.LoadDataAsync();

            if (!importedCharacters.Any())
            {
                await DialogService.ShowMessage("Import CSV", "Aucune donnée trouvée dans le fichier CSV.");
                return;
            }

            var fileName = "Fichier sélectionné";

            var previewViewModel = new CsvImportPreviewViewModel(
                fileName,
                importedCharacters,
                MyGlobals.MyCartoonCharacters,
                OnImportConfirmed,
                OnImportCancelled);

            CurrentPage = previewViewModel;
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Erreur", $"❌ Erreur lors de l'import : {ex.Message}");
        }
    }

    private async void OnImportConfirmed(List<CartoonCharacter> selectedCharacters)
    {
        try
        {
            IsBusy = true;

            var existingCharacters = MyGlobals.MyCartoonCharacters;

            foreach (var selectedChar in selectedCharacters)
            {
                var existing = existingCharacters.FirstOrDefault(e => e.Id == selectedChar.Id);
                if (existing != null)
                {
                    existing.Name = selectedChar.Name;
                    existing.Description = selectedChar.Description;
                    existing.ImagePath = selectedChar.ImagePath;
                }
                else
                {
                    existingCharacters.Add(selectedChar);
                }
            }

            await MyGlobals.SaveDataAsync();

            await DialogService.ShowMessage(
                "Import réussi",
                $"✅ {selectedCharacters.Count} personnage(s) ont été importés avec succès !");

            BackToMain();
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage("Erreur", $"❌ Erreur lors de l'import : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnImportCancelled()
    {
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

            await DialogService.ShowMessage(
                "Export réussi",
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

    private TopLevel? GetTopLevel()
    {
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