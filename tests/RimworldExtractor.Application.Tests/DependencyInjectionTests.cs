using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application;
using RimworldExtractor.Domain.Abstractions;
using Xunit;

namespace RimworldExtractor.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ResolvesExtractionPipeline()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pipeline = provider.GetService<IExtractionPipeline>();

        pipeline.Should().NotBeNull("AddApplication must register IExtractionPipeline");
    }
}
