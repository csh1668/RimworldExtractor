using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class ExtractKeyedStageTests
{
    private static readonly ModMetadata TargetMod =
        new("/target", "tid", "TargetMod", "author", false);

    private static ExtractionContext MakeContext(IReadOnlyList<ExtractableFolder> folders, bool isOfficial = false)
    {
        var mod = isOfficial
            ? new ModMetadata("/target", "tid", "TargetMod", "author", true)
            : TargetMod;
        var request = new ExtractionRequest(
            Target: mod,
            SelectedFolders: folders,
            ReferenceMods: null,
            Settings: AppSettings.Default);
        return new ExtractionContext(request);
    }

    [Fact]
    public async Task ExecuteAsync_EmitsTwoEntriesFromLanguageDataXml()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Keyed/Greetings.xml", """
            <?xml version="1.0"?>
            <LanguageData>
                <Hello>Hello!</Hello>
                <Goodbye>Goodbye!</Goodbye>
            </LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var folder = new ExtractableFolder(TargetMod, "Keyed", null);
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractKeyedStage(fs, gateway, parser, NullLogger<ExtractKeyedStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().HaveCount(2);
        ctx.Results[0].ClassName.Should().Be("Keyed");
        ctx.Results[0].Node.Should().Be("Hello");
        ctx.Results[0].Original.Should().Be("Hello!");
        ctx.Results[1].Node.Should().Be("Goodbye");
        ctx.Results[1].Original.Should().Be("Goodbye!");
    }

    [Fact]
    public async Task ExecuteAsync_MatchesFoldersEndingWithKeyed()
    {
        // "Languages/English/Keyed" should also be matched (ends with "Keyed").
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Languages/English/Keyed/UI.xml", """
            <?xml version="1.0"?>
            <LanguageData>
                <Accept>Accept</Accept>
            </LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var folder = new ExtractableFolder(TargetMod, "Languages/English/Keyed", null);
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractKeyedStage(fs, gateway, parser, NullLogger<ExtractKeyedStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        ctx.Results[0].Node.Should().Be("Accept");
        ctx.Results[0].Original.Should().Be("Accept");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotSetSourceFileForNonOfficialContent()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Keyed/Keys.xml", """
            <?xml version="1.0"?>
            <LanguageData>
                <MyKey>Value</MyKey>
            </LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var folder = new ExtractableFolder(TargetMod, "Keyed", null);
        var ctx = MakeContext(new[] { folder }, isOfficial: false);

        var stage = new ExtractKeyedStage(fs, gateway, parser, NullLogger<ExtractKeyedStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        ctx.Results[0].SourceFile.Should().BeNull("non-official content has no source file");
    }

    [Fact]
    public async Task ExecuteAsync_SetsSourceFileForOfficialContent()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Keyed/Keys.xml", """
            <?xml version="1.0"?>
            <LanguageData>
                <MyKey>Value</MyKey>
            </LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var officialMod = new ModMetadata("/target", "core", "Core", "Ludeon", true);
        var folder = new ExtractableFolder(officialMod, "Keyed", null);
        var request = new ExtractionRequest(
            Target: officialMod,
            SelectedFolders: new[] { folder },
            ReferenceMods: null,
            Settings: AppSettings.Default);
        var ctx = new ExtractionContext(request);

        var stage = new ExtractKeyedStage(fs, gateway, parser, NullLogger<ExtractKeyedStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        ctx.Results[0].SourceFile.Should().Be("Keys");
    }

    [Fact]
    public async Task ExecuteAsync_SetsRequiredModsFromFolder()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Keyed/Keys.xml", """
            <?xml version="1.0"?>
            <LanguageData>
                <MyKey>Value</MyKey>
            </LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var folder = new ExtractableFolder(TargetMod, "Keyed", "mod.author.dep");
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractKeyedStage(fs, gateway, parser, NullLogger<ExtractKeyedStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        var reqMods = ctx.Results[0].RequiredMods;
        reqMods.Should().NotBeNull();
        reqMods!.Allowed.Should().HaveCount(1);
        reqMods.Allowed[0].Should().ContainSingle(r => r.Value == "mod.author.dep");
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresFolderNotEndingWithKeyed()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Defs/things.xml", """
            <?xml version="1.0"?>
            <Defs>
                <ThingDef><defName>Foo</defName></ThingDef>
            </Defs>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var folder = new ExtractableFolder(TargetMod, "Defs", null);
        var ctx = MakeContext(new[] { folder });

        var stage = new ExtractKeyedStage(fs, gateway, parser, NullLogger<ExtractKeyedStage>.Instance);
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().BeEmpty("Defs folder is not a Keyed folder");
    }
}
