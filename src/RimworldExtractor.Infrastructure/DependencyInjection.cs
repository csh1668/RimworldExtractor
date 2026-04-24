using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure;

/// <summary>
/// Provides extension methods to register Infrastructure services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure services, including <see cref="ISettingsStore"/> backed by
    /// <see cref="JsonSettingsStore"/> persisting to <paramref name="settingsPath"/>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string settingsPath)
    {
        services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore(settingsPath));
        return services;
    }
}
