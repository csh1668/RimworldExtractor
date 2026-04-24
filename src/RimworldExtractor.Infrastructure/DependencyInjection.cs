using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Settings;
using RimworldExtractor.Infrastructure.Xml;

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
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<ISettingsStore>(sp =>
            new JsonSettingsStore(sp.GetRequiredService<IFileSystem>(), settingsPath));
        services.AddSingleton<IXmlDefExtractor, XmlDefExtractor>();
        services.AddSingleton<IXPatchProcessor, XPatchProcessor>();
        return services;
    }
}
