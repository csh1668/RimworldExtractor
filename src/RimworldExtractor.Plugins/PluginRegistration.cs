using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace RimworldExtractor.Plugins;

/// <summary>
/// Extension methods for registering <see cref="ICompatPlugin"/> implementations.
/// </summary>
public static class PluginRegistration
{
    /// <summary>
    /// Registers <typeparamref name="T"/> as a singleton <see cref="ICompatPlugin"/>.
    /// Ordering by <see cref="CompatPriorityAttribute"/> is applied by <c>CompatRegistry</c>
    /// at resolution time, not here.
    /// </summary>
    public static IServiceCollection AddCompatPlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IServiceCollection services)
        where T : class, ICompatPlugin
    {
        services.AddSingleton<ICompatPlugin, T>();
        return services;
    }
}
