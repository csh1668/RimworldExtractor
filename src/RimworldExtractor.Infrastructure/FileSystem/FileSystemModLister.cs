using System.Text.RegularExpressions;
using System.Xml.Linq;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Disk-backed <see cref="IModLister"/> implementation. Reads <c>About/About.xml</c>,
/// detects official content via author = "Ludeon Studios", parses modDependencies
/// (including <c>modDependenciesByVersion</c>), and enumerates extractable folders
/// including <c>LoadFolders.xml</c> and version subdirectory resolution.
/// </summary>
/// <remarks>
/// <para><see cref="ReadMetadata"/> is synchronous and uses sync-over-async
/// (<c>GetAwaiter().GetResult()</c>) to keep <see cref="IModLister"/> simple for callers.
/// If this becomes a performance concern in Phase 4, consider an async variant.</para>
/// </remarks>
public sealed partial class FileSystemModLister : IModLister
{
    private const string OfficialContentAuthor = "Ludeon Studios";
    private const string OfficialId = "Official";

    // Regex matching RimWorld version directory names like "1.4", "1.5", "1.6".
    // Hoisted as static readonly to satisfy CA1861 (no repeated array allocation).
    [GeneratedRegex(@"^1\.\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionDirRegex();

    // Target sub-folder names (relative, forward-slash style) that we look for in each scanned root.
    // Built lazily from the originalLanguageFolderName passed to the constructor.
    private readonly string[] _targetFolders;

    private readonly IFileSystem _fs;
    private readonly string _currentVersion;
    private readonly string _originalLanguageFolderName;
    private readonly string _rimworldDir;
    private readonly string _workshopDir;

    /// <param name="fs">File-system abstraction (use <see cref="InMemoryFileSystem"/> in tests).</param>
    /// <param name="currentVersion">Active game version, e.g. "1.6".</param>
    /// <param name="originalLanguageFolderName">Language folder name for the source language, e.g. "English".</param>
    /// <param name="rimworldDir">Root RimWorld installation directory (contains Data/ and Mods/).</param>
    /// <param name="workshopDir">Steam Workshop content directory for app 294100.</param>
    public FileSystemModLister(
        IFileSystem fs,
        string currentVersion,
        string originalLanguageFolderName,
        string rimworldDir,
        string workshopDir)
    {
        _fs = fs;
        _currentVersion = currentVersion;
        _originalLanguageFolderName = originalLanguageFolderName;
        _rimworldDir = rimworldDir;
        _workshopDir = workshopDir;

        // Build the list of extractable sub-folder names.
        // "English" → base name is "English"; if name contains a space, base = first token.
        var langBase = originalLanguageFolderName.Split(' ')[0];
        _targetFolders =
        [
            "Defs",
            "Patches",
            "Keyed",
            $"Languages/{originalLanguageFolderName}/Keyed",
            $"Languages/{langBase}/Keyed",
            $"Languages/{originalLanguageFolderName}/Strings",
            $"Languages/{langBase}/Strings",
        ];
    }

    // -------------------------------------------------------------------------
    // IModLister.ReadMetadata  (legacy ModLister.cs:84-155)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public ModMetadata? ReadMetadata(string modRoot)
    {
        // Normalise path separator so InMemoryFileSystem (forward-slash) works on Windows.
        var aboutPath = Path.Combine(modRoot, "About", "About.xml").Replace('\\', '/');
        if (!_fs.FileExists(aboutPath)) return null;

        string text;
        XDocument doc;
        try
        {
            text = _fs.ReadAllTextAsync(aboutPath).GetAwaiter().GetResult();
            doc = XDocument.Parse(text);
        }
        catch
        {
            // Malformed XML — treat as unreadable mod.
            return null;
        }

        var root = doc.Root;
        if (root is null) return null;

        var packageId = root.Element("packageId")?.Value ?? "UNKNOWN";
        var name = root.Element("name")?.Value ?? "UNKNOWN";
        var author = root.Element("author")?.Value;
        var isOfficial = author == OfficialContentAuthor;

        // legacy L103-105: official content uses the folder name when <name> is missing.
        if (isOfficial && name == "UNKNOWN")
            name = Path.GetFileName(modRoot.TrimEnd('/', '\\'));

        // legacy L108-115: parse <modDependencies><li><packageId>…
        var dependencies = new List<string>();
        var modDeps = root.Element("modDependencies");
        if (modDeps is not null)
            foreach (var li in modDeps.Elements("li"))
                if (li.Element("packageId") is { } pid)
                    dependencies.Add(pid.Value);

        // legacy L118-132: parse <modDependenciesByVersion><v1.6><li><packageId>…
        var modDepsByVersion = root.Element("modDependenciesByVersion");
        if (modDepsByVersion is not null)
        {
            var versionElement = modDepsByVersion.Element("v" + _currentVersion)
                ?? modDepsByVersion.Elements().LastOrDefault();
            if (versionElement is not null)
                foreach (var li in versionElement.Elements("li"))
                    if (li.Element("packageId") is { } pid)
                        dependencies.Add(pid.Value);
        }

        dependencies = [.. dependencies.Distinct()];

        string id;
        if (isOfficial)
        {
            id = OfficialId;
        }
        else
        {
            // legacy L142-151: PublishedFileId.txt fallback, then workshop path heuristic.
            var publishedFileIdPath = Path.Combine(modRoot, "About", "PublishedFileId.txt").Replace('\\', '/');
            if (_fs.FileExists(publishedFileIdPath))
            {
                id = _fs.ReadAllTextAsync(publishedFileIdPath).GetAwaiter().GetResult().Trim();
            }
            else if (modRoot.Replace('\\', '/').Contains("workshop/content/294100"))
            {
                id = Path.GetFileName(modRoot.TrimEnd('/', '\\'));
            }
            else
            {
                id = ModMetadata.UnknownId;
            }
        }

        return new ModMetadata(modRoot, id, name, packageId, isOfficial, dependencies);
    }

    // -------------------------------------------------------------------------
    // IModLister.GetExtractableFolders  (legacy ModLister.cs:157-232)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public IReadOnlyList<ExtractableFolder> GetExtractableFolders(ModMetadata metadata)
    {
        var root = metadata.RootDir;
        // Dedup by "folderName|versionInfo|requiredPackageId" to avoid duplicate entries.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<ExtractableFolder>();

        // Returns relative paths (forward-slash) for each targetFolder that exists under 'basePath'.
        IEnumerable<string> ScanExtractableFolders(string basePath)
        {
            foreach (var folder in _targetFolders)
            {
                // Combine using forward-slash for InMemoryFileSystem compatibility.
                var subDir = (basePath.TrimEnd('/') + "/" + folder).Replace('\\', '/');
                if (_fs.DirectoryExists(subDir))
                    // Return the relative path from mod root.
                    yield return GetRelativePath(root, subDir);
            }
        }

        void AddFolder(string relativePath, string versionInfo, string? requiredPackageId)
        {
            var key = $"{relativePath}|{versionInfo}|{requiredPackageId}";
            if (seen.Add(key))
                results.Add(new ExtractableFolder(metadata, relativePath, requiredPackageId, versionInfo));
        }

        // legacy L183-185: scan root itself → VersionInfo = "default"
        foreach (var relPath in ScanExtractableFolders(root))
            AddFolder(relPath, "default", null);

        var loadFoldersPath = (root.TrimEnd('/') + "/LoadFolders.xml").Replace('\\', '/');
        if (_fs.FileExists(loadFoldersPath))
        {
            // legacy L188-205: parse LoadFolders.xml — each child element is a version block (e.g. <v1.6>).
            var xmlText = _fs.ReadAllTextAsync(loadFoldersPath).GetAwaiter().GetResult();
            var doc = XDocument.Parse(xmlText);
            foreach (var versionNode in doc.Root?.Elements() ?? Enumerable.Empty<XElement>())
            {
                // Element name is like "v1.6" — strip the leading "v" for VersionInfo.
                var versionInfo = versionNode.Name.LocalName.Length > 1
                    ? versionNode.Name.LocalName[1..]
                    : versionNode.Name.LocalName;

                foreach (var li in versionNode.Elements("li"))
                {
                    var requiredPackageId = li.Attribute("IfModActive")?.Value;
                    var folderSegment = li.Value.Trim();
                    if (string.IsNullOrEmpty(folderSegment)) continue;

                    var loadedPath = (root.TrimEnd('/') + "/" + folderSegment).Replace('\\', '/');
                    foreach (var relPath in ScanExtractableFolders(loadedPath))
                        AddFolder(relPath, versionInfo, requiredPackageId);
                }
            }
        }
        else
        {
            // legacy L207-228: no LoadFolders.xml → scan version subdirs + Common/.
            foreach (var dir in _fs.EnumerateDirectories(root))
            {
                var lastDir = Path.GetFileName(dir.TrimEnd('/', '\\'));
                if (lastDir is null) continue;

                if (VersionDirRegex().IsMatch(lastDir))
                {
                    // e.g. "1.6" → version subdir
                    foreach (var relPath in ScanExtractableFolders(dir))
                        AddFolder(relPath, lastDir, null);
                }
                else if (lastDir == "Common")
                {
                    foreach (var relPath in ScanExtractableFolders(dir))
                        AddFolder(relPath, "Common", null);
                }
            }
        }

        return results;
    }

    // -------------------------------------------------------------------------
    // IModLister.DiscoverAll  (legacy ModLister.cs:14-82)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public IReadOnlyList<ModMetadata> DiscoverAll()
    {
        var results = new List<ModMetadata>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Discover(string baseDir)
        {
            if (!_fs.DirectoryExists(baseDir)) return;
            foreach (var dir in _fs.EnumerateDirectories(baseDir).OrderBy(d => d, StringComparer.Ordinal))
            {
                if (!seen.Add(dir)) continue;
                var meta = ReadMetadata(dir);
                if (meta is not null) results.Add(meta);
            }
        }

        // Official: {rimworldDir}/Data/*/
        Discover(_rimworldDir.TrimEnd('/') + "/Data");
        // Local:    {rimworldDir}/Mods/*/
        Discover(_rimworldDir.TrimEnd('/') + "/Mods");
        // Workshop: {workshopDir}/*/
        Discover(_workshopDir);

        return results;
    }

    // -------------------------------------------------------------------------
    // IModLister.FindReferenceMods  (legacy ModLister.cs:234-292)
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public IReadOnlyList<ModMetadata> FindReferenceMods(ModMetadata target)
    {
        var all = DiscoverAll();

        // Build a lookup: normalized packageId → ModMetadata (first wins on duplicates).
        var byPackageId = new Dictionary<string, ModMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in all)
            byPackageId.TryAdd(m.PackageId, m);

        var results = new List<ModMetadata>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. All official mods (excluding target itself) come first.
        foreach (var m in all)
        {
            if (!m.IsOfficialContent || m.RootDir == target.RootDir) continue;
            if (visited.Add(m.PackageId))
                results.Add(m);
        }

        // 2. Transitive walk of ModDependencies.
        void WalkDependencies(ModMetadata current)
        {
            if (current.ModDependencies is null) return;
            foreach (var dep in current.ModDependencies)
            {
                if (!visited.Add(dep)) continue;
                if (byPackageId.TryGetValue(dep, out var depMeta))
                {
                    results.Add(depMeta);
                    WalkDependencies(depMeta);
                }
            }
        }

        WalkDependencies(target);

        return results;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string GetRelativePath(string root, string fullPath)
    {
        var rootNorm = root.TrimEnd('/', '\\').Replace('\\', '/') + "/";
        var fullNorm = fullPath.Replace('\\', '/');
        if (fullNorm.StartsWith(rootNorm, StringComparison.Ordinal))
            return fullNorm[rootNorm.Length..];
        return fullNorm;
    }
}

