using CommunityToolkit.Mvvm.ComponentModel;

namespace RimworldExtractor.Ui.Avalonia.ViewModels;

/// <summary>
/// Shell view model that hosts the Extract and Settings tabs.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _selectedTabIndex;

    public ExtractViewModel Extract { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(ExtractViewModel extract, SettingsViewModel settings)
    {
        Extract = extract;
        Settings = settings;
    }
}
