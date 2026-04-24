using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure.Settings;

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
}
