using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Extracts translatable <c>TranslationEntry</c> values from a resolved Def XElement via BFS.
/// </summary>
public interface IXmlDefExtractor
{
    /// <summary>
    /// Walk <paramref name="rootNode"/> and yield every extractable text value.
    /// </summary>
    /// <param name="defName">The Def's defName (used in path construction).</param>
    /// <param name="className">Def kind (e.g. <c>ThingDef</c>), also determines XmlExtensions dispatch.</param>
    /// <param name="rootNode">The root element of the resolved Def (after inheritance).</param>
    /// <param name="rules">Tag whitelist/blacklist from <c>ExtractionSettings.Rules</c>.</param>
    /// <param name="translationHandles">Ordered list of translation-handle matchers (legacy Prefabs.TranslationHandles).</param>
    /// <param name="enableTKey">When true, honour <c>TKey</c> attribute on leaf nodes.</param>
    /// <param name="isOfficialContent">When true, include SourceFile on entries.</param>
    /// <param name="currentNormalizedPath">Non-null when called recursively for sub-nodes.</param>
    IEnumerable<TranslationEntry> Extract(
        string defName,
        string className,
        XElement rootNode,
        IReadOnlyDictionary<string, ExtractionRule> rules,
        IReadOnlyList<TranslationHandle> translationHandles,
        bool enableTKey,
        bool isOfficialContent,
        string? currentNormalizedPath = null);
}
