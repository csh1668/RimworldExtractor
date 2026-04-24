using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Ui.Avalonia.Services;

namespace RimworldExtractor.Ui.Avalonia.ViewModels;

/// <summary>
/// Manages the extraction flow: select mod, pick folders, run pipeline, show progress.
/// </summary>
public sealed partial class ExtractViewModel : ViewModelBase
{
    private readonly IExtractionPipeline _pipeline;
    private readonly ISettingsStore _settingsStore;
    private readonly IDialogService _dialogService;
    private readonly IFilePicker _filePicker;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isExtracting;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExtractCommand))]
    private bool _hasSelection;

    public ModSelectViewModel ModSelect { get; }

    public ExtractViewModel(
        IExtractionPipeline pipeline,
        ISettingsStore settingsStore,
        IDialogService dialogService,
        IFilePicker filePicker,
        ModSelectViewModel modSelect)
    {
        _pipeline = pipeline;
        _settingsStore = settingsStore;
        _dialogService = dialogService;
        _filePicker = filePicker;
        ModSelect = modSelect;

        // Watch ModSelect.SelectedMod to gate Extract command
        ModSelect.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ModSelectViewModel.SelectedMod))
                HasSelection = ModSelect.SelectedMod is not null;
        };
    }

    [RelayCommand]
    private async Task BrowseOutputPath()
    {
        var path = await _filePicker.PickFolderAsync("Select Output Folder");
        if (path is not null)
            OutputPath = path;
    }

    [RelayCommand(CanExecute = nameof(CanExtract), AllowConcurrentExecutions = false)]
    private async Task Extract(CancellationToken cancellationToken)
    {
        var targetMod = ModSelect.SelectedMod?.Metadata;
        if (targetMod is null)
        {
            await _dialogService.ShowMessageAsync("Extract", "Please select a mod first.");
            return;
        }

        var selectedFolders = ModSelect.SelectedFolders.ToList();
        if (selectedFolders.Count == 0)
        {
            await _dialogService.ShowMessageAsync("Extract", "Please select at least one folder.");
            return;
        }

        IsExtracting = true;
        Progress = 0;
        StatusMessage = "Starting extraction...";

        try
        {
            var settings = await _settingsStore.LoadAsync(cancellationToken);

            var request = new ExtractionRequest(
                Target: targetMod,
                SelectedFolders: selectedFolders,
                ReferenceMods: null,
                Settings: settings);

            var progressReporter = new Progress<ExtractionProgress>(p =>
            {
                Progress = p.Percentage;
                StatusMessage = p.Message;
            });

            var result = await _pipeline.RunAsync(request, progressReporter, cancellationToken);

            Progress = 100;
            StatusMessage = $"Done. {result.Entries.Count} entries extracted.";
            await _dialogService.ShowMessageAsync("Extraction Complete",
                $"Extracted {result.Entries.Count} entries from '{targetMod.ModName}'.");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Extraction cancelled.";
            Progress = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Extraction Error", $"Extraction failed:\n{ex.Message}");
        }
        finally
        {
            IsExtracting = false;
        }
    }

    private bool CanExtract() => HasSelection && !IsExtracting;
}
