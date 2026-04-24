using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Infrastructure.Xml;

/// <summary>
/// Port of <c>legacy/PatchOperations.cs</c> — applies RimWorld Patch XML operations against
/// an <c>XDocument</c> combined-defs tree and yields <c>TranslationEntry</c> values found
/// inside patched subtrees.
/// </summary>
public sealed class XPatchProcessor : IXPatchProcessor
{
    private readonly IXmlDefExtractor _extractor;
    private readonly ILogger<XPatchProcessor> _logger;

    public XPatchProcessor(IXmlDefExtractor extractor, ILogger<XPatchProcessor> logger)
    {
        _extractor = extractor;
        _logger = logger;
    }

    /// <inheritdoc/>
    public PatchProcessingResult Apply(
        XDocument combinedDefs,
        XElement patchRoot,
        IReadOnlyDictionary<string, ExtractionRule> rules,
        IReadOnlyList<TranslationHandle> translationHandles,
        bool enableTKey,
        bool isOfficialContent,
        RequiredMods? initialRequiredMods = null,
        bool prePatchMode = false)
    {
        var entries = new List<TranslationEntry>();
        var defsAdded = new List<XElement>();

        var ctx = new ProcessContext(combinedDefs, rules, translationHandles, enableTKey, isOfficialContent, entries, defsAdded);

        foreach (var operation in patchRoot.Elements("Operation"))
        {
            ProcessOperation(operation, initialRequiredMods, prePatchMode, ctx);
        }

        return new PatchProcessingResult(entries, defsAdded);
    }

    // ──────────────────────────────────────────────
    // Dispatch
    // ──────────────────────────────────────────────

    private void ProcessOperation(XElement operation, RequiredMods? requiredMods, bool prePatchMode, ProcessContext ctx)
    {
        // MayRequire attribute on any operation node adds to required mods
        // Port of legacy PatchOperationRecursive:22-26
        var mayRequire = operation.Attribute("MayRequire")?.Value;
        if (mayRequire is not null)
        {
            var packageIds = mayRequire.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0);
            var newAllowedGroup = packageIds.Select(ModReference.ByPackageId).ToList();
            var combinedAllowed = (requiredMods?.Allowed ?? []).Append(newAllowedGroup);
            var combinedDisallowed = requiredMods?.Disallowed ?? [];
            requiredMods = new RequiredMods(combinedAllowed, combinedDisallowed);
        }

        var opClass = operation.Attribute("Class")?.Value ?? operation.Element("Class")?.Value;

        switch (opClass)
        {
            case "PatchOperationAdd":
                HandleAdd(operation, requiredMods, prePatchMode, ctx);
                break;
            case "PatchOperationReplace":
                HandleReplace(operation, requiredMods, prePatchMode, ctx);
                break;
            case "PatchOperationInsert":
                HandleInsert(operation, requiredMods, prePatchMode, ctx);
                break;
            case "PatchOperationAddModExtension":
                HandleAddModExtension(operation, requiredMods, prePatchMode, ctx);
                break;
            case "PatchOperationSequence":
                HandleSequence(operation, requiredMods, prePatchMode, ctx);
                break;
            case "PatchOperationFindMod":
                HandleFindMod(operation, requiredMods, prePatchMode, ctx);
                break;
            case "PatchOperationAttributeAdd":
                HandleAttribute(operation, requiredMods, PatchAttributeMode.Add, ctx);
                break;
            case "PatchOperationAttributeRemove":
                HandleAttribute(operation, requiredMods, PatchAttributeMode.Remove, ctx);
                break;
            case "PatchOperationAttributeSet":
                HandleAttribute(operation, requiredMods, PatchAttributeMode.Set, ctx);
                break;
            case "JPTools.PatchOperationFindModById":
                // Modder-specific, skipped per spec
                break;
            default:
                _logger.LogWarning("Unknown PatchOperation type: {OpClass}", opClass);
                break;
        }
    }

    // ──────────────────────────────────────────────
    // PatchOperationAdd (legacy:216-272)
    // ──────────────────────────────────────────────

    private void HandleAdd(XElement operation, RequiredMods? requiredMods, bool prePatchMode, ProcessContext ctx)
    {
        if (prePatchMode) return;

        var xpath = operation.Element("xpath")?.Value;
        var value = operation.Element("value");
        if (xpath is null || value is null)
        {
            _logger.LogWarning("PatchOperationAdd: xpath or value is missing.");
            return;
        }

        var selectNodes = SelectElements(ctx.CombinedDefs, xpath);
        foreach (var selectNode in selectNodes)
        {
            var (rootDefNode, nodeName) = GetRootDefNode(selectNode);

            foreach (var valueChild in value.Elements().ToList())
            {
                // Deep-clone and append
                var imported = new XElement(valueChild);
                selectNode.Add(imported);

                if (xpath is "Defs" or "Defs/")
                {
                    ctx.DefsAdded.Add(imported);
                    continue;
                }

                var curRootDefNode = rootDefNode ?? imported;
                var defName = curRootDefNode.Element("defName")?.Value;
                if (defName is null)
                {
                    _logger.LogWarning("PatchOperationAdd: defName not found. xpath={Xpath}", xpath);
                    continue;
                }

                var className = curRootDefNode.Attribute("Class")?.Value ?? curRootDefNode.Name.LocalName;

                foreach (var translation in _extractor.Extract(
                    defName, className, imported,
                    ctx.Rules, ctx.TranslationHandles, ctx.EnableTKey, ctx.IsOfficialContent,
                    currentNormalizedPath: defName))
                {
                    if (translation.ClassName == "Keyed")
                    {
                        ctx.Entries.Add(translation with { RequiredMods = requiredMods });
                    }
                    else
                    {
                        ctx.Entries.Add(translation with
                        {
                            ClassName = $"Patches.{translation.ClassName}",
                            RequiredMods = requiredMods
                        });
                    }
                }
            }
        }
    }

    // ──────────────────────────────────────────────
    // PatchOperationReplace (legacy:171-214)
    // ──────────────────────────────────────────────

    private void HandleReplace(XElement operation, RequiredMods? requiredMods, bool prePatchMode, ProcessContext ctx)
    {
        if (prePatchMode) return;

        var xpath = operation.Element("xpath")?.Value;
        var value = operation.Element("value");
        if (xpath is null || value is null)
        {
            _logger.LogWarning("PatchOperationReplace: xpath or value is missing.");
            return;
        }

        var selectNodes = SelectElements(ctx.CombinedDefs, xpath);
        foreach (var selectNode in selectNodes)
        {
            var parent = selectNode.Parent;
            if (parent is null) continue;

            var (rootDefNode, nodeName) = GetRootDefNode(parent);

            string? defName;
            string? className;

            if (rootDefNode is not null)
            {
                defName = rootDefNode.Element("defName")?.Value;
                className = rootDefNode.Attribute("Class")?.Value ?? rootDefNode.Name.LocalName;
            }
            else
            {
                defName = selectNode.Element("defName")?.Value;
                className = selectNode.Attribute("Class")?.Value ?? selectNode.Name.LocalName;
            }

            if (defName is null || className is null)
                _logger.LogWarning("PatchOperationReplace: defName or className not found. xpath={Xpath}", xpath);

            foreach (var valueChild in value.Elements().ToList())
            {
                var imported = new XElement(valueChild);
                selectNode.AddBeforeSelf(imported);

                foreach (var translation in _extractor.Extract(
                    defName ?? string.Empty, className ?? string.Empty, imported,
                    ctx.Rules, ctx.TranslationHandles, ctx.EnableTKey, ctx.IsOfficialContent,
                    currentNormalizedPath: defName))
                {
                    ctx.Entries.Add(translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods
                    });
                }
            }

            selectNode.Remove();
        }
    }

    // ──────────────────────────────────────────────
    // PatchOperationInsert (legacy:82-126)
    // ──────────────────────────────────────────────

    private void HandleInsert(XElement operation, RequiredMods? requiredMods, bool prePatchMode, ProcessContext ctx)
    {
        if (prePatchMode) return;

        var xpath = operation.Element("xpath")?.Value;
        var value = operation.Element("value");
        if (xpath is null || value is null)
        {
            _logger.LogWarning("PatchOperationInsert: xpath or value is missing.");
            return;
        }

        var selectNodes = SelectElements(ctx.CombinedDefs, xpath);
        foreach (var selectNode in selectNodes)
        {
            var parent = selectNode.Parent;
            if (parent is null)
            {
                _logger.LogWarning("PatchOperationInsert: selected node {Name} has no parent.", selectNode.Name);
                continue;
            }

            var (rootDefNode, nodeName) = GetRootDefNode(parent);

            foreach (var valueChild in value.Elements().ToList())
            {
                var imported = new XElement(valueChild);
                selectNode.AddAfterSelf(imported);

                var curRootDefNode = rootDefNode ?? imported;
                var defName = curRootDefNode.Element("defName")?.Value;
                if (defName is null) continue;

                var className = curRootDefNode.Attribute("Class")?.Value ?? curRootDefNode.Name.LocalName;

                foreach (var translation in _extractor.Extract(
                    defName, className, imported,
                    ctx.Rules, ctx.TranslationHandles, ctx.EnableTKey, ctx.IsOfficialContent,
                    currentNormalizedPath: defName))
                {
                    ctx.Entries.Add(translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods
                    });
                }
            }
        }
    }

    // ──────────────────────────────────────────────
    // PatchOperationAddModExtension (legacy:128-169)
    // ──────────────────────────────────────────────

    private void HandleAddModExtension(XElement operation, RequiredMods? requiredMods, bool prePatchMode, ProcessContext ctx)
    {
        if (prePatchMode) return;

        var xpath = operation.Element("xpath")?.Value;
        var value = operation.Element("value");
        if (xpath is null || value is null)
        {
            _logger.LogWarning("PatchOperationAddModExtension: xpath or value is missing.");
            return;
        }

        var selectNodes = SelectElements(ctx.CombinedDefs, xpath);
        foreach (var selectNode in selectNodes)
        {
            var (rootDefNode, nodeName) = GetRootDefNode(selectNode);

            var modExtensionsNode = selectNode.Element("modExtensions");
            if (modExtensionsNode is null)
            {
                modExtensionsNode = new XElement("modExtensions");
                selectNode.Add(modExtensionsNode);
            }

            foreach (var valueChild in value.Elements().ToList())
            {
                var imported = new XElement(valueChild);
                modExtensionsNode.Add(imported);

                var curRootDefNode = rootDefNode ?? imported;
                var defName = curRootDefNode.Element("defName")?.Value;
                if (defName is null) continue;

                var className = curRootDefNode.Attribute("Class")?.Value ?? curRootDefNode.Name.LocalName;

                foreach (var translation in _extractor.Extract(
                    defName, className, imported,
                    ctx.Rules, ctx.TranslationHandles, ctx.EnableTKey, ctx.IsOfficialContent,
                    currentNormalizedPath: defName))
                {
                    ctx.Entries.Add(translation with
                    {
                        ClassName = $"Patches.{translation.ClassName}",
                        RequiredMods = requiredMods
                    });
                }
            }
        }
    }

    // ──────────────────────────────────────────────
    // PatchOperationSequence (legacy:274-286)
    // ──────────────────────────────────────────────

    private void HandleSequence(XElement operation, RequiredMods? requiredMods, bool prePatchMode, ProcessContext ctx)
    {
        var operations = operation.Element("operations");
        if (operations is null) return;

        foreach (var childOperation in operations.Elements())
        {
            ProcessOperation(childOperation, requiredMods, prePatchMode, ctx);
        }
    }

    // ──────────────────────────────────────────────
    // PatchOperationFindMod (legacy:288-320)
    // ──────────────────────────────────────────────

    private void HandleFindMod(XElement operation, RequiredMods? requiredMods, bool prePatchMode, ProcessContext ctx)
    {
        var modsList = (operation.Element("mods")?.Elements("li")
            .Select(li => li.Value.Trim())
            .Where(v => v.Length > 0)
            .ToList()) ?? [];

        // match branch: mods become allowed
        var matchEl = operation.Element("match");
        if (matchEl is not null)
        {
            var matchRequiredMods = AddAllowedModNames(requiredMods, modsList);
            ProcessOperation(matchEl, matchRequiredMods, prePatchMode, ctx);
        }

        // nomatch branch: mods become disallowed
        var noMatchEl = operation.Element("nomatch");
        if (noMatchEl is not null)
        {
            var noMatchRequiredMods = AddDisallowedModNames(requiredMods, modsList);
            ProcessOperation(noMatchEl, noMatchRequiredMods, prePatchMode, ctx);
        }
    }

    // ──────────────────────────────────────────────
    // PatchOperationAttribute Add/Remove/Set (legacy:322-366)
    // ──────────────────────────────────────────────

    private void HandleAttribute(XElement operation, RequiredMods? requiredMods, PatchAttributeMode mode, ProcessContext ctx)
    {
        var xpath = operation.Element("xpath")?.Value;
        var value = operation.Element("value")?.Value;
        var attribute = operation.Element("attribute")?.Value;

        if (xpath is null || (mode != PatchAttributeMode.Remove && value is null) || attribute is null)
        {
            _logger.LogWarning("PatchOperationAttribute: xpath, value, or attribute is missing.");
            return;
        }

        var selectNodes = SelectElements(ctx.CombinedDefs, xpath);
        foreach (var selectNode in selectNodes)
        {
            switch (mode)
            {
                case PatchAttributeMode.Add:
                    if (selectNode.Attribute(attribute) is null)
                        selectNode.SetAttributeValue(attribute, value);
                    break;
                case PatchAttributeMode.Remove:
                    selectNode.Attribute(attribute)?.Remove();
                    break;
                case PatchAttributeMode.Set:
                    selectNode.SetAttributeValue(attribute, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
        // Attribute operations do not yield translation entries
        _ = requiredMods; // parameter intentionally unused — kept for signature symmetry
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static List<XElement> SelectElements(XDocument doc, string xpath)
    {
        try
        {
            return doc.XPathSelectElements(xpath).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// Walks up from <paramref name="node"/> to find the Def root element
    /// (direct child of <c>&lt;Defs&gt;</c>). Returns null if not found (e.g. inside Keyed).
    /// Second return value is the node name of the Defs container (e.g. "Defs").
    /// Port of legacy <c>Extractor.GetRootDefNode</c>.
    /// </summary>
    private static (XElement? RootDefNode, string? NodeName) GetRootDefNode(XElement node)
    {
        var current = node;
        while (current is not null)
        {
            if (current.Parent?.Name.LocalName == "Defs")
                return (current, current.Parent.Name.LocalName);
            current = current.Parent;
        }

        return (null, null);
    }

    private static RequiredMods AddAllowedModNames(RequiredMods? existing, List<string> modNames)
    {
        if (modNames.Count == 0)
            return existing ?? RequiredMods.Empty;

        var newGroup = (IReadOnlyList<ModReference>)modNames.Select(ModReference.ByModName).ToArray();
        var allowed = (existing?.Allowed ?? []).Append(newGroup);
        var disallowed = existing?.Disallowed ?? [];
        return new RequiredMods(allowed, disallowed);
    }

    private static RequiredMods AddDisallowedModNames(RequiredMods? existing, List<string> modNames)
    {
        if (modNames.Count == 0)
            return existing ?? RequiredMods.Empty;

        var newGroup = (IReadOnlyList<ModReference>)modNames.Select(ModReference.ByModName).ToArray();
        var allowed = existing?.Allowed ?? [];
        var disallowed = (existing?.Disallowed ?? []).Append(newGroup);
        return new RequiredMods(allowed, disallowed);
    }

    // ──────────────────────────────────────────────
    // Inner types
    // ──────────────────────────────────────────────

    private enum PatchAttributeMode { Add, Remove, Set }

    /// <summary>Immutable execution context shared across all handler calls in one Apply invocation.</summary>
    private sealed record ProcessContext(
        XDocument CombinedDefs,
        IReadOnlyDictionary<string, ExtractionRule> Rules,
        IReadOnlyList<TranslationHandle> TranslationHandles,
        bool EnableTKey,
        bool IsOfficialContent,
        List<TranslationEntry> Entries,
        List<XElement> DefsAdded);
}
