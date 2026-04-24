using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 7: For each selected <c>Patches</c> folder, concatenate all <c>&lt;Operation&gt;</c>
/// nodes from its XML files into a single <c>&lt;Patch&gt;</c> document, apply the patch
/// processor, and emit translation entries. If the patch processor reports defs added by
/// patches (<c>PatchOperationAdd</c> with xpath targeting the Defs root), run inheritance
/// resolution on those defs and re-extract them with a <c>Patches.</c> class-name prefix.
/// Ports <c>legacy/Extractor.cs:287–372 ExtractPatches / ExtractPatchesInternal</c>.
/// </summary>
public sealed class ExtractPatchesStage : IExtractionStage
{
    private readonly IXPatchProcessor _patchProcessor;
    private readonly IXmlDefParser _parser;
    private readonly IXmlDefExtractor _defExtractor;
    private readonly IXmlInheritanceResolver _inheritanceResolver;
    private readonly FileSystemGateway _gateway;
    private readonly ILogger<ExtractPatchesStage> _logger;

    public string Name => "Extract Patches";

    public ExtractPatchesStage(
        IXPatchProcessor patchProcessor,
        IXmlDefParser parser,
        IXmlDefExtractor defExtractor,
        IXmlInheritanceResolver inheritanceResolver,
        FileSystemGateway gateway,
        ILogger<ExtractPatchesStage> logger)
    {
        _patchProcessor = patchProcessor;
        _parser = parser;
        _defExtractor = defExtractor;
        _inheritanceResolver = inheritanceResolver;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task ExecuteAsync(ExtractionContext context)
    {
        var settings = context.Request.Settings.Extraction;
        var rules = settings.Rules.ToDictionary(r => r.Tag, r => r);
        var handles = settings.TranslationHandles;
        var enableTKey = settings.EnableTkey;

        foreach (var folder in context.Request.SelectedFolders
                     .Where(f => f.FolderName.EndsWith("Patches", StringComparison.Ordinal)))
        {
            await ProcessPatchFolderAsync(context, folder, rules, handles, enableTKey);
        }
    }

    private async Task ProcessPatchFolderAsync(
        ExtractionContext context,
        Domain.Entities.ExtractableFolder folder,
        IReadOnlyDictionary<string, Domain.Rules.ExtractionRule> rules,
        IReadOnlyList<Domain.Rules.TranslationHandle> handles,
        bool enableTKey)
    {
        // Build RequiredMods from the folder's RequiredPackageId.
        RequiredMods? folderMods = BuildRequiredMods(folder.RequiredPackageId);

        // Concatenate all <Operation> elements from all xml files under the folder
        // into a single synthetic <Patch> XElement — mirroring the legacy pattern
        // of building a single XmlDocument.
        var patchRoot = new XElement("Patch");
        var patchesRoot = folder.FullPath;

        foreach (var filePath in _gateway.DescendantFiles(patchesRoot))
        {
            if (!filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;

            XDocument childDoc;
            try
            {
                childDoc = await _parser.ParseAsync(filePath, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse patch file {FilePath}", filePath);
                continue;
            }

            if (childDoc.Root is null) continue;

            foreach (var operation in childDoc.Root.Elements("Operation"))
            {
                patchRoot.Add(new XElement(operation));
            }
        }

        if (!patchRoot.HasElements) return;

        // Apply all operations: emits entries and collects any defs added by PatchOperationAdd targeting Defs.
        var result = _patchProcessor.Apply(
            context.CombinedDefs,
            patchRoot,
            rules,
            handles,
            enableTKey,
            context.IsOfficialContent,
            initialRequiredMods: null,
            prePatchMode: false);

        // Emit entries with folder RequiredMods combined.
        foreach (var entry in result.Entries)
        {
            var combined = folderMods is null
                ? entry.RequiredMods
                : entry.RequiredMods?.Combine(folderMods) ?? folderMods;
            context.Results.Add(entry with { RequiredMods = combined });
        }

        // Post-patch def extraction: if any defs were added directly to the Defs root by
        // PatchOperationAdd (xpath="Defs"), resolve inheritance and re-extract them with
        // a "Patches." class-name prefix — mirroring legacy Extractor.cs:346-371.
        var addedDefs = result.DefsAddedByPatches;
        if (addedDefs.Count == 0) return;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Post-patch: resolving inheritance for {Count} defs added by patches", addedDefs.Count);

        // Update ParentLookup with Name-bearing defs before resolving inheritance.
        foreach (var def in addedDefs)
        {
            var nameAttr = def.Attribute("Name")?.Value;
            if (nameAttr is not null)
            {
                context.ParentLookup[nameAttr] = def;
            }
        }

        // Build a synthetic document containing the added defs together with the full
        // CombinedDefs so inheritance can be resolved properly.
        var syntheticDoc = new XDocument(new XElement("Defs"));
        foreach (var existing in context.CombinedDefs.Root!.Elements())
        {
            syntheticDoc.Root!.Add(new XElement(existing));
        }

        foreach (var def in addedDefs)
        {
            syntheticDoc.Root!.Add(new XElement(def));
        }

        var resolvedDoc = _inheritanceResolver.Resolve(syntheticDoc);

        // Identify the newly-added defs by defName in the resolved document.
        var addedDefNames = addedDefs
            .Select(d => d.Element("defName")?.Value)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.Ordinal);

        var resolvedAdded = resolvedDoc.Root!.Elements()
            .Where(e => addedDefNames.Contains(e.Element("defName")?.Value))
            .ToList();

        foreach (var def in resolvedAdded)
        {
            var defName = def.Element("defName")?.Value;
            if (defName is null) continue;

            var className = def.Attribute("Class")?.Value ?? def.Name.LocalName;
            className = char.ToUpper(className[0], CultureInfo.InvariantCulture) + className[1..];

            foreach (var entry in _defExtractor.Extract(
                defName, className, def, rules, handles, enableTKey, context.IsOfficialContent))
            {
                var entryMods = folderMods is null
                    ? entry.RequiredMods
                    : entry.RequiredMods?.Combine(folderMods) ?? folderMods;

                context.Results.Add(entry with
                {
                    ClassName = $"Patches.{entry.ClassName}",
                    RequiredMods = entryMods,
                });
            }
        }
    }

    private static RequiredMods? BuildRequiredMods(string? requiredPackageId)
    {
        if (string.IsNullOrEmpty(requiredPackageId)) return null;

        var refs = requiredPackageId.Split(',')
            .Select(p => ModReference.ByPackageId(p.Trim()))
            .ToArray();
        return new RequiredMods(
            allowed: new[] { (IEnumerable<ModReference>)refs },
            disallowed: Array.Empty<IEnumerable<ModReference>>());
    }
}
