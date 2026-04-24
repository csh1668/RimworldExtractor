using FluentAssertions;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Tests.Rules;

public class TranslationHandleTests
{
    [Theory]
    [InlineData("*verbClass", "verbClass", true)]
    [InlineData("*compClass", "compClass", true)]
    [InlineData("label", "label", false)]
    public void Parse_ExtractsTagAndWildcardFlag(string raw, string expectedTag, bool expectedWildcard)
    {
        var handle = TranslationHandle.Parse(raw);

        handle.Tag.Should().Be(expectedTag);
        handle.IsWildcardClass.Should().Be(expectedWildcard);
        handle.ToString().Should().Be(raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("*")]
    public void Parse_WithEmptyOrJustStar_Throws(string raw)
    {
        var act = () => TranslationHandle.Parse(raw);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = TranslationHandle.Parse("*verbClass");
        var b = TranslationHandle.Parse("*verbClass");

        a.Should().Be(b);
    }
}
