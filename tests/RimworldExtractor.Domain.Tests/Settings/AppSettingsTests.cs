using FluentAssertions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Domain.Tests.Settings;

public class AppSettingsTests
{
    [Fact]
    public void CurrentSchemaVersion_IsTwo()
    {
        AppSettings.CurrentSchemaVersion.Should().Be(2);
    }

    [Fact]
    public void Default_HasSensibleValues()
    {
        var s = AppSettings.Default;

        s.SchemaVersion.Should().Be(2);
        s.Paths.Should().NotBeNull();
        s.Paths.Rimworld.Should().BeEmpty();
        s.Paths.Workshop.Should().BeEmpty();
        s.Paths.BaseRefList.Should().BeEmpty();

        s.Languages.Original.Display.Should().Be("English");
        s.Languages.Translation.Display.Should().Be("Korean (한국어)");

        s.Extraction.CurrentVersion.ToString().Should().Be("1.6");
        s.Extraction.CommentOriginal.Should().BeFalse();
        s.Extraction.EnableTkey.Should().BeFalse();
        s.Extraction.Rules.Should().BeEmpty();
        s.Extraction.FullListTags.Should().BeEmpty();
        s.Extraction.NodeReplacements.Should().BeEmpty();
        s.Extraction.TranslationHandles.Should().BeEmpty();

        s.Output.Policy.Should().Be(DuplicatesPolicy.Overwrite);
        s.Output.Format.Should().Be(ExtractionFormat.Languages);
    }

    [Fact]
    public void With_IsImmutableClone()
    {
        var original = AppSettings.Default;
        var modified = original with { Paths = original.Paths with { Rimworld = "/rw" } };

        modified.Paths.Rimworld.Should().Be("/rw");
        original.Paths.Rimworld.Should().BeEmpty();
    }
}
