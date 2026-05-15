using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;
using CartoonCharacters.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CartoonCharacters.ViewModels;

public enum SortOption
{
    OldestFirst,
    NewestFirst,
    RatingHighest,
    RatingLowest,
    NameAscending,
    NameDescending
}

public partial class CollectionViewModel : ViewModelBase
{
    public const string AllCollectionsFilter = "Toutes les collections";
    private const string MyCollectionsFilter = "Mes collections";

    public IRelayCommand<string> FromParentCommand { get; }
    public IRelayCommand<string?> EditCommand { get; }
    public IAsyncRelayCommand<string?> DeleteCommand { get; }
    public IAsyncRelayCommand ShowTop5Command { get; }

    private ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    public ObservableCollection<CartoonCharacter> FilteredCartoonCharacters { get; }
    public ObservableCollection<string> FilterOptions { get; }
    public ObservableCollection<SortOption> SortOptions { get; }

    [ObservableProperty]
    private CartoonCharacter? _selectedCartoonCharacter;

    [ObservableProperty]
    private string _selectedFilter = AllCollectionsFilter;

    [ObservableProperty]
    private bool _isTop5DialogOpen;

    [ObservableProperty]
    private ObservableCollection<TopCharacter> _top5Characters = [];

    [ObservableProperty]
    private ISeries[] _top5Series = [];

    [ObservableProperty]
    private Axis[] _top5XAxes = [];

    [ObservableProperty]
    private Axis[] _top5YAxes = [];

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private SortOption _selectedSortOption = SortOption.OldestFirst;

    private readonly MainWindowViewModel? _mainWindowViewModel;
    private readonly Func<string, Task>? _showDeleteMessageAsync;
    private readonly DatabaseServices _databaseServices = new();
    private readonly ChartServices _chartServices = new();

    public CollectionViewModel()
    {
        FromParentCommand = new RelayCommand<string>(unusedParameter => { });
        EditCommand = new RelayCommand<string?>(unusedParameter => { });
        DeleteCommand = new AsyncRelayCommand<string?>(unusedParameter => Task.CompletedTask);
        ShowTop5Command = new AsyncRelayCommand(ShowTop5Async);

        MyObservableCartoonCharacters = [];
        FilteredCartoonCharacters = [];
        FilterOptions =
        [
            AllCollectionsFilter,
            MyCollectionsFilter
        ];

        SortOptions =
        [
            SortOption.OldestFirst,
            SortOption.NewestFirst,
            SortOption.RatingHighest,
            SortOption.RatingLowest,
            SortOption.NameAscending,
            SortOption.NameDescending
        ];

        IsAdmin = false;
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
        ShowTop5Command = new AsyncRelayCommand(ShowTop5Async);

        MyObservableCartoonCharacters = [];
        FilteredCartoonCharacters = [];
        Top5Characters = [];
        FilterOptions =
        [
            AllCollectionsFilter,
            MyCollectionsFilter
        ];

        SortOptions =
        [
            SortOption.OldestFirst,
            SortOption.NewestFirst,
            SortOption.RatingHighest,
            SortOption.RatingLowest,
            SortOption.NameAscending,
            SortOption.NameDescending
        ];

        IsAdmin = MyGlobals.CurrentUser?.IsAdmin ?? false;

        UpdateList();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplySearchFilter(_mainWindowViewModel?.SearchText);
    }

    partial void OnSelectedSortOptionChanged(SortOption value)
    {
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

        filtered = ApplySorting(filtered);

        foreach (var character in filtered)
        {
            FilteredCartoonCharacters.Add(character);
        }
    }

    private IEnumerable<CartoonCharacter> ApplySorting(IEnumerable<CartoonCharacter> characters)
    {
        return SelectedSortOption switch
        {
            SortOption.OldestFirst => characters.OrderBy(c => c.Id),
            SortOption.NewestFirst => characters.OrderByDescending(c => c.Id),
            SortOption.RatingHighest => characters.OrderByDescending(c => c.Rating),
            SortOption.RatingLowest => characters.OrderBy(c => c.Rating),
            SortOption.NameAscending => characters.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase),
            SortOption.NameDescending => characters.OrderByDescending(c => c.Name, StringComparer.OrdinalIgnoreCase),
            var fallbackSortOption => characters.OrderBy(c => c.Id)
        };
    }

    public bool CanEditOrDelete(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        if (IsAdmin)
            return true;

        var currentUser = MyGlobals.CurrentUser;

        return currentUser?.CartoonCharacterIds.Contains(characterId) == true;
    }

    public string GetSortOptionDisplayName(SortOption option)
    {
        return option switch
        {
            SortOption.OldestFirst => "📅 Plus ancien → récent",
            SortOption.NewestFirst => "📅 Plus récent → ancien",
            SortOption.RatingHighest => "⭐ Note la plus haute",
            SortOption.RatingLowest => "⭐ Note la plus basse",
            SortOption.NameAscending => "🔤 Nom A → Z",
            SortOption.NameDescending => "🔤 Nom Z → A",
            var fallbackSortOption => "📅 Plus ancien → récent"
        };
    }

    private async Task ShowTop5Async()
    {
        var top5 = await _chartServices.GetTop5ByRatingAsync();

        Top5Characters.Clear();

        foreach (var character in top5)
        {
            Top5Characters.Add(character);
        }

        var top5Names = top5.Select(t => t.Name).ToHashSet();

        var top5Ordered = MyGlobals.MyCartoonCharacters
            .Where(c => top5Names.Contains(c.Name))
            .OrderByDescending(c => c.Rating)
            .ThenBy(c => c.Id)
            .Select(c => new TopCharacter
            {
                Name = c.Name,
                Rating = c.Rating,
                RatingVotes = c.RatingVotes
            })
            .ToList();

        var names = top5Ordered.Select(c => c.Name).Reverse().ToArray();
        var ratings = top5Ordered.Select(c => c.Rating).Reverse().ToArray();

        var color = GetColorForRating(ratings);

        Top5Series =
        [
            new RowSeries<double>
            {
                Values = ratings,
                Name = "Note moyenne",
                MaxBarWidth = 40,
                Fill = new SolidColorPaint(color),
                Stroke = null
            }
        ];

        Top5XAxes =
        [
            new Axis
            {
                Name = "Note moyenne ( /5.0 )",
                Labeler = value => $"{value:F1} ⭐",
                MinLimit = 0,
                MaxLimit = 5,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 13,
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 200, 200))
            }
        ];

        Top5YAxes =
        [
            new Axis
            {
                Labels = names,
                LabelsRotation = 0,
                Name = "Personnages",
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 14,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray),
                SeparatorsAtCenter = false,
                ShowSeparatorLines = true,
                Position = LiveChartsCore.Measure.AxisPosition.Start
            }
        ];

        IsTop5DialogOpen = true;
    }

    private static SKColor GetColorForRating(double[] ratings)
    {
        if (ratings.Length == 0)
            return new SKColor(255, 107, 107);

        var bestRating = ratings[^1];

        if (bestRating >= 4.8)
            return new SKColor(71, 200, 71);

        if (bestRating >= 4.5)
            return new SKColor(100, 200, 100);

        if (bestRating >= 4.0)
            return new SKColor(255, 200, 71);

        if (bestRating >= 3.5)
            return new SKColor(255, 144, 71);

        return new SKColor(255, 71, 71);
    }

    public void CloseTop5Dialog()
    {
        IsTop5DialogOpen = false;
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

        // 🔥 NOUVEAU : Supprimer ce personnage de TOUS les utilisateurs
        await _databaseServices.RemoveCharacterFromAllUsersAsync(id);

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