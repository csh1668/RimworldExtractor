using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application.Tests.Plugins.BuiltIn;

public class FactionDefCompatPluginTests
{
    private readonly FactionDefCompatPlugin _plugin = new();

    private static XDocument MakeDefs(string innerXml) =>
        XDocument.Parse($"<Defs>{innerXml}</Defs>");

    [Fact]
    public void PreProcess_InjectsDefaults_WhenMissing()
    {
        var doc = MakeDefs("""
            <FactionDef>
              <defName>TestFaction</defName>
            </FactionDef>
            """);

        _plugin.PreProcess(doc);

        var faction = doc.Root!.Element("FactionDef")!;
        faction.Element("pawnSingular")!.Value.Should().Be("member");
        faction.Element("pawnsPlural")!.Value.Should().Be("members");
        faction.Element("leaderTitle")!.Value.Should().Be("leader");
    }

    [Fact]
    public void PreProcess_DoesNotOverwrite_ExistingElements()
    {
        var doc = MakeDefs("""
            <FactionDef>
              <defName>TestFaction</defName>
              <pawnSingular>colonist</pawnSingular>
              <pawnsPlural>colonists</pawnsPlural>
              <leaderTitle>commander</leaderTitle>
            </FactionDef>
            """);

        _plugin.PreProcess(doc);

        var faction = doc.Root!.Element("FactionDef")!;
        faction.Element("pawnSingular")!.Value.Should().Be("colonist");
        faction.Element("pawnsPlural")!.Value.Should().Be("colonists");
        faction.Element("leaderTitle")!.Value.Should().Be("commander");
    }
}
