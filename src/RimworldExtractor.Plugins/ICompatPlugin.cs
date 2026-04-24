using System.Xml.Linq;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Plugins;

/// <summary>
/// A compat plugin that can pre-process the combined XDocument before extraction
/// and/or post-process the resulting <see cref="TranslationEntry"/> list.
/// Plugins are ordered by <see cref="CompatPriorityAttribute"/> (lower value = earlier).
/// </summary>
public interface ICompatPlugin
{
    /// <summary>Short, stable identifier used in logs.</summary>
    string Name { get; }

    /// <summary>
    /// Called before Def extraction. Implementations may mutate <paramref name="combinedDefs"/>
    /// in-place to inject synthetic nodes (e.g. filling in default field values).
    /// </summary>
    void PreProcess(XDocument combinedDefs);

    /// <summary>
    /// Called after all Def/Keyed/Strings/Patches extraction. Implementations may filter,
    /// rewrite or synthesize <see cref="TranslationEntry"/> values.
    /// </summary>
    IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries);
}

/// <summary>
/// Convenience base class. Provides a no-op implementation for both methods so
/// concrete plugins only override what they need.
/// </summary>
public abstract class CompatPluginBase : ICompatPlugin
{
    /// <inheritdoc />
    public virtual string Name => GetType().Name;

    /// <inheritdoc />
    public virtual void PreProcess(XDocument combinedDefs) { }

    /// <inheritdoc />
    public virtual IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries)
        => entries;
}
