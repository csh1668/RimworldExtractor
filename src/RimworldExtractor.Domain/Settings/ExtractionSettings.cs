using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings;

public sealed record ExtractionSettings(
    GameVersion CurrentVersion,
    bool CommentOriginal,
    bool EnableTkey,
    IReadOnlyList<ExtractionRule> Rules,
    IReadOnlyList<string> FullListTags,
    IReadOnlyList<NodeReplacementRule> NodeReplacements,
    IReadOnlyList<TranslationHandle> TranslationHandles)
{
    public static ExtractionSettings Default { get; } = new(
        CurrentVersion: GameVersion.Parse("1.6"),
        CommentOriginal: false,
        EnableTkey: false,
        Rules: Array.Empty<ExtractionRule>(),
        FullListTags: Array.Empty<string>(),
        NodeReplacements: Array.Empty<NodeReplacementRule>(),
        TranslationHandles: Array.Empty<TranslationHandle>());
}
