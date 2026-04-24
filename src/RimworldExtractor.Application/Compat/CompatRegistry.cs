using RimworldExtractor.Plugins;

namespace RimworldExtractor.Application.Compat;

/// <summary>
/// Holds the ordered list of registered <see cref="ICompatPlugin"/> implementations.
/// Ordering is determined by <see cref="CompatPriorityAttribute"/> at construction time
/// (lower priority value = earlier in the list). Plugins without the attribute default to 100.
/// </summary>
public sealed class CompatRegistry
{
    /// <summary>Plugins sorted ascending by <see cref="CompatPriorityAttribute.Priority"/>.</summary>
    public IReadOnlyList<ICompatPlugin> Ordered { get; }

    public CompatRegistry(IEnumerable<ICompatPlugin> plugins)
    {
        Ordered = plugins
            .OrderBy(p => p.GetType()
                .GetCustomAttributes(typeof(CompatPriorityAttribute), inherit: false)
                .OfType<CompatPriorityAttribute>()
                .FirstOrDefault()?.Priority ?? 100)
            .ToArray();
    }
}
