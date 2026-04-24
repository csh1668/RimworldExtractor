using System.Globalization;
using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 4: Walk every non-reference Def in <see cref="ExtractionContext.CombinedDefs"/>,
/// call <see cref="IXmlDefExtractor.Extract"/> on each, and add resulting
/// <see cref="TranslationEntry"/> values to <see cref="ExtractionContext.Results"/>.
/// Ports <c>legacy/Extractor.cs:190–230 ExtractDefsInternal</c>.
/// </summary>
public sealed class ExtractDefsStage : IExtractionStage
{
    private readonly IXmlDefExtractor _defExtractor;
    private readonly ILogger<ExtractDefsStage> _logger;

    public string Name => "Extract Defs";

    public ExtractDefsStage(IXmlDefExtractor defExtractor, ILogger<ExtractDefsStage> logger)
    {
        _defExtractor = defExtractor;
        _logger = logger;
    }

    public Task ExecuteAsync(ExtractionContext context)
    {
        var settings = context.Request.Settings.Extraction;
        var rules = settings.Rules.ToDictionary(r => r.Tag, r => r);
        var handles = settings.TranslationHandles;
        var enableTKey = settings.EnableTkey;

        foreach (var node in context.CombinedDefs.Root!.Elements()
                     .Where(n => !string.Equals(n.Attribute("Reference")?.Value, "true", StringComparison.OrdinalIgnoreCase)))
        {
            var defName = node.Element("defName")?.Value;
            if (defName is null)
            {
                if (node.Name.LocalName != "SongDef")
                    _logger.LogWarning("Element {Name} has no defName", node.Name.LocalName);
                continue;
            }

            // Build RequiredMods from RequiredPackageId attribute on the root node.
            RequiredMods? rootMods = null;
            var reqPid = node.Attribute("RequiredPackageId")?.Value;
            if (!string.IsNullOrEmpty(reqPid))
            {
                var refs = reqPid.Split(',').Select(p => ModReference.ByPackageId(p.Trim())).ToArray();
                rootMods = new RequiredMods(
                    allowed: new[] { (IEnumerable<ModReference>)refs },
                    disallowed: Array.Empty<IEnumerable<ModReference>>());
            }

            var className = node.Attribute("Class")?.Value ?? node.Name.LocalName;
            className = char.ToUpper(className[0], CultureInfo.InvariantCulture) + className[1..];

            foreach (var entry in _defExtractor.Extract(
                defName, className, node, rules, handles, enableTKey, context.IsOfficialContent))
            {
                var combined = rootMods is null
                    ? entry.RequiredMods
                    : entry.RequiredMods?.Combine(rootMods) ?? rootMods;
                context.Results.Add(entry with { RequiredMods = combined });
            }
        }

        return Task.CompletedTask;
    }
}
