using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly Action<UserProfile> _onLoginSuccess;
    private readonly Action _goToRegister;
    private readonly DatabaseServices _databaseServices;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isPasswordVisible;

    public LoginViewModel(Action<UserProfile> onLoginSuccess, Action goToRegister)
    {
        _onLoginSuccess = onLoginSuccess;
        _goToRegister = goToRegister;
        _databaseServices = new DatabaseServices();
    }

    public LoginViewModel()
    {
        _onLoginSuccess = _ => { };
        _goToRegister = () => { };
        _databaseServices = new DatabaseServices();
    }

    [RelayCommand]
    private async Task Login()
    {
        try
        {
            IsBusy = true;

            var user = await _databaseServices.AuthenticateUserAsync(Username, Password);

            if (user != null)
            {
                _onLoginSuccess.Invoke(user);
            }
            else
            {
                PopupService.Error("Connexion", "Nom d'utilisateur ou mot de passe incorrect.");
            }
        }
        catch (Exception ex)
        {
            PopupService.Error("Connexion", $"Erreur lors de la connexion : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToRegister()
    {
        _goToRegister();
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }
}