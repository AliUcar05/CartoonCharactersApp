using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;
using MyProjectBase.Services;

namespace CartoonCharacters.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _version = "Version : 1.0";    
    [ObservableProperty] private string _qrCode = "";
    private readonly CsvServices _csvServices;
    
    public MainWindowViewModel(CsvServices cscServices)
    {
        // Plus besoin d'ajouter les personnages ici, ils sont chargés depuis JSON

        _csvServices = cscServices;
        
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);  // ← MODIFIÉ: passer this
        try
        {
            MyScanner = new ScannerManager();
            MyScanner.SerialBuffer.Changed += QRCodeManager;
            MyScanner.OpenPort();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private void QRCodeManager(object? sender, EventArgs e)
    {
        QrCode = MyScanner.SerialBuffer.Dequeue().ToString();
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
    
    // NOUVELLE méthode pour aller à l'édition
    public void GoToEditCartoonCharacter(ObjectId id)
    {
        CurrentPage = new CollectionEditViewModel(id, BackToMain);
    }
    
    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);  // ← MODIFIÉ: passer this
    }
}