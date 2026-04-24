namespace RimworldExtractor.Ui.Avalonia.Services;

/// <summary>
/// Abstracts folder and file selection dialogs.
/// Allows ViewModels to trigger file pickers without referencing Avalonia types directly.
/// </summary>
public interface IFilePicker
{
    /// <summary>Opens a folder picker and returns the selected path, or null if cancelled.</summary>
    Task<string?> PickFolderAsync(string title);

    /// <summary>Opens a file picker and returns the selected file path, or null if cancelled.</summary>
    Task<string?> PickFileAsync(string title, IReadOnlyList<string>? extensions = null);
}
