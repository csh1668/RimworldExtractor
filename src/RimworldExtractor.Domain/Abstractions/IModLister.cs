using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Domain.Abstractions;

/// <summary>
/// Discovers mods on disk and provides metadata + extractable folder enumeration.
/// </summary>
public interface IModLister
{
    /// <summary>All mods across configured roots (Rimworld/Data, Mods, Workshop). Deterministic order.</summary>
    IReadOnlyList<ModMetadata> DiscoverAll();

    /// <summary>Parses a single mod root directory into a <c>ModMetadata</c>. Returns null if the directory is not a mod.</summary>
    ModMetadata? ReadMetadata(string modRoot);

    /// <summary>Lists the Defs/Keyed/Strings/Patches folders a mod exposes, including version subdirs and LoadFolders.xml resolution.</summary>
    IReadOnlyList<ExtractableFolder> GetExtractableFolders(ModMetadata metadata);

    /// <summary>Finds the transitive closure of reference mods (for Defs inheritance resolution).</summary>
    IReadOnlyList<ModMetadata> FindReferenceMods(ModMetadata target);
}
