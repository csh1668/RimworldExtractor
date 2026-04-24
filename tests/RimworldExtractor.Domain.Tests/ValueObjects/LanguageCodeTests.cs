using FluentAssertions;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Tests.ValueObjects;

public class LanguageCodeTests
{
    [Theory]
    [InlineData("English", "English")]
    [InlineData("Korean (한국어)", "Korean")]
    [InlineData("ChineseSimplified (简体中文)", "ChineseSimplified")]
    [InlineData("Japanese (日本語)", "Japanese")]
    public void FolderName_StripsParentheticalNativeLabel(string display, string expectedFolder)
    {
        var lang = LanguageCode.Create(display);

        lang.Display.Should().Be(display);
        lang.FolderName.Should().Be(expectedFolder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(한국어)")]
    public void Create_WithEmptyOrMissingName_Throws(string input)
    {
        var act = () => LanguageCode.Create(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsCaseSensitiveOnDisplay()
    {
        var a = LanguageCode.Create("English");
        var b = LanguageCode.Create("English");
        var c = LanguageCode.Create("english");

        a.Should().Be(b);
        a.Should().NotBe(c, "RimWorld folder names are case-sensitive on POSIX");
    }
}
