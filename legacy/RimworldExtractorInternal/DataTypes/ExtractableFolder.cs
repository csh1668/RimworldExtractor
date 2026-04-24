namespace RimworldExtractorInternal.DataTypes
{
    public record ExtractableFolder
        (ModMetadata Root, string FolderName, string? RequiredPackageId, string VersionInfo = "default")
    {
        public string FullPath => Path.Combine(Root.RootDir, FolderName);
        public override string ToString()
        {
            return $"{VersionInfo}:::{Path.GetFileName(FolderName)}" + (RequiredPackageId != null ? $"\n[모드 의존성={RequiredPackageId}]" : "");
        }
    }

    /// <summary>
    /// LoadFolder.xml로 인한 중복 폴더 방지
    /// </summary>
    public class ExtractableFolderComparer : IEqualityComparer<ExtractableFolder>
    {
        public bool Equals(ExtractableFolder? x, ExtractableFolder? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.FolderName == y.FolderName;
        }

        public int GetHashCode(ExtractableFolder obj)
        {
            return HashCode.Combine(obj.FolderName, obj.VersionInfo);
        }
    }
}
