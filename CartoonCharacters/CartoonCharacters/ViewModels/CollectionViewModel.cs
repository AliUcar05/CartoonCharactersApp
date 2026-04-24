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

public partial class CollectionViewModel : ViewModelBase
{
    public const string AllCollectionsFilter = "Toutes les collections";
    public const string MyCollectionsFilter = "Mes collections";

    public IRelayCommand<string> FromParentCommand { get; }
    public IRelayCommand<string?> EditCommand { get; }
    public IAsyncRelayCommand<string?> DeleteCommand { get; }
    public IAsyncRelayCommand ShowTop5Command { get; }

    public ObservableCollection<CartoonCharacter> MyObservableCartoonCharacters { get; }
    public ObservableCollection<CartoonCharacter> FilteredCartoonCharacters { get; }
    public ObservableCollection<string> FilterOptions { get; }

    [ObservableProperty]
    private CartoonCharacter? _selectedCartoonCharacter;

    [ObservableProperty]
    private string _selectedFilter = AllCollectionsFilter;

    [ObservableProperty]
    private bool _isTop5DialogOpen;

    [ObservableProperty]
    private ObservableCollection<TopCharacter> _top5Characters = new();

    [ObservableProperty]
    private ISeries[] _top5Series = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _top5XAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _top5YAxes = Array.Empty<Axis>();

    private readonly MainWindowViewModel? _mainWindowViewModel;
    private readonly Func<string, Task>? _showDeleteMessageAsync;
    private readonly DatabaseServices _databaseServices = new();
    private readonly ChartServices _chartServices = new();

    public CollectionViewModel()
    {
        FromParentCommand = new RelayCommand<string>(_ => { });
        EditCommand = new RelayCommand<string?>(_ => { });
        DeleteCommand = new AsyncRelayCommand<string?>(_ => Task.CompletedTask);
        ShowTop5Command = new AsyncRelayCommand(ShowTop5Async);

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
        ShowTop5Command = new AsyncRelayCommand(ShowTop5Async);

        MyObservableCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        FilteredCartoonCharacters = new ObservableCollection<CartoonCharacter>();
        Top5Characters = new ObservableCollection<TopCharacter>();
        FilterOptions = new ObservableCollection<string>
        {
            AllCollectionsFilter,
            MyCollectionsFilter
        };

        UpdateList();
    }

    partial void OnSelectedFilterChanged(string value)
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

        foreach (var character in filtered)
        {
            FilteredCartoonCharacters.Add(character);
        }
    }

    private async Task ShowTop5Async()
    {
        var top5 = await _chartServices.GetTop5ByRatingAsync();
        
        Top5Characters.Clear();
        foreach (var character in top5)
        {
            Top5Characters.Add(character);
        }
        
        // On récupère les personnages originaux depuis MyGlobals pour avoir les ID
        var allCharacters = MyGlobals.MyCartoonCharacters;
        
        // On filtre les 5 meilleurs avec les données complètes (incluant l'ID)
        var top5WithIds = allCharacters
            .Where(c => top5.Select(t => t.Name).Contains(c.Name))
            .OrderByDescending(c => c.Rating)   // Note la plus haute en premier
            .ThenBy(c => c.Id)                   // En cas d'égalité, tri par ID
            .ToList();
        
        // On convertit en TopCharacter après le tri
        var top5Ordered = top5WithIds
            .Select(c => new TopCharacter
            {
                Name = c.Name,
                Rating = c.Rating,
                RatingVotes = c.RatingVotes
            })
            .ToList();
        
        // On inverse pour l'affichage (le meilleur en haut)
        var names = top5Ordered.Select(c => c.Name).Reverse().ToArray();
        var ratings = top5Ordered.Select(c => c.Rating).Reverse().ToArray();
        
        // Couleur selon le meilleur score
        var color = GetColorForRating(ratings);
        
        // RowSeries = barres horizontales
        Top5Series = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = ratings,
                Name = "Note moyenne",
                MaxBarWidth = 40,
                Fill = new SolidColorPaint(color),
                Stroke = null
            }
        };
        
        // Axe X (horizontal) = les notes de 0 à 5
        Top5XAxes = new[]
        {
            new Axis
            {
                Name = "Note moyenne ( /5.0 )",
                Labeler = (value) => $"{value:F1} ⭐",
                MinLimit = 0,
                MaxLimit = 5,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                NamePaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 13,
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 200, 200))
            }
        };
        
        // Axe Y (vertical) = les noms des personnages (meilleur en haut)
        Top5YAxes = new[]
        {
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
        };
        
        IsTop5DialogOpen = true;
    }

    private SKColor GetColorForRating(double[] ratings)
    {
        if (ratings.Length == 0) return new SKColor(255, 107, 107);
        
        // Prendre la meilleure note (la dernière après inversion)
        var bestRating = ratings[ratings.Length - 1];
        
        if (bestRating >= 4.8) return new SKColor(71, 200, 71);   // Vert intense
        if (bestRating >= 4.5) return new SKColor(100, 200, 100); // Vert clair
        if (bestRating >= 4.0) return new SKColor(255, 200, 71);  // Jaune orangé
        if (bestRating >= 3.5) return new SKColor(255, 144, 71);  // Orange
        return new SKColor(255, 71, 71);                           // Rouge très vif
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