using FluentAssertions;
using RimworldExtractor.Application.Compat;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application.Tests.Plugins.BuiltIn;

public class NodeReplacementCompatPluginTests
{
    private static TranslationEntry Entry(string className, string node)
        => new(className, node, "val", null, null, null);

    private static NodeReplacementCompatPlugin MakePlugin(params NodeReplacementRule[] rules)
    {
        var provider = new NodeReplacementRuleProvider(rules);
        return new NodeReplacementCompatPlugin(provider);
    }

    [Fact]
    public void PostProcess_ReplacesDefType_WhenRuleMatches()
    {
        var rule = new NodeReplacementRule("CombatExtended.AmmoDef+*", "ThingDef+*");
        var plugin = MakePlugin(rule);
        var entry = Entry("CombatExtended.AmmoDef", "HE_Ammo.label");

        var result = plugin.PostProcess([entry]).ToList();

        result.Should().Contain(e => e.ClassName == "ThingDef" && e.Node == "HE_Ammo.label");
    }

    [Fact]
    public void PostProcess_ReplacesNodePath_WhenSpecificNodeMatches()
    {
        var rule = new NodeReplacementRule("JecsTools.BackstoryDef+baseDesc", "BackstoryDef+description");
        var plugin = MakePlugin(rule);
        var entry = Entry("JecsTools.BackstoryDef", "SomeDef.baseDesc");

        var result = plugin.PostProcess([entry]).ToList();

        result.Should().Contain(e => e.ClassName == "BackstoryDef" && e.Node == "SomeDef.description");
    }

    [Fact]
    public void PostProcess_PassesThrough_WhenNoRuleMatches()
    {
        var rule = new NodeReplacementRule("OtherDef+*", "ThingDef+*");
        var plugin = MakePlugin(rule);
        var entry = Entry("ThingDef", "Sword.label");

        var result = plugin.PostProcess([entry]).ToList();

        result.Should().Contain(e => e.ClassName == "ThingDef" && e.Node == "Sword.label");
    }

    [Fact]
    public void PostProcess_YieldsKeyedEntries_Twice_LegacyBehavior()
    {
        // Legacy bug: Keyed entries get yielded once unconditionally, then again from DoNodeReplacement
        var plugin = MakePlugin(); // no rules
        var entry = Entry("Keyed", "SomeKey");

        var result = plugin.PostProcess([entry]).ToList();

        // DistinctBy in CompatPostProcessStage collapses them, but here we confirm raw output is 2
        result.Should().HaveCount(2);
        result.All(e => e.Node == "SomeKey").Should().BeTrue();
    }

    [Fact]
    public void PostProcess_HandlesPatchesPrefix()
    {
        var rule = new NodeReplacementRule("CombatExtended.AmmoDef+*", "ThingDef+*");
        var plugin = MakePlugin(rule);
        var entry = Entry("Patches.CombatExtended.AmmoDef", "HE_Ammo.label");

        var result = plugin.PostProcess([entry]).ToList();

        result.Should().Contain(e => e.ClassName == "Patches.ThingDef" && e.Node == "HE_Ammo.label");
    }
}
