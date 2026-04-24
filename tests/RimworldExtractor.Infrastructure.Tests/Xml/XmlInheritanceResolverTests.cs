using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

public class XmlInheritanceResolverTests
{
    private static XDocument Doc(string xml) => XDocument.Parse(xml);

    [Fact]
    public void Resolve_WithoutInheritance_ReturnsCopyWithoutAbstracts()
    {
        var combined = Doc("""
            <Defs>
              <ThingDef>
                <defName>X</defName>
                <label>x</label>
              </ThingDef>
              <ThingDef Name="Base" Abstract="True">
                <label>base</label>
              </ThingDef>
            </Defs>
            """);
        var resolver = new XmlInheritanceResolver();

        var result = resolver.Resolve(combined);

        result.Root!.Elements("ThingDef").Should().ContainSingle();
        result.Root!.Element("ThingDef")!.Element("defName")!.Value.Should().Be("X");
    }

    [Fact]
    public void Resolve_ChildWithParentName_InheritsParentFields()
    {
        var combined = Doc("""
            <Defs>
              <ThingDef Name="Base" Abstract="True">
                <description>inherited description</description>
              </ThingDef>
              <ThingDef ParentName="Base">
                <defName>Child</defName>
                <label>child</label>
              </ThingDef>
            </Defs>
            """);
        var resolver = new XmlInheritanceResolver();

        var result = resolver.Resolve(combined);

        var child = result.Root!.Elements("ThingDef").Single(e => e.Element("defName")!.Value == "Child");
        child.Element("description")!.Value.Should().Be("inherited description");
        child.Element("label")!.Value.Should().Be("child");
    }

    [Fact]
    public void Resolve_ChildOverridesParentField()
    {
        var combined = Doc("""
            <Defs>
              <ThingDef Name="Base" Abstract="True">
                <label>base-label</label>
              </ThingDef>
              <ThingDef ParentName="Base">
                <defName>Child</defName>
                <label>child-label</label>
              </ThingDef>
            </Defs>
            """);

        var result = new XmlInheritanceResolver().Resolve(combined);

        var child = result.Root!.Elements("ThingDef").Single(e => e.Element("defName")!.Value == "Child");
        child.Element("label")!.Value.Should().Be("child-label");
    }
}
