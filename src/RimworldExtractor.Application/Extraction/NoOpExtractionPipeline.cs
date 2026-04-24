using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application.Extraction;

internal sealed class NoOpExtractionPipeline : IExtractionPipeline
{
    public Task<ExtractionResult> RunAsync(
        ExtractionRequest request,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ExtractionResult.Empty);
}
