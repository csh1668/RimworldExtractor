using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class ExtractStringsStageTests
{
    private static readonly ModMetadata TargetMod =
        new("/target", "tid", "TargetMod", "author", false);

    private static ExtractionContext MakeContext(IReadOnlyList<ExtractableFolder> folders)
    {
        var request = new ExtractionRequest(
            Target: TargetMod,
            SelectedFolders: folders,
            ReferenceMods: null,
            Settings: AppSettings.Default);
        return new ExtractionContext(request);
    }

    [Fact]
    public async Task ExecuteAsync_EmitsOneEntryPerLine()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Strings/Names/Last.txt", "Smith\nJones\nWilliams");

        var gateway = new FileSystemGateway(fs);
        var folder = new ExtractableFolder(TargetMod, "Strings", null);
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractStringsStage(fs, gateway, NullLogger<ExtractStringsStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().HaveCount(3);
        ctx.Results[0].ClassName.Should().Be("Strings");
        ctx.Results[0].Node.Should().Be("Names.Last.0");
        ctx.Results[0].Original.Should().Be("Smith");
        ctx.Results[1].Node.Should().Be("Names.Last.1");
        ctx.Results[1].Original.Should().Be("Jones");
        ctx.Results[2].Node.Should().Be("Names.Last.2");
        ctx.Results[2].Original.Should().Be("Williams");
    }

    [Fact]
    public async Task ExecuteAsync_NodeName_StripsExtensionAndReplacesSeparators()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Strings/First/Names.txt", "Alice\nBob");

        var gateway = new FileSystemGateway(fs);
        var folder = new ExtractableFolder(TargetMod, "Strings", null);
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractStringsStage(fs, gateway, NullLogger<ExtractStringsStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().HaveCount(2);
        // Relative path from Strings root: "First/Names.txt"
        // Dots: "First.Names.txt" → strip ext → "First.Names"
        ctx.Results[0].Node.Should().Be("First.Names.0");
        ctx.Results[1].Node.Should().Be("First.Names.1");
    }

    [Fact]
    public async Task ExecuteAsync_SourceFileIsAlwaysNull()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Strings/Words.txt", "hello");

        var gateway = new FileSystemGateway(fs);
        var folder = new ExtractableFolder(TargetMod, "Strings", null);
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractStringsStage(fs, gateway, NullLogger<ExtractStringsStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        ctx.Results[0].SourceFile.Should().BeNull("Strings entries never have a SourceFile");
    }

    [Fact]
    public async Task ExecuteAsync_SetsRequiredModsFromFolder()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Strings/Words.txt", "hello");

        var gateway = new FileSystemGateway(fs);
        var folder = new ExtractableFolder(TargetMod, "Strings", "mod.author.dep");
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractStringsStage(fs, gateway, NullLogger<ExtractStringsStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        var reqMods = ctx.Results[0].RequiredMods;
        reqMods.Should().NotBeNull();
        reqMods!.Allowed.Should().HaveCount(1);
        reqMods.Allowed[0].Should().ContainSingle(r => r.Value == "mod.author.dep");
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresFolderNotEndingWithStrings()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Keyed/Words.xml", """
            <?xml version="1.0"?>
            <LanguageData><MyKey>Value</MyKey></LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var folder = new ExtractableFolder(TargetMod, "Keyed", null);
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractStringsStage(fs, gateway, NullLogger<ExtractStringsStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().BeEmpty("Keyed folder is not a Strings folder");
    }
}
