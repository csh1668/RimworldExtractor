using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using RimworldExtractor.Application;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure;
using RimworldExtractor.Infrastructure.Excel;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Cli.Commands;

/// <summary>
/// rimextract convert --input &lt;file.xlsx&gt; --output &lt;dir&gt;
/// Reads an XLSX produced by the Extract command and writes it back as
/// a RimWorld Languages/ XML tree.
/// </summary>
internal sealed class ConvertCommand : Command
{
    private readonly Option<string> _inputOption = new("--input")
    {
        Description = "Path to the source .xlsx file.",
        Required = true,
    };

    private readonly Option<string> _outputOption = new("--output")
    {
        Description = "Output directory for Languages/ XML tree.",
        Required = true,
    };

    private readonly Option<string?> _configOption;

    public ConvertCommand(Option<string?> configOption) : base("convert", "Convert an XLSX extraction file back to RimWorld Languages/ XML.")
    {
        _configOption = configOption;

        Add(_inputOption);
        Add(_outputOption);

        SetAction(HandleAsync);
    }

    private async Task<int> HandleAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var inputPath = parseResult.GetRequiredValue(_inputOption);
        var outputDir = parseResult.GetRequiredValue(_outputOption);
        var configPath = parseResult.GetValue(_configOption);

        var settingsPath = configPath ?? DefaultSettingsPath();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .MinimumLevel.Information()
            .CreateLogger();

        try
        {
            if (!File.Exists(inputPath))
            {
                await Console.Error.WriteLineAsync($"[error] Input file not found: {inputPath}");
                return 1;
            }

            // Build DI container
            var services = new ServiceCollection();
            services.AddLogging(lb => lb.AddSerilog(Log.Logger));
            services.AddInfrastructure(settingsPath);
            services.AddApplication();

            await using var provider = services.BuildServiceProvider(validateScopes: true);

            var settings = await provider.GetRequiredService<ISettingsStore>().LoadAsync(cancellationToken);

            Console.WriteLine($"[convert] Reading: {inputPath}");
            var reader = provider.GetRequiredService<IExcelReader>();
            var entries = await reader.ReadAsync(inputPath, cancellationToken);
            Console.WriteLine($"[convert] {entries.Count} entries read.");

            Directory.CreateDirectory(outputDir);

            var writer = provider.GetRequiredService<IXmlLanguagesWriter>();
            var baseFileName = Path.GetFileNameWithoutExtension(inputPath);

            await writer.WriteAsync(
                entries,
                outputDir,
                settings.Languages.Translation,
                baseFileName,
                skipNoTranslation: false,
                commentOriginal: false,
                fullListTags: Array.Empty<string>(),
                cancellationToken);

            Console.WriteLine($"[convert] Languages/ XML written to: {outputDir}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[error] {ex.Message}");
            Log.Error(ex, "Convert failed");
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
}
