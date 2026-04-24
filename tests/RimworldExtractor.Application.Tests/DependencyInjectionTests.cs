using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ResolvesExtractionPipeline()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pipeline = provider.GetRequiredService<IExtractionPipeline>();

        pipeline.Should().NotBeNull();
    }
}
