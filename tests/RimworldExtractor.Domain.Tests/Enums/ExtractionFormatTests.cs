using FluentAssertions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Tests.Enums;

public class ExtractionFormatTests
{
    [Fact]
    public void Enum_HasExpectedMembers_InLegacyOrder()
    {
        var values = Enum.GetValues<ExtractionFormat>();

        values.Should().Equal(
            ExtractionFormat.Excel,
            ExtractionFormat.Languages,
            ExtractionFormat.LanguagesWithComments);
    }

    [Theory]
    [InlineData(ExtractionFormat.Excel, 0)]
    [InlineData(ExtractionFormat.Languages, 1)]
    [InlineData(ExtractionFormat.LanguagesWithComments, 2)]
    public void EnumValue_PreservesLegacyOrdinal(ExtractionFormat format, int expected)
    {
        ((int)format).Should().Be(expected);
    }
}
