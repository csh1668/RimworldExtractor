using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Ui.Avalonia.Services;

namespace RimworldExtractor.Ui.Avalonia.ViewModels;

/// <summary>
/// Represents a single mod entry in the mod list with its selection state.
/// </summary>
public sealed partial class ModItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    public ModMetadata Metadata { get; }

    public string DisplayName => Metadata.Identifier;

    public ModItemViewModel(ModMetadata metadata)
    {
        Metadata = metadata;
    }
}

/// <summary>
/// Manages mod discovery, display and selection for the extract tab.
/// </summary>
public sealed partial class ModSelectViewModel : ViewModelBase
{
    private readonly ModDiscoveryService _modDiscoveryService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ModItemViewModel? _selectedMod;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<ModItemViewModel> Mods { get; } = new();
    public ObservableCollection<ExtractableFolder> AvailableFolders { get; } = new();
    public ObservableCollection<ExtractableFolder> SelectedFolders { get; } = new();

    public ModSelectViewModel(ModDiscoveryService modDiscoveryService, IDialogService dialogService)
    {
        _modDiscoveryService = modDiscoveryService;
        _dialogService = dialogService;
    }

    partial void OnSearchTextChanged(string value)
    {
        // Filtering is done reactively; trigger a reload of filtered items
        RefreshFilteredMods();
    }

    partial void OnSelectedModChanged(ModItemViewModel? value)
    {
        AvailableFolders.Clear();
        SelectedFolders.Clear();

        if (value is null)
            return;

        var folders = _modDiscoveryService.GetExtractableFolders(value.Metadata);
        foreach (var folder in folders)
        {
            AvailableFolders.Add(folder);
            // Select all folders by default
            SelectedFolders.Add(folder);
        }
    }

    private void RefreshFilteredMods()
    {
        // The Mods collection is already populated; filtering is done by the View via CollectionView
        // For MVP, we reload and filter manually when search changes
        _ = LoadModsAsync();
    }

    [RelayCommand]
    private async Task LoadMods()
    {
        await LoadModsAsync();
    }

    private async Task LoadModsAsync()
    {
        IsLoading = true;
        StatusMessage = "Discovering mods...";

        try
        {
            var allMods = await Task.Run(() => _modDiscoveryService.DiscoverAll());

            Mods.Clear();
            var filter = SearchText?.Trim() ?? string.Empty;

            foreach (var mod in allMods)
            {
                if (filter.Length == 0 ||
                    mod.ModName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    mod.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    Mods.Add(new ModItemViewModel(mod));
                }
            }

            StatusMessage = $"{Mods.Count} mods found";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading mods: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to discover mods:\n{ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleFolderSelection(ExtractableFolder? folder)
    {
        if (folder is null)
            return;

        if (!SelectedFolders.Remove(folder))
            SelectedFolders.Add(folder);
    }

    [RelayCommand]
    private void SelectAllFolders()
    {
        SelectedFolders.Clear();
        foreach (var folder in AvailableFolders)
            SelectedFolders.Add(folder);
    }

    [RelayCommand]
    private void ClearFolderSelection()
    {
        SelectedFolders.Clear();
    }
}
