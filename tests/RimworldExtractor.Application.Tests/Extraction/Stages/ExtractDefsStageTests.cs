using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class ExtractDefsStageTests
{
    private static readonly ModMetadata TargetMod =
        new("/target", "tid", "TargetMod", "author", false);

    private static ExtractionContext MakeContext(XDocument combinedDefs, AppSettings? settings = null)
    {
        var request = new ExtractionRequest(
            Target: TargetMod,
            SelectedFolders: Array.Empty<ExtractableFolder>(),
            ReferenceMods: null,
            Settings: settings ?? AppSettings.Default);
        var ctx = new ExtractionContext(request);
        ctx.ReplaceCombinedDefs(combinedDefs);
        return ctx;
    }

    private static AppSettings SettingsWithRules(params string[] tags)
    {
        var rules = tags.Select(t => new ExtractionRule(t)).ToArray();
        var extraction = ExtractionSettings.Default with { Rules = rules };
        return AppSettings.Default with { Extraction = extraction };
    }

    [Fact]
    public async Task ExecuteAsync_ExtractsLabelFromThingDef()
    {
        var combinedDefs = XDocument.Parse("""
            <?xml version="1.0"?>
            <Defs>
                <ThingDef>
                    <defName>WoodSpear</defName>
                    <label>wooden spear</label>
                </ThingDef>
            </Defs>
            """);

        var ctx = MakeContext(combinedDefs, SettingsWithRules("label"));
        var extractor = new XmlDefExtractor();
        var stage = new ExtractDefsStage(extractor, NullLogger<ExtractDefsStage>.Instance);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        var entry = ctx.Results[0];
        entry.ClassName.Should().Be("ThingDef");
        entry.Node.Should().Be("WoodSpear.label");
        entry.Original.Should().Be("wooden spear");
    }

    [Fact]
    public async Task ExecuteAsync_SkipsNodesWithReferenceTrue()
    {
        var combinedDefs = XDocument.Parse("""
            <?xml version="1.0"?>
            <Defs>
                <ThingDef Reference="True">
                    <defName>RefDef</defName>
                    <label>reference def</label>
                </ThingDef>
                <ThingDef>
                    <defName>RealDef</defName>
                    <label>real def</label>
                </ThingDef>
            </Defs>
            """);

        var ctx = MakeContext(combinedDefs, SettingsWithRules("label"));
        var extractor = new XmlDefExtractor();
        var stage = new ExtractDefsStage(extractor, NullLogger<ExtractDefsStage>.Instance);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        ctx.Results[0].Node.Should().Be("RealDef.label");
    }

    [Fact]
    public async Task ExecuteAsync_SkipsNodesWithNoDefName_LogsWarnForNonSongDef()
    {
        var combinedDefs = XDocument.Parse("""
            <?xml version="1.0"?>
            <Defs>
                <ThingDef>
                    <label>no defname</label>
                </ThingDef>
                <SongDef>
                    <label>song with no defname</label>
                </SongDef>
            </Defs>
            """);

        var ctx = MakeContext(combinedDefs, SettingsWithRules("label"));
        var extractor = new XmlDefExtractor();
        var stage = new ExtractDefsStage(extractor, NullLogger<ExtractDefsStage>.Instance);

        // Should not throw; both nodes are skipped (no defName)
        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_UsesClassAttributeAsClassName()
    {
        var combinedDefs = XDocument.Parse("""
            <?xml version="1.0"?>
            <Defs>
                <ThingDef Class="MyMod.CustomThingDef">
                    <defName>CustomDef</defName>
                    <label>custom thing</label>
                </ThingDef>
            </Defs>
            """);

        var ctx = MakeContext(combinedDefs, SettingsWithRules("label"));
        var extractor = new XmlDefExtractor();
        var stage = new ExtractDefsStage(extractor, NullLogger<ExtractDefsStage>.Instance);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        ctx.Results[0].ClassName.Should().Be("MyMod.CustomThingDef");
    }

    [Fact]
    public async Task ExecuteAsync_CombinesRequiredModsFromRequiredPackageId()
    {
        var combinedDefs = XDocument.Parse("""
            <?xml version="1.0"?>
            <Defs>
                <ThingDef RequiredPackageId="mod.author.modid">
                    <defName>ModdedDef</defName>
                    <label>modded label</label>
                </ThingDef>
            </Defs>
            """);

        var ctx = MakeContext(combinedDefs, SettingsWithRules("label"));
        var extractor = new XmlDefExtractor();
        var stage = new ExtractDefsStage(extractor, NullLogger<ExtractDefsStage>.Instance);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
        var reqMods = ctx.Results[0].RequiredMods;
        reqMods.Should().NotBeNull();
        reqMods!.Allowed.Should().HaveCount(1);
        reqMods.Allowed[0].Should().ContainSingle(r => r.Value == "mod.author.modid");
    }
}
