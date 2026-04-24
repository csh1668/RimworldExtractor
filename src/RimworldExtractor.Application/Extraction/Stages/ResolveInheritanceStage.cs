using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Extraction.Stages;

/// <summary>
/// Stage 3: Resolve XML inheritance (Name/ParentName chains). Thin wrapper over
/// <see cref="IXmlInheritanceResolver"/>. Replaces <see cref="ExtractionContext.CombinedDefs"/>
/// with the resolved document. Abstract Defs are dropped; non-abstract Defs inherit parent fields.
/// Ports <c>legacy/Extractor.DefsUtils.cs:284–371 DoXmlInheritance</c> (already ported in Phase 3).
/// </summary>
public sealed class ResolveInheritanceStage : IExtractionStage
{
    private readonly IXmlInheritanceResolver _resolver;

    public string Name => "Resolve inheritance";

    public ResolveInheritanceStage(IXmlInheritanceResolver resolver)
    {
        _resolver = resolver;
    }

    public Task ExecuteAsync(ExtractionContext context)
    {
        var resolved = _resolver.Resolve(context.CombinedDefs);
        context.ReplaceCombinedDefs(resolved);
        return Task.CompletedTask;
    }
}
