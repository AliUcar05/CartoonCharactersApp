using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    public const string AllCollectionsFilter = "Toutes les collections";
    public const string MyCollectionsFilter = "Mes collections";

    public IRelayCommand<string> FromParentCommand { get; }
    public IRelayCommand<string?> EditCommand { get; }
    public IAsyncRelayCommand<string?> DeleteCommand { get; }

    public ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    public ObservableCollection<CartoonCharacter> FilteredCartoonCharacters { get; }
    public ObservableCollection<string> FilterOptions { get; }

    [ObservableProperty]
    private CartoonCharacter? _selectedCartoonCharacter;

    [ObservableProperty]
    private string _selectedFilter = AllCollectionsFilter;

    private readonly MainWindowViewModel? _mainWindowViewModel;
    private readonly Func<string, Task>? _showDeleteMessageAsync;
    private readonly DatabaseServices _databaseServices = new();

    public CollectionViewModel()
    {
        FromParentCommand = new RelayCommand<string>(_ => { });
        EditCommand = new RelayCommand<string?>(_ => { });
        DeleteCommand = new AsyncRelayCommand<string?>(_ => Task.CompletedTask);

        MyObservableCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        FilteredCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        FilterOptions = new ObservableCollection<string>
        {
            AllCollectionsFilter,
            MyCollectionsFilter
        };
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
        FilterOptions = new ObservableCollection<string>
        {
            AllCollectionsFilter,
            MyCollectionsFilter
        };

        UpdateList();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        _ = value;
        ApplySearchFilter(_mainWindowViewModel?.SearchText);
    }

    public void ApplySearchFilter(string? searchText)
    {
        FilteredCartoonCharacters.Clear();

        IEnumerable<CartoonCharacter> filtered = MyObservableCartoonCharacters;

        if (SelectedFilter == MyCollectionsFilter)
        {
            var currentUserIds = MyGlobals.CurrentUser?.CartoonCharacterIds ?? [];
            filtered = filtered.Where(c => currentUserIds.Contains(c.Id));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(c =>
                c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

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

        if (MyGlobals.CurrentUser != null &&
            MyGlobals.CurrentUser.CartoonCharacterIds.Contains(id))
        {
            MyGlobals.CurrentUser.CartoonCharacterIds.Remove(id);
            await _databaseServices.UpdateUserProfileAsync(MyGlobals.CurrentUser);
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