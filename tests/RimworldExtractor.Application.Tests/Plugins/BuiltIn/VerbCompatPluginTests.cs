using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application.Tests.Plugins.BuiltIn;

public class VerbCompatPluginTests
{
    private readonly VerbCompatPlugin _plugin = new();

    private static XDocument MakeDefs(string innerXml) =>
        XDocument.Parse($"<Defs>{innerXml}</Defs>");

    [Fact]
    public void PreProcess_InjectsLabelOnShootVerb_WhenMissing()
    {
        var doc = MakeDefs("""
            <ThingDef>
              <defName>TestGun</defName>
              <label>Test Gun</label>
              <verbs>
                <li><verbClass>Verb_Shoot</verbClass></li>
              </verbs>
            </ThingDef>
            """);

        _plugin.PreProcess(doc);

        var verbLabel = doc.Descendants("verbs").First().Elements().First().Element("label");
        verbLabel.Should().NotBeNull();
        verbLabel!.Value.Should().Be("Test Gun");
    }

    [Fact]
    public void PreProcess_DoesNotOverwrite_ExistingVerbLabel()
    {
        var doc = MakeDefs("""
            <ThingDef>
              <defName>TestGun</defName>
              <label>Test Gun</label>
              <verbs>
                <li><verbClass>Verb_Shoot</verbClass><label>My Custom Label</label></li>
              </verbs>
            </ThingDef>
            """);

        _plugin.PreProcess(doc);

        var verbLabel = doc.Descendants("verbs").First().Elements().First().Element("label");
        verbLabel!.Value.Should().Be("My Custom Label");
    }

    [Fact]
    public void PreProcess_SkipsAbstractDefs()
    {
        var doc = MakeDefs("""
            <ThingDef Abstract="True">
              <defName>AbstractGun</defName>
              <label>Gun</label>
              <verbs>
                <li><verbClass>Verb_Shoot</verbClass></li>
              </verbs>
            </ThingDef>
            """);

        _plugin.PreProcess(doc);

        var verbLabel = doc.Descendants("verbs").First().Elements().First().Element("label");
        verbLabel.Should().BeNull();
    }

    [Fact]
    public void PreProcess_SkipsNonShootVerbs()
    {
        var doc = MakeDefs("""
            <ThingDef>
              <defName>MeleeWeapon</defName>
              <label>Sword</label>
              <verbs>
                <li><verbClass>Verb_MeleeAttack</verbClass></li>
              </verbs>
            </ThingDef>
            """);

        _plugin.PreProcess(doc);

        var verbLabel = doc.Descendants("verbs").First().Elements().First().Element("label");
        verbLabel.Should().BeNull();
    }
}
