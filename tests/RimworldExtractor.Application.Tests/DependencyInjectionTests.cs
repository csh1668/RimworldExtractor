using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "xunit test method naming convention")]
    public void AddApplication_ResolvesExtractionPipeline()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pipeline = provider.GetService<IExtractionPipeline>();

        pipeline.Should().NotBeNull("AddApplication must register IExtractionPipeline");
    }
}
