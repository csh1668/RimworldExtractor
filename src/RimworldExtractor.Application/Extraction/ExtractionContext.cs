using System.Xml.Linq;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Application.Extraction;

/// <summary>
/// Mutable state for one extraction run. Replaces legacy <c>Extractor.CombinedDefs</c>,
/// <c>Extractor.ParentNodeLookUp</c>, <c>Extractor._isOfficialContent</c>,
/// <c>PatchOperations.DefsAddedByPatches</c> — all were static globals before.
/// </summary>
public sealed class ExtractionContext
{
    public ExtractionRequest Request { get; }
    public IProgress<ExtractionProgress>? Progress { get; }
    public CancellationToken CancellationToken { get; }

    public XDocument CombinedDefs { get; private set; } = new(new XElement("Defs"));
    public Dictionary<string, XElement> ParentLookup { get; } = new(StringComparer.Ordinal);
    public List<TranslationEntry> Results { get; } = new();
    public List<XElement> DefsAddedByPatches { get; } = new();

    public bool IsOfficialContent => Request.Target.IsOfficialContent;

    public ExtractionContext(
        ExtractionRequest request,
        IProgress<ExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        Progress = progress;
        CancellationToken = cancellationToken;
    }

    public void ReplaceCombinedDefs(XDocument doc) => CombinedDefs = doc;

    public void ReportProgress(double percentage, string message)
        => Progress?.Report(new ExtractionProgress(percentage, message));
}
