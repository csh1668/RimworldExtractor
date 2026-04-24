namespace RimworldExtractor.Ui.Avalonia.Services;

/// <summary>
/// Provides localized strings for the UI. Implementations may load from JSON resources or
/// fall back to returning the key itself.
/// </summary>
public interface ILocalizer
{
    string CurrentLanguage { get; }
    string GetString(string key);
    void SwitchLanguage(string language);
}
