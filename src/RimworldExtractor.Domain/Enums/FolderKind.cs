namespace RimworldExtractor.Domain.Enums;

/// <summary>
/// The four translation-bearing folder kinds in a RimWorld mod tree.
/// Names match the on-disk folder names exactly (case-sensitive).
/// </summary>
public enum FolderKind
{
    Defs = 0,
    Keyed = 1,
    Strings = 2,
    Patches = 3,
}
