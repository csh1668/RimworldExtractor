using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class PackageIdTests
{
    [Fact]
    public void Create_PreservesInputButNormalizesForComparison()
    {
        var id = PackageId.Create("Ludeon.RimWorld");

        id.Value.Should().Be("Ludeon.RimWorld", "display form keeps original casing");
        id.Normalized.Should().Be("ludeon.rimworld", "comparison key is lowercase");
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        var a = PackageId.Create("Ludeon.RimWorld");
        var b = PackageId.Create("ludeon.rimworld");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NoDotInside")]
    public void Create_WithEmptyOrMissingDot_Throws(string input)
    {
        var act = () => PackageId.Create(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_ReturnsOriginalCasing()
    {
        PackageId.Create("Ludeon.RimWorld").ToString().Should().Be("Ludeon.RimWorld");
    }
}
