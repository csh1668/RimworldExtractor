using FluentAssertions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Domain.Tests.Enums;

public class FolderKindTests
{
    [Fact]
    public void Enum_HasExactlyFourMembers()
    {
        Enum.GetValues<FolderKind>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData("Defs", FolderKind.Defs)]
    [InlineData("Keyed", FolderKind.Keyed)]
    [InlineData("Strings", FolderKind.Strings)]
    [InlineData("Patches", FolderKind.Patches)]
    public void TryParse_WithCanonicalName_ReturnsKind(string input, FolderKind expected)
    {
        Enum.TryParse<FolderKind>(input, ignoreCase: false, out var kind).Should().BeTrue();
        kind.Should().Be(expected);
    }

    [Theory]
    [InlineData("defs")]
    [InlineData("DEFS")]
    [InlineData("unknown")]
    public void TryParse_WithWrongCasingOrUnknown_ReturnsFalseOrCaseSensitiveResult(string input)
    {
        var caseSensitive = Enum.TryParse<FolderKind>(input, ignoreCase: false, out _);
        caseSensitive.Should().BeFalse();
    }
}
