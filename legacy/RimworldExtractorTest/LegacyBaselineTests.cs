using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RimworldExtractorInternal;
using RimworldExtractorInternal.DataTypes;

namespace RimworldExtractorTest;

[TestClass]
public class LegacyBaselineTests
{
    private static string RepoRoot => typeof(LegacyBaselineTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(a => a.Key == "RepositoryRoot")
        .Value!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestMethod]
    public void ExtractSampleMod_MatchesCommittedSnapshot()
    {
        // Arrange
        Prefabs.Init();
        var sampleModRoot = Path.Combine(RepoRoot, "samples", "sample-mod");
        Prefabs.PathRimworld = RepoRoot; // irrelevant for this mod, but Init requires it
        Prefabs.PathWorkshop = RepoRoot;
        Prefabs.CurrentVersion = "1.6";

        var mod = ModLister.GetModMetadataByModRoot(sampleModRoot)
            ?? throw new InvalidOperationException("Fixture mod not discoverable");
        var folders = ModLister.GetExtractableFolders(mod).ToList();

        // Act
        var entries = Extractor.ExtractTranslationData(mod, folders, referenceMods: null);
        var ordered = entries
            .OrderBy(e => e.ClassName, StringComparer.Ordinal)
            .ThenBy(e => e.Node, StringComparer.Ordinal)
            .ThenBy(e => e.Original, StringComparer.Ordinal)
            .Select(e => new
            {
                e.ClassName,
                e.Node,
                e.Original,
                e.SourceFile
            })
            .ToList();
        var actual = NormalizeLineEndings(JsonSerializer.Serialize(ordered, JsonOptions));

        // Assert / capture
        var snapshotPath = Path.Combine(
            RepoRoot, "tests", "__snapshots__", "legacy", "sample-mod.extraction.json");
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        if (!File.Exists(snapshotPath))
        {
            File.WriteAllText(snapshotPath, actual);
            Assert.Fail($"Snapshot created at {snapshotPath}. Review it and re-run the test.");
        }

        var expected = NormalizeLineEndings(File.ReadAllText(snapshotPath));
        Assert.AreEqual(expected, actual, "Extraction output drifted from committed snapshot.");
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n").Replace("\r", "\n");
}
