using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application.Tests.Plugins.BuiltIn;

public class MvcfCompatPluginTests
{
    private static TranslationEntry Entry(string className, string node, string original = "val", string? translated = null)
        => new(className, node, original, translated, null, null);

    private readonly MvcfCompatPlugin _plugin = new();

    [Fact]
    public void PostProcess_SynthesizesVisualLabelAndDescription_WhenMissing()
    {
        // VerbPropsLabel entry: DefName.comps.Comp_VerbProps.verbProps.0.label
        var labelEntry = Entry("ThingDef", "Sword.comps.Comp_VerbProps.verbProps.0.label", "Slash");
        var otherEntry = Entry("ThingDef", "Sword.label", "Sword");

        var result = _plugin.PostProcess([labelEntry, otherEntry]).ToList();

        // Should synthesize visualLabel and description, filter original label entry
        result.Should().NotContain(e => e.Node == "Sword.comps.Comp_VerbProps.verbProps.0.label");
        result.Should().Contain(e => e.Node == "Sword.comps.Comp_VerbProps.verbProps.0.visualLabel");
        result.Should().Contain(e => e.Node == "Sword.comps.Comp_VerbProps.verbProps.0.description" && e.Original == "");
        result.Should().Contain(e => e.Node == "Sword.label");
    }

    [Fact]
    public void PostProcess_UsesExistingVisualLabelAndDescription_WhenPresent()
    {
        var labelEntry = Entry("ThingDef", "Sword.comps.Comp_VerbProps.verbProps.0.label", "Slash");
        var visualLabelEntry = Entry("ThingDef", "Sword.comps.Comp_VerbProps.verbProps.0.visualLabel", "Slash Visual");
        var descEntry = Entry("ThingDef", "Sword.comps.Comp_VerbProps.verbProps.0.description", "Slash desc");

        var result = _plugin.PostProcess([labelEntry, visualLabelEntry, descEntry]).ToList();

        result.Should().Contain(e => e.Node == "Sword.comps.Comp_VerbProps.verbProps.0.visualLabel" && e.Original == "Slash Visual");
        result.Should().Contain(e => e.Node == "Sword.comps.Comp_VerbProps.verbProps.0.description" && e.Original == "Slash desc");
        // Original label entry should be filtered
        result.Should().NotContain(e => e.Node == "Sword.comps.Comp_VerbProps.verbProps.0.label");
    }

    [Fact]
    public void PostProcess_FiltersVerbLabelEntry_WhenItMatchesVerbPropsLabel()
    {
        var verbPropsLabel = Entry("ThingDef", "Sword.comps.Comp_VerbProps.verbProps.0.label", "Slash");
        var verbLabel = Entry("ThingDef", "Sword.verbs.0.label", "Slash"); // same original → matched

        var result = _plugin.PostProcess([verbPropsLabel, verbLabel]).ToList();

        // verbLabel should be filtered (it's replaced by synthesized entries)
        result.Should().NotContain(e => e.Node == "Sword.verbs.0.label");
        result.Should().Contain(e => e.Node == "Sword.comps.Comp_VerbProps.verbProps.0.visualLabel");
    }

    [Fact]
    public void PostProcess_PassesThroughUnrelatedEntries()
    {
        var unrelated = Entry("ThingDef", "Bow.label", "Bow");

        var result = _plugin.PostProcess([unrelated]).ToList();

        result.Should().ContainSingle().Which.Should().Be(unrelated);
    }
}
