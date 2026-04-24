using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings;

public sealed record LanguageSettings(
    LanguageCode Original,
    LanguageCode Translation)
{
    public static LanguageSettings Default { get; } = new(
        LanguageCode.Create("English"),
        LanguageCode.Create("Korean (한국어)"));
}
