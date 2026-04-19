using System;
using System.Net.Mail;
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

    [ObservableProperty]
    private string _userName = string.Empty;

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
            var normalizedEmail = Email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedUserName) ||
                string.IsNullOrWhiteSpace(normalizedEmail) ||
                string.IsNullOrWhiteSpace(Password))
            {
                PopupService.Warning("Registration", "Username, email and password are required.");
                return;
            }

            if (!IsValidEmail(normalizedEmail))
            {
                PopupService.Warning("Registration", "Please enter a valid email address.");
                return;
            }

            var userExists = await _databaseServices.UserExistsAsync(normalizedUserName);
            if (userExists)
            {
                PopupService.Error("Registration", "This username already exists.");
                return;
            }

            var emailExists = await _databaseServices.EmailExistsAsync(normalizedEmail);
            if (emailExists)
            {
                PopupService.Error("Registration", "This email already exists.");
                return;
            }

            var newUser = new UserProfile
            {
                UserName = normalizedUserName,
                Email = normalizedEmail,
                Password = Password,
                IsAdmin = false
            };

            var inserted = await _databaseServices.InsertUserProfileAsync(newUser);

            if (inserted)
            {
                PopupService.Success("Registration", "Account created successfully.");
                _onLoginSuccess.Invoke(newUser);
            }
            else
            {
                PopupService.Error("Registration", "Unable to create the account.");
            }
        }
        catch (Exception ex)
        {
            PopupService.Error("Registration", $"Error during registration: {ex.Message}");
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
        try
        {
            var mailAddress = new MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }
}