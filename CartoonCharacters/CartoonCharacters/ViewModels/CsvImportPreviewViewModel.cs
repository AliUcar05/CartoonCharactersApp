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
    New,        // Nouveau personnage
    Modified,   // Existant mais modifié
    Unchanged,  // Identique
    Conflict    // Conflit (même nom mais IDs différents)
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

    public string DisplayName => CsvCharacter.Name;
    public string DisplayDescription => CsvCharacter.Description;
    public string DisplayImage => CsvCharacter.ImagePath ?? "";
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
            // Chercher par ID d'abord, puis par nom
            var existingById = existingCharacters.FirstOrDefault(e => e.Id == csvChar.Id);
            var existingByName = existingCharacters.FirstOrDefault(e => e.Name.Equals(csvChar.Name, StringComparison.OrdinalIgnoreCase));
            
            var item = new CsvImportItem
            {
                CsvCharacter = csvChar,
                ExistingCharacter = existingById ?? existingByName
            };

            if (item.ExistingCharacter == null)
            {
                // Nouveau personnage
                item.Status = ImportItemStatus.New;
                item.IsSelected = true;
            }
            else if (item.ExistingCharacter.Id != csvChar.Id && 
                     item.ExistingCharacter.Name.Equals(csvChar.Name, StringComparison.OrdinalIgnoreCase))
            {
                // Conflit : même nom mais ID différent
                item.Status = ImportItemStatus.Conflict;
                item.IsSelected = false; // Par défaut, ne pas importer en conflit
                item.Changes = new[] { 
                    $"ID existant: {item.ExistingCharacter.Id}", 
                    $"ID CSV: {csvChar.Id}",
                    "Conflit - vérification manuelle requise"
                };
            }
            else
            {
                // Personnage existant, vérifier les modifications
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

                item.Changes = changes.ToArray();
                item.Status = changes.Any() ? ImportItemStatus.Modified : ImportItemStatus.Unchanged;
                item.IsSelected = changes.Any(); // Par défaut, sélectionner seulement les modifiés
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
        AllSelected = Items.All(i => i.IsSelected);
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
    private void ToggleItemSelection(CsvImportItem item)
    {
        if (item != null)
        {
            item.IsSelected = !item.IsSelected;
            UpdateCounts();
        }
    }

    [RelayCommand]
    private void CancelImport()
    {
        _onImportCancelled();
    }
}