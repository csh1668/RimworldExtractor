using FluentAssertions;
using NSubstitute;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Application.Tests.Extraction;

public class ExtractionPipelineTests
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
    public async Task RunAsync_InvokesBothStagesInOrder()
    {
        var callOrder = new List<string>();

        var stage1 = Substitute.For<IExtractionStage>();
        stage1.Name.Returns("Stage1");
        stage1.ExecuteAsync(Arg.Any<ExtractionContext>())
            .Returns(callInfo =>
            {
                callOrder.Add("Stage1");
                return Task.CompletedTask;
            });

        var stage2 = Substitute.For<IExtractionStage>();
        stage2.Name.Returns("Stage2");
        stage2.ExecuteAsync(Arg.Any<ExtractionContext>())
            .Returns(callInfo =>
            {
                callOrder.Add("Stage2");
                return Task.CompletedTask;
            });

        var pipeline = new ExtractionPipeline(new[] { stage1, stage2 });
        await pipeline.RunAsync(MakeRequest(), cancellationToken: TestContext.Current.CancellationToken);

        callOrder.Should().Equal("Stage1", "Stage2");
    }

    [Fact]
    public async Task RunAsync_ReportsProgressForEachStage()
    {
        var progressReports = new List<ExtractionProgress>();
        IProgress<ExtractionProgress> progress = new SynchronousProgress<ExtractionProgress>(p => progressReports.Add(p));

        var stage1 = Substitute.For<IExtractionStage>();
        stage1.Name.Returns("Alpha");
        stage1.ExecuteAsync(Arg.Any<ExtractionContext>()).Returns(Task.CompletedTask);

        var stage2 = Substitute.For<IExtractionStage>();
        stage2.Name.Returns("Beta");
        stage2.ExecuteAsync(Arg.Any<ExtractionContext>()).Returns(Task.CompletedTask);

        var pipeline = new ExtractionPipeline(new[] { stage1, stage2 });
        await pipeline.RunAsync(MakeRequest(), progress, TestContext.Current.CancellationToken);

        // 2 stages + 1 final "Complete" = 3 progress reports
        progressReports.Should().HaveCount(3);
        progressReports[0].Message.Should().Contain("Alpha");
        progressReports[1].Message.Should().Contain("Beta");
        progressReports[2].Message.Should().Be("Complete");
        progressReports[2].Percentage.Should().Be(1.0);
    }

    [Fact]
    public async Task RunAsync_ReturnsResultBuiltFromContextResults()
    {
        var entry = new TranslationEntry("ThingDef", "Sword.label", "Sword", null, null, null);

        var stage = Substitute.For<IExtractionStage>();
        stage.Name.Returns("Extract");
        stage.ExecuteAsync(Arg.Do<ExtractionContext>(ctx => ctx.Results.Add(entry)))
            .Returns(Task.CompletedTask);

        var pipeline = new ExtractionPipeline(new[] { stage });
        var result = await pipeline.RunAsync(MakeRequest(), cancellationToken: TestContext.Current.CancellationToken);

        result.Entries.Should().ContainSingle().Which.Should().Be(entry);
    }

    [Fact]
    public async Task RunAsync_ThrowsOnCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var stage = Substitute.For<IExtractionStage>();
        stage.Name.Returns("ShouldNotRun");

        var pipeline = new ExtractionPipeline(new[] { stage });

        var act = async () => await pipeline.RunAsync(MakeRequest(), cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _action;
        public SynchronousProgress(Action<T> action) => _action = action;
        public void Report(T value) => _action(value);
    }
}
