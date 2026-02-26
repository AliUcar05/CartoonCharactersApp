using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
    
namespace CartoonCharacters.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _version = "Version : 1.0";
    
    
    public MainWindowViewModel()
    {
        /*
        for (var i = 0; i < 5; i++)
        {
            MyGlobals.MyCartoonCharacters.Add(new CartoonCharacter()
            {
                Id = ObjectId.GenerateNewId(),
                Name = "Sponge bob",
                Description = "A cartoon character from sponge bob.",
                Picture = ImageHelper.LoadFromResource(new Uri("avares://CartoonCharacters/Assets/sponge_bob.png"))
            });
            
            MyGlobals.MyCartoonCharacters.Add(new CartoonCharacter()
            {
                Id = ObjectId.GenerateNewId(),
                Name = "Sponge bob",
                Description = "A cartoon character from sponge bob.",
                Picture = ImageHelper.LoadFromResource(new Uri("avares://CartoonCharacters/Assets/sponge_bob.png"))
            });
        }
        */
        // Plus besoin d'ajouter les personnages ici, ils sont chargés depuis JSON
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
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
    
    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }
}