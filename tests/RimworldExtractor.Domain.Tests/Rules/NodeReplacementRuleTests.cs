using FluentAssertions;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Tests.Rules;

public class NodeReplacementRuleTests
{
    [Fact]
    public void Ctor_StoresFromAndTo()
    {
        var rule = new NodeReplacementRule("CombatExtended.AmmoDef+*", "ThingDef+*");

        rule.From.Should().Be("CombatExtended.AmmoDef+*");
        rule.To.Should().Be("ThingDef+*");
    }

    [Theory]
    [InlineData("", "ThingDef+*")]
    [InlineData("CombatExtended.AmmoDef+*", "")]
    public void Ctor_WithEmptyPart_Throws(string from, string to)
    {
        var act = () => new NodeReplacementRule(from, to);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new NodeReplacementRule("a", "b");
        var b = new NodeReplacementRule("a", "b");

        a.Should().Be(b);
    }
}
