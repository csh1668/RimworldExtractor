using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 1: Load reference mod Defs (with <c>Reference="True"</c>) and the target mod's own
/// Defs into <see cref="ExtractionContext.CombinedDefs"/>. Populates
/// <see cref="ExtractionContext.ParentLookup"/> with all Name-bearing nodes.
/// Ports <c>legacy/Extractor.DefsUtils.cs:LoadReferenceDefs</c> +
/// <c>legacy/Extractor.cs:PrepareDefs</c> (lines 111–148).
/// </summary>
public sealed class LoadReferenceDefsStage : IExtractionStage
{
    private readonly IFileSystem _fileSystem;
    private readonly IXmlDefParser _parser;
    private readonly IModLister _modLister;
    private readonly FileSystemGateway _gateway;
    private readonly ILogger<LoadReferenceDefsStage> _logger;

    public string Name => "Load reference defs";

    public LoadReferenceDefsStage(
        IFileSystem fileSystem,
        IXmlDefParser parser,
        IModLister modLister,
        FileSystemGateway gateway,
        ILogger<LoadReferenceDefsStage> logger)
    {
        _fileSystem = fileSystem;
        _parser = parser;
        _modLister = modLister;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task ExecuteAsync(ExtractionContext context)
    {
        // 1. Load reference mods' Defs (with Reference="True")
        if (context.Request.ReferenceMods is { Count: > 0 } refMods)
        {
            foreach (var refMod in refMods)
            {
                var refFolders = _modLister.GetExtractableFolders(refMod);
                foreach (var folder in refFolders)
                {
                    var folderBaseName = Path.GetFileName(folder.FolderName);
                    if (folderBaseName != "Defs") continue;

                    await LoadDefsFromFolder(
                        context,
                        folder.FullPath,
                        requiredPackageId: null,
                        isReference: true,
                        addSourceFile: false);
                }
            }
        }

        // 2. Load the target mod's own Defs (without Reference="True")
        foreach (var folder in context.Request.SelectedFolders)
        {
            var folderBaseName = Path.GetFileName(folder.FolderName);
            if (folderBaseName != "Defs") continue;

            await LoadDefsFromFolder(
                context,
                folder.FullPath,
                requiredPackageId: folder.RequiredPackageId,
                isReference: false,
                addSourceFile: context.IsOfficialContent);
        }
    }

    private async Task LoadDefsFromFolder(
        ExtractionContext context,
        string folderRoot,
        string? requiredPackageId,
        bool isReference,
        bool addSourceFile)
    {
        foreach (var filePath in _gateway.DescendantFiles(folderRoot))
        {
            if (!filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;

            XDocument childDoc;
            try
            {
                childDoc = await _parser.ParseAsync(filePath, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse {FilePath}", filePath);
                continue;
            }

            if (childDoc.Root is null) continue;

            var defsRoot = context.CombinedDefs.Root!;
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            foreach (var node in childDoc.Root.Elements())
            {
                var imported = new XElement(node);

                if (isReference)
                {
                    imported.SetAttributeValue("Reference", "True");
                }

                if (requiredPackageId is not null)
                {
                    imported.SetAttributeValue("RequiredPackageId", requiredPackageId);
                }

                if (addSourceFile)
                {
                    imported.SetAttributeValue("SourceFile", fileName);
                }

                defsRoot.Add(imported);

                var nameAttr = imported.Attribute("Name")?.Value;
                if (nameAttr is not null)
                {
                    if (context.ParentLookup.ContainsKey(nameAttr))
                    {
                        _logger.LogWarning("Duplicate parent Name={Name}; overwriting with later node", nameAttr);
                    }

                    context.ParentLookup[nameAttr] = imported;
                }
            }
        }
    }
}
