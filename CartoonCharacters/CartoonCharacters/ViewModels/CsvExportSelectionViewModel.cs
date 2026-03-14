using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;

namespace CartoonCharacters.ViewModels;

public partial class ExportItem : ObservableObject
{
    [ObservableProperty]
    private CartoonCharacter _character;

    [ObservableProperty]
    private bool _isSelected = true;
}

public partial class CsvExportSelectionViewModel : ViewModelBase
{
    private readonly Action<List<CartoonCharacter>> _onExportConfirmed;
    private readonly Action _onExportCancelled;

    [ObservableProperty]
    private ObservableCollection<ExportItem> _items = new();

    [ObservableProperty]
    private bool _allSelected = true;

    [ObservableProperty]
    private int _selectedCount;

    public CsvExportSelectionViewModel(
        List<CartoonCharacter> characters,
        Action<List<CartoonCharacter>> onExportConfirmed,
        Action onExportCancelled)
    {
        _onExportConfirmed = onExportConfirmed;
        _onExportCancelled = onExportCancelled;

        foreach (var character in characters)
        {
            Items.Add(new ExportItem
            {
                Character = character,
                IsSelected = true
            });
        }

        UpdateSelectedCount();
        
        // S'abonner aux changements de chaque item
        foreach (var item in Items)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ExportItem.IsSelected))
                {
                    UpdateSelectedCount();
                }
            };
        }
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Items.Count(i => i.IsSelected);
        // Mettre à jour AllSelected en fonction de la sélection
        AllSelected = Items.All(i => i.IsSelected);
    }

    partial void OnAllSelectedChanged(bool value)
    {
        // Cette méthode est appelée quand AllSelected change
        foreach (var item in Items)
        {
            item.IsSelected = value;
        }
        // Pas besoin d'appeler UpdateSelectedCount ici car les PropertyChanged des items le feront
    }

    [RelayCommand]
    private void ConfirmExport()
    {
        var selectedCharacters = Items
            .Where(i => i.IsSelected)
            .Select(i => i.Character)
            .ToList();

        _onExportConfirmed(selectedCharacters);
    }

    [RelayCommand]
    private void CancelExport()
    {
        _onExportCancelled();
    }
}