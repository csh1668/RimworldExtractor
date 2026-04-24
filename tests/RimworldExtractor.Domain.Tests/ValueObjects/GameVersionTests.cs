using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class GameVersionTests
{
    [Theory]
    [InlineData("1.0", 1, 0)]
    [InlineData("1.6", 1, 6)]
    [InlineData("1.12", 1, 12)]
    public void Parse_WithMajorMinor_SetsComponents(string input, int major, int minor)
    {
        var v = GameVersion.Parse(input);

        v.Major.Should().Be(major);
        v.Minor.Should().Be(minor);
        v.ToString().Should().Be(input);
    }

    [Theory]
    [InlineData("v1.6")]
    [InlineData("1.6.0")]
    [InlineData("2.0")]
    [InlineData("Common")]
    [InlineData("")]
    public void Parse_WithInvalid_Throws(string input)
    {
        var act = () => GameVersion.Parse(input);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("1.6", true)]
    [InlineData("v1.6", true)]
    [InlineData("v1.0", true)]
    [InlineData("Common", false)]
    [InlineData("default", false)]
    [InlineData("1", false)]
    public void TryParseAny_AcceptsBothBareAndVPrefixedForms(string input, bool expectedOk)
    {
        var ok = GameVersion.TryParseAny(input, out var result);

        ok.Should().Be(expectedOk);
        if (expectedOk)
        {
            result.Major.Should().Be(1);
        }
    }

    [Fact]
    public void Compare_OrdersByMajorThenMinor()
    {
        var a = GameVersion.Parse("1.5");
        var b = GameVersion.Parse("1.6");
        var c = GameVersion.Parse("1.12");

        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(c).Should().BeNegative();
        c.CompareTo(GameVersion.Parse("1.12")).Should().Be(0);
    }
}
