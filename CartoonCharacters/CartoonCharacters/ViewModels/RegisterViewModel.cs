using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CartoonCharacters.Models;
using CartoonCharacters.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartoonCharacters.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly Action<UserProfile> _onLoginSuccess;
    private readonly Action _goToLogin;
    private readonly DatabaseServices _databaseServices;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase
    );

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
    private bool _isBusy;

    public RegisterViewModel()
    {
        _onLoginSuccess = _ => { };
        _goToLogin = () => { };
        _databaseServices = new DatabaseServices();
    }

    public RegisterViewModel(Action<UserProfile> onLoginSuccess, Action goToLogin)
    {
        _onLoginSuccess = onLoginSuccess;
        _goToLogin = goToLogin;
        _databaseServices = new DatabaseServices();
    }

    [RelayCommand]
    private async Task Register()
    {
        try
        {
            IsBusy = true;

            var normalizedUserName = UserName.Trim();
            var normalizedFirstName = FirstName.Trim();
            var normalizedLastName = LastName.Trim();
            var normalizedEmail = Email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedUserName) ||
                string.IsNullOrWhiteSpace(normalizedFirstName) ||
                string.IsNullOrWhiteSpace(normalizedLastName) ||
                string.IsNullOrWhiteSpace(normalizedEmail) ||
                string.IsNullOrWhiteSpace(Password))
            {
                PopupService.Warning("Inscription", "Tous les champs sont obligatoires.");
                return;
            }

            if (!IsValidEmail(normalizedEmail))
            {
                PopupService.Warning("Inscription", "Veuillez entrer une adresse email valide.");
                return;
            }

            var userExists = await _databaseServices.UserExistsAsync(normalizedUserName);
            if (userExists)
            {
                PopupService.Error("Inscription", "Ce nom d'utilisateur existe déjà.");
                return;
            }

            var emailExists = await _databaseServices.EmailExistsAsync(normalizedEmail);
            if (emailExists)
            {
                PopupService.Error("Inscription", "Cette adresse email existe déjà.");
                return;
            }

            var newUser = new UserProfile
            {
                UserName = normalizedUserName,
                FirstName = normalizedFirstName,
                LastName = normalizedLastName,
                Email = normalizedEmail,
                Password = Password,
                IsAdmin = false
            };

            var inserted = await _databaseServices.InsertUserProfileAsync(newUser);

            if (inserted)
            {
                PopupService.Success("Inscription", "Compte créé avec succès.");
                _onLoginSuccess.Invoke(newUser);
            }
            else
            {
                PopupService.Error("Inscription", "Impossible de créer le compte.");
            }
        }
        catch (Exception ex)
        {
            PopupService.Error("Inscription", $"Erreur pendant l'inscription : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToLogin()
    {
        _goToLogin();
    }

    private bool IsValidEmail(string email)
    {
        return EmailRegex.IsMatch(email);
    }
}