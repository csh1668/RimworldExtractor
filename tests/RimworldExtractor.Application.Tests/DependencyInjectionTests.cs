using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure;

namespace RimworldExtractor.Application.Tests;

public sealed class DependencyInjectionTests : IDisposable
{
    private readonly string _tempDir;

    public DependencyInjectionTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("rwx-di-tests-").FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void AddApplication_ResolvesExtractionPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(Path.Combine(_tempDir, "settings.json"));
        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pipeline = provider.GetRequiredService<IExtractionPipeline>();

        pipeline.Should().BeOfType<ExtractionPipeline>();
    }
}
