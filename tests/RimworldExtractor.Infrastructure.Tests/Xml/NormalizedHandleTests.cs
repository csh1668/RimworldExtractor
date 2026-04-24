using FluentAssertions;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

public class NormalizedHandleTests
{
    [Fact]
    public void Normalize_NormalText_ReturnsUnchanged()
    {
        NormalizedHandle.Normalize("SteelPlate").Should().Be("SteelPlate");
    }

    [Fact]
    public void Normalize_SpacesAndTabs_ReplacedWithUnderscores_Collapsed()
    {
        // "hello world" → "hello_world"
        NormalizedHandle.Normalize("hello world").Should().Be("hello_world");
    }

    [Fact]
    public void Normalize_Braces_Stripped()
    {
        // "some {tag} text" → "some__text" → "some_text"
        NormalizedHandle.Normalize("some {tag} text").Should().Be("some_text");
    }

    [Fact]
    public void Normalize_AllDigits_PrefixedWithUnderscore()
    {
        NormalizedHandle.Normalize("12345").Should().Be("_12345");
    }

    [Fact]
    public void Normalize_EmptyString_ReturnsEmpty()
    {
        NormalizedHandle.Normalize(string.Empty).Should().Be(string.Empty);
    }

    [Fact]
    public void Normalize_LeadingTrailingUnderscores_Trimmed()
    {
        NormalizedHandle.Normalize("  _hello_ ").Should().Be("hello");
    }
}
