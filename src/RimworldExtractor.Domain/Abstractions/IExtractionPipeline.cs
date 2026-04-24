namespace RimworldExtractor.Domain.Abstractions;

/// <summary>
/// Runs an extraction pipeline end-to-end. Phase 1 ships a no-op implementation;
/// real stages arrive in Phase 4.
/// </summary>
public interface IExtractionPipeline
{
    Task<ExtractionResult> RunAsync(ExtractionRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Inputs to a single extraction run.
/// </summary>
/// <param name="ModPath">Absolute path to the mod root directory.</param>
public sealed record ExtractionRequest(string ModPath);

/// <summary>
/// Outputs of a single extraction run. Phase 4 will enrich this shape; Phase 1 keeps a minimal record so the DI smoke test can complete.
/// </summary>
public sealed record ExtractionResult(IReadOnlyList<string> Messages)
{
    public static ExtractionResult Empty { get; } = new(Array.Empty<string>());
}
