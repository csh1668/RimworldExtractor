namespace RimworldExtractor.Ui.Avalonia.Services;

/// <summary>
/// MVP localizer that returns the key as-is. Full i18n (JSON resource loading) is deferred.
/// </summary>
public sealed class NullLocalizer : ILocalizer
{
    public string CurrentLanguage { get; private set; } = "ko";

    public string GetString(string key) => key;

    public void SwitchLanguage(string language)
    {
        CurrentLanguage = language;
    }
}
