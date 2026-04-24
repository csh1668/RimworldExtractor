using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application.Tests.Plugins.BuiltIn;

public class NoTranslateCompatPluginTests
{
    private readonly NoTranslateCompatPlugin _plugin = new();

    private static TranslationEntry Entry(string node)
        => new("ThingDef", node, "val", null, null, null);

    [Fact]
    public void PostProcess_FiltersColorChannels()
    {
        var entries = new[]
        {
            Entry("Sword.colorChannels.0.value"),
            Entry("Sword.label")
        };

        var result = _plugin.PostProcess(entries).ToList();

        result.Should().ContainSingle().Which.Node.Should().Be("Sword.label");
    }

    [Fact]
    public void PostProcess_PassesThroughUnrelated()
    {
        var entries = new[]
        {
            Entry("Sword.label"),
            Entry("Sword.description")
        };

        var result = _plugin.PostProcess(entries).ToList();

        result.Should().HaveCount(2);
    }
}
