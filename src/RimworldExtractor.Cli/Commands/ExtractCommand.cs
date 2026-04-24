using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using RimworldExtractor.Application;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure;
using RimworldExtractor.Infrastructure.Output;

namespace RimworldExtractor.Cli.Commands;

/// <summary>
/// rimextract extract --mod &lt;name-or-id&gt; --out &lt;dir&gt;
///     [--format xlsx|languages|comments] [--version &lt;x.y&gt;]
/// </summary>
internal sealed class ExtractCommand : Command
{
    private readonly Option<string> _modOption = new("--mod")
    {
        Description = "Mod name, packageId, or folder ID to extract.",
        Required = true,
    };

    private readonly Option<string> _outOption = new("--out")
    {
        Description = "Output directory for generated files.",
        Required = true,
    };

    private readonly Option<string> _formatOption = new("--format")
    {
        Description = "Output format: xlsx | languages | comments (default: languages).",
        Required = false,
    };

    private readonly Option<string?> _versionOption = new("--version")
    {
        Description = "Override the RimWorld version used for extraction (e.g. 1.6).",
    };

    private readonly Option<string?> _configOption;

    public ExtractCommand(Option<string?> configOption) : base("extract", "Extract translatable strings from a mod.")
    {
        _configOption = configOption;

        Add(_modOption);
        Add(_outOption);
        Add(_formatOption);
        Add(_versionOption);

        SetAction(HandleAsync);
    }

    private async Task<int> HandleAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var modQuery = parseResult.GetRequiredValue(_modOption);
        var outDir = parseResult.GetRequiredValue(_outOption);
        var formatStr = parseResult.GetValue(_formatOption) ?? "languages";
        var versionStr = parseResult.GetValue(_versionOption);
        var configPath = parseResult.GetValue(_configOption);

        // Resolve settings path
        var settingsPath = configPath ?? DefaultSettingsPath();

        // Set up console logging via Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .MinimumLevel.Information()
            .CreateLogger();

        try
        {
            // Parse format
            var format = formatStr.ToLowerInvariant() switch
            {
                "xlsx" or "excel" => ExtractionFormat.Excel,
                "languages" => ExtractionFormat.Languages,
                "comments" => ExtractionFormat.LanguagesWithComments,
                _ => ExtractionFormat.Languages,
            };

            // Build DI container
            var services = new ServiceCollection();
            services.AddLogging(lb => lb.AddSerilog(Log.Logger));
            services.AddInfrastructure(settingsPath);
            services.AddApplication();

            await using var provider = services.BuildServiceProvider(validateScopes: true);

            var settings = await provider.GetRequiredService<ISettingsStore>().LoadAsync(cancellationToken);

            // Override version if specified
            AppSettings effectiveSettings = settings;
            if (versionStr is not null)
            {
                var gameVersion = Domain.ValueObjects.GameVersion.Parse(versionStr);
                effectiveSettings = settings with
                {
                    Extraction = settings.Extraction with { CurrentVersion = gameVersion },
                };
            }

            // Discover mod
            var discovery = provider.GetRequiredService<ModDiscoveryService>();
            var allMods = discovery.DiscoverAll();

            var target = allMods.FirstOrDefault(m =>
                string.Equals(m.ModName, modQuery, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.PackageId, modQuery, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Id, modQuery, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                await Console.Error.WriteLineAsync(
                    string.Create(CultureInfo.InvariantCulture, $"[error] Mod not found: {modQuery}"));
                await Console.Error.WriteLineAsync(
                    string.Create(CultureInfo.InvariantCulture, $"Available mods ({allMods.Count}):"));
                foreach (var m in allMods.Take(20))
                    await Console.Error.WriteLineAsync($"  {m.Identifier}  [{m.PackageId}]");
                if (allMods.Count > 20)
                    await Console.Error.WriteLineAsync($"  ... and {allMods.Count - 20} more");
                return 1;
            }

            Console.WriteLine($"[extract] Target: {target.Identifier}");

            var folders = discovery.GetExtractableFolders(target);
            var refMods = discovery.FindReferenceMods(target);

            var request = new ExtractionRequest(
                Target: target,
                SelectedFolders: folders,
                ReferenceMods: refMods,
                Settings: effectiveSettings);

            var pipeline = provider.GetRequiredService<IExtractionPipeline>();

            var progress = new Progress<ExtractionProgress>(p =>
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"  [{p.Percentage:F0}%] {p.Message}")));

            var result = await pipeline.RunAsync(request, progress, cancellationToken);

            Console.WriteLine($"[extract] {result.Entries.Count} entries extracted.");

            // Write output
            Directory.CreateDirectory(outDir);

            var strategies = provider.GetServices<IOutputStrategy>().ToList();
            var strategy = strategies.FirstOrDefault(s => s.Format == format)
                ?? strategies.First(s => s.Format == ExtractionFormat.Languages);

            var baseFileName = SanitizeFileName(target.ModName);
            await strategy.WriteAsync(
                result.Entries,
                outDir,
                baseFileName,
                effectiveSettings.Languages.Original,
                effectiveSettings.Languages.Translation,
                cancellationToken);

            Console.WriteLine($"[extract] Output written to: {outDir}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[error] {ex.Message}");
            Log.Error(ex, "Extraction failed");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static string DefaultSettingsPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "RimworldExtractor", "settings.json");
        }
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(configHome, "rimworld-extractor", "settings.json");
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
