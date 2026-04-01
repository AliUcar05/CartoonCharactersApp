using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartoonCharacters.Models;

namespace CartoonCharacters.ViewModels;

public enum ImportItemStatus
{
    New,
    Modified,
    Unchanged,
    Conflict
}

public partial class CsvImportItem : ObservableObject
{
    [ObservableProperty]
    private CartoonCharacter _csvCharacter;

    [ObservableProperty]
    private CartoonCharacter? _existingCharacter;

    [ObservableProperty]
    private ImportItemStatus _status;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string[] _changes = Array.Empty<string>();
    
    public CsvImportItem(CartoonCharacter csvCharacter)
    {
        _csvCharacter = csvCharacter;
    }
}

public partial class CsvImportPreviewViewModel : ViewModelBase
{
    private readonly Action<List<CartoonCharacter>> _onImportConfirmed;
    private readonly Action _onImportCancelled;

    [ObservableProperty]
    private ObservableCollection<CsvImportItem> _items = new();

    [ObservableProperty]
    private bool _allSelected = true;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _newCount;

    [ObservableProperty]
    private int _modifiedCount;

    [ObservableProperty]
    private int _unchangedCount;

    [ObservableProperty]
    private int _conflictCount;

    [ObservableProperty]
    private string _fileName = "";

    public CsvImportPreviewViewModel(
        string fileName,
        List<CartoonCharacter> csvCharacters,
        List<CartoonCharacter> existingCharacters,
        Action<List<CartoonCharacter>> onImportConfirmed,
        Action onImportCancelled)
    {
        _onImportConfirmed = onImportConfirmed;
        _onImportCancelled = onImportCancelled;
        FileName = fileName;

        AnalyzeCharacters(csvCharacters, existingCharacters);
        UpdateCounts();
    }

    private void AnalyzeCharacters(List<CartoonCharacter> csvCharacters, List<CartoonCharacter> existingCharacters)
    {
        Items.Clear();

        foreach (var csvChar in csvCharacters)
        {
            var existingById = existingCharacters.FirstOrDefault(e => e.Id == csvChar.Id);
            var existingByName = existingCharacters.FirstOrDefault(
                e => e.Name.Equals(csvChar.Name, StringComparison.OrdinalIgnoreCase));

            var item = new CsvImportItem(csvChar)
            {
                ExistingCharacter = existingById ?? existingByName
            };

            if (item.ExistingCharacter == null)
            {
                item.Status = ImportItemStatus.New;
                item.IsSelected = true;
            }
            else if (item.ExistingCharacter.Id != csvChar.Id &&
                     item.ExistingCharacter.Name.Equals(csvChar.Name, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = ImportItemStatus.Conflict;
                item.IsSelected = false;
                item.Changes =
                [
                    $"ID existant: {item.ExistingCharacter.Id}",
                    $"ID CSV: {csvChar.Id}",
                    "Conflit - vérification manuelle requise"
                ];
            }
            else
            {
                var changes = new List<string>();

                if (item.ExistingCharacter.Name != csvChar.Name)
                    changes.Add($"Nom: {item.ExistingCharacter.Name} → {csvChar.Name}");

                if (item.ExistingCharacter.Description != csvChar.Description)
                    changes.Add("Description modifiée");

                if (item.ExistingCharacter.ImagePath != csvChar.ImagePath)
                {
                    var oldFile = System.IO.Path.GetFileName(item.ExistingCharacter.ImagePath ?? "");
                    var newFile = System.IO.Path.GetFileName(csvChar.ImagePath ?? "");
                    changes.Add($"Image: {oldFile} → {newFile}");
                }

                if (Math.Abs(item.ExistingCharacter.Rating - csvChar.Rating) > 0.0001)
                    changes.Add($"Note moyenne: {item.ExistingCharacter.Rating:F1} → {csvChar.Rating:F1}");

                if (item.ExistingCharacter.RatingVotes != csvChar.RatingVotes)
                    changes.Add($"Votes: {item.ExistingCharacter.RatingVotes} → {csvChar.RatingVotes}");

                item.Changes = changes.ToArray();
                item.Status = changes.Any() ? ImportItemStatus.Modified : ImportItemStatus.Unchanged;
                item.IsSelected = changes.Any();
            }

            Items.Add(item);
        }
    }

    private void UpdateCounts()
    {
        NewCount = Items.Count(i => i.Status == ImportItemStatus.New);
        ModifiedCount = Items.Count(i => i.Status == ImportItemStatus.Modified);
        UnchangedCount = Items.Count(i => i.Status == ImportItemStatus.Unchanged);
        ConflictCount = Items.Count(i => i.Status == ImportItemStatus.Conflict);
        SelectedCount = Items.Count(i => i.IsSelected);
        AllSelected = Items.Count > 0 && Items.All(i => i.IsSelected);
    }

    partial void OnAllSelectedChanged(bool value)
    {
        foreach (var item in Items)
        {
            item.IsSelected = value;
        }

        UpdateCounts();
    }

    [RelayCommand]
    private void ItemSelectionChanged()
    {
        UpdateCounts();
    }

    [RelayCommand]
    private void ConfirmImport()
    {
        var selectedCharacters = Items
            .Where(i => i.IsSelected && i.Status != ImportItemStatus.Conflict)
            .Select(i => i.CsvCharacter)
            .ToList();

        _onImportConfirmed(selectedCharacters);
    }

    [RelayCommand]
    private void ToggleItemSelection(CsvImportItem? item)
    {
        if (item == null)
            return;

        item.IsSelected = !item.IsSelected;
        UpdateCounts();
    }

    [RelayCommand]
    private void CancelImport()
    {
        _onImportCancelled();
    }
}