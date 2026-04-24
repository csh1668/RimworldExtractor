using RimworldExtractor.Application.Compat;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 3 (pre-extraction): iterates all registered compat plugins in priority order
/// and calls <c>PreProcess(context.CombinedDefs)</c> on each.
/// Ports legacy <c>CompatManager.DoPreProcessing</c>.
/// </summary>
public sealed class CompatPreProcessStage : IExtractionStage
{
    private readonly CompatRegistry _registry;

    public string Name => "Compat PreProcess";

    public CompatPreProcessStage(CompatRegistry registry)
    {
        _registry = registry;
    }

    public Task ExecuteAsync(ExtractionContext context)
    {
        foreach (var plugin in _registry.Ordered)
        {
            plugin.PreProcess(context.CombinedDefs);
        }

        return Task.CompletedTask;
    }
}
