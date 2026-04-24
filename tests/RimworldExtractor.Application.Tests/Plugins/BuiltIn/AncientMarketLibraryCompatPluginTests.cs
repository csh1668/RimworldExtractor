using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application.Tests.Plugins.BuiltIn;

public class AncientMarketLibraryCompatPluginTests
{
    private readonly AncientMarketLibraryCompatPlugin _plugin = new();

    private static XDocument MakeDefs(string innerXml) =>
        XDocument.Parse($"<Defs>{innerXml}</Defs>");

    [Fact]
    public void PreProcess_RemovesParentheticalTextNodes()
    {
        var doc = MakeDefs("""
            <AncientMarket_Libraray.CustomMapDataDef>
              <defName>TestMap</defName>
              <someField>(internal-id)</someField>
              <label>Market</label>
            </AncientMarket_Libraray.CustomMapDataDef>
            """);

        _plugin.PreProcess(doc);

        var def = doc.Root!.Elements().First();
        // someField should have its text node removed
        var someField = def.Element("someField")!;
        someField.Value.Should().BeEmpty();
        def.Element("label")!.Value.Should().Be("Market");
    }

    [Fact]
    public void PreProcess_PreservesNonParentheticalText()
    {
        var doc = MakeDefs("""
            <AncientMarket_Libraray.CustomMapDataDef>
              <defName>TestMap</defName>
              <label>The Market</label>
            </AncientMarket_Libraray.CustomMapDataDef>
            """);

        _plugin.PreProcess(doc);

        var def = doc.Root!.Elements().First();
        def.Element("label")!.Value.Should().Be("The Market");
    }

    [Fact]
    public void PreProcess_DoesNotAffectOtherDefTypes()
    {
        var doc = MakeDefs("""
            <ThingDef>
              <defName>SomeItem</defName>
              <label>(something)</label>
            </ThingDef>
            """);

        _plugin.PreProcess(doc);

        // ThingDef label should be unchanged
        doc.Root!.Element("ThingDef")!.Element("label")!.Value.Should().Be("(something)");
    }
}
