using System.Collections.ObjectModel;
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
        UpdateList();
    }

    private void GoToEdit(string id)
    {
        _mainWindowViewModel.GoToEditCartoonCharacter(id);
    }

    private async Task DeleteCartoonCharacterAsync(string id)
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

        await MyGlobals.SaveDataAsync();
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