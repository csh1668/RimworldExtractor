using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Entities;

/// <summary>
/// A single translatable value extracted from a RimWorld mod.
/// </summary>
/// <param name="ClassName">Def kind (e.g. <c>ThingDef</c>, <c>Keyed</c>, <c>Strings</c>, <c>Patches.ThingDef</c>).</param>
/// <param name="Node">Position within the Def (e.g. <c>DefName.label</c>, <c>Names.Last.0</c>, or a keyed identifier).</param>
/// <param name="Original">Source-language text.</param>
/// <param name="Translated">Target-language text, or null when untranslated.</param>
/// <param name="RequiredMods">Mods required/excluded for this entry (Phase 3 writes these into Patches XML).</param>
/// <param name="SourceFile">Relative path to the source XML/text file within the mod, or null for synthetic entries.</param>
public sealed record TranslationEntry(
    string ClassName,
    string Node,
    string Original,
    string? Translated,
    RequiredMods? RequiredMods,
    string? SourceFile)
{
    /// <summary>Compound key used in duplicate detection: <c>ClassName+Node</c>.</summary>
    public string ClassNode => $"{ClassName}+{Node}";

    /// <summary>The DefName portion of <see cref="Node"/> (everything before the first dot), or the whole Node if no dot.</summary>
    public string DefName => Node.Contains('.') ? Node[..Node.IndexOf('.')] : Node;

    /// <summary>The path-within-def portion of <see cref="Node"/> (everything after the first dot), or the whole Node if no dot.</summary>
    public string RealNode => Node.Contains('.') ? Node[(Node.IndexOf('.') + 1)..] : Node;
}
