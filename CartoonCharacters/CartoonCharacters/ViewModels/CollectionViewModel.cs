using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;

namespace CartoonCharacters.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    public IRelayCommand<string> FromParentCommand { get; set; }
    public IRelayCommand<string> EditCommand { get; }
    public IAsyncRelayCommand<string> DeleteCommand { get; }

    public ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    public ObservableCollection<CartoonCharacter> FilteredCartoonCharacters { get; }

    [ObservableProperty]
    private CartoonCharacter? _selectedCartoonCharacter;

    private readonly MainWindowViewModel _mainWindowViewModel;

    public CollectionViewModel(IRelayCommand<string> fromParentCommand, MainWindowViewModel mainWindowViewModel)
    {
        FromParentCommand = fromParentCommand;
        _mainWindowViewModel = mainWindowViewModel;

        EditCommand = new RelayCommand<string>(GoToEdit);
        DeleteCommand = new AsyncRelayCommand<string>(DeleteCartoonCharacterAsync);

        MyObservableCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        FilteredCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        
        UpdateList();
    }

    // Méthode publique pour appliquer le filtre de recherche
    public void ApplySearchFilter(string searchText)
    {
        FilteredCartoonCharacters.Clear();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Afficher tous les personnages
            foreach (var character in MyObservableCartoonCharacters)
            {
                FilteredCartoonCharacters.Add(character);
            }
        }
        else
        {
            // Filtrer par nom (insensible à la casse)
            var filtered = MyObservableCartoonCharacters
                .Where(c => c.Name.Contains(searchText, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var character in filtered)
            {
                FilteredCartoonCharacters.Add(character);
            }
        }
    }

    private void GoToEdit(string id)
    {
        _mainWindowViewModel.GoToEditCartoonCharacter(id);
    }

    private async Task DeleteCartoonCharacterAsync(string id)
    {
        // Supprimer de la liste globale
        for (int i = 0; i < MyGlobals.MyCartoonCharacters.Count; i++)
        {
            if (MyGlobals.MyCartoonCharacters[i].Id == id)
            {
                MyGlobals.MyCartoonCharacters.RemoveAt(i);
                break;
            }
        }

        // Supprimer de la liste observable principale
        for (int i = 0; i < MyObservableCartoonCharacters.Count; i++)
        {
            if (MyObservableCartoonCharacters[i].Id == id)
            {
                MyObservableCartoonCharacters.RemoveAt(i);
                break;
            }
        }

        // Re-appliquer le filtre après suppression
        ApplySearchFilter(_mainWindowViewModel.SearchText);

        await MyGlobals.SaveDataAsync();
    }

    private void UpdateList()
    {
        MyObservableCartoonCharacters.Clear();
        foreach (var cartoonCharacter in MyGlobals.MyCartoonCharacters)
        {
            MyObservableCartoonCharacters.Add(cartoonCharacter);
        }
        
        // Appliquer le filtre après la mise à jour de la liste
        ApplySearchFilter(_mainWindowViewModel.SearchText);
    }
}