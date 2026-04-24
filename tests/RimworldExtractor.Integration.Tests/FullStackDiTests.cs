using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application;
using RimworldExtractor.Application.Compat;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Integration.Tests;

public sealed class FullStackDiTests : IDisposable
{
    private readonly string _tempDir;

    public FullStackDiTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("rwx-fullstack-di-").FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FullStack_ResolvesExtractionPipeline_WithAllStages()
    {
        // Arrange: pre-save AppSettings so sync-over-async factories (IModLister, INodeReplacementRuleProvider) can load
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        var fs = new PhysicalFileSystem();
        var store = new JsonSettingsStore(fs, settingsPath);
        await store.SaveAsync(AppSettings.Default, TestContext.Current.CancellationToken);

        // Build DI
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(settingsPath);
        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        // Assert: critical services resolve
        provider.GetRequiredService<IExtractionPipeline>().Should().BeOfType<ExtractionPipeline>();
        provider.GetRequiredService<ModDiscoveryService>().Should().NotBeNull();
        provider.GetRequiredService<CompatRegistry>().Should().NotBeNull();

        var stages = provider.GetServices<IExtractionStage>().ToList();
        stages.Should().HaveCount(9);

        var expectedStageNames = new[]
        {
            "Load reference defs",
            "Apply pre-patches",
            "Resolve inheritance",
            "Compat PreProcess",
            "Extract Defs",
            "Extract Keyed",
            "Extract Strings",
            "Extract Patches",
            "Compat PostProcess",
        };
        stages.Select(s => s.Name).Should().Equal(expectedStageNames);

        var plugins = provider.GetServices<Plugins.ICompatPlugin>().ToList();
        plugins.Should().HaveCount(7);
    }
}
