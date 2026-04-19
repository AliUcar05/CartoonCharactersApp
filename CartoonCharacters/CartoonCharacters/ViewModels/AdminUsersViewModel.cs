using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class AdminUsersViewModel : ViewModelBase
{
    private readonly DatabaseServices _databaseServices = new();
    private readonly MainWindowViewModel _mainWindowViewModel;

    [ObservableProperty]
    private ObservableCollection<UserProfile> _users = [];

    [ObservableProperty]
    private bool _isBusy;

    public AdminUsersViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        if (MyGlobals.CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette page est réservée aux administrateurs.");
            _mainWindowViewModel.BackToMain();
            return;
        }

        _ = LoadUsersAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadUsersAsync();
    }

    [RelayCommand]
    private void CreateUser()
    {
        _mainWindowViewModel.GoToCreateUser();
    }

    [RelayCommand]
    private void EditUser(UserProfile? user)
    {
        if (user == null)
            return;

        _mainWindowViewModel.GoToEditUser(user.Id);
    }

    [RelayCommand]
    private async Task DeleteUser(UserProfile? user)
    {
        if (user == null)
            return;

        if (MyGlobals.CurrentUser == null || MyGlobals.CurrentUser.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette action est réservée aux administrateurs.");
            return;
        }

        if (user.Id == MyGlobals.CurrentUser.Id)
        {
            PopupService.Warning("Suppression refusée", "Tu ne peux pas supprimer ton propre compte.");
            return;
        }

        if (user.IsAdmin)
        {
            var adminCount = await _databaseServices.CountAdminsAsync();
            if (adminCount <= 1)
            {
                PopupService.Warning("Suppression refusée", "Impossible de supprimer le dernier administrateur.");
                return;
            }
        }

        var deleted = await _databaseServices.DeleteUserProfileAsync(user.Id);

        if (deleted)
        {
            PopupService.Success("Suppression réussie", $"L'utilisateur {user.UserName} a été supprimé.");
            await LoadUsersAsync();
        }
        else
        {
            PopupService.Error("Erreur", "La suppression de l'utilisateur a échoué.");
        }
    }

    [RelayCommand]
    private void Back()
    {
        _mainWindowViewModel.BackToMain();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;

            var allUsers = await _databaseServices.GetAllUserProfilesAsync();

            Users = new ObservableCollection<UserProfile>(
                allUsers
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ThenBy(u => u.UserName));
        }
        catch (Exception ex)
        {
            PopupService.Error("Erreur", $"Impossible de charger les utilisateurs : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}