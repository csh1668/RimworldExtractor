using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Settings;

public sealed record OutputSettings(
    DuplicatesPolicy Policy,
    ExtractionFormat Format)
{
    public static OutputSettings Default { get; } = new(
        Policy: DuplicatesPolicy.Overwrite,
        Format: ExtractionFormat.Languages);
}
