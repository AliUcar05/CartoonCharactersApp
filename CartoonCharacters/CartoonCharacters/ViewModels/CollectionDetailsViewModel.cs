using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MongoDB.Bson;
using CartoonCharacters.Models;
 
namespace CartoonCharacters.ViewModels;
 
public partial class CollectionDetailsViewModel : ViewModelBase
{
    [ObservableProperty] private CartoonCharacter _myCartoonCharacter;
 
    public CollectionDetailsViewModel(string id)
    {
        MyCartoonCharacter = MyGlobals.MyCartoonCharacters.First(cc => cc.Id == id);
    }
}