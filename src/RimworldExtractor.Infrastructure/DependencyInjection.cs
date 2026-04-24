using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Infrastructure.Excel;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Output;
using RimworldExtractor.Infrastructure.Settings;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure;

/// <summary>
/// Provides extension methods to register Infrastructure services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the complete Infrastructure service graph.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string settingsPath)
    {
        // Logging — required by XPatchProcessor; AddLogging is idempotent
        services.AddLogging();

        // Core IO
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<FileSystemGateway>();
        services.AddSingleton<SafeFileWriter>();

        // Settings
        services.AddSingleton<ISettingsStore>(sp =>
            new JsonSettingsStore(sp.GetRequiredService<IFileSystem>(), settingsPath));

        // Conflict resolution — default: non-interactive Overwrite policy.
        // Phase 4 may override with a factory that reads from ISettingsStore.
        services.AddSingleton<IConflictResolver>(_ =>
            new PolicyBasedConflictResolver(DuplicatesPolicy.Overwrite));

        // Mod discovery — the factory synchronously blocks on LoadAsync() to read AppSettings
        // for constructor arguments. This is acceptable for Phase 3; Phase 4 will refactor to
        // accept an IOptions<AppSettings> or equivalent to avoid sync-over-async at resolution time.
        services.AddSingleton<IModLister>(sp =>
        {
            var fs = sp.GetRequiredService<IFileSystem>();
            var store = sp.GetRequiredService<ISettingsStore>();
            var settings = store.LoadAsync().GetAwaiter().GetResult();
            return new FileSystemModLister(
                fs,
                settings.Extraction.CurrentVersion.ToString(),
                settings.Languages.Original.FolderName,
                settings.Paths.Rimworld,
                settings.Paths.Workshop);
        });

        // XML
        services.AddSingleton<IXmlDefParser, XDocumentDefParser>();
        services.AddSingleton<IXmlInheritanceResolver, XmlInheritanceResolver>();
        services.AddSingleton<IXmlDefExtractor, XmlDefExtractor>();
        services.AddSingleton<IXPatchProcessor, XPatchProcessor>();
        services.AddSingleton<IXmlLanguagesWriter, XmlLanguagesWriter>();
        services.AddSingleton<IXmlLanguagesReader, XmlLanguagesReader>();

        // Excel
        services.AddSingleton<IExcelReader, ClosedXmlReader>();
        services.AddSingleton<IExcelWriter, ClosedXmlWriter>();

        // Output strategies — all 3 registered; consumers enumerate IEnumerable<IOutputStrategy>
        // and filter by Format.
        services.AddSingleton<IOutputStrategy, ExcelOutputStrategy>();
        services.AddSingleton<IOutputStrategy, LanguagesOutputStrategy>();
        services.AddSingleton<IOutputStrategy, LanguagesWithCommentsOutputStrategy>();

        return services;
    }
}
