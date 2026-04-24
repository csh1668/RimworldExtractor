using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Excel;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Output;
using RimworldExtractor.Infrastructure.Settings;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ResolvesISettingsStore()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(settingsPath: "/tmp/settings.json");

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var store = provider.GetRequiredService<ISettingsStore>();

        store.Should().BeOfType<JsonSettingsStore>();
    }

    [Fact]
    public async Task AddInfrastructure_ResolvesFullServiceGraph()
    {
        // Arrange: settings.json must exist with AppSettings.Default — use real temp path
        var tempDir = Directory.CreateTempSubdirectory("rwx-di-").FullName;
        var settingsPath = Path.Combine(tempDir, "settings.json");
        try
        {
            // Pre-save defaults so the IModLister factory can load
            var fs = new PhysicalFileSystem();
            var initialStore = new JsonSettingsStore(fs, settingsPath);
            await initialStore.SaveAsync(AppSettings.Default, TestContext.Current.CancellationToken);

            var services = new ServiceCollection();
            services.AddInfrastructure(settingsPath);

            using var provider = services.BuildServiceProvider(validateScopes: true);

            // Assert every critical service resolves
            provider.GetRequiredService<IFileSystem>().Should().NotBeNull();
            provider.GetRequiredService<ISettingsStore>().Should().NotBeNull();
            provider.GetRequiredService<IConflictResolver>().Should().NotBeNull();
            provider.GetRequiredService<IModLister>().Should().NotBeNull();
            provider.GetRequiredService<IXmlDefParser>().Should().NotBeNull();
            provider.GetRequiredService<IXmlInheritanceResolver>().Should().NotBeNull();
            provider.GetRequiredService<IXmlDefExtractor>().Should().NotBeNull();
            provider.GetRequiredService<IXPatchProcessor>().Should().NotBeNull();
            provider.GetRequiredService<IXmlLanguagesWriter>().Should().NotBeNull();
            provider.GetRequiredService<IXmlLanguagesReader>().Should().NotBeNull();
            provider.GetRequiredService<IExcelReader>().Should().NotBeNull();
            provider.GetRequiredService<IExcelWriter>().Should().NotBeNull();

            var strategies = provider.GetServices<IOutputStrategy>().ToList();
            strategies.Should().HaveCount(3);
            strategies.Select(s => s.Format).Should().BeEquivalentTo(new[]
            {
                ExtractionFormat.Excel,
                ExtractionFormat.Languages,
                ExtractionFormat.LanguagesWithComments,
            });
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
