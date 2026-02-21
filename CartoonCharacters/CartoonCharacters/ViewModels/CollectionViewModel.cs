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
    public ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    
    [ObservableProperty] 
    private CartoonCharacter? _selectedCartoonCharacter;
    
    public CollectionViewModel(IRelayCommand<ObjectId> fromParentCommand)
    {
        FromParentCommand = fromParentCommand;
        
        MyObservableCartoonCharacters = [];
            
        foreach (var cartoonCharacter in MyGlobals.MyCartoonCharacters)
        {
            MyObservableCartoonCharacters.Add(new CartoonCharacter()
            {
                Id = cartoonCharacter.Id,
                Name = cartoonCharacter.Name,
                Description = cartoonCharacter.Description,
                Picture = cartoonCharacter.Picture
            });
        }
    }
}