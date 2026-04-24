using FluentAssertions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Tests.Enums;

public class DuplicatesPolicyTests
{
    [Fact]
    public void Enum_HasExpectedMembers_InLegacyOrder()
    {
        var values = Enum.GetValues<DuplicatesPolicy>();

        values.Should().Equal(
            DuplicatesPolicy.Stop,
            DuplicatesPolicy.Overwrite,
            DuplicatesPolicy.KeepOriginal);
    }

    [Theory]
    [InlineData(DuplicatesPolicy.Stop, 0)]
    [InlineData(DuplicatesPolicy.Overwrite, 1)]
    [InlineData(DuplicatesPolicy.KeepOriginal, 2)]
    public void EnumValue_PreservesLegacyOrdinal(DuplicatesPolicy policy, int expected)
    {
        ((int)policy).Should().Be(expected);
    }
}
