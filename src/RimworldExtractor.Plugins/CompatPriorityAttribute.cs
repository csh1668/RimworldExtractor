namespace RimworldExtractor.Plugins;

/// <summary>
/// Controls the order in which <see cref="ICompatPlugin"/> implementations are applied.
/// Lower priority values run first. Default is 100.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CompatPriorityAttribute : Attribute
{
    /// <summary>Execution order — lower values run earlier.</summary>
    public int Priority { get; }

    /// <param name="priority">Execution order. Lower = earlier. Default is 100.</param>
    public CompatPriorityAttribute(int priority = 100)
    {
        Priority = priority;
    }
}
