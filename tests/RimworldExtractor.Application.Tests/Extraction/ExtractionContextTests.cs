using FluentAssertions;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Application.Tests.Extraction;

public class ExtractionContextTests
{
    private static ExtractionRequest MakeRequest()
    {
        var mod = new ModMetadata("/mod", "id", "TestMod", "author.testmod", false);
        return new ExtractionRequest(
            Target: mod,
            SelectedFolders: Array.Empty<ExtractableFolder>(),
            ReferenceMods: null,
            Settings: AppSettings.Default);
    }

    [Fact]
    public void Constructor_StoresRequest()
    {
        var request = MakeRequest();
        var ctx = new ExtractionContext(request);

        ctx.Request.Should().BeSameAs(request);
    }

    [Fact]
    public void Results_StartsEmpty()
    {
        var ctx = new ExtractionContext(MakeRequest());

        ctx.Results.Should().BeEmpty();
        ctx.ParentLookup.Should().BeEmpty();
        ctx.DefsAddedByPatches.Should().BeEmpty();
    }

    [Fact]
    public void ReportProgress_ForwardsToIProgress()
    {
        var received = new List<ExtractionProgress>();
        var progress = new Progress<ExtractionProgress>(p => received.Add(p));

        var ctx = new ExtractionContext(MakeRequest(), progress);
        ctx.ReportProgress(0.5, "halfway");

        // Progress callbacks may be async; force synchronous by calling directly.
        // Use a synchronous IProgress<T> to avoid timing issues.
        var syncReceived = new List<ExtractionProgress>();
        IProgress<ExtractionProgress> syncProgress = new SynchronousProgress<ExtractionProgress>(p => syncReceived.Add(p));
        var ctx2 = new ExtractionContext(MakeRequest(), syncProgress);
        ctx2.ReportProgress(0.75, "three-quarters");

        syncReceived.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ExtractionProgress(0.75, "three-quarters"));
    }

    [Fact]
    public void ReplaceCombinedDefs_ReplacesCombinedDefs()
    {
        var ctx = new ExtractionContext(MakeRequest());
        var original = ctx.CombinedDefs;

        var newDoc = new System.Xml.Linq.XDocument(new System.Xml.Linq.XElement("Defs"));
        ctx.ReplaceCombinedDefs(newDoc);

        ctx.CombinedDefs.Should().BeSameAs(newDoc);
        ctx.CombinedDefs.Should().NotBeSameAs(original);
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _action;
        public SynchronousProgress(Action<T> action) => _action = action;
        public void Report(T value) => _action(value);
    }
}
