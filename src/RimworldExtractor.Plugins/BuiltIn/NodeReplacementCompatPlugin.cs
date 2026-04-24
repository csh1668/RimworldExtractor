using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Plugins.BuiltIn;

/// <summary>
/// NodeReplacement compat plugin. Rewrites <see cref="TranslationEntry.ClassName"/> and/or
/// <see cref="TranslationEntry.Node"/> according to configured <see cref="NodeReplacementRule"/>
/// values, allowing mod-specific Def types to be treated as their base-class equivalents.
///
/// Ports <c>legacy/Compats/Compat_NodeReplacement.cs</c> + <c>legacy/Prefabs.NodeReplacement</c>.
/// Priority: 100 (default — no [CompatPriority] in legacy).
///
/// Rules use the format <c>SourceDef+SourceNode</c> → <c>TargetDef+TargetNode</c>,
/// where <c>*</c> means "keep the original value".
/// Example: <c>CombatExtended.AmmoDef+*</c> → <c>ThingDef+*</c>.
///
/// NOTE: The legacy code has an unintentional double-yield for Keyed/Strings entries
/// (they get yielded once unconditionally, then again from DoNodeReplacement). This is
/// preserved here for parity; the downstream DistinctBy in CompatPostProcessStage collapses
/// the duplicates.
/// </summary>
public sealed class NodeReplacementCompatPlugin : CompatPluginBase
{
    private readonly INodeReplacementRuleProvider _ruleProvider;

    public override string Name => "NodeReplacement";

    public NodeReplacementCompatPlugin(INodeReplacementRuleProvider ruleProvider)
    {
        _ruleProvider = ruleProvider;
    }

    public override IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries)
    {
        var translations = entries.ToList();
        var rules = _ruleProvider.GetRules();

        foreach (var entry in translations)
        {
            // Legacy bug preserved: Keyed/Strings are yielded here AND again below
            if (entry.ClassName is "Keyed" or "Strings")
                yield return entry;

            yield return DoNodeReplacement(entry, rules);
        }
    }

    private static TranslationEntry DoNodeReplacement(TranslationEntry entry, IReadOnlyList<NodeReplacementRule> rules)
    {
        var isPatches = entry.ClassName.StartsWith("Patches.", StringComparison.Ordinal);
        // Note: legacy had [("Patches.".Length + 1)..] which is an off-by-one bug (skips first char of class name).
        // Fixed here to use ["Patches.".Length..] so "Patches.ThingDef" → "ThingDef".
        var defType = isPatches ? entry.ClassName["Patches.".Length..] : entry.ClassName;

        var dotIdx = entry.Node.IndexOf('.');
        if (dotIdx < 0)
            return entry; // no dot → no nodeAfterDefName → skip

        var defName = entry.Node[..dotIdx];
        var nodeAfterDefName = entry.Node[(dotIdx + 1)..];

        foreach (var rule in rules)
        {
            var tokenKey = rule.From.Split('+');
            var tokenValue = rule.To.Split('+');
            if (tokenKey.Length != 2 || tokenValue.Length != 2)
                continue;

            var targetDef = tokenKey[0];
            var targetNode = tokenKey[1] == "*" ? nodeAfterDefName : tokenKey[1];
            var changedDef = tokenValue[0];
            var changedNode = tokenValue[1] == "*" ? nodeAfterDefName : tokenValue[1];

            if (defType == targetDef && nodeAfterDefName == targetNode)
            {
                return entry with
                {
                    ClassName = isPatches ? $"Patches.{changedDef}" : changedDef,
                    Node = $"{defName}.{changedNode}"
                };
            }
        }

        return entry;
    }
}
