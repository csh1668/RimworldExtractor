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

    public bool Equals(ExtractionSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return CurrentVersion == other.CurrentVersion
            && CommentOriginal == other.CommentOriginal
            && EnableTkey == other.EnableTkey
            && Rules.SequenceEqual(other.Rules)
            && FullListTags.SequenceEqual(other.FullListTags)
            && NodeReplacements.SequenceEqual(other.NodeReplacements)
            && TranslationHandles.SequenceEqual(other.TranslationHandles);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CurrentVersion);
        hash.Add(CommentOriginal);
        hash.Add(EnableTkey);
        foreach (var r in Rules) hash.Add(r);
        foreach (var f in FullListTags) hash.Add(f);
        foreach (var n in NodeReplacements) hash.Add(n);
        foreach (var t in TranslationHandles) hash.Add(t);
        return hash.ToHashCode();
    }
}
