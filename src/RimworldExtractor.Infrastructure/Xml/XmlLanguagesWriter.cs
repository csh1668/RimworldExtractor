using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Writes <see cref="TranslationEntry"/> collections to a RimWorld Languages-format directory tree.
/// Ported from <c>legacy/RimworldExtractorInternal/IO.cs:349-666</c> with XDocument instead of XmlDocument.
/// </summary>
public sealed class XmlLanguagesWriter : IXmlLanguagesWriter
{
    private readonly SafeFileWriter _fileWriter;
    private readonly IFileSystem _fs;

    public XmlLanguagesWriter(SafeFileWriter fileWriter, IFileSystem fs)
    {
        _fileWriter = fileWriter;
        _fs = fs;
    }

    public async Task WriteAsync(
        IReadOnlyList<TranslationEntry> translations,
        string rootDir,
        LanguageCode translationLanguage,
        string fileName,
        bool skipNoTranslation,
        bool commentOriginal,
        IReadOnlyList<string> fullListTags,
        CancellationToken cancellationToken = default)
    {
        var defInjected = new List<TranslationEntry>();
        var defInjectedFullList = new List<TranslationEntry>();
        var keyed = new List<TranslationEntry>();
        var strings = new List<TranslationEntry>();
        var patches = new List<TranslationEntry>();

        var isOfficial = translations.Any(x => x.SourceFile != null);

        foreach (var entry in translations)
        {
            if (skipNoTranslation && entry.ClassName != "Strings" && string.IsNullOrEmpty(entry.Translated))
                continue;

            switch (entry.ClassName)
            {
                case "Keyed":
                    keyed.Add(entry);
                    break;
                case "Strings":
                    strings.Add(entry);
                    break;
                default:
                    if (entry.ClassName.StartsWith("Patches.", StringComparison.Ordinal))
                        patches.Add(entry);
                    else if (!isOfficial && fullListTags.Any(tag => entry.Node.Contains(tag, StringComparison.Ordinal)))
                        defInjectedFullList.Add(entry);
                    else
                        defInjected.Add(entry);
                    break;
            }
        }

        var translationDir = JoinPath(rootDir, "Languages", translationLanguage.FolderName);

        await WritePatchesAsync(patches, rootDir, fileName, commentOriginal, translations, cancellationToken);
        await WriteDefInjectedAsync(defInjected, translationDir, fileName, isOfficial, commentOriginal, fullListTags, translations, cancellationToken);
        await WriteDefInjectedFullListAsync(defInjectedFullList, translationDir, fileName, commentOriginal, fullListTags, translations, cancellationToken);
        await WriteKeyedAsync(keyed, translationDir, fileName, isOfficial, commentOriginal, cancellationToken);
        await WriteStringsAsync(strings, translationDir, cancellationToken);
    }

    // ─── Patches ────────────────────────────────────────────────────────────────

    private async Task WritePatchesAsync(
        List<TranslationEntry> patches,
        string rootDir,
        string fileName,
        bool commentOriginal,
        IReadOnlyList<TranslationEntry> allTranslations,
        CancellationToken cancellationToken)
    {
        if (patches.Count == 0) return;

        var outputPath = JoinPath(rootDir, "Patches");
        var root = new XElement("Patch");
        var doc = new XDocument(root);

        // Build skeleton: one entry per RequiredMods group.
        // Key → the <operations> XElement that receives <li> children (or null → direct child of root).
        var entryDict = new Dictionary<string, XElement>(StringComparer.Ordinal);

        foreach (var entry in patches)
        {
            var rm = entry.RequiredMods;
            if (rm is null) continue;
            var key = GroupKey(rm);
            if (entryDict.ContainsKey(key)) continue;

            // Build nested PatchOperationFindMod wrappers around a PatchOperationSequence.
            XElement aboveNode = root;

            foreach (var allowedGroup in rm.Allowed)
            {
                var opFindMod = new XElement("Operation",
                    new XAttribute("Class", "PatchOperationFindMod"));
                var mods = new XElement("mods");
                foreach (var mod in allowedGroup)
                    mods.Add(new XElement("li", mod.Value));
                opFindMod.Add(mods);
                var match = new XElement("match");
                opFindMod.Add(match);
                aboveNode.Add(opFindMod);
                aboveNode = match;
            }

            foreach (var disallowedGroup in rm.Disallowed)
            {
                var opFindMod = new XElement("Operation",
                    new XAttribute("Class", "PatchOperationFindMod"));
                var mods = new XElement("mods");
                foreach (var mod in disallowedGroup)
                    mods.Add(new XElement("li", mod.Value));
                opFindMod.Add(mods);
                var nomatch = new XElement("nomatch");
                opFindMod.Add(nomatch);
                aboveNode.Add(opFindMod);
                aboveNode = nomatch;
            }

            var opSequence = new XElement("Operation",
                new XAttribute("Class", "PatchOperationSequence"));
            opSequence.Add(new XElement("success", "Always"));
            var operations = new XElement("operations");
            opSequence.Add(operations);
            aboveNode.Add(opSequence);
            entryDict[key] = operations;
        }

        // Emit each patch entry.
        foreach (var entry in patches)
        {
            var rm = entry.RequiredMods;
            XElement operation;
            if (rm is not null && !rm.IsEmpty)
            {
                var key = GroupKey(rm);
                var li = new XElement("li");
                entryDict[key].Add(li);
                operation = li;
            }
            else
            {
                operation = new XElement("Operation");
                root.Add(operation);
            }

            operation.Add(new XAttribute("Class", "PatchOperationReplace"));
            operation.Add(new XElement("success", "Always"));
            if (commentOriginal)
                operation.Add(new XComment($"Original={EscapeXml(entry.Original).Replace('-', 'ー')}"));

            // className for Patches entries is "Patches.<ActualClassName>"
            var dotIdx = entry.ClassName.IndexOf('.', StringComparison.Ordinal);
            var patchClassName = dotIdx >= 0 ? entry.ClassName[(dotIdx + 1)..] : entry.ClassName;
            operation.Add(new XElement("xpath", XmlHelpers.BuildXPath(patchClassName, entry.Node)));

            var lastNode = entry.Node.Split('.').Last();
            if (int.TryParse(lastNode, out _)) lastNode = "li";
            var value = new XElement("value", new XElement(lastNode, entry.Translated ?? entry.Original));
            operation.Add(value);
        }

        var filePath = JoinPath(outputPath, fileName + ".xml");
        await SaveXDocumentAsync(doc, filePath, cancellationToken);
    }

    // ─── DefInjected (normal) ────────────────────────────────────────────────────

    private async Task WriteDefInjectedAsync(
        List<TranslationEntry> defInjected,
        string translationDir,
        string fileName,
        bool isOfficial,
        bool commentOriginal,
        IReadOnlyList<string> fullListTags,
        IReadOnlyList<TranslationEntry> allTranslations,
        CancellationToken cancellationToken)
    {
        if (defInjected.Count == 0) return;

        var defInjectedDir = JoinPath(translationDir, "DefInjected");
        var xmls = new Dictionary<string, XElement>(StringComparer.Ordinal); // key → LanguageData root

        foreach (var entry in defInjected)
        {
            var dictKey = $"{entry.ClassName}|{entry.SourceFile}";
            if (!xmls.TryGetValue(dictKey, out var languageData))
            {
                languageData = new XElement("LanguageData");
                xmls[dictKey] = languageData;
            }

            if (commentOriginal)
                languageData.Add(new XComment($"Original={EscapeXml(entry.Original).Replace('-', 'ー')}"));

            var text = ResolveInterpolation(entry.Translated ?? entry.Original, allTranslations);
            languageData.Add(new XElement(entry.Node, text));
        }

        foreach (var (key, languageData) in xmls)
        {
            var tokens = key.Split('|');
            var className = tokens[0];
            var sourceFile = tokens[1]; // may be "" if null was stored

            var outputPath = isOfficial
                ? JoinPath(defInjectedDir, className, sourceFile + ".xml")
                : JoinPath(defInjectedDir, className, fileName + ".xml");

            DoFullListTranslation(languageData, fullListTags);
            var xmlDoc = WrapInDocument(languageData);
            await SaveXDocumentAsync(xmlDoc, outputPath, cancellationToken);
        }
    }

    // ─── DefInjected FullList ────────────────────────────────────────────────────

    private async Task WriteDefInjectedFullListAsync(
        List<TranslationEntry> defInjectedFullList,
        string translationDir,
        string fileName,
        bool commentOriginal,
        IReadOnlyList<string> fullListTags,
        IReadOnlyList<TranslationEntry> allTranslations,
        CancellationToken cancellationToken)
    {
        if (defInjectedFullList.Count == 0) return;

        var defInjectedDir = JoinPath(translationDir, "DefInjected");
        var xmls = new Dictionary<(string ClassName, string NodeParent), XElement>();

        foreach (var entry in defInjectedFullList)
        {
            var nodeParent = entry.Node[..entry.Node.LastIndexOf('.')];
            var dictKey = (entry.ClassName, nodeParent);

            if (!xmls.TryGetValue(dictKey, out var languageData))
            {
                languageData = new XElement("LanguageData");
                xmls[dictKey] = languageData;
            }

            if (commentOriginal)
                languageData.Add(new XComment($"Original={EscapeXml(entry.Original).Replace('-', 'ー')}"));

            var text = ResolveInterpolation(entry.Translated ?? entry.Original, allTranslations);
            languageData.Add(new XElement(entry.Node, text));
        }

        int counter = 0;
        foreach (var ((className, _), languageData) in xmls)
        {
            var outputPath = JoinPath(defInjectedDir, className, $"{fileName}-{counter++:D2}.xml");
            DoFullListTranslation(languageData, fullListTags);
            var xmlDoc = WrapInDocument(languageData);
            await SaveXDocumentAsync(xmlDoc, outputPath, cancellationToken);
        }
    }

    // ─── Keyed ──────────────────────────────────────────────────────────────────

    private async Task WriteKeyedAsync(
        List<TranslationEntry> keyed,
        string translationDir,
        string fileName,
        bool isOfficial,
        bool commentOriginal,
        CancellationToken cancellationToken)
    {
        if (keyed.Count == 0) return;

        var keyedDir = JoinPath(translationDir, "Keyed");
        var xmls = new Dictionary<string, XElement>(StringComparer.Ordinal);

        foreach (var entry in keyed)
        {
            var dictKey = isOfficial ? entry.SourceFile ?? "default" : "default";
            if (!xmls.TryGetValue(dictKey, out var languageData))
            {
                languageData = new XElement("LanguageData");
                xmls[dictKey] = languageData;
            }

            if (commentOriginal)
                languageData.Add(new XComment($"Original={EscapeXml(entry.Original).Replace('-', 'ー')}"));
            languageData.Add(new XElement(entry.Node, entry.Translated ?? entry.Original));
        }

        foreach (var (key, languageData) in xmls)
        {
            var outputPath = isOfficial
                ? JoinPath(keyedDir, $"{key}.xml")
                : JoinPath(keyedDir, $"{fileName}.xml");

            var xmlDoc = WrapInDocument(languageData);
            await SaveXDocumentAsync(xmlDoc, outputPath, cancellationToken);
        }
    }

    // ─── Strings ────────────────────────────────────────────────────────────────

    private async Task WriteStringsAsync(
        List<TranslationEntry> strings,
        string translationDir,
        CancellationToken cancellationToken)
    {
        if (strings.Count == 0) return;

        var stringDir = JoinPath(translationDir, "Strings");
        var txts = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var entry in strings)
        {
            // Node is like "Category.SubCat.FileName.lineContent" — group by everything except last segment
            var dotIdx = entry.Node.LastIndexOf('.');
            var fileKey = dotIdx >= 0 ? entry.Node[..dotIdx] : entry.Node;

            if (!txts.TryGetValue(fileKey, out var lines))
            {
                lines = new List<string>();
                txts[fileKey] = lines;
            }

            lines.Add(entry.Translated ?? entry.Original);
        }

        foreach (var (fileKey, lines) in txts)
        {
            // fileKey like "Category.SubCat.FileName" →  dir = "Category/SubCat", file = "FileName.txt"
            var parts = fileKey.Split('.');
            var dirParts = parts[..^1];
            var baseName = parts[^1];

            var dirPath = stringDir;
            foreach (var part in dirParts)
                dirPath = JoinPath(dirPath, part);

            var filePath = JoinPath(dirPath, baseName + ".txt");
            await _fileWriter.WriteAsync(filePath, async stream =>
            {
                await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
                foreach (var line in lines)
                    await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
            }, cancellationToken);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Collapses indexed nodes matching <c>.+\.{tag}.\d+</c> pattern into
    /// <c>&lt;tag&gt;&lt;li&gt;...&lt;/li&gt;...&lt;/tag&gt;</c> elements.
    /// Ported from <c>legacy IO.cs:819-858</c>.
    /// </summary>
    private static void DoFullListTranslation(XElement languageData, IReadOnlyList<string> fullListTags)
    {
        if (fullListTags.Count == 0) return;

        // Build regex patterns: ".+?\.{tag}\.\d+"
        var patterns = fullListTags
            .Select(tag => new Regex($@"^.+?\.{Regex.Escape(tag)}\.\d+$", RegexOptions.Compiled))
            .ToList();

        var fullListDict = new Dictionary<string, XElement>(StringComparer.Ordinal);
        var removedNodes = new Dictionary<string, List<XElement>>(StringComparer.Ordinal);

        foreach (var child in languageData.Elements().ToList())
        {
            var nodeName = child.Name.LocalName;
            if (!patterns.Any(p => p.IsMatch(nodeName)))
                continue;

            // Strip the trailing ".N" index to get the parent tag name
            var parentName = nodeName[..nodeName.LastIndexOf('.')];

            if (!fullListDict.TryGetValue(parentName, out var fullList))
            {
                fullList = new XElement(parentName);
                fullListDict[parentName] = fullList;
            }

            if (!removedNodes.TryGetValue(parentName, out var removed))
            {
                removed = new List<XElement>();
                removedNodes[parentName] = removed;
            }

            fullList.Add(new XElement("li", child.Value));
            removed.Add(child);
        }

        foreach (var (parentName, fullList) in fullListDict)
        {
            var removed = removedNodes[parentName];
            var lastRemoved = removed.Last();

            // Insert consolidated element right after last removed node
            lastRemoved.AddAfterSelf(fullList);
            foreach (var node in removed)
                node.Remove();
        }
    }

    /// <summary>
    /// Resolves <c>{*ClassName+Node}</c> interpolation tokens in translated text.
    /// Ported from <c>legacy IO.cs:535-543</c>.
    /// </summary>
    private static string ResolveInterpolation(string text, IReadOnlyList<TranslationEntry> allTranslations)
    {
        if (!text.Contains("{*", StringComparison.Ordinal))
            return text;

        return Regex.Replace(text, @"\{\*(.*?)\}", match =>
        {
            var targetIdentifier = match.Groups[1].Value;
            var replacement = allTranslations.FirstOrDefault(x => x.ClassNode == targetIdentifier);
            return replacement is not null
                ? replacement.Translated ?? replacement.Original
                : "ERR";
        });
    }

    /// <summary>Produces a stable grouping key for a <see cref="RequiredMods"/> instance.</summary>
    private static string GroupKey(RequiredMods rm)
    {
        var sb = new StringBuilder();
        sb.Append("A:");
        foreach (var group in rm.Allowed)
        {
            sb.Append('[');
            sb.Append(string.Join(',', group.Select(r => $"{r.Kind}:{r.Value}")));
            sb.Append(']');
        }
        sb.Append("|D:");
        foreach (var group in rm.Disallowed)
        {
            sb.Append('[');
            sb.Append(string.Join(',', group.Select(r => $"{r.Kind}:{r.Value}")));
            sb.Append(']');
        }
        return sb.ToString();
    }

    private async Task SaveXDocumentAsync(XDocument doc, string path, CancellationToken cancellationToken)
    {
        await _fileWriter.WriteAsync(path, async stream =>
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                Async = true,
            };
            await using var xmlWriter = XmlWriter.Create(stream, settings);
            await doc.SaveAsync(xmlWriter, cancellationToken);
            await xmlWriter.FlushAsync();
        }, cancellationToken);
    }

    private static XDocument WrapInDocument(XElement root) =>
        new(new XDeclaration("1.0", "utf-8", null), root);

    /// <summary>Path join that always uses forward slashes (IFileSystem normalises anyway).</summary>
    private static string JoinPath(params string[] parts) =>
        string.Join('/', parts.Select(p => p.TrimEnd('/', '\\')));

    private static string EscapeXml(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;")
         .Replace("\"", "&quot;")
         .Replace("'", "&apos;");
}
