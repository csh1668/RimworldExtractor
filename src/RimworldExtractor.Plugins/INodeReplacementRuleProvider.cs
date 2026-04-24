using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Plugins;

/// <summary>
/// Provides the list of node-replacement rules at plugin execution time.
/// The default Application-layer implementation reads from
/// <see cref="Domain.Settings.AppSettings.Extraction"/>.NodeReplacements via
/// <see cref="Domain.Abstractions.ISettingsStore"/>.
/// </summary>
public interface INodeReplacementRuleProvider
{
    /// <summary>Returns the current set of node-replacement rules.</summary>
    IReadOnlyList<NodeReplacementRule> GetRules();
}
