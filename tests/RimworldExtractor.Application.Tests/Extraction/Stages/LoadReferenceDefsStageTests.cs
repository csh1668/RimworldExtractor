using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class LoadReferenceDefsStageTests
{
    private static readonly ModMetadata TargetMod =
        new("/target", "tid", "TargetMod", "author.target", false);

    private static ExtractionContext MakeContext(
        IReadOnlyList<ExtractableFolder> folders,
        IReadOnlyList<ModMetadata>? refMods = null)
    {
        var request = new ExtractionRequest(
            Target: TargetMod,
            SelectedFolders: folders,
            ReferenceMods: refMods,
            Settings: AppSettings.Default);
        return new ExtractionContext(request);
    }

    [Fact]
    public async Task ExecuteAsync_LoadsTargetDefsIntoContext()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Defs/things.xml", """
            <?xml version="1.0"?>
            <Defs>
                <ThingDef Name="BaseSword">
                    <defName>IronSword</defName>
                    <label>Iron sword</label>
                </ThingDef>
            </Defs>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var modLister = Substitute.For<IModLister>();
        var logger = NullLogger<LoadReferenceDefsStage>.Instance;

        var targetFolder = new ExtractableFolder(TargetMod, "Defs", null);
        var context = MakeContext(new[] { targetFolder });

        var stage = new LoadReferenceDefsStage(fs, parser, modLister, gateway, logger);
        await stage.ExecuteAsync(context);

        var defsRoot = context.CombinedDefs.Root!;
        defsRoot.Elements().Should().HaveCount(1);
        defsRoot.Elements().First().Name.LocalName.Should().Be("ThingDef");
        // Should NOT have Reference="True"
        defsRoot.Elements().First().Attribute("Reference").Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_LoadsReferenceDefsWithReferenceAttribute()
    {
        var refMod = new ModMetadata("/refmod", "rid", "RefMod", "author.ref", false);

        var fs = new InMemoryFileSystem();
        fs.AddFile("/refmod/Defs/base.xml", """
            <?xml version="1.0"?>
            <Defs>
                <ThingDef Name="BaseThing" Abstract="True">
                    <label>Base</label>
                </ThingDef>
            </Defs>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var modLister = Substitute.For<IModLister>();
        modLister.GetExtractableFolders(refMod).Returns(new[]
        {
            new ExtractableFolder(refMod, "Defs", null)
        });

        var logger = NullLogger<LoadReferenceDefsStage>.Instance;
        var context = MakeContext(Array.Empty<ExtractableFolder>(), refMods: new[] { refMod });

        var stage = new LoadReferenceDefsStage(fs, parser, modLister, gateway, logger);
        await stage.ExecuteAsync(context);

        var defsRoot = context.CombinedDefs.Root!;
        var refNode = defsRoot.Elements().Should().ContainSingle().Subject;
        refNode.Attribute("Reference")?.Value.Should().Be("True");
    }

    [Fact]
    public async Task ExecuteAsync_PopulatesParentLookupForNamedNodes()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Defs/things.xml", """
            <?xml version="1.0"?>
            <Defs>
                <ThingDef Name="AbstractBase" Abstract="True">
                    <label>Base</label>
                </ThingDef>
            </Defs>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var modLister = Substitute.For<IModLister>();
        var logger = NullLogger<LoadReferenceDefsStage>.Instance;

        var targetFolder = new ExtractableFolder(TargetMod, "Defs", null);
        var context = MakeContext(new[] { targetFolder });

        var stage = new LoadReferenceDefsStage(fs, parser, modLister, gateway, logger);
        await stage.ExecuteAsync(context);

        context.ParentLookup.Should().ContainKey("AbstractBase");
        context.ParentLookup["AbstractBase"].Name.LocalName.Should().Be("ThingDef");
    }

    [Fact]
    public async Task ExecuteAsync_SkipsNonDefsFolder()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Keyed/keys.xml", """
            <?xml version="1.0"?>
            <LanguageData>
                <HelloWorld>Hello</HelloWorld>
            </LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var modLister = Substitute.For<IModLister>();
        var logger = NullLogger<LoadReferenceDefsStage>.Instance;

        var keyedFolder = new ExtractableFolder(TargetMod, "Keyed", null);
        var context = MakeContext(new[] { keyedFolder });

        var stage = new LoadReferenceDefsStage(fs, parser, modLister, gateway, logger);
        await stage.ExecuteAsync(context);

        // Keyed folder ignored — CombinedDefs stays empty
        context.CombinedDefs.Root!.Elements().Should().BeEmpty();
    }
}
