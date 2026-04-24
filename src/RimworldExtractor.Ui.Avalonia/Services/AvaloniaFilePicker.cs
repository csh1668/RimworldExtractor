using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace RimworldExtractor.Ui.Avalonia.Services;

/// <summary>
/// Avalonia 11 implementation of <see cref="IFilePicker"/> using StorageProvider.
/// Falls back to null (cancelled) if no main window is available.
/// </summary>
public sealed class AvaloniaFilePicker : IFilePicker
{
    private static Window? GetMainWindow()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var window = GetMainWindow();
        if (window is null)
            return null;

        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null)
            return null;

        var results = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
            });

        return results.Count > 0 ? results[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickFileAsync(string title, IReadOnlyList<string>? extensions = null)
    {
        var window = GetMainWindow();
        if (window is null)
            return null;

        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null)
            return null;

        List<FilePickerFileType>? fileTypeFilters = null;
        if (extensions is { Count: > 0 })
        {
            fileTypeFilters = new List<FilePickerFileType>
            {
                new("Files")
                {
                    Patterns = extensions.Select(e => $"*.{e.TrimStart('.')}").ToList(),
                },
            };
        }

        var results = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = fileTypeFilters,
            });

        return results.Count > 0 ? results[0].TryGetLocalPath() : null;
    }
}
