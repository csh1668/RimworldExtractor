namespace RimworldExtractor.Ui.Avalonia.Services;

/// <summary>
/// Abstracts user-facing message dialogs and confirmation prompts.
/// Allows ViewModels to trigger dialogs without referencing Avalonia types directly.
/// </summary>
public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmAsync(string title, string message);
}
