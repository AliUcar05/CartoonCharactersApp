using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;

namespace CartoonCharacters.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    public IRelayCommand<string> FromParentCommand { get; }
    public IRelayCommand<string?> EditCommand { get; }
    public IAsyncRelayCommand<string?> DeleteCommand { get; }

    public ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    public ObservableCollection<CartoonCharacter> FilteredCartoonCharacters { get; }

    [ObservableProperty]
    private CartoonCharacter? _selectedCartoonCharacter;

    private readonly MainWindowViewModel? _mainWindowViewModel;
    private readonly Func<string, Task>? _showDeleteMessageAsync;

    public CollectionViewModel()
    {
        FromParentCommand = new RelayCommand<string>(_ => { });
        EditCommand = new RelayCommand<string?>(_ => { });
        DeleteCommand = new AsyncRelayCommand<string?>(_ => Task.CompletedTask);

        MyObservableCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        FilteredCartoonCharacters = new ObservableCollection<CartoonCharacter>();

        var demoCharacter = new CartoonCharacter
        {
            Id = "1",
            Name = "Exemple",
            Description = "Personnage de démonstration",
            ImagePath = ""
        };

        MyObservableCartoonCharacters.Add(demoCharacter);
        FilteredCartoonCharacters.Add(demoCharacter);
    }

    public CollectionViewModel(
        IRelayCommand<string> fromParentCommand,
        MainWindowViewModel mainWindowViewModel,
        Func<string, Task> showDeleteMessageAsync)
    {
        FromParentCommand = fromParentCommand;
        _mainWindowViewModel = mainWindowViewModel;
        _showDeleteMessageAsync = showDeleteMessageAsync;

        EditCommand = new RelayCommand<string?>(GoToEdit);
        DeleteCommand = new AsyncRelayCommand<string?>(DeleteCartoonCharacterAsync);

        MyObservableCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        FilteredCartoonCharacters = new ObservableCollection<CartoonCharacter>();

        UpdateList();
    }

    public void ApplySearchFilter(string? searchText)
    {
        FilteredCartoonCharacters.Clear();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var character in MyObservableCartoonCharacters)
            {
                FilteredCartoonCharacters.Add(character);
            }

            return;
        }

        var filtered = MyObservableCartoonCharacters
            .Where(c => c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        foreach (var character in filtered)
        {
            FilteredCartoonCharacters.Add(character);
        }
    }

    private void GoToEdit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || _mainWindowViewModel == null)
            return;

        _mainWindowViewModel.GoToEditCartoonCharacter(id);
    }

    private async Task DeleteCartoonCharacterAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || _mainWindowViewModel == null)
            return;

        var character = MyGlobals.MyCartoonCharacters.FirstOrDefault(c => c.Id == id);
        if (character == null)
            return;

        var deletedName = character.Name;

        MyGlobals.MyCartoonCharacters.Remove(character);

        var observableCharacter = MyObservableCartoonCharacters.FirstOrDefault(c => c.Id == id);
        if (observableCharacter != null)
        {
            MyObservableCartoonCharacters.Remove(observableCharacter);
        }

        ApplySearchFilter(_mainWindowViewModel.SearchText);

        await MyGlobals.SaveDataAsync();

        if (_showDeleteMessageAsync != null)
        {
            await _showDeleteMessageAsync(deletedName);
        }
    }

    private void UpdateList()
    {
        MyObservableCartoonCharacters.Clear();

        foreach (var cartoonCharacter in MyGlobals.MyCartoonCharacters)
        {
            MyObservableCartoonCharacters.Add(cartoonCharacter);
        }

        ApplySearchFilter(_mainWindowViewModel?.SearchText);
    }
}