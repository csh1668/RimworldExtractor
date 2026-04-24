using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application.Extraction;

public sealed class ExtractionPipeline : IExtractionPipeline
{
    private readonly IExtractionStage[] _stages;

    public ExtractionPipeline(IEnumerable<IExtractionStage> stages)
    {
        _stages = stages.ToArray();
    }

    public async Task<ExtractionResult> RunAsync(
        ExtractionRequest request,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var context = new ExtractionContext(request, progress, cancellationToken);
        var total = _stages.Length;

        for (var i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stage = _stages[i];
            context.ReportProgress(
                percentage: (double)i / total,
                message: $"Stage {i + 1}/{total}: {stage.Name}");
            await stage.ExecuteAsync(context);
        }

        context.ReportProgress(1.0, "Complete");
        return new ExtractionResult(context.Results.ToArray());
    }
}
