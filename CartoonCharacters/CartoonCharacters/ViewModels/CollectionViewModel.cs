using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using CartoonCharacters.Helpers;
using CartoonCharacters.Models;

namespace CartoonCharacters.ViewModels;

public partial class CollectionViewModel: ViewModelBase
{
    public IRelayCommand<ObjectId> FromParentCommand { get; set; }
    
    // MODIFIER: Changement de type ici
    public IRelayCommand<ObjectId> EditCommand { get; }
    
    public ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    
    [ObservableProperty] 
    private CartoonCharacter? _selectedCartoonCharacter;
    
    private readonly MainWindowViewModel _mainWindowViewModel;
    
    public CollectionViewModel(IRelayCommand<ObjectId> fromParentCommand, MainWindowViewModel mainWindowViewModel)
    {
        FromParentCommand = fromParentCommand;
        _mainWindowViewModel = mainWindowViewModel;
        
        // MODIFIER: Création correcte du RelayCommand
        EditCommand = new RelayCommand<ObjectId>(GoToEdit);
        
        MyObservableCartoonCharacters = [];
            
        foreach (var cartoonCharacter in MyGlobals.MyCartoonCharacters)
        {
            MyObservableCartoonCharacters.Add(cartoonCharacter);
        }
    }
    
    // La méthode doit être en paramètre ObjectId, pas ObjectId?
    private void GoToEdit(ObjectId id)
    {
        _mainWindowViewModel.GoToEditCartoonCharacter(id);
    }
}