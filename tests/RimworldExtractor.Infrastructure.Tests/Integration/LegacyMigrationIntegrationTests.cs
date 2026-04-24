using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Legacy;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure.Tests.Integration;

public class LegacyMigrationIntegrationTests : IDisposable
{
    private readonly string _tmpDir;

    public LegacyMigrationIntegrationTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-migrate-").FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { /* swallow */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Migrate_LegacyPrefabsToJsonSettings_RoundTripsEqual()
    {
        // Arrange: build a realistic legacy Prefabs.dat
        var prefabsPath = Path.Combine(_tmpDir, "Prefabs.dat");
        File.WriteAllLines(prefabsPath,
        [
            "DO NOT EDIT THIS MANUALLY",
            "9",
            "False",
            "/games/RimWorld",
            "/games/workshop/294100",
            "",
            "1.6",
            @"^[1]\.\d+",
            @"^v[1]\.\d+",
            "English",
            "Korean (한국어)",
            "False",
            "label/description+ThingDef-TrashDef",
            "rulesStrings",
            "Mod.X+*|ThingDef+*",
            "*verbClass",
            "Overwrite",
            "Languages",
        ]);

        // Act: legacy read → JSON save → JSON load
        var readFromLegacy = LegacyPrefabsReader.Read(prefabsPath);
        var jsonPath = Path.Combine(_tmpDir, "settings.json");
        var store = new JsonSettingsStore(new PhysicalFileSystem(), jsonPath);
        await store.SaveAsync(readFromLegacy, TestContext.Current.CancellationToken);
        var readFromJson = await store.LoadAsync(TestContext.Current.CancellationToken);

        // Assert: lossless
        readFromJson.Should().Be(readFromLegacy);
        File.Exists(jsonPath).Should().BeTrue();
    }
}
