using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;
    
namespace CartoonCharacters.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _version = "Version : 1.0";
    
    public MainWindowViewModel()
    {
        // Plus besoin d'ajouter les personnages ici, ils sont chargés depuis JSON
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);  // ← MODIFIÉ: passer this
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