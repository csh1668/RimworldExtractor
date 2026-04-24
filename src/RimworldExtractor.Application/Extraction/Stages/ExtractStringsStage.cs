using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 6: For each selected folder whose name ends with <c>Strings</c>, enumerate all
/// <c>.txt</c> files, read each line, and emit one <see cref="TranslationEntry"/> per line.
/// The <c>Node</c> is the relative path within the Strings folder (with <c>/</c> and <c>\</c>
/// replaced by <c>.</c>, extension stripped) suffixed with the line index.
/// Ports <c>legacy/Extractor.cs:259–284 ExtractStrings</c>.
/// </summary>
public sealed class ExtractStringsStage : IExtractionStage
{
    private readonly IFileSystem _fileSystem;
    private readonly FileSystemGateway _gateway;
    private readonly ILogger<ExtractStringsStage> _logger;

    public string Name => "Extract Strings";

    public ExtractStringsStage(
        IFileSystem fileSystem,
        FileSystemGateway gateway,
        ILogger<ExtractStringsStage> logger)
    {
        _fileSystem = fileSystem;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task ExecuteAsync(ExtractionContext context)
    {
        foreach (var folder in context.Request.SelectedFolders
                     .Where(f => f.FolderName.EndsWith("Strings", StringComparison.Ordinal)))
        {
            var requiredMods = BuildRequiredMods(folder.RequiredPackageId);
            var stringsRoot = folder.FullPath;

            foreach (var filePath in _gateway.DescendantFiles(stringsRoot))
            {
                if (!filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) continue;

                string[] lines;
                try
                {
                    lines = await _fileSystem.ReadAllLinesAsync(filePath, context.CancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read Strings file {FilePath}", filePath);
                    continue;
                }

                // Compute nodeName: relative path from stringsRoot, path separators → dots, strip extension.
                var relativePath = Path.GetRelativePath(stringsRoot, filePath);
                // Replace both Windows and Unix separators with dots, then strip the .txt extension.
                var nodeName = Path.GetFileNameWithoutExtension(
                    relativePath.Replace('\\', '.').Replace('/', '.'));

                for (var i = 0; i < lines.Length; i++)
                {
                    context.Results.Add(new TranslationEntry(
                        ClassName: "Strings",
                        Node: $"{nodeName}.{i}",
                        Original: lines[i],
                        Translated: null,
                        RequiredMods: requiredMods,
                        SourceFile: null));
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
