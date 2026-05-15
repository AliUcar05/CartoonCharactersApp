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

    [ObservableProperty]
    private bool _isRefreshing;

    public AdminUsersViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        if (MyGlobals.CurrentUser?.IsAdmin != true)
        {
            PopupService.Warning("Accès refusé", "Cette page est réservée aux administrateurs.");
            _mainWindowViewModel.BackToMain();
            return;
        }

        InitializeUsersLoading();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (IsRefreshing) return;

        var areUsersLoaded = await LoadUsersAsync(true);

        if (!areUsersLoaded)
            return;
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
            var areUsersReloaded = await LoadUsersAsync();

            if (!areUsersReloaded)
                return;
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

    private async void InitializeUsersLoading()
    {
        var areUsersLoaded = await LoadUsersAsync();

        if (!areUsersLoaded)
            return;
    }

    private async Task<bool> LoadUsersAsync(bool isRefresh = false)
    {
        try
        {
            if (isRefresh)
                IsRefreshing = true;
            else
                IsBusy = true;

            var allUsers = await _databaseServices.GetAllUserProfilesAsync();

            Users = new ObservableCollection<UserProfile>(
                allUsers
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ThenBy(u => u.UserName));

            return true;
        }
        catch (Exception ex)
        {
            PopupService.Error("Erreur", $"Impossible de charger les utilisateurs : {ex.Message}");
            return false;
        }
        finally
        {
            if (isRefresh)
                IsRefreshing = false;
            else
                IsBusy = false;
        }
    }
}