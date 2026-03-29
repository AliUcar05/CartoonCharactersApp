using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage = null!;

    [ObservableProperty]
    private string _version = "Version : 1.0";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _hasSearchText;

    private readonly CsvServices _csvServices;

    public CollectionViewModel? CurrentCollectionViewModel { get; private set; }

    public MainWindowViewModel(CsvServices cscServices)
    {
        _csvServices = cscServices;

        var collectionVM = new CollectionViewModel(GoToDetailsFromChildCommand, this);
        CurrentPage = collectionVM;
        CurrentCollectionViewModel = collectionVM;

        _ = InitializeDataAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        HasSearchText = !string.IsNullOrWhiteSpace(value);
        CurrentCollectionViewModel?.ApplySearchFilter(value);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
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
            await DialogService.ShowMessage(
                "Erreur",
                $"❌ Erreur lors du chargement des données : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        try
        {
            var importedCharacters = await _csvServices.LoadDataAsync();

            if (!importedCharacters.Any())
            {
                await DialogService.ShowMessage(
                    "Import CSV",
                    "Aucune donnée trouvée dans le fichier CSV.");
                return;
            }

            var previewViewModel = new CsvImportPreviewViewModel(
                "Fichier sélectionné",
                importedCharacters,
                MyGlobals.MyCartoonCharacters,
                OnImportConfirmed,
                OnImportCancelled);

            CurrentPage = previewViewModel;
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage(
                "Erreur",
                $"❌ Erreur lors de l'import : {ex.Message}");
        }
    }

    private async void OnImportConfirmed(List<CartoonCharacter> selectedCharacters)
    {
        try
        {
            IsBusy = true;

            foreach (var selectedChar in selectedCharacters)
            {
                var existing = MyGlobals.MyCartoonCharacters
                    .FirstOrDefault(c => c.Id == selectedChar.Id);

                if (existing != null)
                {
                    existing.Name = selectedChar.Name;
                    existing.Description = selectedChar.Description;
                    existing.ImagePath = selectedChar.ImagePath;
                    existing.Rating = selectedChar.Rating;
                    existing.RatingVotes = selectedChar.RatingVotes;
                }
                else
                {
                    MyGlobals.MyCartoonCharacters.Add(selectedChar);
                }
            }

            await MyGlobals.SaveDataAsync();

            await DialogService.ShowMessage(
                "Import réussi",
                $"✅ {selectedCharacters.Count} personnage(s) ont été importés avec succès !");
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage(
                "Erreur",
                $"❌ Erreur lors de l'import : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BackToMain();
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
                _ = DialogService.ShowMessage(
                    "Export CSV",
                    "Aucun personnage à exporter.");
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
            _ = DialogService.ShowMessage(
                "Erreur",
                $"❌ Erreur : {ex.Message}");
        }
    }

    private async void OnExportConfirmed(List<CartoonCharacter> selectedCharacters)
    {
        try
        {
            await _csvServices.SaveDataAsync(selectedCharacters);

            await DialogService.ShowMessage(
                "Export réussi",
                $"✅ {selectedCharacters.Count} personnage(s) ont été exportés avec succès !");
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessage(
                "Erreur",
                $"❌ Erreur lors de l'export : {ex.Message}");
        }
        finally
        {
            BackToMain();
        }
    }

    private void OnExportCancelled()
    {
        BackToMain();
    }

    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }

    [RelayCommand]
    private void GoToDetailsFromChild(string cartoonCharacterId)
    {
        CurrentPage = new CollectionDetailsViewModel(cartoonCharacterId);
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
        SearchText = string.Empty;

        var collectionVM = new CollectionViewModel(GoToDetailsFromChildCommand, this);
        CurrentPage = collectionVM;
        CurrentCollectionViewModel = collectionVM;
    }
}