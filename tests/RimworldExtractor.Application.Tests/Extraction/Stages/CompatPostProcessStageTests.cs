using FluentAssertions;
using RimworldExtractor.Application.Compat;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Plugins;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class CompatPostProcessStageTests
{
    private static ExtractionContext MakeContext(IEnumerable<TranslationEntry>? initial = null)
    {
        var mod = new ModMetadata("/mod", "id", "TestMod", "author.testmod", false);
        var request = new ExtractionRequest(mod, Array.Empty<ExtractableFolder>(), null, AppSettings.Default);
        var ctx = new ExtractionContext(request);
        if (initial is not null) ctx.Results.AddRange(initial);
        return ctx;
    }

    private static TranslationEntry MakeEntry(string className, string node, string original = "val")
        => new(className, node, original, null, null, null);

    private sealed class PrefixPlugin : CompatPluginBase
    {
        private readonly string _prefix;
        public PrefixPlugin(string prefix) => _prefix = prefix;

        public override IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries)
        {
            foreach (var e in entries)
                yield return e with { Original = _prefix + e.Original };
        }
    }

    [Fact]
    public async Task ExecuteAsync_AppliesPluginsInOrder()
    {
        var entries = new[] { MakeEntry("ThingDef", "Sword.label", "sword") };
        var ctx = MakeContext(entries);

        // Plugin 1 prefixes "A-", Plugin 2 prefixes "B-"
        var plugin1 = new PrefixPlugin("A-");
        var plugin2 = new PrefixPlugin("B-");
        var registry = new CompatRegistry([plugin1, plugin2]);
        var stage = new CompatPostProcessStage(registry);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle()
            .Which.Original.Should().Be("B-A-sword");
    }

    [Fact]
    public async Task ExecuteAsync_DeduplicatesByClassNamePlusNode()
    {
        var entries = new[]
        {
            MakeEntry("ThingDef", "Sword.label", "first"),
            MakeEntry("ThingDef", "Sword.label", "duplicate"),
            MakeEntry("ThingDef", "Sword.description", "desc")
        };
        var ctx = MakeContext(entries);
        var registry = new CompatRegistry(Array.Empty<ICompatPlugin>());
        var stage = new CompatPostProcessStage(registry);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().HaveCount(2);
        ctx.Results[0].Node.Should().Be("Sword.label");
        ctx.Results[0].Original.Should().Be("first"); // first wins
        ctx.Results[1].Node.Should().Be("Sword.description");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPlugins_ReplacesResultsWithDeduped()
    {
        var entries = new[]
        {
            MakeEntry("ThingDef", "A.label"),
            MakeEntry("ThingDef", "A.label"),
        };
        var ctx = MakeContext(entries);
        var registry = new CompatRegistry(Array.Empty<ICompatPlugin>());
        var stage = new CompatPostProcessStage(registry);

        await stage.ExecuteAsync(ctx);

        ctx.Results.Should().ContainSingle();
    }
}
