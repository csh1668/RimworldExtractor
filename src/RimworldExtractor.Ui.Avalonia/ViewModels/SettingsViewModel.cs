using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Ui.Avalonia.Services;

namespace RimworldExtractor.Ui.Avalonia.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private static readonly string[] BaseRefListExtensions = ["txt", "csv"];

    private readonly ISettingsStore _settingsStore;
    private readonly IFilePicker _filePicker;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _rimworldPath = string.Empty;

    [ObservableProperty]
    private string _workshopPath = string.Empty;

    [ObservableProperty]
    private string _baseRefListPath = string.Empty;

    [ObservableProperty]
    private string _originalLanguage = "English";

    [ObservableProperty]
    private string _translationLanguage = "Korean (한국어)";

    [ObservableProperty]
    private string _gameVersion = "1.6";

    [ObservableProperty]
    private bool _commentOriginal;

    [ObservableProperty]
    private bool _enableTkey;

    [ObservableProperty]
    private int _duplicatesPolicy;

    [ObservableProperty]
    private int _extractionFormat;

    [ObservableProperty]
    private bool _isSaving;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        IFilePicker filePicker,
        IDialogService dialogService)
    {
        _settingsStore = settingsStore;
        _filePicker = filePicker;
        _dialogService = dialogService;
    }

    public async Task LoadAsync()
    {
        var settings = await _settingsStore.LoadAsync();
        ApplySettings(settings);
    }

    private void ApplySettings(AppSettings settings)
    {
        RimworldPath = settings.Paths.Rimworld;
        WorkshopPath = settings.Paths.Workshop;
        BaseRefListPath = settings.Paths.BaseRefList;
        OriginalLanguage = settings.Languages.Original.Display;
        TranslationLanguage = settings.Languages.Translation.Display;
        GameVersion = settings.Extraction.CurrentVersion.ToString();
        CommentOriginal = settings.Extraction.CommentOriginal;
        EnableTkey = settings.Extraction.EnableTkey;
        DuplicatesPolicy = (int)settings.Output.Policy;
        ExtractionFormat = (int)settings.Output.Format;
    }

    private AppSettings BuildSettings(AppSettings original)
    {
        var paths = new PathSettings(RimworldPath, WorkshopPath, BaseRefListPath);
        var languages = new LanguageSettings(
            LanguageCode.Create(string.IsNullOrWhiteSpace(OriginalLanguage) ? "English" : OriginalLanguage),
            LanguageCode.Create(string.IsNullOrWhiteSpace(TranslationLanguage) ? "Korean (한국어)" : TranslationLanguage));
        var extraction = original.Extraction with
        {
            CurrentVersion = Domain.ValueObjects.GameVersion.Parse(
                string.IsNullOrWhiteSpace(GameVersion) ? "1.6" : GameVersion),
            CommentOriginal = CommentOriginal,
            EnableTkey = EnableTkey,
        };
        var output = new OutputSettings(
            (Domain.Enums.DuplicatesPolicy)DuplicatesPolicy,
            (Domain.Enums.ExtractionFormat)ExtractionFormat);

        return original with
        {
            Paths = paths,
            Languages = languages,
            Extraction = extraction,
            Output = output,
        };
    }

    [RelayCommand]
    private async Task BrowseRimworldPath()
    {
        var path = await _filePicker.PickFolderAsync("Select RimWorld Folder");
        if (path is not null)
            RimworldPath = path;
    }

    [RelayCommand]
    private async Task BrowseWorkshopPath()
    {
        var path = await _filePicker.PickFolderAsync("Select Workshop Folder");
        if (path is not null)
            WorkshopPath = path;
    }

    [RelayCommand]
    private async Task BrowseBaseRefListPath()
    {
        var path = await _filePicker.PickFileAsync("Select Base Reference List File", BaseRefListExtensions);
        if (path is not null)
            BaseRefListPath = path;
    }

    [RelayCommand]
    private async Task Save()
    {
        IsSaving = true;
        try
        {
            var current = await _settingsStore.LoadAsync();
            var updated = BuildSettings(current);
            await _settingsStore.SaveAsync(updated);
            await _dialogService.ShowMessageAsync("Settings", "Settings saved successfully.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", $"Failed to save settings:\n{ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
