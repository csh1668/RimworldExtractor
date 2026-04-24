namespace RimworldExtractor.Domain.Entities;

/// <summary>
/// Metadata about a RimWorld mod discovered on disk. <see cref="Id"/> is the workshop ID
/// for Steam mods, the folder name for local mods, or <c>"???"</c> when unknown.
/// </summary>
public sealed record ModMetadata(
    string RootDir,
    string Id,
    string ModName,
    string PackageId,
    bool IsOfficialContent,
    IReadOnlyList<string>? ModDependencies = null)
{
    public const string UnknownId = "???";

    /// <summary>Human-readable identifier: "ModName" for official content, "ModName - Id" otherwise (falling back to "ModName" when Id is unknown).</summary>
    public string Identifier
    {
        get
        {
            if (IsOfficialContent) return ModName;
            return Id == UnknownId ? ModName : $"{ModName} - {Id}";
        }
    }

    public static ModMetadata Empty { get; } = new("", "", "", "", IsOfficialContent: false);
}
