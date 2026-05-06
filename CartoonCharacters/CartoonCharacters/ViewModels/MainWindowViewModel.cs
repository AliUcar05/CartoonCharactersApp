using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private string _version = "Version : 1.1";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _hasSearchText;

    [ObservableProperty]
    private UserProfile? _currentUser;

    public bool IsUserLoggedIn => CurrentUser != null;
    public bool IsCurrentUserAdmin => CurrentUser?.IsAdmin == true;

    public bool IsAdminPage =>
        CurrentPage is AdminUsersViewModel ||
        CurrentPage is AdminUserEditViewModel;

    public bool ShowNavigationBar => IsUserLoggedIn && !IsAdminPage;

    private readonly CsvServices _csvServices;
    private CollectionViewModel? _currentCollectionViewModel;

    public MainWindowViewModel(CsvServices csvServices)
    {
        _csvServices = csvServices;

        if (IsUserLoggedIn)
        {
            ShowMainCollection();
            _ = InitializeDataAsync();
        }
        else
        {
            ShowLoginPage();
        }
    }

    partial void OnCurrentUserChanged(UserProfile? value)
    {
        MyGlobals.CurrentUser = value;
        OnPropertyChanged(nameof(IsUserLoggedIn));
        OnPropertyChanged(nameof(IsCurrentUserAdmin));
        OnPropertyChanged(nameof(ShowNavigationBar));
    }

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsAdminPage));
        OnPropertyChanged(nameof(ShowNavigationBar));
    }

    partial void OnSearchTextChanged(string value)
    {
        HasSearchText = !string.IsNullOrWhiteSpace(value);
        _currentCollectionViewModel?.ApplySearchFilter(value);
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
            PopupService.Error("Erreur", $"Erreur lors du chargement des données : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnLoginSuccess(UserProfile userProfile)
    {
        CurrentUser = userProfile;
        ShowMainCollection();
        _ = InitializeDataAsync();
    }

    [RelayCommand]
    private void Logout()
    {
        CurrentUser = null;
        ShowLoginPage();
    }

    private void ShowMainCollection()
    {
        var collectionVm = new CollectionViewModel(
            GoToDetailsFromChildCommand,
            this,
            ShowDeleteMessageAsync);

        CurrentPage = collectionVm;
        _currentCollectionViewModel = collectionVm;
    }

    private void ShowLoginPage()
    {
        CurrentPage = new LoginViewModel(OnLoginSuccess, ShowRegisterPage);
    }

    private void ShowRegisterPage()
    {
        CurrentPage = new RegisterViewModel(OnLoginSuccess, ShowLoginPage);
    }

    [RelayCommand]
    private void GoToAdminUsers()
    {
        if (CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette page est réservée aux administrateurs.");
            return;
        }

        CurrentPage = new AdminUsersViewModel(this);
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        try
        {
            var importedCharacters = await _csvServices.LoadDataAsync();

            if (!importedCharacters.Any())
            {
                PopupService.Warning("Import CSV", "Aucune donnée trouvée dans le fichier CSV.");
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
            PopupService.Error("Erreur", $"Erreur lors de l'import : {ex.Message}");
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

            PopupService.Success("Import réussi", $"{selectedCharacters.Count} personnage(s) ont été importés avec succès !");
        }
        catch (Exception ex)
        {
            PopupService.Error("Erreur", $"Erreur lors de l'import : {ex.Message}");
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
                PopupService.Warning("Export CSV", "Aucun personnage à exporter.");
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
            PopupService.Error("Erreur", $"Erreur : {ex.Message}");
        }
    }

    private async void OnExportConfirmed(List<CartoonCharacter> selectedCharacters)
    {
        try
        {
            await _csvServices.SaveDataAsync(selectedCharacters);
            PopupService.Success("Export réussi", $"{selectedCharacters.Count} personnage(s) ont été exportés avec succès !");
        }
        catch (Exception ex)
        {
            PopupService.Error("Erreur", $"Erreur lors de l'export : {ex.Message}");
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

    private Task ShowDeleteMessageAsync(string characterName)
    {
        PopupService.Success("Suppression réussie", $"{characterName} a été supprimé avec succès !");
        return Task.CompletedTask;
    }

    private Task OnUpdateAsync(string characterName)
    {
        PopupService.Success("Modification réussie", $"{characterName} a été modifié avec succès !");
        BackToMain();

        return Task.CompletedTask;
    }

    private void OnCancelUpdate()
    {
        BackToMain();
    }

    private Task OnAddAsync(string characterName)
    {
        PopupService.Success("Ajout réussi", $"{characterName} a été ajouté avec succès !");
        BackToMain();

        return Task.CompletedTask;
    }

    private void OnCancelAdd()
    {
        BackToMain();
    }

    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase newValue)
    {
        _ = newValue;
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
        CurrentPage = new CollectionAddViewModel(
            OnAddAsync,
            OnCancelAdd);
    }

    public void GoToEditCartoonCharacter(string id)
    {
        CurrentPage = new CollectionEditViewModel(
            id,
            OnUpdateAsync,
            OnCancelUpdate);
    }

    public void GoToEditUser(string userId)
    {
        if (CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette page est réservée aux administrateurs.");
            return;
        }

        CurrentPage = new AdminUserEditViewModel(this, userId);
    }

    public void GoToCreateUser()
    {
        if (CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette page est réservée aux administrateurs.");
            return;
        }

        CurrentPage = new AdminUserEditViewModel(this);
    }

    [RelayCommand]
    public void BackToMain()
    {
        SearchText = string.Empty;
        ShowMainCollection();
    }

    public void BackToAdminUsers()
    {
        CurrentPage = new AdminUsersViewModel(this);
    }
}