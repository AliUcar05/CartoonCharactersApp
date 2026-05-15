using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public partial class CollectionDetailsViewModel : ViewModelBase
{
    [ObservableProperty]
    private CartoonCharacter _myCartoonCharacter;

    [ObservableProperty]
    private int _currentUserRating;

    private readonly DatabaseServices _databaseServices = new();

    public IBrush Star1Brush => CurrentUserRating >= 1 ? Brushes.Gold : Brushes.LightGray;
    public IBrush Star2Brush => CurrentUserRating >= 2 ? Brushes.Gold : Brushes.LightGray;
    public IBrush Star3Brush => CurrentUserRating >= 3 ? Brushes.Gold : Brushes.LightGray;
    public IBrush Star4Brush => CurrentUserRating >= 4 ? Brushes.Gold : Brushes.LightGray;
    public IBrush Star5Brush => CurrentUserRating >= 5 ? Brushes.Gold : Brushes.LightGray;

    public CollectionDetailsViewModel()
    {
        MyCartoonCharacter = new CartoonCharacter();
    }

    public CollectionDetailsViewModel(string id)
    {
        MyCartoonCharacter = MyGlobals.MyCartoonCharacters.First(cc => cc.Id == id);

        if (MyGlobals.CurrentUser != null &&
            MyGlobals.CurrentUser.CharacterRatings.TryGetValue(id, out var savedRating))
        {
            CurrentUserRating = savedRating;
        }
    }

    partial void OnCurrentUserRatingChanged(int value)
    {
        OnPropertyChanged(nameof(Star1Brush));
        OnPropertyChanged(nameof(Star2Brush));
        OnPropertyChanged(nameof(Star3Brush));
        OnPropertyChanged(nameof(Star4Brush));
        OnPropertyChanged(nameof(Star5Brush));
    }

    [RelayCommand]
    private Task Rate1() => RateAsync(1);

    [RelayCommand]
    private Task Rate2() => RateAsync(2);

    [RelayCommand]
    private Task Rate3() => RateAsync(3);

    [RelayCommand]
    private Task Rate4() => RateAsync(4);

    [RelayCommand]
    private Task Rate5() => RateAsync(5);

    private async Task RateAsync(int stars)
    {
        if (stars < 1 || stars > 5)
            return;

        if (MyGlobals.CurrentUser == null)
            return;

        var user = MyGlobals.CurrentUser;
        var characterId = MyCartoonCharacter.Id;

        var hasAlreadyVoted = user.CharacterRatings.TryGetValue(characterId, out var previousRating);

        var totalBefore = MyCartoonCharacter.Rating * MyCartoonCharacter.RatingVotes;

        if (!hasAlreadyVoted)
        {
            MyCartoonCharacter.RatingVotes++;
            MyCartoonCharacter.Rating = Math.Round(
                (totalBefore + stars) / MyCartoonCharacter.RatingVotes,
                1);
        }
        else
        {
            var correctedTotal = totalBefore - previousRating + stars;

            MyCartoonCharacter.Rating = MyCartoonCharacter.RatingVotes == 0
                ? 0
                : Math.Round(correctedTotal / MyCartoonCharacter.RatingVotes, 1);
        }

        user.CharacterRatings[characterId] = stars;
        CurrentUserRating = stars;

        OnPropertyChanged(nameof(MyCartoonCharacter));

        await MyGlobals.SaveDataAsync();
        await _databaseServices.UpdateUserProfileAsync(user);
    }
}