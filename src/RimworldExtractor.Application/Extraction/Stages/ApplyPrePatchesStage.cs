using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 2: Apply Patches from the target mod (and reference mods) in
/// <c>prePatchMode=true</c> to mutate <see cref="ExtractionContext.CombinedDefs"/> before
/// inheritance resolution. In prePatchMode the patches add/remove/replace nodes but emit
/// no <c>TranslationEntry</c> values. After all patches run, the stage re-scans
/// CombinedDefs for Name-bearing nodes added by patches and updates
/// <see cref="ExtractionContext.ParentLookup"/>.
/// Ports <c>legacy/Extractor.cs:155–173 DoPrePatch</c>.
/// </summary>
public sealed class ApplyPrePatchesStage : IExtractionStage
{
    private readonly IXPatchProcessor _patchProcessor;
    private readonly IXmlDefParser _parser;
    private readonly IModLister _modLister;
    private readonly FileSystemGateway _gateway;
    private readonly ILogger<ApplyPrePatchesStage> _logger;

    public string Name => "Apply pre-patches";

    public ApplyPrePatchesStage(
        IXPatchProcessor patchProcessor,
        IXmlDefParser parser,
        IModLister modLister,
        FileSystemGateway gateway,
        ILogger<ApplyPrePatchesStage> logger)
    {
        _patchProcessor = patchProcessor;
        _parser = parser;
        _modLister = modLister;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task ExecuteAsync(ExtractionContext context)
    {
        var settings = context.Request.Settings.Extraction;

        // Collect patch folders: target mod's Patches folders first, then reference mod patches.
        var patchFolders = context.Request.SelectedFolders
            .Where(f => Path.GetFileName(f.FolderName) == "Patches")
            .Select(f => f.FullPath)
            .ToList();

        if (context.Request.ReferenceMods is { Count: > 0 } refMods)
        {
            foreach (var refMod in refMods)
            {
                var refFolders = _modLister.GetExtractableFolders(refMod);
                foreach (var folder in refFolders)
                {
                    if (Path.GetFileName(folder.FolderName) == "Patches")
                    {
                        patchFolders.Add(folder.FullPath);
                    }
                }
            }
        }

        // Use last-wins to handle duplicate tags (legacy Prefabs.dat can have them).
        var rulesDictionary = new Dictionary<string, Domain.Rules.ExtractionRule>(StringComparer.Ordinal);
        foreach (var rule in settings.Rules)
            rulesDictionary[rule.Tag] = rule;

        foreach (var patchDir in patchFolders)
        {
            await ApplyPatchesFromDirectory(context, patchDir, settings, rulesDictionary);
        }

        // Re-scan CombinedDefs for Name-bearing nodes that may have been added by patches.
        // This mirrors legacy DoPrePatch lines 165-172.
        foreach (var node in context.CombinedDefs.Root!.Elements())
        {
            var nameAttr = node.Attribute("Name")?.Value;
            if (nameAttr is not null)
            {
                context.ParentLookup[nameAttr] = node;
            }
        }
    }

    private async Task ApplyPatchesFromDirectory(
        ExtractionContext context,
        string patchDir,
        Domain.Settings.ExtractionSettings settings,
        Dictionary<string, Domain.Rules.ExtractionRule> rulesDictionary)
    {
        foreach (var filePath in _gateway.DescendantFiles(patchDir))
        {
            if (!filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;

            XDocument patchDoc;
            try
            {
                patchDoc = await _parser.ParseAsync(filePath, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse patch file {FilePath}", filePath);
                continue;
            }

            if (patchDoc.Root is null) continue;

            // Apply in prePatchMode=true: patches mutate CombinedDefs but emit no entries.
            var result = _patchProcessor.Apply(
                context.CombinedDefs,
                patchDoc.Root,
                rulesDictionary,
                settings.TranslationHandles,
                settings.EnableTkey,
                context.IsOfficialContent,
                initialRequiredMods: null,
                prePatchMode: true);

            // Collect defs added by patches (e.g. PatchOperationAdd targeting Defs root).
            context.DefsAddedByPatches.AddRange(result.DefsAddedByPatches);
        }
    }
}
