namespace RimworldExtractor.Domain.Settings;

/// <summary>
/// Complete user-facing settings tree. Persisted as <c>settings.json</c> in Phase 2+.
/// Schema version is bumped whenever a breaking change occurs; <see cref="CurrentSchemaVersion"/>
/// is the target version for new files.
/// </summary>
public sealed record AppSettings(
    int SchemaVersion,
    PathSettings Paths,
    LanguageSettings Languages,
    ExtractionSettings Extraction,
    OutputSettings Output)
{
    public const int CurrentSchemaVersion = 2;

    public static AppSettings Default { get; } = new(
        SchemaVersion: CurrentSchemaVersion,
        Paths: PathSettings.Default,
        Languages: LanguageSettings.Default,
        Extraction: ExtractionSettings.Default,
        Output: OutputSettings.Default);
}
