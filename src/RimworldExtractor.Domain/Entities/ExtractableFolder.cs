namespace RimworldExtractor.Domain.Entities;

/// <summary>
/// A folder within a mod that contains translation sources (Defs/Keyed/Strings/Patches).
/// <see cref="FolderName"/> is relative to the mod root and may include a version prefix (e.g. <c>1.6/Defs</c>).
/// </summary>
public sealed record ExtractableFolder(
    ModMetadata Root,
    string FolderName,
    string? RequiredPackageId,
    string VersionInfo = "default")
{
    public string FullPath => Path.Combine(Root.RootDir, FolderName);
}
