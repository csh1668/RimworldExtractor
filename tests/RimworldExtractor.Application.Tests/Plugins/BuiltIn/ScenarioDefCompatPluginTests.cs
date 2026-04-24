using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application.Tests.Plugins.BuiltIn;

public class ScenarioDefCompatPluginTests
{
    private readonly ScenarioDefCompatPlugin _plugin = new();

    private static XDocument MakeDefs(string innerXml) =>
        XDocument.Parse($"<Defs>{innerXml}</Defs>");

    private static TranslationEntry Entry(string className, string node, string original = "val")
        => new(className, node, original, null, null, null);

    [Fact]
    public void PreProcess_InjectsScenarioNameAndDescription()
    {
        var doc = MakeDefs("""
            <ScenarioDef>
              <defName>TestScenario</defName>
              <label>Lost Tribe</label>
              <description>Survive.</description>
            </ScenarioDef>
            """);

        _plugin.PreProcess(doc);

        var scenario = doc.Descendants("scenario").First();
        scenario.Element("name")!.Value.Should().Be("Lost Tribe");
        scenario.Element("description")!.Value.Should().Be("Survive.");
    }

    [Fact]
    public void PreProcess_DoesNotInject_WhenScenarioAlreadyExists()
    {
        var doc = MakeDefs("""
            <ScenarioDef>
              <defName>TestScenario</defName>
              <label>Lost Tribe</label>
              <description>Survive.</description>
              <scenario><name>Existing</name></scenario>
            </ScenarioDef>
            """);

        _plugin.PreProcess(doc);

        var scenarioNames = doc.Descendants("scenario").SelectMany(s => s.Elements("name")).ToList();
        // Only the existing one should be present
        scenarioNames.Should().ContainSingle().Which.Value.Should().Be("Existing");
    }

    [Fact]
    public void PostProcess_RewritesScenarioName_ToLabel_WhenNoTopLevelLabel()
    {
        var entries = new[]
        {
            Entry("ScenarioDef", "TestScenario.scenario.name", "Lost Tribe")
        };

        var result = _plugin.PostProcess(entries).ToList();

        // Should contain both the original scenario.name and the rewritten label
        result.Should().Contain(e => e.Node == "TestScenario.label");
        result.Should().Contain(e => e.Node == "TestScenario.scenario.name");
    }

    [Fact]
    public void PostProcess_DoesNotRewrite_WhenTopLevelLabelExists()
    {
        var entries = new[]
        {
            Entry("ScenarioDef", "TestScenario.label", "Lost Tribe"),
            Entry("ScenarioDef", "TestScenario.scenario.name", "Lost Tribe")
        };

        var result = _plugin.PostProcess(entries).ToList();

        // scenario.name should NOT produce an extra label entry
        result.Count(e => e.Node == "TestScenario.label").Should().Be(1);
    }
}
