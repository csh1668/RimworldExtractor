using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

public class XmlHelpersTests
{
    [Fact]
    public void IsListNode_True_ForLiElement()
    {
        var el = new XElement("li", "x");
        el.IsListNode().Should().BeTrue();
    }

    [Fact]
    public void IsListNode_False_ForNonLi()
    {
        new XElement("label", "x").IsListNode().Should().BeFalse();
    }

    [Fact]
    public void IsTextNode_True_ForSingleTextChild()
    {
        new XElement("label", "some text").IsTextNode().Should().BeTrue();
    }

    [Fact]
    public void IsTextNode_False_ForElementChildren()
    {
        var parent = new XElement("parent", new XElement("child"));
        parent.IsTextNode().Should().BeFalse();
    }

    [Fact]
    public void GetIdxOfListNode_ReturnsPositionWithinSiblings()
    {
        var parent = new XElement("list",
            new XElement("li", "a"),
            new XElement("li", "b"),
            new XElement("li", "c"));
        var third = parent.Elements().ElementAt(2);

        third.GetIdxOfListNode().Should().Be(2);
    }

    [Theory]
    [InlineData("ThingDef", "Wooden.label", "/Defs/ThingDef[defName=\"Wooden\"]/label")]
    [InlineData("ThingDef", "Wooden.verbs.0.label", "/Defs/ThingDef[defName=\"Wooden\"]/verbs/li[1]/label")]
    public void BuildXPath_MatchesLegacyFormat(string className, string nodeName, string expected)
    {
        XmlHelpers.BuildXPath(className, nodeName).Should().Be(expected);
    }
}
