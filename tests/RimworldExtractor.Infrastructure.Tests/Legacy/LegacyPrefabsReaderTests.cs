using FluentAssertions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Infrastructure.Legacy;

namespace RimworldExtractor.Infrastructure.Tests.Legacy;

public class LegacyPrefabsReaderTests : IDisposable
{
    private readonly string _tmpDir;

    public LegacyPrefabsReaderTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-legacy-").FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { /* swallow */ }
        GC.SuppressFinalize(this);
    }

    private string WritePrefabs(string[] lines)
    {
        var path = Path.Combine(_tmpDir, "Prefabs.dat");
        File.WriteAllLines(path, lines);
        return path;
    }

    private static string[] SampleLines() =>
    [
        "DO NOT EDIT THIS MANUALLY",
        "9",                                                    // version
        "False",                                                // enableTkey
        "/games/RimWorld",                                      // pathRimworld
        "/games/workshop/294100",                               // pathWorkshop
        "",                                                     // pathBaseRefList
        "1.6",                                                  // currentVersion
        @"^[1]\.\d+",                                           // patternVersion (dropped)
        @"^v[1]\.\d+",                                          // patternVersionWithV (dropped)
        "English",                                              // originalLanguage
        "Korean (한국어)",                                      // translationLanguage
        "False",                                                // commentOriginal
        "label/description/baseDesc+ThingDef-TrashDef",         // extractableTags
        "rulesFiles/rulesStrings",                              // fullListTags
        "CombatExtended.AmmoDef+*|ThingDef+*/Meow.X+*|ThingDef+*", // nodeReplacement
        "*verbClass/*compClass",                                // translationHandles
        "Overwrite",                                            // policy
        "Languages",                                            // method
    ];

    [Fact]
    public void Read_ValidFile_ReturnsAppSettingsWithExpectedShape()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.SchemaVersion.Should().Be(Domain.Settings.AppSettings.CurrentSchemaVersion);
        settings.Paths.Rimworld.Should().Be("/games/RimWorld");
        settings.Paths.Workshop.Should().Be("/games/workshop/294100");
        settings.Paths.BaseRefList.Should().BeEmpty();
        settings.Languages.Original.Display.Should().Be("English");
        settings.Languages.Translation.Display.Should().Be("Korean (한국어)");
        settings.Extraction.CurrentVersion.ToString().Should().Be("1.6");
        settings.Extraction.CommentOriginal.Should().BeFalse();
        settings.Extraction.EnableTkey.Should().BeFalse();
        settings.Output.Policy.Should().Be(DuplicatesPolicy.Overwrite);
        settings.Output.Format.Should().Be(ExtractionFormat.Languages);
    }

    [Fact]
    public void Read_ValidFile_ParsesExtractionRulesViaDsl()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.Rules.Should().HaveCount(3);
        var baseDesc = settings.Extraction.Rules.Single(r => r.Tag == "baseDesc");
        baseDesc.Whitelist.Should().ContainSingle().Which.Should().Be("ThingDef");
        baseDesc.Blacklist.Should().ContainSingle().Which.Should().Be("TrashDef");
    }

    [Fact]
    public void Read_ValidFile_ParsesFullListTags()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.FullListTags.Should().BeEquivalentTo(["rulesFiles", "rulesStrings"]);
    }

    [Fact]
    public void Read_ValidFile_ParsesNodeReplacements()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.NodeReplacements.Should().HaveCount(2);
        settings.Extraction.NodeReplacements[0].From.Should().Be("CombatExtended.AmmoDef+*");
        settings.Extraction.NodeReplacements[0].To.Should().Be("ThingDef+*");
    }

    [Fact]
    public void Read_ValidFile_ParsesTranslationHandles()
    {
        var path = WritePrefabs(SampleLines());

        var settings = LegacyPrefabsReader.Read(path);

        settings.Extraction.TranslationHandles.Should().HaveCount(2);
        settings.Extraction.TranslationHandles[0].Tag.Should().Be("verbClass");
        settings.Extraction.TranslationHandles[0].IsWildcardClass.Should().BeTrue();
    }

    [Fact]
    public void Read_WithWrongVersion_ThrowsInvalidDataException()
    {
        var lines = SampleLines();
        lines[1] = "8";
        var path = WritePrefabs(lines);

        var act = () => LegacyPrefabsReader.Read(path);

        act.Should().Throw<InvalidDataException>().WithMessage("*version*");
    }

    [Fact]
    public void Read_NonexistentPath_ThrowsFileNotFoundException()
    {
        var act = () => LegacyPrefabsReader.Read(Path.Combine(_tmpDir, "missing.dat"));

        act.Should().Throw<FileNotFoundException>();
    }
}
