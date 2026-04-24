using System.Globalization;
using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Port of <c>Extractor.DefsUtils.cs:54-231</c> — BFS walk of a resolved Def element that
/// matches tags against <c>ExtractionRule</c> whitelist/blacklist and yields
/// <c>TranslationEntry</c> values. Also handles the <c>XmlExtensions.SettingsMenuDef</c>
/// special case and translation-handle matching.
/// </summary>
public sealed class XmlDefExtractor : IXmlDefExtractor
{
    private static readonly string[] ExtractableTagsXmlExtensionSettings = ["label", "text", "tooltip"];

    /// <inheritdoc/>
    public IEnumerable<TranslationEntry> Extract(
        string defName,
        string className,
        XElement rootNode,
        IReadOnlyDictionary<string, ExtractionRule> rules,
        IReadOnlyList<TranslationHandle> translationHandles,
        bool enableTKey,
        bool isOfficialContent,
        string? currentNormalizedPath = null)
    {
        if (className == "XmlExtensions.SettingsMenuDef")
        {
            foreach (var entry in ExtractXmlExtensionSettings(defName, className, rootNode, currentNormalizedPath))
                yield return entry;
            yield break;
        }

        // REQUIREDMODS is a legacy synthetic attribute added during reference-def loading;
        // in the new pipeline it is passed externally. We read it here for completeness but
        // cannot parse the legacy DSL without the legacy type — leave RequiredMods as null.
        var fileName = isOfficialContent ? rootNode.Attribute("SourceFile")?.Value : null;

        // BFS: (currentNode, currentPath)
        var q = new Queue<(XElement, string)>();

        if (currentNormalizedPath is null)
        {
            foreach (var child in rootNode.Elements())
            {
                var name = child.IsListNode()
                    ? child.GetIdxOfListNode().ToString(CultureInfo.InvariantCulture)
                    : child.Name.LocalName;
                q.Enqueue((child, name));
            }
        }
        else
        {
            var segment = rootNode.IsListNode()
                ? rootNode.GetIdxOfListNode().ToString(CultureInfo.InvariantCulture)
                : rootNode.Name.LocalName;
            q.Enqueue((rootNode, currentNormalizedPath + "." + segment));
        }

        while (q.Count > 0)
        {
            var (curNode, curPath) = q.Dequeue();
            var token = curPath.Split('.');
            var lastTag = token[^1];

            if (curNode.IsTextNode())
            {
                var isListNode = token.Length > 1
                    && int.TryParse(lastTag, out _)
                    && rules.TryGetValue(token[^2], out var parentRule)
                    && parentRule.CanExtract(defName);

                var canExtract = (rules.TryGetValue(lastTag, out var rule) && rule.CanExtract(defName))
                    || isListNode;

                if (canExtract)
                {
                    string nodeName;
                    if (currentNormalizedPath is not null)
                    {
                        nodeName = curPath;
                    }
                    else if (enableTKey && curNode.Attribute("TKey")?.Value is { } tKey)
                    {
                        nodeName = $"{defName}.{tKey}.slateRef";
                    }
                    else
                    {
                        nodeName = $"{defName}.{curPath}";
                    }

                    yield return new TranslationEntry(className, nodeName, curNode.Value, null, null, fileName);
                }

                continue;
            }

            foreach (var childNode in curNode.Elements())
            {
                string path;
                if (childNode.IsListNode())
                {
                    if (MatchTranslationHandle(childNode, translationHandles, out var handleValue))
                        path = $"{curPath}.{handleValue}";
                    else
                        path = $"{curPath}.{childNode.GetIdxOfListNode().ToString(CultureInfo.InvariantCulture)}";
                }
                else
                {
                    path = $"{curPath}.{childNode.Name.LocalName}";
                }

                q.Enqueue((childNode, path));
            }
        }
    }

    /// <summary>
    /// Special-case BFS for <c>XmlExtensions.SettingsMenuDef</c>. Port of
    /// <c>Extractor.DefsUtils.cs:134-203</c>.
    /// </summary>
    public static IEnumerable<TranslationEntry> ExtractXmlExtensionSettings(
        string defName,
        string className,
        XElement rootNode,
        string? currentNormalizedPath = null)
    {
        var q = new Queue<(XElement, string)>();

        if (currentNormalizedPath is null)
        {
            foreach (var child in rootNode.Elements())
            {
                var name = child.IsListNode()
                    ? child.GetIdxOfListNode().ToString(CultureInfo.InvariantCulture)
                    : child.Name.LocalName;
                q.Enqueue((child, name));
            }
        }
        else
        {
            var segment = rootNode.IsListNode()
                ? rootNode.GetIdxOfListNode().ToString(CultureInfo.InvariantCulture)
                : rootNode.Name.LocalName;
            q.Enqueue((rootNode, currentNormalizedPath + "." + segment));
        }

        while (q.Count > 0)
        {
            var (curNode, curPath) = q.Dequeue();
            var token = curPath.Split('.');
            var lastTag = token[^1];

            if (curNode.IsTextNode())
            {
                var isListNode = token.Length > 1
                    && int.TryParse(lastTag, out _)
                    && ExtractableTagsXmlExtensionSettings.Contains(token[^2]);

                var canExtract = ExtractableTagsXmlExtensionSettings.Contains(lastTag) || isListNode;

                if (canExtract)
                {
                    var tKey = curNode.Parent?.Element("tKey")?.Value;
                    var tKeyTip = curNode.Parent?.Element("tKeyTip")?.Value;

                    if (lastTag is "label" or "text" && tKey is not null)
                    {
                        yield return new TranslationEntry("Keyed", tKey, curNode.Value, null, null, null);
                    }
                    else if (lastTag == "tooltip" && tKeyTip is not null)
                    {
                        yield return new TranslationEntry("Keyed", tKeyTip, curNode.Value, null, null, null);
                    }
                    else
                    {
                        yield return new TranslationEntry(className, $"{defName}.{curPath}", curNode.Value, null, null, null);
                    }
                }

                continue;
            }

            foreach (var childNode in curNode.Elements())
            {
                string path;
                if (childNode.IsListNode())
                {
                    // Translation handle matching is intentionally disabled for XmlExtensions
                    // (commented out in legacy code: DefsUtils.cs:193-196)
                    path = $"{curPath}.{childNode.GetIdxOfListNode().ToString(CultureInfo.InvariantCulture)}";
                }
                else
                {
                    path = $"{curPath}.{childNode.Name.LocalName}";
                }

                q.Enqueue((childNode, path));
            }
        }
    }

    /// <summary>
    /// Port of <c>Extractor.DefsUtils.cs:205-231</c> (<c>MatchTranslationHandle</c>) — checks
    /// each handle in <paramref name="handles"/> against the children of <paramref name="node"/>.
    /// Returns the normalized path segment via <paramref name="result"/> when a match is found.
    /// Non-wildcard handles use <c>NormalizedHandle.Normalize</c>; wildcard (<c>*</c>) handles
    /// use <c>.Split('.').Last()</c> to extract the unqualified type name.
    /// </summary>
    private static bool MatchTranslationHandle(
        XElement node,
        IReadOnlyList<TranslationHandle> handles,
        out string result)
    {
        result = string.Empty;
        if (!node.HasElements) return false;

        foreach (var handle in handles)
        {
            foreach (var child in node.Elements())
            {
                if (child.Name.LocalName != handle.Tag) continue;
                if (!child.IsTextNode()) continue;

                result = handle.IsWildcardClass
                    ? child.Value.Split('.').Last()
                    : NormalizedHandle.Normalize(child.Value);

                if (string.IsNullOrWhiteSpace(result))
                    return false;

                return true;
            }
        }

        return false;
    }
}
