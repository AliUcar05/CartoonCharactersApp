using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

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

    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }

    [RelayCommand]
    private void GoToDetailsFromChild(ObjectId animalId)
    {
        CurrentPage = new CollectionDetailsViewModel(animalId);
    }

    [RelayCommand]
    private void GoToAddCartoonCharacters()
    {
        CurrentPage = new CollectionAddViewModel(BackToMain);
    }

    public void GoToEditCartoonCharacter(ObjectId id)
    {
        CurrentPage = new CollectionEditViewModel(id, BackToMain);
    }

    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);
    }
}