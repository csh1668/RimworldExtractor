using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Domain.Abstractions;

/// <summary>
/// Inputs to a single extraction run.
/// </summary>
public sealed record ExtractionRequest(
    ModMetadata Target,
    IReadOnlyList<ExtractableFolder> SelectedFolders,
    IReadOnlyList<ModMetadata>? ReferenceMods,
    AppSettings Settings);

/// <summary>
/// Outputs of a single extraction run.
/// </summary>
public sealed record ExtractionResult(IReadOnlyList<TranslationEntry> Entries)
{
    public static ExtractionResult Empty { get; } = new(Array.Empty<TranslationEntry>());
}

/// <summary>
/// Progress update emitted by the pipeline during a run.
/// </summary>
public sealed record ExtractionProgress(double Percentage, string Message);

/// <summary>
/// Runs an extraction pipeline end-to-end. Phase 1 ships a no-op implementation;
/// real stages arrive in Phase 4.
/// </summary>
public interface IExtractionPipeline
{
    Task<ExtractionResult> RunAsync(
        ExtractionRequest request,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
