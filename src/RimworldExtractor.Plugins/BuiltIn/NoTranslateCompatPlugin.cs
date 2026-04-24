using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Plugins.BuiltIn;

/// <summary>
/// NoTranslate compat plugin. Filters out entries where the Node contains
/// specific tags that should never be translated (e.g. <c>colorChannels</c>).
///
/// Ports <c>legacy/Compats/Compat_NoTranslate.cs</c>.
/// Priority: 100 (default — no [CompatPriority] in legacy).
/// </summary>
public sealed class NoTranslateCompatPlugin : CompatPluginBase
{
    private static readonly string[] NoTranslateTags = ["colorChannels"];

    public override string Name => "NoTranslate";

    public override IEnumerable<TranslationEntry> PostProcess(IEnumerable<TranslationEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (NoTranslateTags.Any(tag => entry.Node.Contains(tag, StringComparison.Ordinal)))
                continue;
            yield return entry;
        }
    }
}
