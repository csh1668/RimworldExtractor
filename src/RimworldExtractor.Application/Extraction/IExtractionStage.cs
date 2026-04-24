namespace RimworldExtractor.Application.Extraction;

/// <summary>
/// One stage of the extraction pipeline. Stages run in fixed order; each may mutate
/// <see cref="ExtractionContext"/>. Implementations should be stateless (injected dependencies only)
/// so a single instance can be reused across runs.
/// </summary>
public interface IExtractionStage
{
    /// <summary>Short, stable identifier used in progress messages and logs.</summary>
    string Name { get; }

    Task ExecuteAsync(ExtractionContext context);
}
