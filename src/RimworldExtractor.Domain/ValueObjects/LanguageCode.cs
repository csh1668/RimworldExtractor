namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// A RimWorld language identifier. Carries both display form (e.g. <c>Korean (한국어)</c>)
/// and the folder-name form (e.g. <c>Korean</c>) used for <c>Languages/{FolderName}/</c> paths.
/// </summary>
public readonly record struct LanguageCode
{
    public string Display { get; }
    public string FolderName { get; }

    private LanguageCode(string display, string folderName)
    {
        Display = display;
        FolderName = folderName;
    }

    public static LanguageCode Create(string display)
    {
        if (string.IsNullOrWhiteSpace(display))
            throw new ArgumentException("LanguageCode display must be non-empty.", nameof(display));
        var folder = StripParenthetical(display).Trim();
        if (folder.Length == 0)
            throw new ArgumentException("LanguageCode must contain a folder-name portion before any parenthetical.", nameof(display));
        return new LanguageCode(display, folder);
    }

    private static string StripParenthetical(string input)
    {
        var open = input.IndexOf('(');
        return open < 0 ? input : input[..open];
    }

    public override string ToString() => Display;
}
