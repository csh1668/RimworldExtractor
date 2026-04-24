using RimworldExtractor.Application.Compat;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 8 (post-extraction): chains all compat plugins' <c>PostProcess</c> methods
/// sequentially over <see cref="ExtractionContext.Results"/>, then deduplicates by
/// <c>ClassName+Node</c> key (mirroring legacy <c>ExtractTranslationData</c> DistinctBy).
/// Ports legacy <c>CompatManager.DoPostProcessing</c>.
/// </summary>
public sealed class CompatPostProcessStage : IExtractionStage
{
    private readonly CompatRegistry _registry;

    public string Name => "Compat PostProcess";

    public CompatPostProcessStage(CompatRegistry registry)
    {
        _registry = registry;
    }

    public Task ExecuteAsync(ExtractionContext context)
    {
        IEnumerable<TranslationEntry> current = context.Results;
        foreach (var plugin in _registry.Ordered)
        {
            current = plugin.PostProcess(current).ToList(); // materialize to avoid multi-pass over plugin
        }

        var final = current.DistinctBy(e => $"{e.ClassName}+{e.Node}").ToList();
        context.Results.Clear();
        context.Results.AddRange(final);
        return Task.CompletedTask;
    }
}
