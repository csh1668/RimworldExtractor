using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class ExtractPatchesStageTests
{
    private static readonly ModMetadata TargetMod =
        new("/target", "tid", "TargetMod", "author", false);

    private static ExtractionContext MakeContext(
        IReadOnlyList<ExtractableFolder> folders,
        XDocument? initialDefs = null,
        AppSettings? settings = null)
    {
        var request = new ExtractionRequest(
            Target: TargetMod,
            SelectedFolders: folders,
            ReferenceMods: null,
            Settings: settings ?? AppSettings.Default);
        var ctx = new ExtractionContext(request);
        if (initialDefs is not null)
            ctx.ReplaceCombinedDefs(initialDefs);
        return ctx;
    }

    private static AppSettings SettingsWithRules(params string[] tags)
    {
        var rules = tags.Select(t => new ExtractionRule(t)).ToArray();
        var extraction = ExtractionSettings.Default with { Rules = rules };
        return AppSettings.Default with { Extraction = extraction };
    }

    private static ExtractPatchesStage MakeStage()
    {
        var xmlDefExtractor = new XmlDefExtractor();
        var patchLogger = NullLogger<XPatchProcessor>.Instance;
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, patchLogger);
        var inheritanceResolver = new XmlInheritanceResolver();
        return new ExtractPatchesStage(
            patchProcessor,
            new XDocumentDefParser(new InMemoryFileSystem()),
            xmlDefExtractor,
            inheritanceResolver,
            new FileSystemGateway(new InMemoryFileSystem()),
            NullLogger<ExtractPatchesStage>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_PatchOperationReplace_EmitsEntry()
    {
        // Arrange: CombinedDefs has ThingDef with label="old label"; patch replaces it.
        var combinedDefs = XDocument.Parse("""
            <?xml version="1.0"?>
            <Defs>
                <ThingDef>
                    <defName>WoodSpear</defName>
                    <label>old label</label>
                </ThingDef>
            </Defs>
            """);

        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Patches/Patch_WoodSpear.xml", """
            <?xml version="1.0"?>
            <Patch>
                <Operation Class="PatchOperationReplace">
                    <xpath>/Defs/ThingDef[defName="WoodSpear"]/label</xpath>
                    <value><label>new label</label></value>
                </Operation>
            </Patch>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var xmlDefExtractor = new XmlDefExtractor();
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, NullLogger<XPatchProcessor>.Instance);
        var inheritanceResolver = new XmlInheritanceResolver();

        var folder = new ExtractableFolder(TargetMod, "Patches", null);
        var ctx = MakeContext(new[] { folder }, combinedDefs, SettingsWithRules("label"));

        var stage = new ExtractPatchesStage(
            patchProcessor, parser, xmlDefExtractor, inheritanceResolver,
            gateway, NullLogger<ExtractPatchesStage>.Instance);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        var entry = ctx.Results[0];
        entry.ClassName.Should().Be("Patches.ThingDef");
        entry.Node.Should().Be("WoodSpear.label");
        entry.Original.Should().Be("new label");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyPatchFolder_ProducesNoResults()
    {
        var combinedDefs = XDocument.Parse("<Defs/>");
        var fs = new InMemoryFileSystem();
        fs.CreateDirectory("/target/Patches");

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var xmlDefExtractor = new XmlDefExtractor();
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, NullLogger<XPatchProcessor>.Instance);
        var inheritanceResolver = new XmlInheritanceResolver();

        var folder = new ExtractableFolder(TargetMod, "Patches", null);
        var ctx = MakeContext(new[] { folder }, combinedDefs);

        var stage = new ExtractPatchesStage(
            patchProcessor, parser, xmlDefExtractor, inheritanceResolver,
            gateway, NullLogger<ExtractPatchesStage>.Instance);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresFolderNotEndingWithPatches()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Keyed/Keys.xml", """
            <?xml version="1.0"?>
            <LanguageData><Key>Value</Key></LanguageData>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var xmlDefExtractor = new XmlDefExtractor();
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, NullLogger<XPatchProcessor>.Instance);
        var inheritanceResolver = new XmlInheritanceResolver();

        var folder = new ExtractableFolder(TargetMod, "Keyed", null);
        var ctx = MakeContext(new[] { folder }, XDocument.Parse("<Defs/>"));

        var stage = new ExtractPatchesStage(
            patchProcessor, parser, xmlDefExtractor, inheritanceResolver,
            gateway, NullLogger<ExtractPatchesStage>.Instance);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().BeEmpty("Keyed folder should not be processed by ExtractPatchesStage");
    }

    [Fact]
    public async Task ExecuteAsync_PatchOperationAdd_ToDefsRoot_ExtractsWithPatchesPrefix()
    {
        // PatchOperationAdd targeting Defs root adds a new def. The stage should
        // run post-patch extraction and emit entries with "Patches.<ClassName>" prefix.
        var combinedDefs = XDocument.Parse("<Defs/>");

        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Patches/AddDef.xml", """
            <?xml version="1.0"?>
            <Patch>
                <Operation Class="PatchOperationAdd">
                    <xpath>Defs</xpath>
                    <value>
                        <ThingDef>
                            <defName>NewDef</defName>
                            <label>new def label</label>
                        </ThingDef>
                    </value>
                </Operation>
            </Patch>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var xmlDefExtractor = new XmlDefExtractor();
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, NullLogger<XPatchProcessor>.Instance);
        var inheritanceResolver = new XmlInheritanceResolver();

        var folder = new ExtractableFolder(TargetMod, "Patches", null);
        var ctx = MakeContext(new[] { folder }, combinedDefs, SettingsWithRules("label"));

        var stage = new ExtractPatchesStage(
            patchProcessor, parser, xmlDefExtractor, inheritanceResolver,
            gateway, NullLogger<ExtractPatchesStage>.Instance);

        await stage.ExecuteAsync(ctx);

        // Should have entry from post-patch extraction with "Patches.ThingDef" class
        ctx.Results.Should().Contain(e =>
            e.ClassName == "Patches.ThingDef" &&
            e.Node == "NewDef.label" &&
            e.Original == "new def label");
    }
}
