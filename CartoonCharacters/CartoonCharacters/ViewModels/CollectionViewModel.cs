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
    // Changer tous les ObjectId en string
    public IRelayCommand<string> FromParentCommand { get; set; }
    public IRelayCommand<string> EditCommand { get; }   
    public IRelayCommand<string> DeleteCommand { get; }
    
    public ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    
    [ObservableProperty] 
    private CartoonCharacter? _selectedCartoonCharacter;
    
    private readonly MainWindowViewModel _mainWindowViewModel;
    
    public CollectionViewModel(IRelayCommand<string> fromParentCommand, MainWindowViewModel mainWindowViewModel)
    {
        FromParentCommand = fromParentCommand;
        _mainWindowViewModel = mainWindowViewModel;
        
        EditCommand = new RelayCommand<string>(GoToEdit);
        DeleteCommand = new RelayCommand<string>(DeleteCartoonCharacter);
        
        MyObservableCartoonCharacters = [];
        UpdateList();
    }
    
    private void GoToEdit(string id)  // ← string
    {
        _mainWindowViewModel.GoToEditCartoonCharacter(id);
    }

    private void DeleteCartoonCharacter(string id)  // ← string
    {
        for (int i = 0; i < MyGlobals.MyCartoonCharacters.Count; i++)
        {
            if (MyGlobals.MyCartoonCharacters[i].Id == id)
            {
                MyGlobals.MyCartoonCharacters.RemoveAt(i);
                break;
            }
        }

        for (int i = 0; i < MyObservableCartoonCharacters.Count; i++)
        {
            if (MyObservableCartoonCharacters[i].Id == id)
            {
                MyObservableCartoonCharacters.RemoveAt(i);
                break;
            }
        }

        MyGlobals.SaveData();
    }
    
    private void UpdateList()
    {
        MyObservableCartoonCharacters.Clear();
        foreach (var cartoonCharacter in MyGlobals.MyCartoonCharacters)
        {
            MyObservableCartoonCharacters.Add(cartoonCharacter);
        }
    }
}