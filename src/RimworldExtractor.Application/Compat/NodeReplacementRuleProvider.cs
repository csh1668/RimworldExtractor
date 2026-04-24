using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Plugins;

namespace RimworldExtractor.Application.Compat;

/// <summary>
/// Default <see cref="INodeReplacementRuleProvider"/> implementation that holds a
/// fixed list of rules supplied at construction time (read once from
/// <see cref="Domain.Settings.AppSettings.Extraction"/> via <c>AddApplication</c>).
/// </summary>
public sealed class NodeReplacementRuleProvider : INodeReplacementRuleProvider
{
    private readonly IReadOnlyList<NodeReplacementRule> _rules;

    public NodeReplacementRuleProvider(IReadOnlyList<NodeReplacementRule> rules)
    {
        _rules = rules;
    }

    public IReadOnlyList<NodeReplacementRule> GetRules() => _rules;
}
