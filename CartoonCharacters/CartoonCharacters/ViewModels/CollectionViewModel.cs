using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
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
            // MODIFIER: Ne plus créer de nouvelle instance, utiliser l'original
            // et surtout, NE PAS copier Picture (qui n'existe plus)
            MyObservableCartoonCharacters.Add(cartoonCharacter);  // ← Changé
        }
    }
}