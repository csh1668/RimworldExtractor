using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Applies a RimWorld Patch XML document against a combined-defs tree and yields
/// <c>TranslationEntry</c> values found inside patched subtrees.
/// Port of <c>legacy/PatchOperations.cs</c>.
/// </summary>
public interface IXPatchProcessor
{
    /// <summary>
    /// Apply every <c>&lt;Operation&gt;</c> child of <paramref name="patchRoot"/> against
    /// <paramref name="combinedDefs"/> and return extracted entries plus any Defs added
    /// directly to the Defs root (for downstream extraction).
    /// </summary>
    PatchProcessingResult Apply(
        XDocument combinedDefs,
        XElement patchRoot,
        IReadOnlyDictionary<string, ExtractionRule> rules,
        IReadOnlyList<TranslationHandle> translationHandles,
        bool enableTKey,
        bool isOfficialContent,
        RequiredMods? initialRequiredMods = null,
        bool prePatchMode = false);
}

/// <summary>
/// Result of applying a Patch XML document. Contains extracted translation entries
/// and any <c>XElement</c> Defs that were added directly to the Defs root by
/// <c>PatchOperationAdd</c> with xpath="Defs" — these need subsequent extraction.
/// </summary>
public sealed record PatchProcessingResult(
    IReadOnlyList<TranslationEntry> Entries,
    IReadOnlyList<XElement> DefsAddedByPatches);
