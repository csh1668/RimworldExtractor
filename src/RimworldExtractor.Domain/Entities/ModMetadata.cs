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

    public bool Equals(ModMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return RootDir == other.RootDir
            && Id == other.Id
            && ModName == other.ModName
            && PackageId == other.PackageId
            && IsOfficialContent == other.IsOfficialContent
            && (ModDependencies is null
                ? other.ModDependencies is null
                : other.ModDependencies is not null && ModDependencies.SequenceEqual(other.ModDependencies));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(RootDir);
        hash.Add(Id);
        hash.Add(ModName);
        hash.Add(PackageId);
        hash.Add(IsOfficialContent);
        if (ModDependencies is not null)
            foreach (var d in ModDependencies) hash.Add(d);
        return hash.ToHashCode();
    }
}
