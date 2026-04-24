using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 5: For each selected folder whose name ends with <c>Keyed</c>, enumerate all
/// <c>.xml</c> files, parse <c>&lt;LanguageData&gt;</c> children, and emit one
/// <see cref="TranslationEntry"/> per child.
/// Ports <c>legacy/Extractor.cs:232–257 ExtractKeyed</c>.
/// </summary>
/// <remarks>
/// The filter uses <c>EndsWith("Keyed")</c> (ordinal) so it matches both bare
/// <c>Keyed</c> folders and versioned paths like <c>Languages/English/Keyed</c>
/// or <c>1.6/Keyed</c>.
/// </remarks>
public sealed class ExtractKeyedStage : IExtractionStage
{
    private readonly IFileSystem _fileSystem;
    private readonly FileSystemGateway _gateway;
    private readonly IXmlDefParser _parser;
    private readonly ILogger<ExtractKeyedStage> _logger;

    public string Name => "Extract Keyed";

    public ExtractKeyedStage(
        IFileSystem fileSystem,
        FileSystemGateway gateway,
        IXmlDefParser parser,
        ILogger<ExtractKeyedStage> logger)
    {
        _fileSystem = fileSystem;
        _gateway = gateway;
        _parser = parser;
        _logger = logger;
    }

    public async Task ExecuteAsync(ExtractionContext context)
    {
        var isOfficialContent = context.IsOfficialContent;

        foreach (var folder in context.Request.SelectedFolders
                     .Where(f => f.FolderName.EndsWith("Keyed", StringComparison.Ordinal)))
        {
            var requiredMods = BuildRequiredMods(folder.RequiredPackageId);
            var folderRoot = folder.FullPath;

            foreach (var filePath in _gateway.DescendantFiles(folderRoot))
            {
                if (!filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;

                XDocument doc;
                try
                {
                    doc = await _parser.ParseAsync(filePath, context.CancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Keyed file {FilePath}", filePath);
                    continue;
                }

                if (doc.Root is null) continue;

                var fileName = isOfficialContent
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : null;

                foreach (var node in doc.Root.Elements())
                {
                    context.Results.Add(new TranslationEntry(
                        ClassName: "Keyed",
                        Node: node.Name.LocalName,
                        Original: node.Value,
                        Translated: null,
                        RequiredMods: requiredMods,
                        SourceFile: fileName));
                }
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
