using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class AdminUserEditViewModel : ViewModelBase
{
    private readonly DatabaseServices _databaseServices = new();
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly bool _isEditMode;
    private readonly string? _userId;

    [ObservableProperty]
    private string _title = "Créer un utilisateur";

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isPasswordVisible;

    public bool IsEditMode => _isEditMode;

    public AdminUserEditViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        if (MyGlobals.CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette page est réservée aux administrateurs.");
            _mainWindowViewModel.BackToMain();
            return;
        }

        _isEditMode = false;
        Title = "Créer un utilisateur";
    }

    public AdminUserEditViewModel(MainWindowViewModel mainWindowViewModel, string userId)
    {
        _mainWindowViewModel = mainWindowViewModel;

        if (MyGlobals.CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette page est réservée aux administrateurs.");
            _mainWindowViewModel.BackToMain();
            return;
        }

        _isEditMode = true;
        _userId = userId;
        Title = "Modifier un utilisateur";

        _ = LoadUserAsync(userId);
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (MyGlobals.CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette action est réservée aux administrateurs.");
            return;
        }

        if (string.IsNullOrWhiteSpace(UserName) ||
            string.IsNullOrWhiteSpace(FirstName) ||
            string.IsNullOrWhiteSpace(LastName) ||
            string.IsNullOrWhiteSpace(Email))
        {
            PopupService.Warning("Champs obligatoires", "Merci de remplir tous les champs obligatoires.");
            return;
        }

        if (!_isEditMode && string.IsNullOrWhiteSpace(Password))
        {
            PopupService.Warning("Mot de passe requis", "Merci de renseigner un mot de passe.");
            return;
        }

        try
        {
            IsBusy = true;

            if (_isEditMode)
            {
                var existingUser = await _databaseServices.GetUserByIdAsync(_userId!);

                if (existingUser == null)
                {
                    PopupService.Error("Erreur", "Utilisateur introuvable.");
                    return;
                }

                if (await _databaseServices.UserNameExistsForAnotherUserAsync(UserName, existingUser.Id))
                {
                    PopupService.Warning("Nom d'utilisateur déjà utilisé", "Choisis un autre nom d'utilisateur.");
                    return;
                }

                if (await _databaseServices.EmailExistsForAnotherUserAsync(Email, existingUser.Id))
                {
                    PopupService.Warning("Email déjà utilisé", "Choisis une autre adresse email.");
                    return;
                }

                if (existingUser.IsAdmin && !IsAdmin)
                {
                    var adminCount = await _databaseServices.CountAdminsAsync();

                    if (adminCount <= 1)
                    {
                        PopupService.Warning("Modification refusée", "Impossible de retirer le rôle admin au dernier administrateur.");
                        return;
                    }
                }

                existingUser.UserName = UserName.Trim();
                existingUser.FirstName = FirstName.Trim();
                existingUser.LastName = LastName.Trim();
                existingUser.Email = Email.Trim().ToLowerInvariant();
                existingUser.IsAdmin = IsAdmin;

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    existingUser.Password = Password;
                }

                var updated = await _databaseServices.UpdateUserProfileAsync(existingUser);

                if (!updated)
                {
                    PopupService.Error("Erreur", "La modification de l'utilisateur a échoué.");
                    return;
                }

                if (MyGlobals.CurrentUser.Id == existingUser.Id)
                {
                    MyGlobals.CurrentUser = existingUser;
                }

                PopupService.Success("Modification réussie", "L'utilisateur a été modifié avec succès.");
                _mainWindowViewModel.BackToAdminUsers();
            }
            else
            {
                if (await _databaseServices.UserExistsAsync(UserName))
                {
                    PopupService.Warning("Nom d'utilisateur déjà utilisé", "Choisis un autre nom d'utilisateur.");
                    return;
                }

                if (await _databaseServices.EmailExistsAsync(Email))
                {
                    PopupService.Warning("Email déjà utilisé", "Choisis une autre adresse email.");
                    return;
                }

                var newUser = new UserProfile
                {
                    UserName = UserName.Trim(),
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email.Trim().ToLowerInvariant(),
                    Password = Password,
                    IsAdmin = IsAdmin,
                    CharacterRatings = []
                };

                var inserted = await _databaseServices.InsertUserProfileAsync(newUser);

                if (!inserted)
                {
                    PopupService.Error("Erreur", "La création de l'utilisateur a échoué.");
                    return;
                }

                PopupService.Success("Création réussie", "L'utilisateur a été créé avec succès.");
                _mainWindowViewModel.BackToAdminUsers();
            }
        }
        catch (Exception ex)
        {
            PopupService.Error("Erreur", $"Erreur lors de l'enregistrement : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainWindowViewModel.BackToAdminUsers();
    }

    private async Task LoadUserAsync(string userId)
    {
        try
        {
            IsBusy = true;

            var user = await _databaseServices.GetUserByIdAsync(userId);

            if (user == null)
            {
                PopupService.Error("Erreur", "Utilisateur introuvable.");
                _mainWindowViewModel.BackToAdminUsers();
                return;
            }

            UserName = user.UserName;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Password = string.Empty;
            IsAdmin = user.IsAdmin;
        }
        catch (Exception ex)
        {
            PopupService.Error("Erreur", $"Impossible de charger l'utilisateur : {ex.Message}");
            _mainWindowViewModel.BackToAdminUsers();
        }
        finally
        {
            IsBusy = false;
        }
    }
}