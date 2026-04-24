using System.Reflection;
using FluentAssertions;
using RimworldExtractor.Cli.Commands;
using System.CommandLine;

namespace RimworldExtractor.Integration.Tests;

/// <summary>
/// Smoke tests that invoke the CLI in-process against the sample-mod fixture.  They validate:
/// - extract command exits 0 and produces at least one output file
/// - analyze command exits 2 (stub / not implemented)
/// </summary>
public sealed class CliSmokeTests : IDisposable
{
    private static string RepoRoot => typeof(CliSmokeTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(a => a.Key == "RepositoryRoot")
        .Value!;

    private readonly string _tempDir;
    private readonly string _settingsPath;

    public CliSmokeTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("rwx-cli-smoke-").FullName;
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        WriteMinimalSettings(_settingsPath, RepoRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExtractCommand_AgainstSampleMod_ExitsZeroAndCreatesOutput()
    {
        var outDir = Path.Combine(_tempDir, "extract-out");
        const string sampleModName = "RimworldExtractor Sample Mod";

        var exitCode = await InvokeCliAsync(
            "--config", _settingsPath,
            "extract",
            "--mod", sampleModName,
            "--out", outDir,
            "--format", "languages");

        exitCode.Should().Be(0, "extract against sample-mod should succeed");
        Directory.Exists(outDir).Should().BeTrue("output directory must be created");
        var outputFiles = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);
        outputFiles.Should().NotBeEmpty("at least one Languages/ XML file should be written");
    }

    [Fact]
    public async Task AnalyzeCommand_ReturnsExitCode2_NotImplemented()
    {
        var exitCode = await InvokeCliAsync("analyze", "--mod", "AnyMod");

        exitCode.Should().Be(2, "analyze is a stub returning exit code 2 in v2.0");
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static async Task<int> InvokeCliAsync(params string[] args)
    {
        var configOption = new Option<string?>("--config")
        {
            Description = "Path to settings.json.",
            Recursive = true,
        };

        var root = new RootCommand("rimextract CLI (test harness)");
        root.Add(configOption);
        root.Add(new ExtractCommand(configOption));
        root.Add(new ConvertCommand(configOption));
        root.Add(new AnalyzeCommand(configOption));

        var cliConfig = new CommandLineConfiguration(root);
        return await cliConfig.InvokeAsync(args);
    }

    /// <summary>
    /// Writes a minimal settings.json that points mod discovery at the samples/ directory.
    /// The sample-mod lives directly in samples/sample-mod/. We point:
    ///   - workshopDir → {repoRoot}/samples/ so DiscoverAll() finds sample-mod there
    ///   - rimworldDir → a non-existent sub-path so no phantom mods appear from Data/ or Mods/
    /// </summary>
    private static void WriteMinimalSettings(string path, string repoRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // samplesDir is where sample-mod/ lives (directly enumerated by workshop scan)
        var samplesDir = Path.Combine(repoRoot, "samples");
        // Escape backslashes for JSON
        var jsonSamples = samplesDir.Replace("\\", "\\\\");
        var jsonRoot = repoRoot.Replace("\\", "\\\\");
        var json = $$"""
            {
              "schemaVersion": 2,
              "paths": {
                "rimworld": "{{jsonRoot}}",
                "workshop": "{{jsonSamples}}",
                "baseRefList": ""
              },
              "languages": {
                "original": "English",
                "translation": "Korean (한국어)"
              },
              "extraction": {
                "currentVersion": "1.6",
                "commentOriginal": false,
                "enableTkey": false,
                "rules": [],
                "fullListTags": [],
                "nodeReplacements": [],
                "translationHandles": []
              },
              "output": {
                "policy": "Overwrite",
                "format": "Languages"
              }
            }
            """;
        File.WriteAllText(path, json);
    }
}
