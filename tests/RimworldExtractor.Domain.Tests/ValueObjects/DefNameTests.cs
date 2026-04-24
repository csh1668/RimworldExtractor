using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class DefNameTests
{
    [Theory]
    [InlineData("SampleMod_WoodenSpear")]
    [InlineData("A")]
    [InlineData("ThingDef_With.Dots")]
    public void Create_WithNonEmpty_ReturnsValue(string input)
    {
        var result = DefName.Create(input);

        result.Value.Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_WithEmptyOrWhitespace_Throws(string input)
    {
        var act = () => DefName.Create(input);

        act.Should().Throw<ArgumentException>().WithMessage("*DefName*");
    }

    [Fact]
    public void Create_WithNull_Throws()
    {
        var act = () => DefName.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var a = DefName.Create("Foo");
        var b = DefName.Create("Foo");
        var c = DefName.Create("Bar");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        DefName.Create("Foo").ToString().Should().Be("Foo");
    }
}
