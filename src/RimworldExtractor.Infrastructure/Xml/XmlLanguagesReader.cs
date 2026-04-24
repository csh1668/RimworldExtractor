using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Reads a RimWorld Languages-format directory tree and reconstructs <see cref="TranslationEntry"/> records.
/// Ported from <c>legacy/RimworldExtractorInternal/IO.cs:669-720</c> using <see cref="IFileSystem"/>.
/// </summary>
public sealed class XmlLanguagesReader : IXmlLanguagesReader
{
    private readonly IFileSystem _fs;

    public XmlLanguagesReader(IFileSystem fs) => _fs = fs;

    public async Task<IReadOnlyList<TranslationEntry>> ReadAsync(
        string rootDir,
        LanguageCode translationLanguage,
        CancellationToken cancellationToken = default)
    {
        // Normalize: try full display name, then folder name only (first word before space/parenthesis)
        var translationDir = JoinPath(rootDir, "Languages", translationLanguage.FolderName);
        if (!_fs.DirectoryExists(translationDir))
        {
            var shortName = translationLanguage.FolderName.Split(' ')[0];
            translationDir = JoinPath(rootDir, "Languages", shortName);
        }

        var results = new List<TranslationEntry>();

        var defInjectedDir = JoinPath(translationDir, "DefInjected");
        var keyedDir = JoinPath(translationDir, "Keyed");
        var stringsDir = JoinPath(translationDir, "Strings");

        if (_fs.DirectoryExists(defInjectedDir))
            await ReadDefInjectedAsync(defInjectedDir, results, cancellationToken);

        if (_fs.DirectoryExists(keyedDir))
            await ReadKeyedAsync(keyedDir, results, cancellationToken);

        if (_fs.DirectoryExists(stringsDir))
            await ReadStringsAsync(stringsDir, results, cancellationToken);

        return results;
    }

    // ─── DefInjected ────────────────────────────────────────────────────────────

    private async Task ReadDefInjectedAsync(string defInjectedDir, List<TranslationEntry> results, CancellationToken ct)
    {
        foreach (var classDir in EnumerateDirectoriesRecursive(defInjectedDir))
        {
            // The class name is the single path segment directly under defInjectedDir
            var className = GetRelativeSingleSegment(defInjectedDir, classDir);
            if (className is null) continue;

            foreach (var filePath in EnumerateXmlFiles(classDir))
            {
                try
                {
                    var text = await _fs.ReadAllTextAsync(filePath, ct);
                    var doc = XDocument.Parse(text, LoadOptions.None);
                    var langData = doc.Root;
                    if (langData is null) continue;

                    foreach (var node in langData.Elements())
                    {
                        var name = node.Name.LocalName;
                        // FullListTranslation: all children are XElements (<li> items)
                        var childElements = node.Elements().ToList();
                        if (childElements.Count > 0 && node.Nodes().All(n => n is XElement))
                        {
                            for (int i = 0; i < childElements.Count; i++)
                            {
                                results.Add(new TranslationEntry(
                                    ClassName: className,
                                    Node: $"{name}.{i}",
                                    Original: string.Empty,
                                    Translated: childElements[i].Value,
                                    RequiredMods: null,
                                    SourceFile: null));
                            }
                        }
                        else
                        {
                            results.Add(new TranslationEntry(
                                ClassName: className,
                                Node: name,
                                Original: string.Empty,
                                Translated: node.Value,
                                RequiredMods: null,
                                SourceFile: null));
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error reading DefInjected file '{filePath}': {ex.Message}", ex);
                }
            }
        }
    }

    // ─── Keyed ──────────────────────────────────────────────────────────────────

    private async Task ReadKeyedAsync(string keyedDir, List<TranslationEntry> results, CancellationToken ct)
    {
        foreach (var filePath in EnumerateXmlFiles(keyedDir))
        {
            var text = await _fs.ReadAllTextAsync(filePath, ct);
            var doc = XDocument.Parse(text, LoadOptions.None);
            if (doc.Root is null) continue;

            foreach (var node in doc.Root.Elements())
            {
                results.Add(new TranslationEntry(
                    ClassName: "Keyed",
                    Node: node.Name.LocalName,
                    Original: string.Empty,
                    Translated: node.Value,
                    RequiredMods: null,
                    SourceFile: null));
            }
        }
    }

    // ─── Strings ────────────────────────────────────────────────────────────────

    private async Task ReadStringsAsync(string stringsDir, List<TranslationEntry> results, CancellationToken ct)
    {
        foreach (var filePath in EnumerateFilesRecursive(stringsDir))
        {
            if (!filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = await _fs.ReadAllLinesAsync(filePath, ct);

            // Reconstruct the node prefix from directory path relative to stringsDir
            // e.g. stringsDir/Category/SubCat/File.txt  → node prefix = "Category.SubCat.File"
            var relative = GetRelativePath(stringsDir, filePath);
            var withoutExt = relative.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                ? relative[..^4]
                : relative;
            var nodePrefix = withoutExt.Replace('/', '.').Replace('\\', '.');

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                results.Add(new TranslationEntry(
                    ClassName: "Strings",
                    Node: $"{nodePrefix}.{line}",
                    Original: string.Empty,
                    Translated: line,
                    RequiredMods: null,
                    SourceFile: null));
            }
        }
    }

    // ─── IFileSystem traversal helpers ─────────────────────────────────────────

    private IEnumerable<string> EnumerateDirectoriesRecursive(string root)
    {
        if (!_fs.DirectoryExists(root)) yield break;
        foreach (var dir in _fs.EnumerateDirectories(root))
        {
            yield return dir;
            foreach (var child in EnumerateDirectoriesRecursive(dir))
                yield return child;
        }
    }

    private IEnumerable<string> EnumerateFilesRecursive(string root)
    {
        if (!_fs.DirectoryExists(root)) yield break;
        foreach (var file in _fs.EnumerateFiles(root))
            yield return file;
        foreach (var dir in _fs.EnumerateDirectories(root))
            foreach (var file in EnumerateFilesRecursive(dir))
                yield return file;
    }

    private IEnumerable<string> EnumerateXmlFiles(string dir)
    {
        if (!_fs.DirectoryExists(dir)) return Enumerable.Empty<string>();
        return _fs.EnumerateFiles(dir)
            .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns the single directory segment directly under <paramref name="root"/>, or null if deeper.
    /// </summary>
    private static string? GetRelativeSingleSegment(string root, string path)
    {
        root = root.Replace('\\', '/').TrimEnd('/');
        path = path.Replace('\\', '/').TrimEnd('/');
        if (!path.StartsWith(root + "/", StringComparison.Ordinal)) return null;
        var rel = path[(root.Length + 1)..];
        return rel.Contains('/') ? null : rel;
    }

    private static string GetRelativePath(string root, string path)
    {
        root = root.Replace('\\', '/').TrimEnd('/') + "/";
        path = path.Replace('\\', '/');
        return path.StartsWith(root, StringComparison.Ordinal) ? path[root.Length..] : path;
    }

    private static string JoinPath(params string[] parts) =>
        string.Join('/', parts.Select(p => p.TrimEnd('/', '\\')));
}
