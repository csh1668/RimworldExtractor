using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application.Extraction;

internal sealed class NoOpExtractionPipeline : IExtractionPipeline
{
    public Task<ExtractionResult> RunAsync(ExtractionRequest request, CancellationToken cancellationToken)
        => Task.FromResult(ExtractionResult.Empty);
}
